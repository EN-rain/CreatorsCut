# CreatorCut Media Intelligence Setup

This codebase now includes an optional CPU-friendly media intelligence pipeline under `orchestrator/intelligence/`.

## What is implemented

- Scene boundary detection with PySceneDetect.
- Representative frame extraction with CreatorCut's bundled FFmpeg.
- Local speech transcription with faster-whisper using CPU INT8.
- Beat, onset, tempo, and RMS energy analysis with librosa.
- Optional Gemini frame understanding using extracted scene frames.
- Combined output in `media-analysis.json`.

The pipeline degrades gracefully. CreatorCut still starts when the optional packages are missing, and any unavailable analysis stage is added to the output `warnings` list.

## Local agent installation task

From the CreatorCut project root, install the optional dependency group:

```powershell
py -m pip install -e ".[intelligence]"
```

The first faster-whisper run downloads the selected Whisper model. For a 16 GB RAM, CPU-only machine, use `base` or `small` with `compute_type="int8"`. Do not default to large models.

No Demucs or speaker diarization dependency was added. Both are too expensive to run automatically on the target CPU-only machine and are not required for the first implementation.

## Gemini configuration

Create a Gemini API key and expose it only through the environment:

```powershell
$env:GEMINI_API_KEY="your-key"
$env:CREATORCUT_GEMINI_MODEL="gemini-2.0-flash"
```

`CREATORCUT_GEMINI_MODEL` is configurable because free-tier model availability can change. Never commit the API key.

The current adapter sends one representative JPEG per detected scene rather than uploading the entire source video. This reduces bandwidth, API usage, and privacy exposure. Keep `understand_frames=False` for fully local operation.

## Example integration

```python
from pathlib import Path

from orchestrator.intelligence import MediaIntelligencePipeline

pipeline = MediaIntelligencePipeline(Path(".tools/bin/ffmpeg-wrapper.sh"))
analysis = pipeline.run(
    "projects/sample/input/video/source.mp4",
    "projects/sample/analysis/source",
    whisper_model="small",
    understand_frames=False,
)
```

On Windows, pass the same FFmpeg path already resolved by `apps/editor-web/server.py` rather than hard-coding the wrapper shown above.

## Expected output

```text
projects/<project-id>/analysis/<media-id>/
  media-analysis.json
  frames/
    scene_0001.jpg
    scene_0002.jpg
```

The JSON contains scene timestamps, frame paths, transcript segments with word timestamps, music beats/onsets/energy, optional Gemini descriptions/tags, and warnings.

## Remaining integration work for the next local agent

1. Add an editor/API action that starts `MediaIntelligencePipeline.run()` for a registered source.
2. Run analysis as a background task or worker because transcription can take minutes on CPU.
3. Show stage progress and cancellation in the task board.
4. Register `media-analysis.json` in the project manifest/media registry.
5. Make the timeline agent read scene tags, transcript timestamps, beats, and energy.
6. Add caching keyed by source checksum plus analysis settings.
7. Add per-stage toggles in project settings.
8. Add API rate limiting and retry/backoff for Gemini.
9. Validate the currently available Gemini free-tier model before enabling frame understanding by default.
10. Add privacy text warning that free API processing sends extracted frames to a third party.

Do not copy external repositories into CreatorCut. Keep them as dependencies and preserve CreatorCut's own timeline, task, registry, and rendering architecture.
