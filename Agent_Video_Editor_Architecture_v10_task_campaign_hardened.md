# Agent Video Editor Architecture — v10 Task + Campaign Hardened Plan

## v10 correction

This version fixes the missing workflow layer:

```text
The app is not only media upload + preview/editor.
The app also has a task/job system.
The user creates a task.
The agent sees the task through local tools.
The agent does the task only using registered local project media.
The user reviews/edits/approves the result.
```

It also adds the missing **campaign description input**:

```text
The user manually enters or pastes the campaign description/rules.
The app stores it as a Campaign Brief.
The agent reads that Campaign Brief.
The agent does not scrape Clipster or download media from Clipster/social platforms.
```

Working product name in this document: **Agent Video Editor**.

Do not call the product:

```text
ClipsterEditor
ClipsterApp
ClipsterOrchestrator
clipster-editor/
```

Clipster is the external monetization/campaign platform. Our app is a separate local editor.

---

# 1. Correct product boundary

## Clipster

Clipster is the monetization/campaign platform.

The outside-world flow is:

```text
User finds campaign on Clipster
→ user creates an edit outside Clipster
→ user manually posts to TikTok / Instagram / YouTube
→ user manually pastes the public post link into Clipster
→ Clipster handles campaign/view/engagement/payout checks
```

Our app must not pretend to be Clipster.

## Our app

Our app is a local agent-assisted editor for user-provided media.

It includes:

```text
manual campaign/task input
manual raw video import
manual music/audio import
local media registry
agent task queue
automatic analysis/draft generation
preview player
manual timeline editor
approval gate
export package
```

It does not include:

```text
video downloader
music downloader
scraper
auto-poster
Clipster submitter
Clipster account integration
social platform login automation
```

---

# 2. Correct full workflow

This is the workflow every architecture decision must follow:

```text
1. User creates a project.

2. User manually enters/pastes campaign description.
   Example: campaign name, song requirement, required comment,
   platform, duration, hashtags, notes.

3. User creates a task/job.
   Example: "Make a 15-second anime/sports edit using these raw videos
   and this uploaded song, dark villain aura style, vertical 9:16."

4. User manually uploads/imports raw video files.

5. User manually uploads/imports music/audio if needed.

6. App validates and registers local media.

7. Agent sees the open task through the local plugin/MCP/API.

8. Agent checks campaign brief + task + media registry.

9. If inputs are missing, agent marks task BLOCKED.
   Example: "Music required but no audio uploaded."

10. If inputs are complete, agent analyzes registered local media only.

11. Agent creates a draft Timeline JSON.

12. App renders preview.

13. User manually reviews and edits the timeline if needed.

14. User can request revision.
    This creates a new task for the agent.

15. User approves.

16. App exports final MP4 + caption/checklist package.

17. User manually posts to TikTok / Instagram / YouTube.

18. User manually pastes the post link into Clipster.
```

Hard rule:

```text
The agent can only work on local project files registered by the app.
No arbitrary website downloading.
No scraping.
No auto-posting.
No Clipster submission.
```

---

# 3. Manual inputs required

The plan must treat these as first-class inputs, not afterthoughts.

## 3.1 Campaign description input

The app must have a **Campaign Brief** screen.

The user can manually enter or paste:

```text
campaign name
campaign description
required song/audio
required comment
required caption
hashtags
allowed platforms
minimum duration
maximum duration
aspect ratio
deadline
brand/safety notes
watermark rules
lyrics requirement
example style notes
manual notes
```

Important:

```text
The app may parse the pasted campaign description into structured fields.
The user must be able to confirm/edit the parsed fields.
The campaign brief is manually supplied by the user.
The agent must not scrape Clipster to get campaign details.
```

## 3.2 Task/job input

The app must have a **Task Board** or **Task Queue**.

A task is not a chat prompt inside the editor. It is a structured job record that the agent can read.

Example task:

```text
Create a 15-20 second vertical anime/sports edit.
Use uploaded raw videos vid_001 and vid_002.
Use uploaded music aud_001.
Style: dark villain aura, fast cuts, glow, impact flashes.
Follow campaign requirements.
Generate 3 variants.
```

Required task fields:

```text
taskId
projectId
campaignId
title
objective
styleBrief
targetPlatform
targetDuration
requiredMediaIds
requiredAudioIds
allowedTemplates
forbiddenActions
priority
status
createdBy
assignedAgent
baseTimelineVersion
reviewRequired
```

Task statuses:

```text
open
claimed_by_agent
checking_inputs
blocked_missing_inputs
analyzing
planning
rendering_preview
ready_for_review
changes_requested
approved
export_ready
exported
cancelled
failed
```

## 3.3 Manual raw video upload/import

Allowed input methods:

```text
drag-and-drop local video files
file picker import
watched local project folder
copy into project input folder
```

Not allowed in MVP:

```text
URL-to-video import
agent downloading from YouTube/TikTok/Instagram/Facebook/X
agent downloading anime/sports footage
agent scraping websites
browser automation to fetch media
```

## 3.4 Manual music/audio upload/import

Allowed input methods:

```text
drag-and-drop local audio/music files
file picker import
watched local project folder
copy into project input folder
manual lyrics/SRT upload if available
```

Not allowed in MVP:

```text
URL-to-song import
agent downloading from YouTube Music/Spotify/Apple Music/TikTok/Instagram
agent ripping audio from social platforms
agent fetching campaign music automatically
```

## 3.5 Optional manual asset uploads

Missing from previous versions: users may need to manually provide supporting assets.

Optional asset inputs:

```text
sound effects
logos
fonts
overlays
PNG effects
background images
reference images
manual lyrics text
SRT subtitle file
```

Same rule:

```text
User provides assets manually.
Agent does not download asset packs from random websites.
```

---

# 4. Agent visibility model

The agent does not see the whole computer.

When an agent claims a task, it can see only a controlled project bundle:

```text
project manifest
campaign brief
open task details
media registry metadata
analysis outputs
current timeline version
approved templates list
allowed tools
```

The agent should not receive raw absolute paths unless required by a local renderer tool. It should normally reference IDs.

Good:

```json
{ "sourceId": "vid_001" }
```

Good:

```json
{ "audioId": "aud_001" }
```

Bad:

```json
{ "file": "C:/Users/Rei/Desktop/random/anime.mp4" }
```

Bad:

```json
{ "file": "../../Downloads/private_song.mp3" }
```

The renderer resolves IDs internally through the project registry.

---

# 5. Project data model

The plan needs more than Timeline JSON.

Minimum project records:

```text
Project
CampaignBrief
UserTask
MediaRegistry
AudioRegistry
AssetRegistry
AnalysisBundle
Timeline
TimelinePatch
RenderJob
ReviewState
ExportPackage
AuditLog
```

## 5.1 CampaignBrief example

```json
{
  "campaignId": "camp_001",
  "source": "manual_user_input",
  "campaignName": "Dark Magic Anime/Sports",
  "descriptionRaw": "Paste campaign description here...",
  "parsed": {
    "requiredSong": "Quadeca - Dark Magic",
    "requiresLyrics": true,
    "requiredComment": "song name: Quadeca - Dark Magic",
    "allowedPlatforms": ["tiktok", "instagram", "youtube_shorts"],
    "minDuration": 12,
    "maxDuration": 30,
    "hashtags": [],
    "notes": []
  },
  "userConfirmedParsedRules": false
}
```

Rule:

```text
Auto-parsed campaign fields are suggestions.
User-confirmed campaign fields are the source of truth.
```

## 5.2 UserTask example

```json
{
  "taskId": "task_001",
  "projectId": "proj_001",
  "campaignId": "camp_001",
  "title": "Generate first Dark Magic draft",
  "objective": "Create 3 vertical 15-20 second variants from uploaded raw footage and uploaded music.",
  "styleBrief": "Dark villain aura, fast cuts, glow, impact flashes, beat sync, anime/sports energy.",
  "targetPlatform": "tiktok_reels_shorts",
  "targetDuration": { "min": 15, "max": 20 },
  "requiredVideoIds": ["vid_001", "vid_002"],
  "requiredAudioIds": ["aud_001"],
  "allowedTemplates": ["dark_magic_villain_aura"],
  "forbiddenActions": [
    "download_media_from_web",
    "publish_to_social",
    "submit_to_clipster",
    "approve_export"
  ],
  "status": "open",
  "baseTimelineVersion": null,
  "reviewRequired": true
}
```

## 5.3 Media registry example

```json
{
  "sourceId": "vid_001",
  "type": "video",
  "originalName": "raw_anime_clip.mp4",
  "projectPath": "input/video/raw_001.mp4",
  "sha256": "...",
  "duration": 123.4,
  "width": 1920,
  "height": 1080,
  "fps": 30,
  "hasAudio": true,
  "status": "validated"
}
```

## 5.4 Audio registry example

```json
{
  "audioId": "aud_001",
  "type": "music",
  "originalName": "dark_magic.mp3",
  "projectPath": "input/audio/music_001.mp3",
  "sha256": "...",
  "duration": 178.2,
  "sampleRate": 44100,
  "channels": 2,
  "detectedBpm": 148,
  "status": "validated"
}
```

---

# 6. Updated architecture

```text
agent-video-editor/

  packages/
    agent-plugin/                    # public/client layer only
      schemas/
      tools/
      sdk/
      docs/

  apps/
    editor-web/                      # local app: upload + task + preview + editor
      project-dashboard/
      campaign-brief-screen/
      task-board/
      media-import-screen/
      preview-player/
      timeline-editor/
      lyrics-editor/
      effects-panel/
      variants-browser/
      review-screen/
      export-screen/
      settings-licenses/

  orchestrator/                      # local private backend
    api/
    project_manager.py
    campaign_manager.py
    task_manager.py
    agent_task_runner.py
    ingest_manager.py
    media_registry.py
    timeline_manager.py
    patch_manager.py
    render_manager.py
    review_manager.py
    export_manager.py
    audit_log.py
    permissions.py
    cache_manager.py

    analysis/
      video_inspector.py
      audio_inspector.py
      scene_detector.py
      music_analyzer.py
      beat_detector.py
      clip_scorer.py
      duplicate_detector.py
      quality_analyzer.py
      lyric_transcriber.py

    planners/
      input_checker.py
      edit_planner.py
      variant_planner.py
      template_planner.py
      compliance_planner.py

    engines/
      ffmpeg_engine.py
      mcp_video_engine.py
      remotion_engine.py
      blender_engine.py
      opencut_engine.py

    templates/
      dark_magic_villain_aura/
        template.json
        timing_rules.json
        effects.json
        assets/

  absorbed/
    README.md                        # provenance, licenses, pinned commits
    mcp-video-pinned/
    other-license-cleared-code/

  projects/
    project-id/
      campaign/
        campaign_brief.json
      tasks/
        task_001.json
        task_history.jsonl
      input/
        video/
        audio/
        assets/
        metadata/
          source_registry.json
          audio_registry.json
          asset_registry.json
      analysis/
      timelines/
      patches/
      previews/
      proxies/
      cache/
      review/
      exports/
      audit.log
      manifest.json
```

---

# 7. Agent tool design

The agent works by reading tasks and calling safe local tools.

## Allowed agent tools

```text
list_open_tasks
claim_task
read_task
read_campaign_brief
list_registered_media
list_registered_audio
list_registered_assets
check_task_inputs
analyze_video_sources
analyze_audio_sources
get_analysis_summary
plan_edit
create_variant_timelines
render_preview
propose_timeline_patch
read_timeline
read_review_feedback
mark_task_blocked
mark_task_ready_for_review
```

## Conditionally allowed tools

These can exist but must be controlled:

```text
import_media
import_audio
import_asset
```

Rule:

```text
Import tools may only import user-selected local files from the app.
Agent cannot pass arbitrary URLs or random disk paths.
```

## Forbidden tools

```text
download_video_from_url
download_song_from_url
scrape_clipster
scrape_youtube
scrape_tiktok
scrape_instagram
publish_to_tiktok
publish_to_instagram
publish_to_youtube
submit_to_clipster
approve_project
final_export_without_approval
read_browser_cookies
read_social_sessions
read_clipster_credentials
```

---

# 8. Task execution flow

The agent should follow this flow every time:

```text
1. list_open_tasks
2. claim_task
3. read_task
4. read_campaign_brief
5. list_registered_media/audio/assets
6. check_task_inputs
7. if missing input → mark_task_blocked with exact missing items
8. analyze registered local sources
9. create draft/variants
10. render previews
11. mark_task_ready_for_review
12. wait for user review/revision task
```

The agent should not improvise around missing media.

Bad behavior:

```text
Music is missing, so agent downloads it.
Raw video is missing, so agent searches online.
Campaign description is missing, so agent scrapes Clipster.
```

Correct behavior:

```text
Mark task blocked and tell the app what the user must provide manually.
```

Blocked task examples:

```json
{
  "taskId": "task_001",
  "status": "blocked_missing_inputs",
  "missing": [
    "Campaign says music is required, but no audioId is registered.",
    "Task requires at least one raw video, but no sourceId is registered."
  ]
}
```

---

# 9. Campaign description parsing and compliance

The app may parse campaign text, but the user must confirm.

## Parser output

```text
song/audio requirement
required comment
required caption
hashtags
platforms
duration range
deadline
visual/content restrictions
safe-area requirements
manual notes
```

## User confirmation

The app should show:

```text
Raw campaign description
Parsed requirements
Editable fields
Confirm campaign rules button
```

Agent behavior:

```text
If userConfirmedParsedRules == false,
agent may draft but must flag compliance as unconfirmed.
Final export should require confirmed rules if campaign mode is enabled.
```

## Campaign music modes

Some campaigns may require platform-native sound instead of embedded audio.

Add a field:

```text
musicDeliveryMode:
  embedded_audio
  platform_native_sound
  both
  unknown
```

If `embedded_audio`:

```text
User must upload/import the audio file.
App checks audio is present in final render.
```

If `platform_native_sound`:

```text
App should not try to fetch the sound.
App exports a checklist reminder.
User manually applies the platform-native sound while posting.
```

If `unknown`:

```text
App asks user to choose before final export.
```

---

# 10. Timeline JSON updates

Timeline JSON remains the edit source of truth, but it must link to CampaignBrief and UserTask.

Minimum additions:

```text
campaignId
taskId
sourceIds
audioIds
assetIds
baseTaskObjective
campaignComplianceState
```

Example:

```json
{
  "schemaVersion": "1.1",
  "project": {
    "id": "proj_001",
    "name": "Dark Magic Edit 001"
  },
  "origin": {
    "campaignId": "camp_001",
    "taskId": "task_001",
    "createdBy": "agent",
    "createdFromTaskObjective": "Create 3 vertical variants using uploaded raw footage and uploaded music."
  },
  "version": {
    "timelineVersion": 1,
    "baseVersion": null,
    "modifiedBy": "agent"
  },
  "format": {
    "width": 1080,
    "height": 1920,
    "fps": 30,
    "duration": 18,
    "platform": "tiktok_reels_shorts"
  },
  "media": {
    "sourceIds": ["vid_001", "vid_002"],
    "audioIds": ["aud_001"],
    "assetIds": []
  },
  "music": {
    "audioId": "aud_001",
    "deliveryMode": "embedded_audio",
    "bpm": 148,
    "beats": [0.0, 0.41, 0.82, 1.23],
    "dropStart": 3.28
  },
  "tracks": [
    {
      "id": "video_main",
      "type": "video",
      "items": [
        {
          "sourceId": "vid_001",
          "timelineStart": 0,
          "sourceStart": 12.4,
          "duration": 1.6,
          "transform": {
            "scale": 1.1,
            "x": 0,
            "y": 0,
            "rotation": 0
          },
          "keyframes": []
        }
      ]
    }
  ],
  "compliance": {
    "campaignRulesConfirmed": true,
    "durationPass": true,
    "audioRequirementPass": true,
    "requiredCommentGenerated": true,
    "safeAreaPass": null,
    "notes": []
  },
  "review": {
    "requiresHumanApproval": true,
    "approvedByUser": false,
    "changesRequested": []
  }
}
```

---

# 11. Manual review and revision loop

Manual editing exists, but so does task-based revision.

## User can manually edit

Required controls:

```text
trim clip
split clip
delete clip
replace clip
reorder clips
crop/position/scale
change effect timing
change effect intensity
edit subtitles/lyrics
adjust beat markers
change audio offset
choose template
undo/redo
version history
```

## User can request agent revision

A revision is a new task.

Example:

```text
Make variant 2 faster in the first 3 seconds.
Keep my manual subtitle changes.
Do not change the music offset.
```

Revision task must include:

```text
baseTimelineVersion
protectedFields
userFeedback
allowedChangeAreas
```

This prevents the agent from overwriting manual edits.

---

# 12. Version locking and patch safety

Every agent change must be a patch against a known version.

```text
agent reads timeline version 12
agent proposes patch for version 12
user already edited timeline to version 13
patch is rejected or requires merge
```

Rules:

```text
Manual user edits win by default.
Agent cannot silently overwrite manual edits.
Agent patches must declare baseVersion.
Agent patches must explain reason.
Every patch is logged.
Undo/redo required.
```

Patch example:

```json
{
  "taskId": "task_revision_002",
  "baseVersion": 12,
  "author": "agent",
  "reason": "Increase opening pace while preserving user subtitle edits.",
  "protectedPaths": ["/tracks/lyrics"],
  "operations": [
    {
      "op": "replace",
      "path": "/tracks/0/items/0/duration",
      "value": 0.8
    }
  ]
}
```

---

# 13. Preview and render strategy

Preview must not require full export every time.

Preview modes:

```text
instant browser preview
segment render preview
full draft render
final export render
```

Task integration:

```text
Agent can request preview render.
Agent can attach preview to task.
User reviews preview in app.
Final export requires approval.
```

Render jobs should have:

```text
renderJobId
taskId
timelineVersion
renderMode
status
progress
errorLog
outputPreviewPath
```

Required controls:

```text
cancel render
retry render
view error
compare previews
delete old cache
```

---

# 14. Quality and compliance gates

Before marking a task ready for review:

```text
preview rendered successfully
all referenced sourceIds/audioIds exist
campaign duration range checked
music requirement checked
aspect ratio checked
no missing subtitles if required
no black frame at start/end
no silent final if audio required
safe area warning generated
```

Before final export:

```text
campaign rules confirmed
human approval exists
timeline version is locked for export
required comment generated
caption/checklist generated
final render completed
export manifest generated
```

The app may warn about possible copyright/platform issues, but it does not verify rights.

Required user declaration:

```text
I confirm I have the right to use the uploaded video, audio, and assets.
```

---

# 15. Public package / plugin / CDN rule

The public package is only a client/tool layer.

It may contain:

```text
schemas
tool definitions
SDK client
template metadata
example agent instructions
API docs
```

It must not contain:

```text
raw videos
music/audio files
private project files
rendered drafts
exports
approval state
Clipster credentials
social platform credentials
```

Agent plugin should call local orchestrator on:

```text
127.0.0.1
local authenticated session only
```

---

# 16. No runtime git dependency / absorbed repo policy

If repos are involved:

```text
inspect repo
check main license
check dependency licenses
pin exact commit/release
fork or absorb only if allowed
preserve attribution
mark modifications
wrap behind adapter
never let upstream API become product core
```

Preferred core ownership:

```text
our schema
our task system
our campaign model
our orchestrator
our review app
our adapter interfaces
```

Third-party code should stay behind replaceable adapters.

Renderer strategy:

```text
FFmpeg direct adapter = required fallback
mcp-video pieces = optional after license/stability review
Remotion templates = optional after license review
Blender = optional for 3D shots only
OpenCut = future adapter/reference only
```

---

# 17. Updated UI requirements

Required screens:

```text
Project dashboard
Campaign brief input screen
Task board / job queue
Task detail screen
Media import screen
Audio/music import screen
Asset import screen
Analysis status screen
Clip browser
Timeline editor
Preview player
Lyrics/subtitle editor
Effects panel
Variants browser
Review/approval screen
Export package screen
Post checklist screen
Settings/licenses screen
```

Task board must show:

```text
open tasks
claimed/running tasks
blocked tasks with missing inputs
ready-for-review tasks
changes requested
approved/exported status
agent logs/progress
```

Campaign screen must show:

```text
raw campaign description
parsed campaign rules
editable structured fields
confirmed/unconfirmed state
music delivery mode
required comment/caption fields
```

Import screen must show:

```text
raw video files
audio/music files
assets
validation status
duration/resolution/format
rights confirmation checkbox
missing input warnings
```

---

# 18. Updated development order

## Phase 0 — Naming and boundaries

```text
Remove ClipsterEditor naming.
State Clipster is external monetization only.
State no downloader/no scraper/no auto-poster.
State manual video/audio/assets input only.
Create license inventory.
```

## Phase 1 — Project + Campaign + Task model

```text
Create Project schema.
Create CampaignBrief schema.
Create UserTask schema.
Create task status state machine.
Create audit log model.
```

Deliverable:

```text
user can create project → paste campaign description → create task
```

## Phase 2 — Manual media/audio/assets import

```text
Video import.
Audio/music import.
Asset import.
Validation.
Registries.
Rights checkbox.
No URL import.
```

Deliverable:

```text
project has registered sourceId/audioId/assetId
```

## Phase 3 — Local orchestrator + safe agent tools

```text
list_open_tasks
claim_task
read_campaign_brief
read_task
list_registered_media/audio/assets
check_task_inputs
mark_task_blocked
```

Deliverable:

```text
agent can see task and correctly report missing inputs
```

## Phase 4 — Timeline JSON + FFmpeg MVP renderer

```text
Timeline schema.
Simple trim/reorder/crop.
Music placement.
Preview render.
Render job tracking.
```

Deliverable:

```text
registered local media → timeline → preview.mp4
```

## Phase 5 — Preview/editor app

```text
Preview player.
Timeline UI.
Manual editing.
Undo/redo.
Version history.
Task review screen.
```

Deliverable:

```text
user can edit draft without touching JSON
```

## Phase 6 — Analysis and draft planning

```text
Scene detection.
Music beat detection.
Clip scoring.
Duplicate detection.
Quality checks.
Template planner.
```

Deliverable:

```text
agent can generate draft from uploaded local video + uploaded local music
```

## Phase 7 — Variants and revision tasks

```text
Generate variants.
User requests changes.
Revision task created.
Agent applies versioned patches.
Manual edits protected.
```

Deliverable:

```text
review → change request → revised preview
```

## Phase 8 — Campaign compliance + export package

```text
Campaign checklist.
Required comment/caption generation.
Music delivery mode checks.
Approval gate.
Final export.
Manifest.
```

Deliverable:

```text
approved project → final.mp4 + caption/checklist package
```

## Phase 9 — Optional advanced engines

```text
License-cleared mcp-video pieces.
Optional Remotion template renderer.
Optional Blender 3D shot renderer.
Future OpenCut adapter only if mature enough.
```

---

# 19. Remaining risks and mitigations

## Risk: user expects agent to find media

Mitigation:

```text
No downloader features in MVP.
Import screen says user must provide video/music/assets manually.
Agent marks task blocked when media is missing.
```

## Risk: campaign rules parsed incorrectly

Mitigation:

```text
Show raw campaign description.
Show parsed fields.
Require user confirmation before final export.
```

## Risk: agent overwrites manual edits

Mitigation:

```text
versioned patches
protected fields
manual edits win
history/undo
revision task model
```

## Risk: agent produces ugly edit

Mitigation:

```text
clip scoring
beat detection
template timing rules
variant browser
manual editor
human approval
```

## Risk: app becomes too large

Mitigation:

```text
MVP first: campaign input + task queue + manual media/audio import + simple FFmpeg render + manual editor.
Advanced engines later.
```

## Risk: license traps

Mitigation:

```text
license inventory
adapter boundary
FFmpeg fallback
no casual full repo absorption
```

---

# 20. Final v10 definition

The product is:

```text
A local agent-assisted video editor where:

User manually provides campaign description.
User manually creates a task/job.
User manually uploads raw videos.
User manually uploads music/audio/assets.
Agent sees the task through safe local tools.
Agent analyzes only registered local files.
Agent creates draft timelines/previews.
User manually edits/reviews/approves.
App exports MP4 + caption/checklist.
User manually posts.
User manually submits link to Clipster.
```

The product is not:

```text
a Clipster clone
a Clipster integration
a video downloader
a music downloader
a scraper
an auto-poster
an auto-submitter
```

Most important rule:

```text
The user supplies the inputs.
The agent does the assigned local editing task.
The user approves the output.
Nothing is published or submitted automatically.
```
