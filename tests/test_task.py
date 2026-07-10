"""Tests for the UserTask state machine (v10 §3.2, §8)."""

from __future__ import annotations

import pytest

from orchestrator.models import ProjectConfig, TaskStatus, UserTask
from orchestrator.task_manager import InvalidTaskTransition, TaskManager


def _make_task(task_id: str = "task_001") -> UserTask:
    return UserTask(
        task_id=task_id,
        project_id="proj_001",
        campaign_id="camp_001",
        title="Make a draft",
        objective="Create a 15s edit",
        style_brief="Dark villain aura",
        target_platform="tiktok_reels_shorts",
        target_duration={"min": 15.0, "max": 20.0},
        required_video_ids=["vid_001"],
        required_audio_ids=["aud_001"],
    )


@pytest.fixture
def task_manager(tmp_path) -> TaskManager:
    config = ProjectConfig(project_id="proj_001", root=tmp_path / "proj")
    config.tasks_dir.mkdir(parents=True, exist_ok=True)
    config.task_history_path.touch()
    return TaskManager(config)


class TestTaskLifecycle:
    def test_create_and_load(self, task_manager: TaskManager) -> None:
        task = task_manager.create(_make_task())
        loaded = task_manager.load("task_001")
        assert loaded.task_id == "task_001"
        assert loaded.status == TaskStatus.OPEN

    def test_open_path_to_ready_for_review(self, task_manager: TaskManager) -> None:
        task = task_manager.create(_make_task())
        task = task_manager.claim(task, "agent-001")
        assert task.status == TaskStatus.CLAIMED_BY_AGENT
        task = task_manager.transition(task, TaskStatus.CHECKING_INPUTS)
        task = task_manager.transition(task, TaskStatus.ANALYZING)
        task = task_manager.transition(task, TaskStatus.PLANNING)
        task = task_manager.transition(task, TaskStatus.RENDERING_PREVIEW)
        task = task_manager.mark_ready_for_review(task)
        assert task.status == TaskStatus.READY_FOR_REVIEW

    def test_block_when_missing_inputs(self, task_manager: TaskManager) -> None:
        task = task_manager.create(_make_task())
        task = task_manager.claim(task, "agent")
        task = task_manager.transition(task, TaskStatus.CHECKING_INPUTS)
        task = task_manager.mark_blocked(task, missing=["no video registered"])
        assert task.status == TaskStatus.BLOCKED_MISSING_INPUTS
        assert "no video registered" in task.user_feedback

    def test_invalid_transition_raises(self, task_manager: TaskManager) -> None:
        task = task_manager.create(_make_task())
        # OPEN → APPROVED is not allowed
        with pytest.raises(InvalidTaskTransition):
            task_manager.transition(task, TaskStatus.APPROVED)

    def test_revision_loop_open_to_changes_to_open(self, task_manager: TaskManager) -> None:
        task = task_manager.create(_make_task())
        task = task_manager.claim(task, "agent")
        task = task_manager.transition(task, TaskStatus.CHECKING_INPUTS)
        task = task_manager.transition(task, TaskStatus.ANALYZING)
        task = task_manager.transition(task, TaskStatus.PLANNING)
        task = task_manager.transition(task, TaskStatus.RENDERING_PREVIEW)
        task = task_manager.mark_ready_for_review(task)
        task = task_manager.request_changes(task, feedback="Faster opening")
        assert task.status == TaskStatus.CHANGES_REQUESTED
        # Revise: re-claim and walk forward again
        task = task_manager.transition(task, TaskStatus.CLAIMED_BY_AGENT)
        assert task.status == TaskStatus.CLAIMED_BY_AGENT

    def test_history_is_appended(self, task_manager: TaskManager) -> None:
        task = task_manager.create(_make_task())
        task = task_manager.claim(task, "agent")
        history = task_manager.read_history()
        actions = [h["action"] for h in history]
        assert "task_created" in actions
        assert "task_transition" in actions


class TestForbiddenBehavior:
    def test_cannot_claim_non_open_task(self, task_manager: TaskManager) -> None:
        task = task_manager.create(_make_task())
        task = task_manager.claim(task, "agent")
        with pytest.raises(InvalidTaskTransition):
            task_manager.claim(task, "agent-2")
