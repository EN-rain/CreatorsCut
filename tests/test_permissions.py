"""Tests for the tool permission policy (v10 §7)."""

from __future__ import annotations

import pytest

from orchestrator.permissions import (
    ALLOWED_AGENT_TOOLS,
    CONDITIONAL_AGENT_TOOLS,
    FORBIDDEN_AGENT_TOOLS,
    assert_not_forbidden,
    is_tool_allowed,
    is_tool_forbidden,
)


class TestForbiddenTools:
    @pytest.mark.parametrize(
        "tool_name",
        [
            "download_video_from_url",
            "download_song_from_url",
            "scrape_clipster",
            "scrape_youtube",
            "scrape_tiktok",
            "scrape_instagram",
            "publish_to_tiktok",
            "publish_to_instagram",
            "publish_to_youtube",
            "submit_to_clipster",
            "approve_project",
            "final_export_without_approval",
            "read_browser_cookies",
            "read_social_sessions",
            "read_clipster_credentials",
        ],
    )
    def test_forbidden_tool_raises(self, tool_name: str) -> None:
        assert is_tool_forbidden("agent", tool_name) is True
        with pytest.raises(PermissionError):
            assert_not_forbidden(tool_name)

    def test_assert_not_forbidden_allows_normal_tool(self) -> None:
        # Should not raise
        assert_not_forbidden("render_preview")


class TestAllowedTools:
    def test_allowed_tools_subset(self) -> None:
        assert "render_preview" in ALLOWED_AGENT_TOOLS
        assert "check_task_inputs" in ALLOWED_AGENT_TOOLS
        assert "mark_task_blocked" in ALLOWED_AGENT_TOOLS
        assert "mark_task_ready_for_review" in ALLOWED_AGENT_TOOLS

    def test_conditional_tools_exist(self) -> None:
        assert "import_media" in CONDITIONAL_AGENT_TOOLS
        assert "import_audio" in CONDITIONAL_AGENT_TOOLS
        assert "import_asset" in CONDITIONAL_AGENT_TOOLS

    def test_user_can_call_anything(self) -> None:
        # The user (local UI) is never blocked by the policy module itself —
        # API auth/authz is a separate layer.
        assert is_tool_allowed("user", "anything") is True

    def test_agent_cannot_call_unlisted_tool(self) -> None:
        assert is_tool_allowed("agent", "telekinesis") is False

    def test_disjoint_from_forbidden(self) -> None:
        # The allowed and forbidden sets must never overlap
        assert ALLOWED_AGENT_TOOLS.isdisjoint(FORBIDDEN_AGENT_TOOLS)
        assert CONDITIONAL_AGENT_TOOLS.isdisjoint(FORBIDDEN_AGENT_TOOLS)
