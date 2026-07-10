"""Tests for the multi-page editor web app (v10 §17).

These tests load the web server in-process (via FastAPI TestClient) and
exercise the page-rendering and proxy routes WITHOUT requiring a live MCP
server. They:

* render each page (dashboard, settings, campaign, tasks, media, editor, review, export)
* create projects via the dashboard form
* reject forbidden tools via the MCP proxy
* return 503 when MCP is not running
* redirect "/" to "/dashboard"
"""

from __future__ import annotations

import importlib.util
import json
import os
from pathlib import Path

import pytest
from fastapi.testclient import TestClient

from orchestrator.project_manager import ProjectManager


WEB_SERVER_PATH = Path(__file__).resolve().parent.parent / "apps" / "editor-web" / "server.py"


def _load_web_server_module(monkeypatch, projects_root: Path):
    """Load apps/editor-web/server.py and redirect its project root."""
    spec = importlib.util.spec_from_file_location("web_server_under_test", str(WEB_SERVER_PATH))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)

    # Redirect the SESSION_FILE lookup away from the real .kimchi/ dir.
    fake_session = projects_root / ".kimchi" / "mcp-session.json"
    fake_session.parent.mkdir(parents=True, exist_ok=True)
    monkeypatch.setattr(module, "SESSION_FILE", fake_session)

    # Redirect the project manager to a tmp projects root.
    def fake_pm():
        return ProjectManager(projects_root)
    monkeypatch.setattr(module, "_project_manager", fake_pm)

    return module


@pytest.fixture
def web_client(tmp_path, monkeypatch):
    module = _load_web_server_module(monkeypatch, tmp_path / "projects")
    app = module.create_app()
    return TestClient(app), module, tmp_path


@pytest.fixture
def project_client(tmp_path, monkeypatch):
    """A client with one pre-existing project."""
    pm = ProjectManager(tmp_path / "projects")
    pm.create_project("proj_x", "Project X")
    module = _load_web_server_module(monkeypatch, tmp_path / "projects")
    app = module.create_app()
    return TestClient(app), module, tmp_path, "proj_x"


def _seed_editor_timeline(tmp_path: Path, project_id: str):
    from orchestrator.timeline_manager import TimelineManager

    config = ProjectManager(tmp_path / "projects").load_project(project_id)
    timeline = TimelineManager(config).create_initial_timeline(
        "Editor Test",
        format={
            "width": 720,
            "height": 1280,
            "fps": 30.0,
            "duration": 2.0,
            "platform": "tiktok_reels_shorts",
        },
        assets={
            "clips": [
                {"id": "clip_a", "file": "a.mp4", "duration": 5.0},
                {"id": "clip_b", "file": "b.mp4", "duration": 5.0},
            ]
        },
        tracks=[{
            "id": "v1",
            "type": "video",
            "items": [
                {"clipId": "clip_a", "timelineStart": 0.0, "sourceStart": 0.0, "duration": 1.0},
                {"clipId": "clip_b", "timelineStart": 1.0, "sourceStart": 0.0, "duration": 1.0},
            ],
        }],
    )
    timeline.data["render"]["lastPreviewPath"] = "previews/old-preview.mp4"
    timeline.data["review"]["approvedByUser"] = True
    timeline.data["review"]["approvedAt"] = "2026-01-01T00:00:00Z"
    timeline.data["project"]["status"] = "approved"
    TimelineManager(config).save_timeline(timeline)
    return config, timeline


class TestRootRedirect:
    def test_root_redirects_to_dashboard(self, web_client) -> None:
        client, _, _ = web_client
        # TestClient follows redirects by default for GET
        r = client.get("/", follow_redirects=False)
        assert r.status_code in (301, 302, 307, 308)
        assert r.headers["location"] == "/dashboard"


class TestDashboard:
    def test_renders_without_mcp(self, web_client) -> None:
        client, _, _ = web_client
        r = client.get("/dashboard")
        assert r.status_code == 200
        text = r.text
        assert "Projects" in text
        assert "CreatorCut" in text
        # MCP not running → banner should appear
        assert "MCP server is not running" in text

    def test_create_project_redirects_into_campaign(self, web_client) -> None:
        client, module, tmp_path = web_client
        r = client.post("/dashboard/create", data={"name": "My Cool Edit"}, follow_redirects=False)
        assert r.status_code == 303
        assert "/projects/" in r.headers["location"]
        assert "/campaign" in r.headers["location"]
        # Project should now exist on disk
        assert module._project_manager().project_exists(
            r.headers["location"].split("/projects/")[1].split("/")[0]
        )


class TestSettings:
    def test_settings_renders(self, web_client) -> None:
        client, _, _ = web_client
        r = client.get("/settings")
        assert r.status_code == 200
        text = r.text
        assert "Settings" in text
        assert "Licenses" in text
        assert "Hard rules" in text
        # License table should include FFmpeg
        assert "FFmpeg" in text


class TestCampaign:
    def test_campaign_renders_when_no_brief(self, project_client) -> None:
        client, _, _, pid = project_client
        r = client.get(f"/projects/{pid}/campaign")
        assert r.status_code == 200
        text = r.text
        assert "Campaign brief" in text
        assert "Create brief" in text

    def test_campaign_404_for_missing_project(self, web_client) -> None:
        client, _, _ = web_client
        r = client.get("/projects/does_not_exist/campaign")
        assert r.status_code == 404

    def test_campaign_post_creates_brief(self, project_client) -> None:
        client, module, tmp_path, pid = project_client
        r = client.post(f"/projects/{pid}/campaign", data={
            "campaign_id": "camp_test",
            "campaign_name": "Test Campaign",
            "description": "song: test song. 10-15 seconds. tiktok.",
            "music_delivery_mode": "embedded_audio",
        }, follow_redirects=False)
        assert r.status_code == 303
        # Brief file exists
        config = ProjectManager(tmp_path / "projects").load_project(pid)
        from orchestrator.campaign_manager import CampaignManager
        brief = CampaignManager(config).load()
        assert brief.campaign_name == "Test Campaign"
        assert brief.user_confirmed_parsed_rules is False


class TestTasks:
    def test_tasks_renders(self, project_client) -> None:
        client, _, _, pid = project_client
        r = client.get(f"/projects/{pid}/tasks")
        assert r.status_code == 200
        assert "Task board" in r.text
        assert "New task" in r.text


class TestMedia:
    def test_media_renders(self, project_client) -> None:
        client, _, _, pid = project_client
        r = client.get(f"/projects/{pid}/media")
        assert r.status_code == 200
        assert "Media registry" in r.text
        assert "Import a file" in r.text


class TestEditor:
    def test_editor_renders_without_timeline(self, project_client) -> None:
        client, _, _, pid = project_client
        r = client.get(f"/projects/{pid}/editor")
        assert r.status_code == 200
        assert "Editor" in r.text
        assert "No timeline yet" in r.text

    def test_editor_state_returns_react_read_model(self, project_client) -> None:
        client, _, _, pid = project_client
        r = client.get(f"/api/projects/{pid}/editor-state")
        assert r.status_code == 200
        body = r.json()
        assert body["project"]["id"] == pid
        assert body["project"]["name"] == "Project X"
        assert body["timeline"] is None
        assert body["tasks"] == []
        assert body["media"] == []
        assert body["previewUrl"] is None

    def test_clip_update_retimes_following_video_and_updates_duration(self, project_client) -> None:
        client, _, tmp_path, pid = project_client
        config, _ = _seed_editor_timeline(tmp_path, pid)

        response = client.post(
            f"/projects/{pid}/editor/clip/update",
            data={
                "track_index": 0,
                "item_index": 0,
                "source_start": 0.25,
                "duration": 2.0,
                "scale": 1.2,
                "x": 12,
                "y": -8,
                "rotation": 5,
                "base_version": 1,
            },
            follow_redirects=False,
        )
        assert response.status_code == 303

        from orchestrator.timeline_manager import TimelineManager
        timeline = TimelineManager(config).load_timeline()
        items = timeline.data["tracks"][0]["items"]
        assert items[0]["duration"] == 2.0
        assert items[0]["transform"] == {
            "scale": 1.2, "x": 12.0, "y": -8.0, "rotation": 5.0
        }
        assert items[1]["timelineStart"] == 2.0
        assert timeline.data["format"]["duration"] == 3.0
        assert timeline.data["render"]["lastPreviewPath"] is None
        assert timeline.data["review"]["approvedByUser"] is False
        assert timeline.data["review"]["approvedAt"] is None
        assert timeline.data["project"]["status"] == "draft"

    def test_clip_delete_closes_gap_and_updates_duration(self, project_client) -> None:
        client, _, tmp_path, pid = project_client
        config, _ = _seed_editor_timeline(tmp_path, pid)

        response = client.post(
            f"/projects/{pid}/editor/clip/delete",
            data={"track_index": 0, "item_index": 0, "base_version": 1},
            follow_redirects=False,
        )
        assert response.status_code == 303

        from orchestrator.timeline_manager import TimelineManager
        timeline = TimelineManager(config).load_timeline()
        items = timeline.data["tracks"][0]["items"]
        assert len(items) == 1
        assert items[0]["clipId"] == "clip_b"
        assert items[0]["timelineStart"] == 0.0
        assert timeline.data["format"]["duration"] == 1.0
        assert timeline.data["render"]["lastPreviewPath"] is None
        assert timeline.data["review"]["approvedByUser"] is False

    def test_clip_update_rejects_invalid_transform_values(self, project_client) -> None:
        client, _, tmp_path, pid = project_client
        _seed_editor_timeline(tmp_path, pid)
        response = client.post(
            f"/projects/{pid}/editor/clip/update",
            data={
                "track_index": 0,
                "item_index": 0,
                "source_start": -1,
                "duration": 1,
                "scale": 0,
                "x": 0,
                "y": 0,
                "rotation": 0,
                "base_version": 1,
            },
        )
        assert response.status_code == 400

    def test_clip_update_rejects_stale_timeline_version(self, project_client) -> None:
        client, _, tmp_path, pid = project_client
        _seed_editor_timeline(tmp_path, pid)
        response = client.post(
            f"/projects/{pid}/editor/clip/update",
            data={
                "track_index": 0,
                "item_index": 0,
                "source_start": 0,
                "duration": 1,
                "scale": 1,
                "x": 0,
                "y": 0,
                "rotation": 0,
                "base_version": 999,
            },
        )
        assert response.status_code == 409

    def test_add_edit_and_remove_audio_from_timeline(self, project_client) -> None:
        client, _, tmp_path, pid = project_client
        config, _ = _seed_editor_timeline(tmp_path, pid)
        audio_path = config.root / "media" / "music.wav"
        audio_path.parent.mkdir(parents=True, exist_ok=True)
        audio_path.write_bytes(b"test-audio")
        config.input_metadata_dir.mkdir(parents=True, exist_ok=True)
        config.audio_registry_path.write_text(json.dumps([{
            "audioId": "aud_1",
            "originalName": "music.wav",
            "projectPath": str(audio_path.relative_to(config.root)),
            "duration": 8.0,
            "status": "validated",
        }]), encoding="utf-8")

        added = client.post(
            f"/projects/{pid}/editor/audio/add",
            data={"audio_id": "aud_1", "base_version": 1},
            follow_redirects=False,
        )
        assert added.status_code == 303

        from orchestrator.timeline_manager import TimelineManager
        timeline = TimelineManager(config).load_timeline()
        audio_track_index = next(
            index for index, track in enumerate(timeline.data["tracks"])
            if track["type"] == "music"
        )
        item = timeline.data["tracks"][audio_track_index]["items"][0]
        assert item["audioId"] == "aud_1"
        assert timeline.data["music"]["file"] == str(audio_path)

        edited = client.post(
            f"/projects/{pid}/editor/clip/update",
            data={
                "track_index": audio_track_index,
                "item_index": 0,
                "source_start": 0.5,
                "timeline_start": 0.75,
                "duration": 1.5,
                "volume": 0.6,
                "base_version": timeline.version,
            },
            follow_redirects=False,
        )
        assert edited.status_code == 303
        timeline = TimelineManager(config).load_timeline()
        item = timeline.data["tracks"][audio_track_index]["items"][0]
        assert item["sourceStart"] == 0.5
        assert item["timelineStart"] == 0.75
        assert item["duration"] == 1.5
        assert item["volume"] == 0.6

        removed = client.post(
            f"/projects/{pid}/editor/clip/delete",
            data={
                "track_index": audio_track_index,
                "item_index": 0,
                "base_version": timeline.version,
            },
            follow_redirects=False,
        )
        assert removed.status_code == 303
        timeline = TimelineManager(config).load_timeline()
        assert timeline.data["tracks"][audio_track_index]["items"] == []
        assert timeline.data["music"]["file"] == ""

    def test_editor_state_includes_video_audio_and_assets(self, project_client) -> None:
        client, _, tmp_path, pid = project_client
        config = ProjectManager(tmp_path / "projects").load_project(pid)
        config.input_metadata_dir.mkdir(parents=True, exist_ok=True)
        config.source_registry_path.write_text(json.dumps([{
            "sourceId": "vid_1", "originalName": "clip.mp4", "status": "validated"
        }]), encoding="utf-8")
        config.audio_registry_path.write_text(json.dumps([{
            "audioId": "aud_1", "originalName": "music.wav", "status": "validated"
        }]), encoding="utf-8")
        config.asset_registry_path.write_text(json.dumps([{
            "assetId": "asset_1", "kind": "logo", "originalName": "logo.png", "status": "validated"
        }]), encoding="utf-8")

        response = client.get(f"/api/projects/{pid}/editor-state")
        assert response.status_code == 200
        media = response.json()["media"]
        assert {(entry["sourceId"], entry["mediaKind"]) for entry in media} == {
            ("vid_1", "video"),
            ("aud_1", "audio"),
            ("asset_1", "asset"),
        }


class TestReview:
    def test_review_renders(self, project_client) -> None:
        client, _, _, pid = project_client
        r = client.get(f"/projects/{pid}/review")
        assert r.status_code == 200
        assert "Review" in r.text
        assert "No tasks awaiting review" in r.text


class TestExport:
    def test_export_renders(self, project_client) -> None:
        client, _, _, pid = project_client
        r = client.get(f"/projects/{pid}/export")
        assert r.status_code == 200
        assert "Export package" in r.text
        assert "Run final export" in r.text


# ---------------------------------------------------------------------------
# MCP proxy
# ---------------------------------------------------------------------------


class TestMcpProxyGuards:
    def test_proxy_returns_503_when_no_session(self, web_client) -> None:
        client, _, _ = web_client
        r = client.post(
            "/api/mcp/call/list_open_tasks",
            json={"projectId": "proj_x"},
        )
        assert r.status_code == 503
        assert "not running" in r.json()["detail"]

    def test_proxy_rejects_forbidden_tool(self, web_client) -> None:
        client, _, _ = web_client
        # forbidden tools should raise before any MCP call
        for forbidden in ["publish_to_tiktok", "submit_to_clipster", "approve_project",
                          "download_video_from_url", "scrape_youtube"]:
            r = client.post(f"/api/mcp/call/{forbidden}", json={"projectId": "proj_x"})
            assert r.status_code == 403, f"{forbidden} should be rejected"
            assert forbidden in r.json()["detail"]

    def test_proxy_rejects_unknown_tool(self, web_client) -> None:
        client, _, _ = web_client
        r = client.post(
            "/api/mcp/call/telekinesis",
            json={"projectId": "proj_x"},
        )
        # Not in allow-list
        assert r.status_code == 400

    def test_proxy_proxies_when_session_present_and_tool_allowed(
        self, tmp_path, monkeypatch
    ) -> None:
        """With a fake session + canned MCP response, the proxy must forward
        the request and return the response body unchanged."""
        import httpx
        module = _load_web_server_module(monkeypatch, tmp_path / "projects")

        # Write a fake session file pointing at our canned MCP URL.
        session_path = tmp_path / ".kimchi" / "mcp-session.json"
        session_path.parent.mkdir(parents=True, exist_ok=True)
        session_path.write_text(json.dumps({
            "url": "http://mock-mcp/mcp",
            "token": "fake-token",
            "host": "127.0.0.1",
            "port": 1,
            "pid": os.getpid(),
            "startedAt": 0,
        }), encoding="utf-8")
        monkeypatch.setattr(module, "SESSION_FILE", session_path)

        # Mock httpx.post to return a canned JSON-RPC success.
        class _Resp:
            status_code = 200
            def __init__(self):
                self._body = {"jsonrpc": "2.0", "id": 1,
                              "result": {"ok": True, "result": [{"taskId": "t1"}]}}
            def json(self):
                return self._body
            @property
            def text(self):
                return json.dumps(self._body)

        def fake_post(url, json=None, headers=None, timeout=None):  # noqa: A002
            assert url == "http://mock-mcp/mcp", f"unexpected URL {url}"
            assert headers["Authorization"] == "Bearer fake-token"
            return _Resp()

        monkeypatch.setattr(httpx, "post", fake_post)

        app = module.create_app()
        client = TestClient(app)
        r = client.post("/api/mcp/call/list_open_tasks", json={"projectId": "proj_x"})
        assert r.status_code == 200
        body = r.json()
        assert body["ok"] is True
        assert body["result"] == [{"taskId": "t1"}]


class TestStatic:
    def test_static_css_served(self, web_client) -> None:
        _, module, _ = web_client
        text = (module.STATIC_DIR / "style.css").read_text(encoding="utf-8")
        assert "--bg" in text or "background" in text
