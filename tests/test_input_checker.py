"""Tests for the input_checker planner (v10 §8 step 7)."""

from __future__ import annotations

from pathlib import Path

import pytest

from orchestrator.campaign_manager import CampaignManager
from orchestrator.media_registry import MediaRegistry
from orchestrator.models import ProjectConfig, UserTask


@pytest.fixture
def registry(tmp_path: Path) -> tuple[ProjectConfig, MediaRegistry]:
    # We bypass ingest/probe by pre-writing registry files directly so the
    # input_checker tests don't require FFmpeg.
    config = ProjectConfig(project_id="ic", root=tmp_path / "proj")
    for d in (config.input_metadata_dir,):
        d.mkdir(parents=True, exist_ok=True)
    # Fake registry entries
    config.source_registry_path.write_text(
        '[{"sourceId":"vid_001","type":"video","originalName":"r.mp4",'
        '"projectPath":"input/video/vid_001.mp4","sha256":"abc","duration":10.0,'
        '"width":1080,"height":1920,"fps":30.0,"hasAudio":true,"status":"validated"}]'
    )
    config.audio_registry_path.write_text("[]")
    config.asset_registry_path.write_text("[]")
    # No real IngestManager needed — we won't import anything in these tests
    return config, None  # type: ignore[return-value]


def _make_registry_with_entries(tmp_path: Path, videos: list[str], audios: list[str]) -> MediaRegistry:
    """Build a MediaRegistry-like object with hand-crafted entries."""
    config = ProjectConfig(project_id="ic", root=tmp_path / "proj")
    for d in (config.input_video_dir, config.input_audio_dir, config.input_assets_dir, config.input_metadata_dir):
        d.mkdir(parents=True, exist_ok=True)
    config.source_registry_path.write_text(
        "[" + ",".join(
            f'{{"sourceId":"{v}","type":"video","originalName":"r.mp4",'
            f'"projectPath":"input/video/{v}.mp4","sha256":"x","duration":10.0,'
            f'"width":1080,"height":1920,"fps":30.0,"hasAudio":true,"status":"validated"}}'
            for v in videos
        ) + "]"
    )
    config.audio_registry_path.write_text(
        "[" + ",".join(
            f'{{"audioId":"{a}","type":"music","originalName":"r.mp3",'
            f'"projectPath":"input/audio/{a}.mp3","sha256":"x","duration":30.0,'
            f'"sampleRate":44100,"channels":2,"status":"validated"}}'
            for a in audios
        ) + "]"
    )
    config.asset_registry_path.write_text("[]")

    # Build a registry without a real IngestManager (stub it)
    class _StubIngest:
        pass

    return MediaRegistry(config, _StubIngest())  # type: ignore[arg-type]


def _make_task(required_videos: list[str], required_audios: list[str]) -> UserTask:
    return UserTask(
        task_id="task_ic",
        project_id="proj",
        campaign_id="camp_001",
        title="ic",
        objective="o",
        style_brief="s",
        target_platform="tiktok_reels_shorts",
        target_duration={"min": 15, "max": 20},
        required_video_ids=required_videos,
        required_audio_ids=required_audios,
    )


class TestInputCheckResult:
    def test_flags_missing_video(self, tmp_path: Path) -> None:
        reg = _make_registry_with_entries(tmp_path, videos=[], audios=[])
        from orchestrator.planners.input_checker import check_task_inputs

        result = check_task_inputs(_make_task(["vid_001"], []), reg)
        assert not result.ok
        assert any("vid_001" in m for m in result.missing)

    def test_passes_when_registered(self, tmp_path: Path) -> None:
        reg = _make_registry_with_entries(tmp_path, videos=["vid_001"], audios=["aud_001"])
        from orchestrator.planners.input_checker import check_task_inputs

        result = check_task_inputs(_make_task(["vid_001"], ["aud_001"]), reg)
        assert result.ok
        assert result.missing == []

    def test_unconfirmed_campaign_does_not_block_draft(self, tmp_path: Path) -> None:
        config = ProjectConfig(project_id="ic", root=tmp_path / "proj")
        config.campaign_dir.mkdir(parents=True, exist_ok=True)
        cm = CampaignManager(config)
        cm.create_or_replace(
            campaign_id="camp_001",
            campaign_name="X",
            description_raw="X.",
        )
        reg = _make_registry_with_entries(tmp_path, videos=["vid_001"], audios=["aud_001"])
        from orchestrator.planners.input_checker import check_task_inputs

        result = check_task_inputs(_make_task(["vid_001"], ["aud_001"]), reg, campaign=cm)
        assert result.ok
        assert result.missing == []

    def test_passes_after_campaign_confirmed(self, tmp_path: Path) -> None:
        config = ProjectConfig(project_id="ic", root=tmp_path / "proj")
        config.campaign_dir.mkdir(parents=True, exist_ok=True)
        cm = CampaignManager(config)
        cm.create_or_replace(campaign_id="camp_001", campaign_name="X", description_raw="X.")
        cm.confirm_rules()
        reg = _make_registry_with_entries(tmp_path, videos=["vid_001"], audios=["aud_001"])
        from orchestrator.planners.input_checker import check_task_inputs

        result = check_task_inputs(_make_task(["vid_001"], ["aud_001"]), reg, campaign=cm)
        assert result.ok
