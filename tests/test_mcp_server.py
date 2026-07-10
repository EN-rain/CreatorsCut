"""Tests for the MCP-compatible JSON-RPC server (v10 §7, §15).

These tests exercise the server in-process via FastAPI's TestClient,
without binding a network port. They verify:

* Session-token gate (401 without/with bad token, 200 with good token).
* ``tools/list`` exposes the safe surface and never registers forbidden tools.
* ``tools/call`` dispatches to the correct manager.
* Forbidden tools are rejected before any manager is touched.
* Import tools reject URLs and non-absolute paths.
"""

from __future__ import annotations

from pathlib import Path

import pytest
from fastapi.testclient import TestClient

from orchestrator.mcp_server import (
    ALLOWED_AGENT_TOOLS,
    CONDITIONAL_AGENT_TOOLS,
    FORBIDDEN_AGENT_TOOLS,
    create_app,
)
from orchestrator.project_manager import ProjectManager


TOOLS_DIR = Path(__file__).resolve().parent.parent / ".tools" / "bin"
FFMPEG = TOOLS_DIR / "ffmpeg-wrapper.sh"
FFPROBE = TOOLS_DIR / "ffprobe-wrapper.sh"


TOKEN = "test-token-abc"


@pytest.fixture
def app_and_client(tmp_path: Path):
    projects_root = tmp_path / "projects"
    pm = ProjectManager(projects_root)
    pm.create_project("proj_a", "Project A")

    app = create_app(
        projects_root=projects_root,
        ffmpeg_path=FFMPEG,
        ffprobe_path=FFPROBE,
        token=TOKEN,
    )
    client = TestClient(app)
    return app, client


def _auth() -> dict[str, str]:
    return {"Authorization": f"Bearer {TOKEN}"}


def _rpc(method: str, params: dict | None = None, rpc_id: int = 1) -> dict:
    return {"jsonrpc": "2.0", "id": rpc_id, "method": method, "params": params or {}}


class TestAuthGate:
    def test_missing_authorization_header_returns_401(self, app_and_client) -> None:
        _, client = app_and_client
        r = client.post("/mcp", json=_rpc("tools/list"))
        assert r.status_code == 401

    def test_wrong_token_returns_401(self, app_and_client) -> None:
        _, client = app_and_client
        r = client.post("/mcp", json=_rpc("tools/list"),
                         headers={"Authorization": "Bearer wrong-token"})
        assert r.status_code == 401

    def test_correct_token_passes_auth(self, app_and_client) -> None:
        _, client = app_and_client
        r = client.post("/mcp", json=_rpc("tools/list"), headers=_auth())
        assert r.status_code == 200

    def test_health_endpoint_does_not_require_auth(self, app_and_client) -> None:
        _, client = app_and_client
        r = client.get("/health")
        assert r.status_code == 200
        body = r.json()
        assert body["ok"] is True
        assert body["server"] == "agent-video-editor-mcp"


class TestInitialize:
    def test_initialize_returns_capabilities(self, app_and_client) -> None:
        _, client = app_and_client
        r = client.post("/mcp", json=_rpc("initialize"), headers=_auth())
        assert r.status_code == 200
        body = r.json()
        assert body["result"]["server"] == "agent-video-editor-mcp"
        assert "tools" in body["result"]["capabilities"]
        assert body["result"]["toolsRegistered"] > 0


class TestToolsList:
    def test_returns_registered_tools(self, app_and_client) -> None:
        _, client = app_and_client
        r = client.post("/mcp", json=_rpc("tools/list"), headers=_auth())
        assert r.status_code == 200
        tools = r.json()["result"]["tools"]
        names = {t["name"] for t in tools}
        # Allowed tools are all registered
        assert ALLOWED_AGENT_TOOLS.issubset(names)
        # Conditional tools are all registered
        assert CONDITIONAL_AGENT_TOOLS.issubset(names)

    def test_no_forbidden_tools_registered(self, app_and_client) -> None:
        _, client = app_and_client
        r = client.post("/mcp", json=_rpc("tools/list"), headers=_auth())
        body = r.json()["result"]
        # CRITICAL invariant: nothing on the forbidden list is exposed.
        assert body["forbiddenRegistered"] == []
        # And the response advertises which tools are forbidden so clients can audit.
        assert set(body["forbiddenDefined"]) == set(FORBIDDEN_AGENT_TOOLS)

    def test_tool_descriptions_present(self, app_and_client) -> None:
        _, client = app_and_client
        r = client.post("/mcp", json=_rpc("tools/list"), headers=_auth())
        tools = r.json()["result"]["tools"]
        for tool in tools:
            assert tool["description"], f"missing description for {tool['name']}"
            assert tool["category"] in ("allowed", "conditional")


class TestToolsCallDispatch:
    def test_dispatches_list_open_tasks(self, app_and_client) -> None:
        _, client = app_and_client
        r = client.post("/mcp", json=_rpc("tools/call", {
            "name": "list_open_tasks",
            "arguments": {"projectId": "proj_a"},
        }), headers=_auth())
        assert r.status_code == 200
        body = r.json()
        assert body["result"]["ok"] is True
        assert body["result"]["result"] == []  # no tasks yet

    def test_dispatches_read_campaign_brief_missing(self, app_and_client) -> None:
        _, client = app_and_client
        r = client.post("/mcp", json=_rpc("tools/call", {
            "name": "read_campaign_brief",
            "arguments": {"projectId": "proj_a"},
        }), headers=_auth())
        body = r.json()
        # No brief saved yet → error envelope with code -32004
        assert body["result"]["ok"] is False
        assert body["result"]["error"]["code"] == -32004

    def test_unknown_tool_raises(self, app_and_client) -> None:
        _, client = app_and_client
        r = client.post("/mcp", json=_rpc("tools/call", {
            "name": "telekinesis",
            "arguments": {"projectId": "proj_a"},
        }), headers=_auth())
        body = r.json()
        # Goes through the JSON-RPC dispatch path → top-level error code -32000
        assert "error" in body
        assert "Unknown tool" in body["error"]["message"]

    def test_missing_project_id_raises(self, app_and_client) -> None:
        _, client = app_and_client
        r = client.post("/mcp", json=_rpc("tools/call", {
            "name": "list_open_tasks",
            "arguments": {},
        }), headers=_auth())
        body = r.json()
        assert "error" in body
        assert "projectId" in body["error"]["message"]


class TestForbiddenToolsRejected:
    @pytest.mark.parametrize("tool_name", sorted(FORBIDDEN_AGENT_TOOLS))
    def test_forbidden_tool_rejected(self, app_and_client, tool_name: str) -> None:
        _, client = app_and_client
        r = client.post("/mcp", json=_rpc("tools/call", {
            "name": tool_name,
            "arguments": {"projectId": "proj_a"},
        }), headers=_auth())
        # The forbidden guard returns an application-level error envelope
        # (ok=False with forbidden=True), NOT a JSON-RPC error, so the
        # RPC envelope itself is valid.
        assert r.status_code == 200
        body = r.json()
        assert body["result"]["ok"] is False
        assert body["result"]["error"]["forbidden"] is True
        assert body["result"]["error"]["tool"] == tool_name
        assert body["result"]["error"]["code"] == -32003


class TestImportToolsValidation:
    def test_import_media_rejects_http_url(self, app_and_client) -> None:
        _, client = app_and_client
        r = client.post("/mcp", json=_rpc("tools/call", {
            "name": "import_media",
            "arguments": {"projectId": "proj_a", "source": "https://evil.example/x.mp4"},
        }), headers=_auth())
        body = r.json()
        assert "error" in body
        assert "remote URL" in body["error"]["message"]

    def test_import_media_rejects_relative_path(self, app_and_client) -> None:
        _, client = app_and_client
        r = client.post("/mcp", json=_rpc("tools/call", {
            "name": "import_media",
            "arguments": {"projectId": "proj_a", "source": "x.mp4"},
        }), headers=_auth())
        body = r.json()
        assert "error" in body
        assert "non-absolute" in body["error"]["message"]

    def test_import_media_rejects_missing_file(self, app_and_client) -> None:
        _, client = app_and_client
        r = client.post("/mcp", json=_rpc("tools/call", {
            "name": "import_media",
            "arguments": {"projectId": "proj_a", "source": "/no/such/file.mp4"},
        }), headers=_auth())
        body = r.json()
        assert "error" in body
        assert "does not exist" in body["error"]["message"]


class TestRpcEnvelope:
    def test_invalid_jsonrpc_version_returns_error(self, app_and_client) -> None:
        _, client = app_and_client
        r = client.post("/mcp", json={"jsonrpc": "1.0", "id": 1, "method": "tools/list"},
                         headers=_auth())
        assert r.status_code == 200
        body = r.json()
        assert body["error"]["code"] == -32600

    def test_unknown_method_returns_method_not_found(self, app_and_client) -> None:
        _, client = app_and_client
        r = client.post("/mcp", json=_rpc("frobnicate"), headers=_auth())
        assert r.status_code == 200
        body = r.json()
        assert body["error"]["code"] == -32601
