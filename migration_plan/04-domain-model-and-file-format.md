# Domain Model and Project Format

> **Status:** 🟡 PARTIAL — `ProjectId` value type created in `CreatorCut.Domain`. Full domain model (Sequence, Track, Clip, MediaAsset, etc.) is **Phase 1** work.

## Core rule

The timeline model must represent editor intent, not FFmpeg syntax or WPF controls.

## Main entities

```text
Project
├── ProjectMetadata
├── MediaLibrary
├── Sequences[]
├── Campaigns[]
├── Tasks[]
├── AgentAuditLog
└── ProjectSettings

Sequence
├── SequenceSettings
├── Tracks[]
├── Markers[]
└── RenderSettings

Track
├── TrackId
├── TrackKind
├── Order
├── Muted / Solo / Locked / Hidden
└── Items[]

TimelineItem
├── ClipItem
├── TextItem
├── ImageItem
├── StickerItem
├── AdjustmentLayerItem
└── TransitionItem or transition links
```

## Required identifiers

Use stable GUID-backed identifiers. Never use array indexes as durable identity.

```csharp
ProjectId
SequenceId
TrackId
TimelineItemId
MediaAssetId
EffectInstanceId
KeyframeId
TaskId
CampaignId
```

Array order may change. IDs must survive reorder, split, save/reload, and migration.

## Media model

```text
MediaAsset
├── Id
├── Kind: Video | Audio | Image | Subtitle | Font | Other
├── OriginalPath
├── RelativeProjectPath if copied into project
├── FileFingerprint
├── Duration
├── Streams
├── ProbeMetadata
├── ProxyVariants[]
└── OfflineState
```

Do not duplicate a media file for every timeline clip. Timeline clips reference `MediaAssetId` plus source ranges.

## Clip model

A clip must include:

- Stable item ID.
- Referenced media ID.
- Timeline start and duration.
- Source in and source duration.
- Playback direction.
- Speed mapping.
- Transform.
- Opacity.
- Crop.
- Blend mode.
- Audio settings.
- Effect stack.
- Keyframe tracks.

## Speed model

Do not represent all speed behavior as one `double Speed`.

```text
SpeedMap
├── ConstantSpeed segment
└── Curve points mapping timeline time to source time
```

Preset names such as `Bullet`, `Montage`, `HeroTime`, and `FlashInOut` generate editable curve points. The preset name is not the source of truth after application.

## Transform model

```text
Transform2D
├── PositionX
├── PositionY
├── ScaleX
├── ScaleY
├── RotationDegrees
├── AnchorX
├── AnchorY
├── Opacity
└── Crop
```

Each animatable property may use either a constant value or an animation curve.

## Keyframe model

```text
AnimatedProperty<T>
├── DefaultValue
└── Keyframes[]

Keyframe<T>
├── Id
├── Time
├── Value
├── Interpolation: Hold | Linear | Bezier
├── InTangent
└── OutTangent
```

The same keyframe model must serve transforms, opacity, effect parameters, audio gain, text properties where valid, and chroma settings.

## Effect model

```text
EffectInstance
├── EffectTypeId
├── Enabled
├── Parameters: dictionary of typed parameter values
├── KeyframedParameters
├── PreviewPolicy
└── Version
```

Effect type definitions belong in a registry. Unknown effect instances must survive load/save as disabled or unresolved rather than being deleted.

## Transitions

Represent transitions explicitly and validate overlap requirements.

A transition references two neighboring visual or audio items and stores:

- Transition type.
- Duration.
- Parameters.
- Alignment.
- Optional easing.

Do not fake transitions by silently changing clip opacity without a persistent transition object.

## Text model

Text items require:

- Text content.
- Font family and resolved font asset.
- Font size, weight, style.
- Fill, outline, shadow.
- Alignment and text box bounds.
- Transform.
- In/out/loop animation definitions.
- Duration and keyframes.

## Masks and chroma key

Masks are reusable objects:

```text
Mask
├── Shape: Rectangle | Ellipse | Linear | Mirror | Star | Polygon | Split
├── Path/parameters
├── Feather
├── Expansion
├── Inverted
└── Transform/keyframes
```

Chroma key parameters include:

- Key color.
- Similarity/tolerance.
- Edge softness.
- Spill suppression.
- Shadow/highlight preservation where supported.

## Audio model

Each audio-capable clip needs:

- Gain.
- Muted state.
- Pan.
- Fade in/out.
- Audio effect stack.
- Pitch/EQ preset expressed as editable parameters.
- Channel mapping.

Detached audio creates a new audio timeline item referencing the same media asset and stream, not a copied file unless export requires one.

## Project storage

Recommended project layout:

```text
MyProject.creatorcut/
├── project.json
├── timeline/
│   ├── main.sequence.json
│   └── autosave/
├── database/
│   └── project.db
├── media/                 # optional copied media
├── proxies/
├── cache/
│   ├── thumbnails/
│   ├── waveforms/
│   └── analysis/
├── renders/
├── logs/
└── migration/
    └── legacy-import-report.json
```

## JSON rules

- Every document contains `schemaVersion`.
- Use explicit enums as stable strings.
- Use ISO 8601 UTC timestamps.
- Store times using the canonical rational/tick representation.
- Preserve unknown extension fields where practical.
- Never store native handles, temporary pointers, or UI state in timeline documents.

## SQLite responsibilities

SQLite stores indexes and operational data, not the only copy of the creative timeline:

- Media search index.
- Cache entries and last-access timestamps.
- Render jobs.
- Task history.
- Audit log.
- Undo checkpoints if designed.
- Project recent-files metadata.

Portable sequence content remains versioned JSON so projects can be inspected, migrated, and recovered.
