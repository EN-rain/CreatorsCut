"""Tests validating sample timelines against the JSON schema."""

import json
from pathlib import Path

import jsonschema
import pytest


@pytest.fixture
def schema() -> dict:
    # v10 upgrade: the current sample timeline is schemaVersion 1.1
    path = Path("packages/agent-plugin/schemas/timeline.v1.1.schema.json")
    return json.loads(path.read_text())


@pytest.fixture
def validator(schema: dict) -> jsonschema.Draft7Validator:
    jsonschema.Draft7Validator.check_schema(schema)
    return jsonschema.Draft7Validator(schema)


@pytest.fixture
def sample_timeline() -> dict:
    return json.loads(Path("projects/sample/timelines/timeline_v1.json").read_text())


class TestSampleTimeline:
    def test_sample_timeline_valid(self, validator: jsonschema.Draft7Validator, sample_timeline: dict) -> None:
        validator.validate(sample_timeline)

    def test_timeline_requires_project_id(self, validator: jsonschema.Draft7Validator, sample_timeline: dict) -> None:
        broken = sample_timeline.copy()
        broken["project"] = {"id": "", "name": "x", "status": "draft"}
        with pytest.raises(jsonschema.ValidationError):
            validator.validate(broken)

    def test_timeline_requires_schema_version(self, validator: jsonschema.Draft7Validator, sample_timeline: dict) -> None:
        broken = sample_timeline.copy()
        del broken["schemaVersion"]
        with pytest.raises(jsonschema.ValidationError):
            validator.validate(broken)

    def test_timeline_invalid_track_type(self, validator: jsonschema.Draft7Validator, sample_timeline: dict) -> None:
        broken = json.loads(json.dumps(sample_timeline))
        broken["tracks"][0]["type"] = "unknown_track"
        with pytest.raises(jsonschema.ValidationError):
            validator.validate(broken)

    def test_timeline_invalid_platform(self, validator: jsonschema.Draft7Validator, sample_timeline: dict) -> None:
        broken = json.loads(json.dumps(sample_timeline))
        broken["format"]["platform"] = "twitch"
        with pytest.raises(jsonschema.ValidationError):
            validator.validate(broken)
