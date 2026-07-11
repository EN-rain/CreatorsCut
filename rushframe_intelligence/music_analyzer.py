"""CPU-only beat, onset, and energy analysis using optional librosa."""

from __future__ import annotations

import tempfile
from pathlib import Path

from rushframe_intelligence.ffmpeg_tools import resolve_tool_path, run_tool
from rushframe_intelligence.models import MusicAnalysis


class MusicAnalysisUnavailable(RuntimeError):
    pass


def analyze_music(
    media_path: Path | str,
    ffmpeg_path: Path | str | None = None,
) -> MusicAnalysis:
    try:
        import librosa
    except ImportError as exc:
        raise MusicAnalysisUnavailable(
            "librosa is not installed. Install Rushframe's 'intelligence' extras."
        ) from exc

    source = Path(media_path)
    ffmpeg = resolve_tool_path("ffmpeg", ffmpeg_path)

    with tempfile.TemporaryDirectory() as tmp:
        wav_path = Path(tmp) / "audio.wav"
        result = run_tool(
            ffmpeg,
            [
                "-y", "-i", source, "-vn", "-acodec", "pcm_s16le",
                "-ar", "22050", "-ac", "1", wav_path,
            ],
            check=False,
            capture_output=True,
            text=True,
        )
        if result.returncode != 0 or not wav_path.exists():
            raise RuntimeError(
                f"ffmpeg audio extraction failed: {result.stderr.strip()}"
            )
        samples, sample_rate = librosa.load(str(wav_path), sr=22050, mono=True)
    tempo, beats = librosa.beat.beat_track(y=samples, sr=sample_rate)
    onset_frames = librosa.onset.onset_detect(y=samples, sr=sample_rate)
    rms = librosa.feature.rms(y=samples)[0]
    rms_times = librosa.frames_to_time(range(len(rms)), sr=sample_rate)
    tempo_value = float(tempo[0] if hasattr(tempo, "__len__") else tempo)
    return MusicAnalysis(
        tempo_bpm=tempo_value,
        beat_times=[float(value) for value in librosa.frames_to_time(beats, sr=sample_rate)],
        onset_times=[float(value) for value in librosa.frames_to_time(onset_frames, sr=sample_rate)],
        rms_times=[float(value) for value in rms_times],
        rms_energy=[float(value) for value in rms],
    )
