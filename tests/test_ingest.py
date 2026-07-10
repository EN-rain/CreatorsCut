"""Tests for the ingest pipeline.

These tests run against the real FFmpeg/FFprobe binaries shipped in .tools/bin/.
A small synthetic clip is generated per test using FFmpeg's testsrc filter.
"""

from __future__ import annotations

import subprocess
from pathlib import Path

import pytest

from orchestrator.ffmpeg_tools import build_tool_command, resolve_tool_path
from orchestrator.ingest_manager import (
    IngestError,
    IngestManager,
    SUPPORTED_VIDEO_CODECS,
)
from orchestrator.models import ProjectConfig
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
def project_root(tmp_path: Path) -> ProjectConfig:
    manager = ProjectManager(tmp_path / "projects")
    return manager.create_project("ingest-test", "Ingest Test")


@pytest.fixture
def ingest(project_root: ProjectConfig) -> IngestManager:
    return IngestManager(project_root, ffmpeg_path=FFMPEG, ffprobe_path=FFPROBE)


@pytest.fixture
def sample_clip(tmp_path: Path) -> Path:
    """Generate a 2-second 320x240 H.264/AAC test clip."""
    target = tmp_path / "sample_clip.mp4"
    cmd = build_tool_command(FFMPEG, [
        "-y",
        "-f", "lavfi",
        "-i", "testsrc=duration=2:size=320x240:rate=30",
        "-f", "lavfi",
        "-i", "sine=frequency=440:duration=2",
        "-c:v", "libx264",
        "-preset", "veryfast",
        "-c:a", "aac",
        "-shortest",
        target,
    ])
    result = subprocess.run(cmd, check=False, capture_output=True, text=True)
    if result.returncode != 0:
        pytest.skip(f"Could not generate test clip: {result.stderr[-200:]}")
    assert target.exists() and target.stat().st_size > 0
    return target


class TestRegisterInput:
    def test_copies_file_into_input_dir(self, project_root: ProjectConfig, sample_clip: Path) -> None:
        manager = IngestManager(project_root, ffmpeg_path=FFMPEG, ffprobe_path=FFPROBE)
        target = manager.register_input(sample_clip)
        assert target.exists()
        assert target.parent == project_root.input_dir
        assert target.suffix == ".mp4"

    def test_register_input_rejects_missing(self, project_root: ProjectConfig) -> None:
        manager = IngestManager(project_root, ffmpeg_path=FFMPEG, ffprobe_path=FFPROBE)
        with pytest.raises(IngestError):
            manager.register_input(Path("/no/such/file.mp4"))


class TestInspect:
    def test_extracts_video_audio_metadata(self, ingest: IngestManager, sample_clip: Path) -> None:
        meta = ingest.inspect(sample_clip)
        assert meta.container == "mp4"
        assert meta.width == 320
        assert meta.height == 240
        assert meta.orientation == "landscape"
        assert meta.video_codec in SUPPORTED_VIDEO_CODECS
        assert meta.audio_codec is not None
        assert meta.fps is not None and meta.fps > 0
        assert 1.5 <= meta.duration <= 2.5

    def test_validate_returns_empty_for_supported_clip(self, ingest: IngestManager, sample_clip: Path) -> None:
        meta = ingest.inspect(sample_clip)
        issues = ingest.validate(meta)
        # mp4/h264/aac should pass validation cleanly
        assert issues == []


class TestProxyAndThumbnail:
    def test_generate_proxy(self, ingest: IngestManager, sample_clip: Path) -> None:
        proxy = ingest.generate_proxy(sample_clip, target_height=180)
        assert proxy.exists() and proxy.stat().st_size > 0
        assert proxy.suffix == ".mp4"
        # Verify proxy was actually transcoded to the requested height
        probe = ingest.inspect(proxy)
        assert probe.height == 180
        assert probe.video_codec == "h264"

    def test_generate_thumbnail(self, ingest: IngestManager, sample_clip: Path) -> None:
        thumb = ingest.generate_thumbnail(sample_clip, at_time=0.5, width=160)
        assert thumb.exists() and thumb.stat().st_size > 0
        assert thumb.suffix == ".png"
