"""End-to-end smoke test: launch ``scripts/run_local.py`` as a real subprocess
and verify that the web UI displays the MCP URL + token, and that the
MCP server itself responds to health checks.

This proves the documented ``python3 run.py`` / ``python3 scripts/run_local.py``
flow actually works: both services start, the session file is written,
the web page exposes it, and the MCP endpoint is reachable.
"""

from __future__ import annotations

import os
import subprocess
import sys
import time
from pathlib import Path

import httpx
import pytest


REPO_ROOT = Path(__file__).resolve().parent.parent
RUN_LOCAL = REPO_ROOT / "scripts" / "run_local.py"
SESSION_FILE = REPO_ROOT / ".kimchi" / "mcp-session.json"

# High ports unlikely to collide with anything else on the test host.
MCP_PORT = 18765
WEB_PORT = 13000
TOKEN = "smoke-test-token-abc123"
READY_TIMEOUT_S = 30.0
SHUTDOWN_TIMEOUT_S = 8.0


def _wait_ready(url: str, timeout_s: float) -> bool:
    deadline = time.time() + timeout_s
    while time.time() < deadline:
        try:
            r = httpx.get(url, timeout=1.0)
            if r.status_code == 200:
                return True
        except Exception:
            pass
        time.sleep(0.3)
    return False


@pytest.fixture(autouse=True)
def _isolate_session_file():
    """Ensure no leftover session file from a prior run interferes."""
    if SESSION_FILE.exists():
        try:
            SESSION_FILE.unlink()
        except OSError:
            pass
    yield
    if SESSION_FILE.exists():
        try:
            SESSION_FILE.unlink()
        except OSError:
            pass


@pytest.fixture
def run_all_proc(_isolate_session_file, tmp_path):
    """Launch scripts/run_local.py and tear it down after the test."""
    stdout_path = tmp_path / "stdout.log"
    stderr_path = tmp_path / "stderr.log"
    stdout_f = open(stdout_path, "w")
    stderr_f = open(stderr_path, "w")
    env = {**os.environ, "AGENT_VIDEO_EDITOR_TOKEN": TOKEN}
    proc = subprocess.Popen(
        [sys.executable, str(RUN_LOCAL),
         "--mcp-port", str(MCP_PORT),
         "--web-port", str(WEB_PORT),
         "--token", TOKEN],
        cwd=str(REPO_ROOT),
        env=env,
        stdout=stdout_f,
        stderr=stderr_f,
    )
    try:
        # Wait for the web server to become reachable.
        ready = _wait_ready(f"http://127.0.0.1:{WEB_PORT}/api/health", READY_TIMEOUT_S)
        if not ready:
            # Give the child a moment to flush, then surface diagnostics.
            time.sleep(0.5)
            try:
                proc.wait(timeout=1.0)
            except subprocess.TimeoutExpired:
                pass
            stdout_f.flush()
            stderr_f.flush()
            raise AssertionError(
                f"run_local.py did not become ready within {READY_TIMEOUT_S}s.\n"
                f"returncode={proc.poll()}\n"
                f"stdout:\n{stdout_path.read_text(errors='replace')[:2000]}\n"
                f"stderr:\n{stderr_path.read_text(errors='replace')[:2000]}\n"
                f"session_file_exists={SESSION_FILE.exists()}"
            )
        yield proc
    finally:
        if proc.poll() is None:
            proc.terminate()
            try:
                proc.wait(timeout=SHUTDOWN_TIMEOUT_S)
            except subprocess.TimeoutExpired:
                proc.kill()
                proc.wait(timeout=2.0)
        stdout_f.close()
        stderr_f.close()


class TestRunAllSmoke:
    def test_web_health_endpoint_responds(self, run_all_proc) -> None:
        r = httpx.get(f"http://127.0.0.1:{WEB_PORT}/api/health", timeout=2.0)
        assert r.status_code == 200
        body = r.json()
        assert body["ok"] is True
        assert body["mcpAvailable"] is True

    def test_web_mcp_info_reveals_url_and_token(self, run_all_proc) -> None:
        r = httpx.get(f"http://127.0.0.1:{WEB_PORT}/api/mcp-info", timeout=2.0)
        assert r.status_code == 200
        info = r.json()
        assert info["available"] is True
        assert info["url"] == f"http://127.0.0.1:{MCP_PORT}/mcp"
        assert info["token"] == TOKEN
        assert info["host"] == "127.0.0.1"
        assert info["port"] == MCP_PORT
        assert info["pid"]  # MCP launcher PID is recorded
        assert info["ageSeconds"] is not None
        assert info["ageSeconds"] >= 0

    def test_web_index_page_contains_branding(self, run_all_proc) -> None:
        r = httpx.get(f"http://127.0.0.1:{WEB_PORT}/", timeout=2.0)
        assert r.status_code == 200
        text = r.text
        assert "CreatorCut" in text
        assert "Projects" in text  # new server redirects "/" to /dashboard
        # The page fetches the token at runtime — it must NOT be baked in.
        assert TOKEN not in text

    def test_mcp_server_health_responds(self, run_all_proc) -> None:
        r = httpx.get(f"http://127.0.0.1:{MCP_PORT}/health", timeout=2.0)
        assert r.status_code == 200
        body = r.json()
        assert body["ok"] is True
        assert body["server"] == "agent-video-editor-mcp"
        assert body["toolsRegistered"] >= 20

    def test_mcp_server_tools_list_with_token(self, run_all_proc) -> None:
        """The MCP endpoint must accept the shared token and return the tool list."""
        payload = {"jsonrpc": "2.0", "id": 1, "method": "tools/list"}
        r = httpx.post(
            f"http://127.0.0.1:{MCP_PORT}/mcp",
            json=payload,
            headers={"Authorization": f"Bearer {TOKEN}"},
            timeout=2.0,
        )
        assert r.status_code == 200
        body = r.json()
        tools = body["result"]["tools"]
        names = {t["name"] for t in tools}
        # Spot-check a few well-known tools
        assert "list_open_tasks" in names
        assert "render_preview" in names
        assert "import_media" in names
        # Forbidden tools must never appear
        assert "approve_project" not in names
        assert "submit_to_clipster" not in names
        assert body["result"]["forbiddenRegistered"] == []

    def test_mcp_server_rejects_missing_token(self, run_all_proc) -> None:
        payload = {"jsonrpc": "2.0", "id": 1, "method": "tools/list"}
        r = httpx.post(
            f"http://127.0.0.1:{MCP_PORT}/mcp",
            json=payload,
            timeout=2.0,
        )
        assert r.status_code == 401

    def test_session_file_written_and_well_formed(self, run_all_proc) -> None:
        assert SESSION_FILE.exists()
        import json
        data = json.loads(SESSION_FILE.read_text())
        assert data["url"] == f"http://127.0.0.1:{MCP_PORT}/mcp"
        assert data["token"] == TOKEN
        assert data["port"] == MCP_PORT
