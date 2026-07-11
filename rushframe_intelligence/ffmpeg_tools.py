"""FFmpeg/FFprobe command helpers for native execution."""

from __future__ import annotations

import os
import shutil
import subprocess
from pathlib import Path


def _is_windows() -> bool:
    return os.name == "nt"


def resolve_tool_path(tool_name: str, preferred: Path | str | None = None) -> Path | str:
    """Return the best available executable/wrapper for ffmpeg or ffprobe."""
    repo_root = Path(__file__).resolve().parent.parent
    bundled_exe = repo_root / ".tools" / "bin" / f"{tool_name}.exe"
    if _is_windows() and bundled_exe.exists():
        return bundled_exe

    if preferred:
        preferred_path = Path(preferred)
        if preferred_path.exists() and not (_is_windows() and preferred_path.suffix == ".sh"):
            return preferred_path

    found = shutil.which(f"{tool_name}.exe") or shutil.which(tool_name)
    if found:
        return found

    bundled = repo_root / ".tools" / "bin" / f"{tool_name}-wrapper.sh"
    if not _is_windows() and bundled.exists():
        return bundled
    if _is_windows() and preferred and Path(preferred).suffix == ".sh":
        return tool_name
    return preferred or tool_name


def build_tool_command(executable: Path | str, args: list[str | Path]) -> list[str]:
    """Build a subprocess command for native execution."""
    return [str(executable), *(str(arg) for arg in args)]


def run_tool(
    executable: Path | str,
    args: list[str | Path],
    **kwargs,
) -> subprocess.CompletedProcess[str]:
    return subprocess.run(build_tool_command(executable, args), **kwargs)
