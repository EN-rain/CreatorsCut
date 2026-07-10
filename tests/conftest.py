"""Shared test fixtures."""

import shutil
from pathlib import Path

import pytest

from orchestrator.models import ProjectConfig
from orchestrator.project_manager import ProjectManager
from orchestrator.timeline_manager import TimelineManager


@pytest.fixture
def tmp_projects(tmp_path: Path) -> Path:
    """Temporary projects root."""
    return tmp_path / "projects"


@pytest.fixture
def sample_project(tmp_projects: Path) -> ProjectConfig:
    """Create a sample project in a temporary location."""
    manager = ProjectManager(tmp_projects)
    config = manager.create_project("test-sample", "Test Sample Project")
    return config


@pytest.fixture
def sample_timeline_manager(sample_project: ProjectConfig) -> TimelineManager:
    """Return a TimelineManager for the sample project."""
    return TimelineManager(sample_project)


@pytest.fixture(scope="session", autouse=True)
def cleanup_test_projects():
    """No-op placeholder for future session cleanup."""
    yield
