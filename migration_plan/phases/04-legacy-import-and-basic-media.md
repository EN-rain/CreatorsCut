# Phase 4 — Legacy Import and Basic Media

## Goal

Make existing CreatorCut projects usable in the desktop application without modifying or deleting the legacy source data.

## Required import coverage

- Project metadata.
- Media registries for video, audio, and assets.
- Timeline tracks, clips, transforms, markers, music settings, and render references.
- Tasks, campaigns, review state, audit history, and supported analysis metadata.
- Unknown fields recorded in a migration report rather than silently discarded.

## Media services

- Probe duration, dimensions, frame rate, codecs, channels, and sample rate.
- Generate thumbnails asynchronously.
- Generate audio waveforms asynchronously.
- Generate 360p/540p/720p proxies.
- Track original/proxy/cache relationships using stable asset IDs.
- Detect offline files and provide relink, locate-folder, skip, and remove options.

## Basic playback/export

- Play, pause, seek, scrub, loop, and frame-step.
- Preview sequential video clips and one or more audio clips.
- Respect source in/out, timeline position, transform, and volume.
- Export using original media, not proxies.
- Cancellation and progress reporting for proxy and export jobs.

## Safety rules

- Import is read-only against the legacy project.
- Write the migrated project to a new desktop project location.
- Store a source fingerprint and migration version.
- Never replace an original source file.
- Failed imports produce a report and leave no half-valid project marked complete.

## Tests

- Golden legacy fixtures.
- Missing media fixture.
- Unknown-field fixture.
- Import twice without creating duplicate IDs.
- Basic imported timeline preview/export checksum or frame comparison.

## Exit gate

- Existing basic video/music projects import successfully.
- Migration report lists mapped, skipped, transformed, and unknown data.
- Preview and export work with original and proxy media.
- Legacy data remains unchanged.
