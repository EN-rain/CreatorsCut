using System.IO;
using System.Text.Json;
using CreatorCut.Domain;
using CreatorCut.Domain.Serialization;

namespace CreatorCut.LegacyImport;

public sealed class LegacyImporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public ImportResult ImportProject(string projectRoot)
    {
        var result = new ImportResult(projectRoot);
        var root = new DirectoryInfo(projectRoot);
        if (!root.Exists)
        {
            result.AddError($"Project root not found: {projectRoot}");
            return result;
        }

        var projectJson = Path.Combine(projectRoot, "project.json");
        if (!File.Exists(projectJson))
        {
            result.AddError("Missing project.json");
            return result;
        }

        var meta = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(projectJson), JsonOptions);
        var projectName = meta.TryGetProperty("name", out var n) ? n.GetString() ?? "Untitled" : "Untitled";
        var projectId = meta.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";

        var project = new Project { Name = projectName };
        result.ProjectId = project.Id;
        result.ProjectName = projectName;

        // import media registries
        var mediaDir = Path.Combine(projectRoot, "input", "metadata");
        ImportMediaRegistry(Path.Combine(mediaDir, "source_registry.json"), MediaKind.Video, project, result);
        ImportMediaRegistry(Path.Combine(mediaDir, "audio_registry.json"), MediaKind.Audio, project, result);
        ImportMediaRegistry(Path.Combine(mediaDir, "asset_registry.json"), MediaKind.Image, project, result);

        // import the latest timeline
        var timelinesDir = Path.Combine(projectRoot, "timelines");
        if (Directory.Exists(timelinesDir))
        {
            var timelineFiles = Directory.GetFiles(timelinesDir, "timeline_v*.json");
            var latest = timelineFiles.OrderByDescending(f => ExtractVersion(f)).FirstOrDefault();
            if (latest != null)
                ImportTimeline(latest, project, projectRoot, result);
        }

        result.Project = project;
        return result;
    }

    private static void ImportMediaRegistry(string path, MediaKind kind, Project project, ImportResult result)
    {
        if (!File.Exists(path)) return;

        try
        {
            var json = File.ReadAllText(path);
            var entries = JsonSerializer.Deserialize<List<JsonElement>>(json, JsonOptions);
            if (entries == null) return;

            foreach (var entry in entries)
            {
                var sourceId = entry.TryGetProperty("sourceId", out var sid)
                    ? sid.GetString() ?? ""
                    : entry.TryGetProperty("audioId", out var aid) ? aid.GetString() ?? ""
                    : entry.TryGetProperty("assetId", out var asid) ? asid.GetString() ?? ""
                    : Guid.NewGuid().ToString();

                var originalName = GetString(entry, "originalName");
                var projectPath = GetString(entry, "projectPath");
                var duration = GetDouble(entry, "duration");
                var width = GetInt(entry, "width");
                var height = GetInt(entry, "height");
                var fps = GetDouble(entry, "fps");

                var asset = new MediaAsset
                {
                    Kind = kind,
                    OriginalPath = Path.Combine(result.SourceRoot, projectPath),
                    RelativeProjectPath = projectPath,
                    Duration = MediaTime.FromSeconds(duration),
                };

                project.MediaLibrary.Add(asset);
                result.MappedAssets++;
            }

            result.AddInfo($"Imported {entries.Count} {kind} entries from {Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            result.AddWarning($"Failed to import {path}: {ex.Message}");
        }
    }

    private static void ImportTimeline(string timelinePath, Project project, string projectRoot, ImportResult result)
    {
        try
        {
            var json = File.ReadAllText(timelinePath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var seq = project.MainSequence ?? new Sequence { Name = "Main" };
            if (project.MainSequence == null) project.Sequences.Add(seq);

            if (root.TryGetProperty("format", out var fmt))
            {
                seq.Width = GetInt(fmt, "width", 1080);
                seq.Height = GetInt(fmt, "height", 1920);
                seq.Fps = GetDouble(fmt, "fps", 30.0);
            }

            if (root.TryGetProperty("tracks", out var tracksEl))
            {
                foreach (var trackEl in tracksEl.EnumerateArray())
                {
                    var trackType = GetString(trackEl, "type", "video");
                    var track = new Track
                    {
                        Kind = MapTrackKind(trackType),
                        Name = GetString(trackEl, "id", trackType),
                    };

                    if (trackEl.TryGetProperty("items", out var itemsEl))
                    {
                        foreach (var itemEl in itemsEl.EnumerateArray())
                        {
                            var clipId = GetString(itemEl, "clipId", "");
                            var asset = FindAssetByPath(project, clipId);

                            var item = new TimelineItem
                            {
                                Kind = ItemKind.Clip,
                                MediaAssetId = asset?.Id,
                                TimelineStart = MediaTime.FromSeconds(GetDouble(itemEl, "timelineStart")),
                                Duration = MediaTime.FromSeconds(GetDouble(itemEl, "duration")),
                                SourceStart = MediaTime.FromSeconds(GetDouble(itemEl, "sourceStart", 0)),
                                SourceDuration = MediaTime.FromSeconds(GetDouble(itemEl, "duration")),
                            };

                            track.Items.Add(item);
                            result.MappedClips++;
                        }
                    }

                    seq.Tracks.Add(track);
                }
            }

            if (root.TryGetProperty("markers", out var markersEl))
            {
                foreach (var markerEl in markersEl.EnumerateArray())
                {
                    seq.Markers.Add(new Domain.Marker
                    {
                        Label = GetString(markerEl, "label", ""),
                        Time = MediaTime.FromSeconds(GetDouble(markerEl, "time")),
                    });
                    result.MappedMarkers++;
                }
            }

            result.AddInfo($"Imported timeline: {Path.GetFileName(timelinePath)}");
        }
        catch (Exception ex)
        {
            result.AddError($"Failed to import timeline {timelinePath}: {ex.Message}");
        }
    }

    private static MediaAsset? FindAssetByPath(Project project, string clipId)
    {
        foreach (var asset in project.MediaLibrary)
        {
            if (asset.OriginalPath.Contains(clipId, StringComparison.OrdinalIgnoreCase) ||
                asset.RelativeProjectPath.Contains(clipId, StringComparison.OrdinalIgnoreCase))
                return asset;
        }
        return null;
    }

    private static TrackKind MapTrackKind(string type) => type.ToLowerInvariant() switch
    {
        "video" => TrackKind.Video,
        "audio" or "music" => TrackKind.Audio,
        "text" => TrackKind.Text,
        "overlay" => TrackKind.Overlay,
        _ => TrackKind.Video,
    };

    private static int ExtractVersion(string filename)
    {
        var parts = Path.GetFileNameWithoutExtension(filename).Split('_');
        return parts.Length >= 2 && int.TryParse(parts[^1], out var v) ? v : 0;
    }

    private static string GetString(JsonElement el, string prop, string def = "") =>
        el.TryGetProperty(prop, out var v) ? v.GetString() ?? def : def;

    private static double GetDouble(JsonElement el, string prop, double def = 0) =>
        el.TryGetProperty(prop, out var v) ? v.GetDouble() : def;

    private static int GetInt(JsonElement el, string prop, int def = 0) =>
        el.TryGetProperty(prop, out var v) ? v.GetInt32() : def;
}

public sealed class ImportResult
{
    public string SourceRoot { get; }
    public ProjectId? ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public Project? Project { get; set; }
    public int MappedAssets { get; set; }
    public int MappedClips { get; set; }
    public int MappedMarkers { get; set; }
    public List<string> Warnings { get; } = [];
    public List<string> Errors { get; } = [];
    public List<string> Info { get; } = [];
    public bool HasErrors => Errors.Count > 0;

    public ImportResult(string sourceRoot) => SourceRoot = sourceRoot;

    public void AddWarning(string msg) => Warnings.Add(msg);
    public void AddError(string msg) => Errors.Add(msg);
    public void AddInfo(string msg) => Info.Add(msg);

    public string GenerateReport()
    {
        var lines = new List<string>
        {
            $"Import Report for: {SourceRoot}",
            $"Project: {ProjectName} ({ProjectId})",
            $"Assets mapped: {MappedAssets}",
            $"Clips mapped: {MappedClips}",
            $"Markers mapped: {MappedMarkers}",
        };

        if (Errors.Count > 0)
        {
            lines.Add("Errors:");
            lines.AddRange(Errors.Select(e => $"  ERROR: {e}"));
        }

        if (Warnings.Count > 0)
        {
            lines.Add("Warnings:");
            lines.AddRange(Warnings.Select(w => $"  WARN: {w}"));
        }

        return string.Join(Environment.NewLine, lines);
    }
}
