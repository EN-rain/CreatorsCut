"""Tests for v10 §12 protected-path enforcement on patches."""

from __future__ import annotations

import pytest

from orchestrator.models import Modifier, TimelinePatch
from orchestrator.timeline_manager import (
    ApprovalRequiredError,
    ProtectedPathViolation,
    TimelineManager,
    TimelineVersionConflictError,
)


def _make_manager(tmp_path) -> TimelineManager:
    from orchestrator.models import ProjectConfig

    config = ProjectConfig(project_id="pp", root=tmp_path / "proj")
    config.timelines_dir.mkdir(parents=True, exist_ok=True)
    config.review_dir.mkdir(parents=True, exist_ok=True)
    return TimelineManager(config)


class TestProtectedPaths:
    def test_patch_blocked_when_targeting_protected_path(self, tmp_path) -> None:
        tm = _make_manager(tmp_path)
        tm.create_initial_timeline("Demo")

        patch = TimelinePatch(
            base_version=1,
            author="agent",
            operations=[
                {"op": "replace", "path": "/tracks/lyrics/items/0/text", "value": "NEW LYRICS"}
            ],
            reason="oops",
            protected_paths=["/tracks/lyrics"],
        )
        with pytest.raises(ProtectedPathViolation):
            tm.apply_patch(patch)

    def test_patch_allowed_when_not_targeting_protected_path(self, tmp_path) -> None:
        tm = _make_manager(tmp_path)
        tm.create_initial_timeline("Demo")

        patch = TimelinePatch(
            base_version=1,
            author="agent",
            operations=[
                {"op": "replace", "path": "/project/name", "value": "Updated"}
            ],
            reason="rename",
            protected_paths=["/tracks/lyrics"],
        )
        updated = tm.apply_patch(patch)
        assert updated.version == 2
        assert updated.data["project"]["name"] == "Updated"

    def test_protected_path_prefix_matched(self, tmp_path) -> None:
        """A protected prefix should block any nested operation beneath it."""
        tm = _make_manager(tmp_path)
        tm.create_initial_timeline("Demo")

        patch = TimelinePatch(
            base_version=1,
            author="agent",
            operations=[
                {"op": "replace", "path": "/tracks/lyrics/items/2/text", "value": "x"}
            ],
            protected_paths=["/tracks/lyrics"],
        )
        with pytest.raises(ProtectedPathViolation):
            tm.apply_patch(patch)

    def test_exact_protected_path_also_blocked(self, tmp_path) -> None:
        tm = _make_manager(tmp_path)
        tm.create_initial_timeline("Demo")

        patch = TimelinePatch(
            base_version=1,
            author="agent",
            operations=[{"op": "replace", "path": "/tracks/lyrics", "value": "x"}],
            protected_paths=["/tracks/lyrics"],
        )
        with pytest.raises(ProtectedPathViolation):
            tm.apply_patch(patch)
