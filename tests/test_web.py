"""Tests for the local web UI + MCP launcher session-file behavior."""

from __future__ import annotations

import importlib.util
import json
import os
import sys
from pathlib import Path

import pytest
from fastapi.testclient import TestClient


REPO_ROOT = Path(__file__).resolve().parent.parent
WEB_SERVER_PATH = REPO_ROOT / "apps" / "editor-web" / "server.py"
LAUNCHER_PATH = REPO_ROOT / "scripts" / "run_mcp_server.py"


def _load_module(path: Path, name: str):
    """Load a single .py file as a module without requiring a package."""
    spec = importlib.util.spec_from_file_location(name, str(path))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


# ---------------------------------------------------------------------------
# Web server
# ---------------------------------------------------------------------------


@pytest.fixture
def web_server_mod(tmp_path, monkeypatch):
    """Load apps/editor-web/server.py with SESSION_FILE redirected to tmp_path."""
    mod = _load_module(WEB_SERVER_PATH, "web_server_under_test")
    fake_session = tmp_path / ".kimchi" / "mcp-session.json"
    fake_session.parent.mkdir(parents=True, exist_ok=True)
    monkeypatch.setattr(mod, "SESSION_FILE", fake_session)
    return mod, fake_session


@pytest.fixture
def web_client(web_server_mod):
    mod, fake_session = web_server_mod
    app = mod.create_app()
    return TestClient(app), fake_session


def _write_session(path: Path, **overrides) -> dict:
    payload = {
        "url": "http://127.0.0.1:8765/mcp",
        "token": "abc-token-xyz",
        "host": "127.0.0.1",
        "port": 8765,
        "pid": os.getpid(),  # definitely alive
        "startedAt": 1700000000.0,
    }
    payload.update(overrides)
    path.write_text(json.dumps(payload), encoding="utf-8")
    return payload


class TestWebHealth:
    def test_health_ok_no_session(self, web_client) -> None:
        client, _ = web_client
        r = client.get("/api/health")
        assert r.status_code == 200
        body = r.json()
        assert body["ok"] is True
        assert body["mcpAvailable"] is False


class TestWebMcpInfo:
    def test_no_session_returns_unavailable(self, web_client) -> None:
        client, _ = web_client
        r = client.get("/api/mcp-info")
        assert r.status_code == 200
        body = r.json()
        assert body["available"] is False

    def test_with_alive_session_returns_full_info(self, web_client) -> None:
        client, session_path = web_client
        _write_session(session_path)
        r = client.get("/api/mcp-info")
        body = r.json()
        assert body["available"] is True
        assert body["url"] == "http://127.0.0.1:8765/mcp"
        assert body["token"] == "abc-token-xyz"
        assert body["host"] == "127.0.0.1"
        assert body["port"] == 8765
        assert body["pid"] == os.getpid()
        assert body["ageSeconds"] is not None
        assert body["ageSeconds"] >= 0

    def test_stale_session_dead_pid_returns_unavailable(
        self, web_client, web_server_mod, monkeypatch
    ) -> None:
        client, session_path = web_client
        web_mod, _ = web_server_mod
        # A PID that almost certainly does not exist on any sane system.
        _write_session(session_path, pid=999_999)

        original_kill = os.kill

        def fake_kill(pid: int, sig: int) -> None:
            if pid == 999_999:
                raise ProcessLookupError("no such process")
            return original_kill(pid, sig)

        monkeypatch.setattr(web_mod.os, "kill", fake_kill)
        r = client.get("/api/mcp-info")
        body = r.json()
        assert body["available"] is False

    def test_corrupt_session_file_returns_unavailable(self, web_client) -> None:
        client, session_path = web_client
        session_path.write_text("{ this is not valid json", encoding="utf-8")
        r = client.get("/api/mcp-info")
        body = r.json()
        assert body["available"] is False


class TestWebIndex:
    def test_root_redirects_to_dashboard(self, web_client) -> None:
        client, _ = web_client
        r = client.get("/", follow_redirects=False)
        assert r.status_code in (301, 302, 307, 308)
        assert r.headers["location"] == "/dashboard"

    def test_dashboard_serves_html(self, web_client) -> None:
        client, _ = web_client
        r = client.get("/dashboard")
        assert r.status_code == 200
        text = r.text
        assert "CreatorCut" in text
        assert "Projects" in text
        # MCP is mentioned in the runtime/sidebar status.
        assert "MCP" in text  # always present, even when not running

    def test_static_css_accessible(self, web_client) -> None:
        client, _ = web_client
        r = client.get("/static/style.css")
        assert r.status_code == 200
        assert "--bg" in r.text or "background" in r.text

    def test_dashboard_does_not_leak_token_when_no_session(self, web_client) -> None:
        client, _ = web_client
        r = client.get("/dashboard")
        assert r.status_code == 200
        assert "abc-token-xyz" not in r.text


# ---------------------------------------------------------------------------
# MCP launcher session-file helpers
# ---------------------------------------------------------------------------


@pytest.fixture
def launcher(tmp_path, monkeypatch):
    """Import run_mcp_server with SESSION_FILE redirected to tmp_path."""
    module = _load_module(LAUNCHER_PATH, "run_mcp_server_under_test")
    fake = tmp_path / ".kimchi" / "mcp-session.json"
    monkeypatch.setattr(module, "SESSION_FILE", fake)
    return module, fake


class TestLauncherSessionFile:
    def test_write_session_creates_file(self, launcher) -> None:
        mod, fake = launcher
        mod._write_session(url="http://127.0.0.1:9999/mcp", token="xyz", port=9999)
        assert fake.exists()
        data = json.loads(fake.read_text())
        assert data["url"] == "http://127.0.0.1:9999/mcp"
        assert data["token"] == "xyz"
        assert data["port"] == 9999
        assert data["pid"] == os.getpid()
        assert isinstance(data["startedAt"], float)

    def test_clear_session_removes_file(self, launcher) -> None:
        mod, fake = launcher
        mod._write_session(url="http://x", token="y", port=1)
        assert fake.exists()
        mod._clear_session()
        assert not fake.exists()

    def test_clear_session_when_missing_is_safe(self, launcher) -> None:
        mod, fake = launcher
        assert not fake.exists()
        mod._clear_session()  # must not raise
        assert not fake.exists()

    def test_refuses_non_localhost(self, launcher, monkeypatch, capsys) -> None:
        """main() must return code 2 when --host is not localhost.

        The module's __main__ guard does ``raise SystemExit(main())``, so
        when invoked via the CLI the return value becomes SystemExit.
        Here we call main() directly and assert the return code.
        """
        mod, _ = launcher
        monkeypatch.setattr(sys, "argv", ["run_mcp_server.py", "--host", "0.0.0.0"])
        rc = mod.main()
        assert rc == 2
        captured = capsys.readouterr()
        assert "127.0.0.1" in captured.err

    def test_print_token_only_writes_to_stdout(self, launcher, monkeypatch, capsys) -> None:
        mod, _ = launcher
        monkeypatch.setattr(sys, "argv", ["run_mcp_server.py", "--print-token-only"])
        rc = mod.main()
        assert rc == 0
        token = capsys.readouterr().out.strip()
        # Token should be non-empty hex (uuid hex * 2 in generate_session_token)
        assert len(token) >= 32
        int(token, 16)  # must be valid hex
