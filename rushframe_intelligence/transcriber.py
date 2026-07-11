"""CPU-oriented speech transcription using optional faster-whisper."""

from __future__ import annotations

from pathlib import Path

from rushframe_intelligence.models import TranscriptSegment


class TranscriptionUnavailable(RuntimeError):
    pass


def transcribe(
    media_path: Path | str,
    model_size: str = "small",
    language: str | None = None,
    compute_type: str = "int8",
) -> list[TranscriptSegment]:
    try:
        from faster_whisper import WhisperModel
    except ImportError as exc:
        raise TranscriptionUnavailable(
            "faster-whisper is not installed. Install Rushframe's 'intelligence' extras."
        ) from exc

    model = WhisperModel(model_size, device="cpu", compute_type=compute_type)
    segments, _info = model.transcribe(
        str(Path(media_path)),
        language=language,
        word_timestamps=True,
        vad_filter=True,
    )
    output: list[TranscriptSegment] = []
    for segment in segments:
        words = [
            {"start": word.start, "end": word.end, "text": word.word}
            for word in (segment.words or [])
        ]
        output.append(
            TranscriptSegment(
                start=float(segment.start),
                end=float(segment.end),
                text=segment.text.strip(),
                words=words,
            )
        )
    return output
