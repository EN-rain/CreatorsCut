"""Tests for the media/audio/asset registry (v10 §3.3–3.5, §5.3–5.4)."""

from __future__ import annotations

import subprocess
from pathlib import Path

import pytest

from orchestrator.ffmpeg_tools import build_tool_command, resolve_tool_path
from orchestrator.ingest_manager import IngestManager
from orchestrator.media_registry import MediaRegistry
from orchestrator.models import ProjectConfig
from orchestrator.project_manager import ProjectManager


TOOLS_DIR = Path(__file__).resolve().parent.parent / ".tools" / "bin"
FFMPEG = resolve_tool_path("ffmpeg", TOOLS_DIR / "ffmpeg-wrapper.sh")
FFPROBE = resolve_tool_path("ffprobe", TOOLS_DIR / "ffprobe-wrapper.sh")


def _has_ffmpeg() -> bool:
    return Path(FFMPEG).exists()


pytestmark = pytest.mark.skipif(
    not _has_ffmpeg(), reason=f"FFmpeg wrappers not present at {TOOLS_DIR}"
)


@pytest.fixture
def registry(tmp_path: Path) -> tuple[ProjectConfig, MediaRegistry]:
    config = ProjectManager(tmp_path / "projects").create_project("reg-test", "Reg Test")
    ingest = IngestManager(config, ffmpeg_path=FFMPEG, ffprobe_path=FFPROBE)
    return config, MediaRegistry(config, ingest)


@pytest.fixture
def sample_clip(tmp_path: Path) -> Path:
    target = tmp_path / "src_clip.mp4"
    cmd = build_tool_command(FFMPEG, [
        "-y",
        "-f", "lavfi",
        "-i", "testsrc=duration=2:size=320x240:rate=30",
        "-c:v", "libx264",
        "-preset", "veryfast",
        target,
    ])
    r = subprocess.run(cmd, check=False, capture_output=True, text=True)
    if r.returncode != 0:
        pytest.skip(f"Could not generate test clip: {r.stderr[-200:]}")
    return target


@pytest.fixture
def sample_audio(tmp_path: Path) -> Path:
    target = tmp_path / "src_audio.mp3"
    cmd = build_tool_command(FFMPEG, [
        "-y",
        "-f", "lavfi",
        "-i", "sine=frequency=440:duration=2",
        "-c:a", "libmp3lame",
        target,
    ])
    r = subprocess.run(cmd, check=False, capture_output=True, text=True)
    if r.returncode != 0:
        pytest.skip(f"Could not generate test audio: {r.stderr[-200:]}")
    return target


class TestRegistryImport:
    def test_import_video_creates_registry_entry(
        self, registry: tuple[ProjectConfig, MediaRegistry], sample_clip: Path
    ) -> None:
        config, reg = registry
        entry = reg.import_video(sample_clip, source_id="vid_001")
        assert entry.source_id == "vid_001"
        assert entry.status == "validated"
        assert entry.width == 320
        assert entry.height == 240
        assert entry.sha256  # non-empty

        # File is copied under input/video
        target = config.root / entry.project_path
        assert target.exists()

        # Registry is queryable
        videos = reg.list_videos()
        assert len(videos) == 1
        assert reg.get_video("vid_001") is not None
        assert reg.resolve_video_path("vid_001") == target

    def test_import_audio(
        self, registry: tuple[ProjectConfig, MediaRegistry], sample_audio: Path
    ) -> None:
        config, reg = registry
        entry = reg.import_audio(sample_audio, audio_id="aud_001")
        assert entry.audio_id == "aud_001"
        assert entry.status == "validated"
        assert entry.duration > 0
        assert entry.sample_rate > 0

    def test_import_asset(
        self, registry: tuple[ProjectConfig, MediaRegistry], tmp_path: Path
    ) -> None:
        config, reg = registry
        # Create a tiny fake asset
        fake = tmp_path / "logo.png"
        fake.write_bytes(b"\x89PNG\r\n\x1a\n" + b"\x00" * 32)
        entry = reg.import_asset(fake, kind="logo", asset_id="asset_logo")
        assert entry.asset_id == "asset_logo"
        assert entry.kind == "logo"
        assert entry.sha256

    def test_reject_unsupported_extension(
        self, registry: tuple[ProjectConfig, MediaRegistry], tmp_path: Path
    ) -> None:
        config, reg = registry
        bad = tmp_path / "evil.exe"
        bad.write_bytes(b"MZ")
        with pytest.raises(ValueError):
            reg.import_video(bad)

    def test_id_collision_appends_counter(
        self, registry: tuple[ProjectConfig, MediaRegistry], sample_clip: Path
    ) -> None:
        config, reg = registry
        e1 = reg.import_video(sample_clip, source_id="vid_dup")
        e2 = reg.import_video(sample_clip, source_id="vid_dup")
        assert e1.source_id == "vid_dup"
        assert e2.source_id == "vid_dup"
        # The second copy lives at a suffixed path
        assert e1.project_path != e2.project_path
