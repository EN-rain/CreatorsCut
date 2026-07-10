"""Tests for the FFmpeg engine: render a simple timeline to a preview MP4."""

from __future__ import annotations

import subprocess
from pathlib import Path

import pytest

from orchestrator.ffmpeg_tools import build_tool_command, resolve_tool_path
from orchestrator.engines.ffmpeg_engine import EngineError, FFmpegEngine
from orchestrator.ingest_manager import IngestManager
from orchestrator.models import Modifier, Timeline
from orchestrator.project_manager import ProjectManager


TOOLS_DIR = Path(__file__).resolve().parent.parent / ".tools" / "bin"
FFMPEG = resolve_tool_path("ffmpeg", TOOLS_DIR / "ffmpeg-wrapper.sh")
FFPROBE = resolve_tool_path("ffprobe", TOOLS_DIR / "ffprobe-wrapper.sh")


def _has_ffmpeg() -> bool:
    return Path(FFMPEG).exists()


pytestmark = pytest.mark.skipif(
    not _has_ffmpeg(),
    reason=f"FFmpeg wrappers not present at {TOOLS_DIR}",
)


@pytest.fixture
def project_root(tmp_path: Path):
    manager = ProjectManager(tmp_path / "projects")
    return manager.create_project("render-test", "Render Test")


@pytest.fixture
def media_files(tmp_path: Path) -> dict[str, Path]:
    """Generate two short test clips and a music sine wave."""
    clip1 = tmp_path / "clip1.mp4"
    clip2 = tmp_path / "clip2.mp4"
    music = tmp_path / "music.wav"

    for i, target in enumerate([clip1, clip2], start=1):
        cmd = build_tool_command(FFMPEG, [
            "-y",
            "-f", "lavfi",
            "-i", "testsrc=duration=1.5:size=320x240:rate=30",
            "-f", "lavfi",
            "-i", f"sine=frequency={440 + i * 110}:duration=1.5",
            "-c:v", "libx264",
            "-preset", "veryfast",
            "-c:a", "aac",
            "-shortest",
            target,
        ])
        r = subprocess.run(cmd, check=False, capture_output=True, text=True)
        if r.returncode != 0:
            pytest.skip(f"Failed to generate clip{i}: {r.stderr[-200:]}")

    cmd = build_tool_command(FFMPEG, [
        "-y",
        "-f", "lavfi",
        "-i", "sine=frequency=220:duration=4",
        "-c:a", "pcm_s16le",
        music,
    ])
    r = subprocess.run(cmd, check=False, capture_output=True, text=True)
    if r.returncode != 0:
        pytest.skip(f"Failed to generate music: {r.stderr[-200:]}")

    return {"clip1": clip1, "clip2": clip2, "music": music}


def _build_simple_timeline(
    project_id: str,
    clip1_path: Path,
    clip2_path: Path,
    music_path: Path,
) -> Timeline:
    """Build an in-memory Timeline that references two clips and a music file."""
    data = {
        "schemaVersion": "1.0",
        "project": {"id": project_id, "name": "Render Test", "status": "draft"},
        "version": {
            "timelineVersion": 1,
            "parentVersion": None,
            "modifiedBy": Modifier.AGENT.value,
            "modifiedAt": "2026-01-01T00:00:00Z",
        },
        "format": {"width": 480, "height": 360, "fps": 30.0, "duration": 3.0, "platform": "landscape"},
        "campaign": {"source": "manual", "allowedPlatforms": []},
        "music": {"file": str(music_path)},
        "assets": {
            "clips": [
                {"id": "clip1", "file": str(clip1_path), "sourceStart": 0.0, "duration": 1.5},
                {"id": "clip2", "file": str(clip2_path), "sourceStart": 0.0, "duration": 1.5},
            ]
        },
        "tracks": [
            {
                "id": "v1",
                "type": "video",
                "items": [
                    {"clipId": "clip1", "timelineStart": 0.0, "sourceStart": 0.0, "duration": 1.5},
                    {"clipId": "clip2", "timelineStart": 1.5, "sourceStart": 0.0, "duration": 1.5},
                ],
            }
        ],
        "markers": [],
        "review": {"requiresHumanApproval": True, "approvedByUser": False, "approvedAt": None, "notes": []},
        "render": {"previewMode": "instant"},
    }
    return Timeline(data)


def test_render_simple_timeline(project_root, media_files: dict[str, Path]) -> None:
    engine = FFmpegEngine(ffmpeg_path=FFMPEG, ffprobe_path=FFPROBE)
    timeline = _build_simple_timeline(
        project_root.project_id,
        media_files["clip1"],
        media_files["clip2"],
        media_files["music"],
    )
    timeline.data["tracks"][0]["items"][0]["transform"] = {
        "scale": 0.82,
        "x": 24,
        "y": -12,
        "rotation": 7,
    }
    preview = project_root.previews_dir / "v1_preview.mp4"

    output = engine.render_preview(timeline, preview)

    assert output.exists()
    assert output.stat().st_size > 0

    # Probe the output to verify it is a valid video of the expected duration.
    inspect = IngestManager(project_root, ffmpeg_path=FFMPEG, ffprobe_path=FFPROBE).inspect
    meta = inspect(output)
    assert meta.video_codec == "h264"
    assert meta.audio_codec == "aac"
    assert meta.width == 480 and meta.height == 360
    assert 2.5 <= meta.duration <= 3.5


def test_render_rejects_timeline_without_clips(project_root) -> None:
    engine = FFmpegEngine(ffmpeg_path=FFMPEG, ffprobe_path=FFPROBE)
    timeline = _build_simple_timeline(project_root.project_id, Path("/x"), Path("/y"), Path("/z"))
    timeline.data["tracks"] = []
    with pytest.raises(EngineError):
        engine.render_preview(timeline, project_root.previews_dir / "empty.mp4")
