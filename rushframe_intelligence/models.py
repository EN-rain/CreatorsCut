"""Serializable models produced by the media-intelligence pipeline."""

from __future__ import annotations

from dataclasses import asdict, dataclass, field
from typing import Any


@dataclass
class SceneAnalysis:
    scene_id: str
    start: float
    end: float
    frame_path: str | None = None
    description: str | None = None
    tags: list[str] = field(default_factory=list)
    visual_energy: float | None = None


@dataclass
class TranscriptSegment:
    start: float
    end: float
    text: str
    words: list[dict[str, Any]] = field(default_factory=list)


@dataclass
class MusicAnalysis:
    tempo_bpm: float | None = None
    beat_times: list[float] = field(default_factory=list)
    onset_times: list[float] = field(default_factory=list)
    rms_times: list[float] = field(default_factory=list)
    rms_energy: list[float] = field(default_factory=list)


@dataclass
class MediaAnalysis:
    source_path: str
    scenes: list[SceneAnalysis] = field(default_factory=list)
    transcript: list[TranscriptSegment] = field(default_factory=list)
    music: MusicAnalysis | None = None
    warnings: list[str] = field(default_factory=list)
    schema_version: str = "1.0"

    def to_dict(self) -> dict[str, Any]:
        return asdict(self)
