"""Integration test: the full v10 §8 flow via AgentTaskRunner (needs FFmpeg)."""

from __future__ import annotations

import subprocess
from pathlib import Path

import pytest

from orchestrator.agent_task_runner import AgentTaskRunner
from orchestrator.audit_log import AuditLog
from orchestrator.campaign_manager import CampaignManager
from orchestrator.engines.ffmpeg_engine import FFmpegEngine
from orchestrator.ffmpeg_tools import build_tool_command, resolve_tool_path
from orchestrator.ingest_manager import IngestManager
from orchestrator.media_registry import MediaRegistry
from orchestrator.models import (
    MusicDeliveryMode,
    ProjectConfig,
    TaskStatus,
    UserTask,
)
from orchestrator.project_manager import ProjectManager
from orchestrator.render_manager import RenderManager
from orchestrator.task_manager import TaskManager
from orchestrator.timeline_manager import TimelineManager


TOOLS_DIR = Path(__file__).resolve().parent.parent / ".tools" / "bin"
FFMPEG = resolve_tool_path("ffmpeg", TOOLS_DIR / "ffmpeg-wrapper.sh")
FFPROBE = resolve_tool_path("ffprobe", TOOLS_DIR / "ffprobe-wrapper.sh")


def _has_ffmpeg() -> bool:
    return Path(FFMPEG).exists()


pytestmark = pytest.mark.skipif(
    not _has_ffmpeg(), reason=f"FFmpeg wrappers not present at {TOOLS_DIR}"
)


@pytest.fixture
def runner(tmp_path: Path) -> tuple[ProjectConfig, AgentTaskRunner, dict[str, Path]]:
    config = ProjectManager(tmp_path / "projects").create_project("runner-test", "Runner")
    ingest = IngestManager(config, ffmpeg_path=FFMPEG, ffprobe_path=FFPROBE)
    registry = MediaRegistry(config, ingest)
    campaign = CampaignManager(config)
    tasks = TaskManager(config)
    timelines = TimelineManager(config)
    engine = FFmpegEngine(ffmpeg_path=FFMPEG, ffprobe_path=FFPROBE)
    renders = RenderManager(config, engine)
    audit = AuditLog(config)
    audit_log_runner = AgentTaskRunner(config, tasks, campaign, registry, timelines, renders, audit)

    # Generate one tiny clip (with audio) and one tiny audio file
    clip = tmp_path / "src.mp4"
    audio = tmp_path / "src.m4a"
    subprocess.run(
        build_tool_command(FFMPEG, ["-y", "-f", "lavfi", "-i", "testsrc=duration=2:size=320x240:rate=30",
         "-f", "lavfi", "-i", "sine=frequency=440:duration=2",
         "-c:v", "libx264", "-preset", "veryfast", "-c:a", "aac", "-shortest",
         clip]),
        check=True, capture_output=True,
    )
    subprocess.run(
        build_tool_command(FFMPEG, ["-y", "-f", "lavfi", "-i", "sine=frequency=440:duration=2",
         "-c:a", "aac", audio]),
        check=True, capture_output=True,
    )
    video_entry = registry.import_video(clip, source_id="vid_001")
    audio_entry = registry.import_audio(audio, audio_id="aud_001")

    # Confirmed campaign brief
    campaign.create_or_replace(
        campaign_id="camp_001",
        campaign_name="Demo",
        description_raw="song: demo. 1-2 seconds.",
        music_delivery_mode=MusicDeliveryMode.EMBEDDED_AUDIO,
    )
    campaign.confirm_rules()

    return config, audit_log_runner, {"clip": clip, "audio": audio,
                                       "video_entry": video_entry, "audio_entry": audio_entry}


def _make_task() -> UserTask:
    return UserTask(
        task_id="task_runner_001",
        project_id="runner-test",
        campaign_id="camp_001",
        title="Demo draft",
        objective="Create a short draft from uploaded clip and audio",
        style_brief="Simple test",
        target_platform="tiktok_reels_shorts",
        target_duration={"min": 1.0, "max": 2.0},
        required_video_ids=["vid_001"],
        required_audio_ids=["aud_001"],
        forbidden_actions=[
            "download_media_from_web",
            "publish_to_social",
            "submit_to_clipster",
            "approve_export",
        ],
    )


class TestEndToEndFlow:
    def test_happy_path_yields_preview(self, runner) -> None:
        config, atr, _ = runner
        tm = TaskManager(config)
        tm.create(_make_task())

        summary = atr.run_task("task_runner_001", "agent-001")

        assert summary["status"] == "ready_for_review"
        assert summary["preview"]
        assert Path(config.root / summary["preview"]).exists()

        # The task walked the full state machine
        reloaded = tm.load("task_runner_001")
        assert reloaded.status == TaskStatus.READY_FOR_REVIEW

    def test_blocks_when_required_video_missing(self, runner) -> None:
        config, atr, _ = runner
        tm = TaskManager(config)
        bad = _make_task()
        bad.task_id = "task_runner_bad"
        bad.required_video_ids = ["vid_does_not_exist"]
        tm.create(bad)

        summary = atr.run_task("task_runner_bad", "agent-001")
        assert summary["status"] == "blocked"
        assert any("vid_does_not_exist" in m for m in summary["missing"])

        reloaded = tm.load("task_runner_bad")
        assert reloaded.status == TaskStatus.BLOCKED_MISSING_INPUTS

    def test_audit_log_records_steps(self, runner) -> None:
        config, atr, _ = runner
        tm = TaskManager(config)
        tm.create(_make_task())
        atr.run_task("task_runner_001", "agent-001")
        audit = AuditLog(config)
        actions = [e["action"] for e in audit.read(actor="agent")]
        # Spot-check key steps from v10 §8
        assert "claim_task" in actions
        assert "check_task_inputs" in actions
        assert "mark_task_ready_for_review" in actions
