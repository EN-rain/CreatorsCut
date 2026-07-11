"""Orchestrates optional scene, speech, music, and frame understanding."""

from __future__ import annotations

import json
from pathlib import Path

from rushframe_intelligence.ffmpeg_tools import run_tool
from rushframe_intelligence.gemini_analyzer import analyze_frame
from rushframe_intelligence.models import MediaAnalysis
from rushframe_intelligence.music_analyzer import analyze_music
from rushframe_intelligence.scene_detector import detect_scenes
from rushframe_intelligence.transcriber import transcribe


class MediaIntelligencePipeline:
    def __init__(self, ffmpeg_path: Path | str) -> None:
        self.ffmpeg_path = Path(ffmpeg_path)

    def run(
        self,
        media_path: Path | str,
        output_dir: Path | str,
        *,
        detect_visual_scenes: bool = True,
        transcribe_speech: bool = True,
        analyze_audio: bool = True,
        understand_frames: bool = False,
        whisper_model: str = "small",
        language: str | None = None,
    ) -> MediaAnalysis:
        source = Path(media_path).resolve()
        destination = Path(output_dir)
        destination.mkdir(parents=True, exist_ok=True)
        result = MediaAnalysis(source_path=str(source))

        if detect_visual_scenes:
            try:
                result.scenes = detect_scenes(source)
            except Exception as exc:  # Optional feature: retain partial results.
                result.warnings.append(f"scene detection skipped: {exc}")

        if transcribe_speech:
            try:
                result.transcript = transcribe(source, model_size=whisper_model, language=language)
            except Exception as exc:
                result.warnings.append(f"transcription skipped: {exc}")

        if analyze_audio:
            try:
                result.music = analyze_music(source, ffmpeg_path=self.ffmpeg_path)
            except Exception as exc:
                result.warnings.append(f"music analysis skipped: {exc}")

        if result.scenes:
            frame_dir = destination / "frames"
            frame_dir.mkdir(parents=True, exist_ok=True)
            for scene in result.scenes:
                midpoint = scene.start + max(0.0, scene.end - scene.start) / 2
                frame_path = frame_dir / f"{scene.scene_id}.jpg"
                command = [
                    "-y", "-ss", f"{midpoint:.3f}", "-i", source,
                    "-frames:v", "1", "-q:v", "3", frame_path,
                ]
                process = run_tool(
                    self.ffmpeg_path,
                    command,
                    check=False,
                    capture_output=True,
                    text=True,
                )
                if process.returncode != 0 or not frame_path.exists():
                    result.warnings.append(f"frame extraction failed for {scene.scene_id}")
                    continue
                scene.frame_path = str(frame_path)
                if understand_frames:
                    try:
                        understood = analyze_frame(
                            frame_path,
                            prompt=(
                                "Analyze this video scene for an editing agent. Describe the main action, "
                                "subjects, shot type, mood, and whether it is visually important."
                            ),
                        )
                        scene.description = str(understood.get("description", "")) or None
                        scene.tags = [str(tag) for tag in understood.get("tags", [])]
                        energy = understood.get("visual_energy")
                        scene.visual_energy = float(energy) if energy is not None else None
                    except Exception as exc:
                        result.warnings.append(f"Gemini analysis failed for {scene.scene_id}: {exc}")

        output_path = destination / "media-analysis.json"
        output_path.write_text(json.dumps(result.to_dict(), indent=2), encoding="utf-8")
        return result
