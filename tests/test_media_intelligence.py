import json

from orchestrator.intelligence.gemini_analyzer import GeminiAnalysisError, analyze_frame
from orchestrator.intelligence.models import MediaAnalysis, SceneAnalysis
from orchestrator.media_intelligence_manager import (
    MediaIntelligenceManager,
    MediaIntelligenceSettings,
)
from orchestrator.models import MediaRegistryEntry, ProjectConfig


def test_media_analysis_serializes() -> None:
    analysis = MediaAnalysis(
        source_path="input.mp4",
        scenes=[SceneAnalysis(scene_id="scene_0001", start=0.0, end=2.5)],
    )

    payload = analysis.to_dict()

    assert payload["schema_version"] == "1.0"
    assert payload["scenes"][0]["end"] == 2.5


def test_gemini_requires_api_key(tmp_path, monkeypatch) -> None:
    frame = tmp_path / "frame.jpg"
    frame.write_bytes(b"jpeg")
    monkeypatch.delenv("GEMINI_API_KEY", raising=False)

    try:
        analyze_frame(frame, prompt="Describe")
    except GeminiAnalysisError as exc:
        assert "GEMINI_API_KEY" in str(exc)
    else:
        raise AssertionError("Expected GeminiAnalysisError")


def test_media_intelligence_manager_registers_cached_manifest(tmp_path) -> None:
    config = ProjectConfig("proj", tmp_path)
    config.input_metadata_dir.mkdir(parents=True)
    config.source_registry_path.write_text("[]", encoding="utf-8")
    video = tmp_path / "input" / "video" / "vid_001.mp4"
    video.parent.mkdir(parents=True)
    video.write_bytes(b"fake")
    entry = MediaRegistryEntry(
        source_id="vid_001",
        original_name="vid_001.mp4",
        project_path="input/video/vid_001.mp4",
        sha256="abc123",
        status="validated",
    )
    config.source_registry_path.write_text(json.dumps([entry.to_dict()]), encoding="utf-8")

    class Registry:
        def get_video(self, source_id: str):
            return entry if source_id == "vid_001" else None

        def list_videos(self):
            return [entry]

    manager = MediaIntelligenceManager(config, Registry(), ffmpeg_path="ffmpeg")  # type: ignore[arg-type]
    settings = MediaIntelligenceSettings(
        detect_visual_scenes=False,
        transcribe_speech=False,
        analyze_audio=False,
    )
    cache_key = manager._cache_key(entry.sha256, settings)  # noqa: SLF001
    cache = manager.cache_root / cache_key
    cache.mkdir(parents=True)
    (cache / "media-analysis.json").write_text('{"schema_version":"1.0"}', encoding="utf-8")

    result = manager.run_for_source("vid_001", settings=settings)

    assert result["sourceId"] == "vid_001"
    assert result["cached"] is True
    assert (config.analysis_dir / "vid_001" / "media-analysis.json").exists()
    assert "vid_001" in config.manifest_path.read_text(encoding="utf-8")
