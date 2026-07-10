"""Tests for version-locking patch behavior and review gate."""

import pytest

from orchestrator.models import Modifier, TimelinePatch
from orchestrator.timeline_manager import (
    ApprovalRequiredError,
    TimelineManager,
    TimelineVersionConflictError,
)


class TestInitialTimeline:
    def test_create_initial_timeline_starts_at_version_1(
        self, sample_timeline_manager: TimelineManager
    ) -> None:
        timeline = sample_timeline_manager.create_initial_timeline("Demo")
        assert timeline.version == 1
        assert timeline.modified_by == Modifier.SYSTEM.value


class TestPatchVersionLocking:
    def test_patch_increments_version(self, sample_timeline_manager: TimelineManager) -> None:
        timeline = sample_timeline_manager.create_initial_timeline("Demo")
        patch = TimelinePatch(
            base_version=1,
            author="agent",
            operations=[{"op": "replace", "path": "/project/name", "value": "Demo Updated"}],
        )
        updated = sample_timeline_manager.apply_patch(patch)
        assert updated.version == 2
        assert updated.modified_by == Modifier.AGENT.value
        assert updated.data["project"]["name"] == "Demo Updated"

    def test_stale_base_version_rejected(self, sample_timeline_manager: TimelineManager) -> None:
        sample_timeline_manager.create_initial_timeline("Demo")
        stale_patch = TimelinePatch(
            base_version=0,
            author="agent",
            operations=[{"op": "replace", "path": "/project/name", "value": "Bad"}],
        )
        with pytest.raises(TimelineVersionConflictError):
            sample_timeline_manager.apply_patch(stale_patch)

    def test_user_edit_wins_over_agent(self, sample_timeline_manager: TimelineManager) -> None:
        sample_timeline_manager.create_initial_timeline("Demo")
        user_patch = TimelinePatch(
            base_version=1,
            author="user",
            operations=[{"op": "replace", "path": "/project/name", "value": "User Edit"}],
        )
        sample_timeline_manager.apply_patch(user_patch)

        agent_patch = TimelinePatch(
            base_version=2,
            author="agent",
            operations=[{"op": "replace", "path": "/project/name", "value": "Agent Override"}],
        )
        with pytest.raises(ApprovalRequiredError):
            sample_timeline_manager.apply_patch(agent_patch)

    def test_agent_can_continue_after_agent(self, sample_timeline_manager: TimelineManager) -> None:
        sample_timeline_manager.create_initial_timeline("Demo")
        patch1 = TimelinePatch(
            base_version=1,
            author="agent",
            operations=[{"op": "replace", "path": "/project/name", "value": "Agent V2"}],
        )
        sample_timeline_manager.apply_patch(patch1)

        patch2 = TimelinePatch(
            base_version=2,
            author="agent",
            operations=[{"op": "replace", "path": "/project/name", "value": "Agent V3"}],
        )
        updated = sample_timeline_manager.apply_patch(patch2)
        assert updated.version == 3


class TestReviewGate:
    def test_approval_gate(self, sample_timeline_manager: TimelineManager) -> None:
        timeline = sample_timeline_manager.create_initial_timeline("Demo")
        assert not timeline.review.approved_by_user

        timeline = sample_timeline_manager.approve(note="Looks good")
        assert timeline.review.approved_by_user
        assert timeline.review.approved_at is not None
        assert "Looks good" in timeline.review.notes
        assert timeline.data["project"]["status"] == "approved"

    def test_request_changes(self, sample_timeline_manager: TimelineManager) -> None:
        timeline = sample_timeline_manager.create_initial_timeline("Demo")
        sample_timeline_manager.approve()
        timeline = sample_timeline_manager.request_changes(note="Fix lyrics timing")
        assert not timeline.review.approved_by_user
        assert timeline.review.approved_at is None
        assert "Fix lyrics timing" in timeline.review.notes
        assert timeline.data["project"]["status"] == "changes_requested"


class TestPatchHistory:
    def test_patch_history_preserved(self, sample_timeline_manager: TimelineManager) -> None:
        sample_timeline_manager.create_initial_timeline("Demo")
        patch = TimelinePatch(
            base_version=1,
            author="agent",
            operations=[{"op": "replace", "path": "/project/name", "value": "Updated"}],
            reason="Tune title",
        )
        sample_timeline_manager.apply_patch(patch)
        history = sample_timeline_manager.patch_history_path
        assert history.exists()
        import json

        records = json.loads(history.read_text())
        assert len(records) == 1
        assert records[0]["version"] == 2
        assert records[0]["baseVersion"] == 1
        assert records[0]["author"] == "agent"
        assert records[0]["reason"] == "Tune title"
