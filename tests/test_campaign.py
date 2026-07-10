"""Tests for CampaignBrief persistence and parsed-rules confirmation (v10 §5.1, §9)."""

from __future__ import annotations

import pytest

from orchestrator.campaign_manager import CampaignManager
from orchestrator.models import MusicDeliveryMode, ProjectConfig


@pytest.fixture
def campaign_manager(tmp_path) -> CampaignManager:
    config = ProjectConfig(project_id="camp-test", root=tmp_path / "proj")
    config.campaign_dir.mkdir(parents=True, exist_ok=True)
    return CampaignManager(config)


class TestParseDescription:
    def test_parses_required_song(self) -> None:
        parsed = CampaignManager.parse_description(
            "Make a vertical video. song: Quadeca - Dark Magic. 15-20 seconds."
        )
        assert parsed["requiredSong"] == "Quadeca - Dark Magic"
        assert parsed["minDuration"] == 15.0
        assert parsed["maxDuration"] == 20.0

    def test_parses_platforms_and_hashtags(self) -> None:
        parsed = CampaignManager.parse_description(
            "Post to tiktok and instagram. Use #sample #test tags."
        )
        assert "tiktok" in parsed["allowedPlatforms"]
        assert "instagram" in parsed["allowedPlatforms"]
        assert "#sample" in parsed["hashtags"]
        assert "#test" in parsed["hashtags"]

    def test_detects_lyrics_requirement(self) -> None:
        parsed = CampaignManager.parse_description("Include lyrics on screen.")
        assert parsed["requiresLyrics"] is True


class TestCampaignLifecycle:
    def test_create_or_replace_saves_unconfirmed(self, campaign_manager: CampaignManager) -> None:
        brief = campaign_manager.create_or_replace(
            campaign_id="camp_001",
            campaign_name="Dark Magic",
            description_raw="song: Dark Magic. tiktok. 15 seconds.",
        )
        assert campaign_manager.exists()
        assert brief.user_confirmed_parsed_rules is False

        loaded = campaign_manager.load()
        assert loaded.campaign_name == "Dark Magic"
        assert loaded.user_confirmed_parsed_rules is False

    def test_confirm_rules_persists(self, campaign_manager: CampaignManager) -> None:
        campaign_manager.create_or_replace(
            campaign_id="camp_001",
            campaign_name="Test",
            description_raw="song: Test.",
        )
        campaign_manager.confirm_rules()
        reloaded = campaign_manager.load()
        assert reloaded.user_confirmed_parsed_rules is True

    def test_music_delivery_mode_persists(self, campaign_manager: CampaignManager) -> None:
        campaign_manager.create_or_replace(
            campaign_id="camp_001",
            campaign_name="Native",
            description_raw="Apply platform-native sound.",
            music_delivery_mode=MusicDeliveryMode.PLATFORM_NATIVE_SOUND,
        )
        loaded = campaign_manager.load()
        assert loaded.music_delivery_mode == MusicDeliveryMode.PLATFORM_NATIVE_SOUND
