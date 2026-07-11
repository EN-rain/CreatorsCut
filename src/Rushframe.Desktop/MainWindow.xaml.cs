using System.Windows;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Rushframe.Desktop.Commands;
using Rushframe.Desktop.Timeline;
using Rushframe.Domain;
using Rushframe.Domain.Editing;
using Rushframe.Desktop.Panels;
using Rushframe.Desktop.Workspace;
using Rushframe.Infrastructure;
using Rushframe.Application;
using Rushframe.Media.Native;
using Microsoft.Win32;
using System.Globalization;
using System.Windows.Threading;

namespace Rushframe.Desktop;

public partial class MainWindow : Window
{
    private readonly WorkspaceLayoutService _workspaceService;
    private readonly string _appData;
    private WorkspaceLayout _layout;

    private Project _project = new();
    private readonly UndoRedoStack _undoRedo = new();
    private readonly AutosaveService _autosave;
    private readonly ProjectRepository _projectRepo = new();
    private readonly MigrationService _migrationService;
    private readonly FfmpegMediaService _mediaService = new();
    private readonly StabilizationAnalysisService _stabilizationService;
    private readonly MediaIntelligenceImportService _mediaIntelligenceImportService = new();
    private readonly EffectRegistry _effectRegistry = new();
    private readonly RippleState _rippleState = new();
    private readonly DispatcherTimer _previewTimer = new() { Interval = TimeSpan.FromMilliseconds(200) };
    private TimelineControl? _timeline;
    private CopyClipCommand? _clipboard;
    private TimelineItem? _selectedInspectorItem;
    private MediaKind? _mediaKindFilter;
    private string _mediaSearchText = string.Empty;
    private string? _currentProjectPath;
    private bool _isPreviewSeeking;
    public MainWindow()
    {
        _appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Rushframe");
        _workspaceService = new WorkspaceLayoutService(_appData);
        _layout = _workspaceService.Load();
        _autosave = new AutosaveService(Path.Combine(_appData, "autosave"));
        _migrationService = new MigrationService(Path.Combine(_appData, "backups"));
        _stabilizationService = new StabilizationAnalysisService(Path.Combine(_appData, "analysis", "stabilization"));

        InitializeComponent();

        MinimizeWindowButton.Click += (_, _) => WindowState = WindowState.Minimized;
        MaximizeWindowButton.Click += (_, _) =>
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        };
        CloseWindowButton.Click += (_, _) => Close();

        RippleToggle.Click += (_, _) => _rippleState.Enabled = RippleToggle.IsChecked ?? false;
        SnapToggle.Click += (_, _) =>
        {
            if (_timeline != null) _timeline.SnapEnabled = SnapToggle.IsChecked ?? true;
        };
        ZoomSlider.ValueChanged += (_, _) => _timeline?.SetZoomScale(ZoomSlider.Value);

        MediaList.MouseDoubleClick += (_, _) => AddSelectedMediaToTimeline();
        MediaList.SelectionChanged += (_, _) =>
        {
            var hasSelection = MediaList.SelectedItem != null;
            AddToTimelineButton.IsEnabled = hasSelection;
            PreviewSelectedMediaButton.IsEnabled = hasSelection;
            PreviewSelectedMedia();
        };
        AddToTimelineButton.Click += (_, _) => AddSelectedMediaToTimeline();
        PreviewSelectedMediaButton.Click += (_, _) => PreviewSelectedMedia();
        RunMediaIntelligenceButton.Click += async (_, _) => await RunMediaIntelligenceAsync();
        ApplyMediaIntelligenceButton.Click += async (_, _) => await ApplyCurrentMediaIntelligenceToTimelineAsync();
        OpenMediaAnalysisButton.Click += (_, _) => OpenSelectedMediaAnalysisOutput();
        MediaSearchBox.TextChanged += (_, _) =>
        {
            _mediaSearchText = MediaSearchBox.Text.Trim();
            MediaSearchHint.Visibility = string.IsNullOrEmpty(MediaSearchBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
            RefreshMediaList();
        };
        ClearMediaSearchButton.Click += (_, _) => MediaSearchBox.Clear();
        AllMediaFilterButton.Click += (_, _) => SetMediaFilter(null);
        VideoFilterButton.Click += (_, _) => SetMediaFilter(MediaKind.Video);
        ImageFilterButton.Click += (_, _) => SetMediaFilter(MediaKind.Image);
        AudioFilterButton.Click += (_, _) => SetMediaFilter(MediaKind.Audio);

        PreviewPlayButton.Click += (_, _) => PreviewPlayer.Play();
        PreviewPauseButton.Click += (_, _) => PreviewPlayer.Pause();
        PreviewStopButton.Click += (_, _) =>
        {
            PreviewPlayer.Stop();
            PreviewPlayer.Position = TimeSpan.Zero;
            UpdatePreviewProgress();
        };
        PreviewPlayer.MediaOpened += (_, _) => OnPreviewMediaOpened();
        PreviewPlayer.MediaEnded += (_, _) =>
        {
            PreviewPlayer.Stop();
            PreviewPlayer.Position = TimeSpan.Zero;
            UpdatePreviewProgress();
        };
        PreviewPlayer.MediaFailed += (_, args) =>
        {
            PreviewSourceNameText.Text = $"Preview failed: {args.ErrorException?.Message ?? "unknown error"}";
        };
        PreviewSeekSlider.PreviewMouseLeftButtonDown += (_, _) => _isPreviewSeeking = true;
        PreviewSeekSlider.PreviewMouseLeftButtonUp += (_, _) =>
        {
            _isPreviewSeeking = false;
            PreviewPlayer.Position = TimeSpan.FromSeconds(PreviewSeekSlider.Value);
            UpdatePreviewProgress();
        };
        _previewTimer.Tick += (_, _) => UpdatePreviewProgress();
        _previewTimer.Start();

        ApplyInspectorButton.Click += (_, _) => ApplyInspectorSettings();
        AddEffectButton.Click += (_, _) => AddSelectedEffect();
        AnalyzeStabilizationButton.Click += async (_, _) => await AnalyzeSelectedStabilizationAsync();
        UpdateMediaFilterButtons();

        BuildPanelsMenu();
        ApplyLayout();
        InitTimeline();

        _autosave.StartBackground(_project, TimeSpan.FromSeconds(30));

        CommandBindings.Add(new CommandBinding(EditorCommands.OpenProject, OpenProject_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.SaveProject, SaveProject_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.ImportMedia, ImportMedia_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.RelinkMedia, RelinkMedia_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.GenerateMediaCache, GenerateMediaCache_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.ExtractAudio, ExtractAudio_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.ImportMediaIntelligence, ImportMediaIntelligence_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.Render, Render_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.AddText, AddText_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.AddMarker, AddMarker_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.Undo, Undo_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.Redo, Redo_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.Cut, Cut_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.Copy, Copy_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.Paste, Paste_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.SplitClip, SplitClip_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.DeleteClip, DeleteClip_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.Duplicate, Duplicate_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.RippleDelete, RippleDelete_Executed));

        Closed += (_, _) =>
        {
            _previewTimer.Stop();
            PreviewPlayer.Stop();
            _autosave.StopBackground();
            SaveLayout();
        };
    }

    private void InitTimeline()
    {
        _timeline = new TimelineControl();
        _timeline.Sequence = _project.MainSequence;
        _timeline.ContextMenu = (ContextMenu)FindResource("TimelineClipContextMenu");
        _timeline.SnapEnabled = SnapToggle.IsChecked ?? true;

        _timeline.ClipSelected += (_, item) =>
        {
            _contextTrackIndex = item != null ? _timeline.SelectedTrackIndex : -1;
            if (item == null) _contextTrackIndex = -1;
            _selectedInspectorItem = item;
            UpdateInspector(item);
            if (item != null) PreviewTimelineItem(item);
        };
        _timeline.DeleteSelectedClipRequested += (_, _) => DeleteSelectedClip();
        _timeline.ClipMoveRequested += (_, args) => MoveClip(args);
        _timeline.ClipTrimRequested += (_, args) => TrimClip(args);

        TimelineHost.Content = _timeline;
        RefreshMediaList();
        EffectCombo.ItemsSource = _effectRegistry.GetAll();
        UpdateInspector(null);
    }

    private int _contextTrackIndex = -1;

    private void BuildPanelsMenu()
    {
        foreach (var panel in PanelRegistry.All)
        {
            var item = new MenuItem
            {
                Header = panel.Title,
                IsCheckable = true,
                IsChecked = panel.CanClose ? _layout.IsPanelOpen(panel.Id) : true,
                IsEnabled = panel.CanClose,
                ToolTip = panel.CanClose ? null : "The timeline is always available.",
                Tag = panel.Id,
            };
            if (panel.CanClose) item.Click += PanelMenuItem_Click;
            PanelsMenu.Items.Add(item);
        }
    }

    private void PanelMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem item || item.Tag is not PanelId panelId) return;
        if (PanelRegistry.Find(panelId)?.CanClose == false) return;
        var current = _layout.IsPanelOpen(panelId);
        _layout = _layout.WithPanelToggled(panelId, !current);
        item.IsChecked = !current;
        ApplyLayout();
    }

    private void TogglePanel_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is not string panelKey) return;
        var panelId = new PanelId(panelKey);
        if (PanelRegistry.Find(panelId)?.CanClose == false) return;
        var current = _layout.IsPanelOpen(panelId);
        _layout = _layout.WithPanelToggled(panelId, !current);
        foreach (MenuItem item in PanelsMenu.Items)
            if (item.Tag is PanelId id && id == panelId) item.IsChecked = !current;
        ApplyLayout();
    }

    private void ApplyLayout()
    {
        var mediaOpen = _layout.IsPanelOpen(PanelId.Media);
        var previewOpen = _layout.IsPanelOpen(PanelId.Preview);
        var inspectorOpen = _layout.IsPanelOpen(PanelId.Inspector);
        var tasksOpen = _layout.IsPanelOpen(PanelId.Tasks);
        var renderQueueOpen = _layout.IsPanelOpen(PanelId.RenderQueue);
        var lowerRightOpen = tasksOpen || renderQueueOpen;
        var rightColumnOpen = inspectorOpen || lowerRightOpen;
        var anyTopPanelOpen = mediaOpen || previewOpen || inspectorOpen;

        MediaBorder.Visibility = Vis(mediaOpen);
        MediaSplitter.Visibility = Vis(mediaOpen);
        MediaColumn.Width = mediaOpen ? new GridLength(285) : new GridLength(0);
        MediaSplitterColumn.Width = mediaOpen ? new GridLength(5) : new GridLength(0);

        PreviewBorder.Visibility = Vis(previewOpen);
        Grid.SetColumnSpan(PreviewBorder, inspectorOpen ? 1 : 3);

        InspectorBorder.Visibility = Vis(inspectorOpen);
        InspectorSplitter.Visibility = Vis(inspectorOpen);
        InspectorColumn.Width = rightColumnOpen ? new GridLength(330) : new GridLength(0);
        InspectorSplitterColumn.Width = rightColumnOpen ? new GridLength(5) : new GridLength(0);

        TimelineBorder.Visibility = Visibility.Visible;
        TasksBorder.Visibility = Vis(lowerRightOpen);
        TasksTab.Visibility = Vis(tasksOpen);
        RenderQueueTab.Visibility = Vis(renderQueueOpen);
        TimelineTasksSplitter.Visibility = Vis(lowerRightOpen);
        Grid.SetColumnSpan(TimelineBorder, lowerRightOpen ? 3 : 5);

        PreviewTimelineSplitter.Visibility = Vis(anyTopPanelOpen);
    }

    private static Visibility Vis(bool open) => open ? Visibility.Visible : Visibility.Collapsed;
    private void SaveLayout() => _workspaceService.Save(_layout);

    private void Execute(IEditCommand cmd)
    {
        if (_project.MainSequence == null) return;
        _undoRedo.Execute(_project.MainSequence, cmd);
        _timeline?.InvalidateVisual();
        UpdateInspector(_selectedInspectorItem);
    }

    private void OpenProject_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Rushframe Project (*.rushframe)|*.rushframe|Legacy Project (*/project.json)|project.json" };
        if (dialog.ShowDialog() != true) return;

        if (dialog.FileName.EndsWith("project.json"))
        {
            var legacyDir = Path.GetDirectoryName(dialog.FileName)!;
            var result = _migrationService.MigrateLegacyProject(legacyDir);
            if (result.Success && result.Project != null)
            {
                _project = result.Project;
                _currentProjectPath = null;
                _autosave.StartBackground(_project, TimeSpan.FromSeconds(30));
                _undoRedo.Clear();
                _timeline!.Sequence = _project.MainSequence;
                RefreshMediaList();
            }
            else
            {
                MessageBox.Show($"Migration failed:\n{string.Join("\n", result.Errors)}", "Migration Error");
            }
        }
        else
        {
            var loaded = _projectRepo.Load(dialog.FileName);
            if (loaded != null)
            {
                _project = loaded;
                _currentProjectPath = dialog.FileName;
                _autosave.StartBackground(_project, TimeSpan.FromSeconds(30));
                _undoRedo.Clear();
                _timeline!.Sequence = _project.MainSequence;
                RefreshMediaList();
            }
        }
    }

    private void SaveProject_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "Rushframe Project (*.rushframe)|*.rushframe", FileName = _project.Name + ".rushframe" };
        if (dialog.ShowDialog() == true)
        {
            _projectRepo.Save(_project, dialog.FileName);
            _currentProjectPath = dialog.FileName;
            ProjectNameText.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
        }
    }

    private async void ImportMedia_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Media Files (*.mp4;*.mov;*.avi;*.wav;*.mp3;*.png;*.jpg;*.jpeg)|*.mp4;*.mov;*.avi;*.wav;*.mp3;*.png;*.jpg;*.jpeg",
            Multiselect = true,
        };
        if (dialog.ShowDialog() == true)
        {
            foreach (var file in dialog.FileNames)
            {
                var duration = MediaTime.Zero;
                try
                {
                    var probe = await _mediaService.ProbeAsync(file);
                    duration = MediaTime.FromSeconds(probe.Duration.TotalSeconds);
                }
                catch
                {
                    // Keep import usable without FFmpeg; probing can be retried later.
                }

                _project.MediaLibrary.Add(new MediaAsset
                {
                    Kind = GetMediaKind(file),
                    OriginalPath = file,
                    RelativeProjectPath = file,
                    Duration = duration,
                });
            }
            RefreshMediaList();
        }
    }

    private void RelinkMedia_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (MediaList.SelectedItem is not MediaListItem selected) return;
        var dialog = new OpenFileDialog { Filter = "Media Files|*.mp4;*.mov;*.avi;*.mkv;*.webm;*.wav;*.mp3;*.aac;*.m4a;*.flac;*.png;*.jpg;*.jpeg;*.webp;*.bmp|All Files|*.*" };
        if (dialog.ShowDialog() != true) return;

        var replacement = new MediaAsset
        {
            Id = selected.Asset.Id,
            Kind = GetMediaKind(dialog.FileName),
            OriginalPath = dialog.FileName,
            RelativeProjectPath = dialog.FileName,
            Duration = selected.Asset.Duration,
            IsOffline = false,
        };
        var index = _project.MediaLibrary.FindIndex(a => a.Id == selected.Asset.Id);
        if (index >= 0) _project.MediaLibrary[index] = replacement;
        RefreshMediaList();
        RenderQueueList.Items.Add($"Relinked: {Path.GetFileName(dialog.FileName)}");
    }

    private async void GenerateMediaCache_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (MediaList.SelectedItem is not MediaListItem selected) return;
        var asset = selected.Asset;
        if (!File.Exists(asset.OriginalPath))
        {
            RenderQueueList.Items.Add($"Cache skipped, offline: {Path.GetFileName(asset.OriginalPath)}");
            return;
        }

        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Rushframe", "Cache");
        Directory.CreateDirectory(appData);
        try
        {
            if (asset.Kind is MediaKind.Video or MediaKind.Image)
                await _mediaService.GenerateThumbnailAsync(new(asset.OriginalPath, Path.Combine(appData, "thumbnails", $"{asset.Id}.jpg"), TimeSpan.FromSeconds(1)));
            if (asset.Kind is MediaKind.Video)
                await _mediaService.GenerateProxyAsync(new(asset.OriginalPath, Path.Combine(appData, "proxy", $"{asset.Id}.mp4"), 540));
            if (asset.Kind is MediaKind.Video or MediaKind.Audio)
                await _mediaService.GenerateWaveformAsync(new(asset.OriginalPath, Path.Combine(appData, "waveforms", $"{asset.Id}.png")));

            RenderQueueList.Items.Add($"Cache generated: {Path.GetFileName(asset.OriginalPath)}");
            RefreshMediaList();
        }
        catch (Exception ex)
        {
            RenderQueueList.Items.Add($"Cache failed: {ex.Message}");
        }
    }

    private async Task RunMediaIntelligenceAsync()
    {
        if (MediaList.SelectedItem is not MediaListItem selected)
        {
            AddMediaIntelligenceMessage("Select a media file first.");
            return;
        }

        var asset = selected.Asset;
        if (!File.Exists(asset.OriginalPath))
        {
            AddMediaIntelligenceMessage($"Analysis skipped, offline: {Path.GetFileName(asset.OriginalPath)}");
            return;
        }

        var outputDir = GetMediaAnalysisOutputDirectory(asset);
        Directory.CreateDirectory(outputDir);

        RunMediaIntelligenceButton.IsEnabled = false;
        MediaIntelligenceTab.IsSelected = true;
        AddMediaIntelligenceMessage($"Analyzing: {Path.GetFileName(asset.OriginalPath)}");
        AddMediaIntelligenceMessage($"Output: {outputDir}");

        try
        {
            var scriptPath = WriteMediaIntelligenceRunner();
            var repoRoot = FindRepoRoot();
            var ffmpegPath = ResolveFfmpegPath(repoRoot);
            var model = GetSelectedWhisperModel();
            var args = new List<string>
            {
                scriptPath,
                asset.OriginalPath,
                outputDir,
                ffmpegPath,
                BoolArg(AnalyzeScenesToggle.IsChecked),
                BoolArg(AnalyzeTranscriptToggle.IsChecked),
                BoolArg(AnalyzeMusicToggle.IsChecked),
                BoolArg(AnalyzeGeminiToggle.IsChecked),
                model,
            };

            var result = await RunPythonAsync(args, repoRoot);
            if (!string.IsNullOrWhiteSpace(result.StandardOutput))
                AddMediaIntelligenceMessage(result.StandardOutput.Trim());
            if (!string.IsNullOrWhiteSpace(result.StandardError))
                AddMediaIntelligenceMessage(result.StandardError.Trim());

            if (result.ExitCode != 0)
            {
                AddMediaIntelligenceMessage($"Analysis failed with exit code {result.ExitCode}.");
                return;
            }

            var analysisPath = Path.Combine(outputDir, "media-analysis.json");
            SummarizeMediaAnalysis(analysisPath);
            await ApplyMediaIntelligenceToTimelineAsync(analysisPath, asset, autoApply: true);
            RenderQueueList.Items.Add($"Media intelligence complete: {Path.GetFileName(asset.OriginalPath)}");
            RenderQueueEmptyText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            AddMediaIntelligenceMessage($"Analysis failed: {ex.Message}");
        }
        finally
        {
            RunMediaIntelligenceButton.IsEnabled = true;
        }
    }

    private async Task ApplyCurrentMediaIntelligenceToTimelineAsync()
    {
        var asset = ResolveMediaIntelligenceAsset();
        if (asset == null)
        {
            AddMediaIntelligenceMessage("Select a media item or a timeline clip first.");
            return;
        }

        var analysisPath = Path.Combine(GetMediaAnalysisOutputDirectory(asset), "media-analysis.json");
        await ApplyMediaIntelligenceToTimelineAsync(analysisPath, asset, autoApply: false);
    }

    private async void ImportMediaIntelligence_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var asset = ResolveMediaIntelligenceAsset();
        if (asset == null)
        {
            MessageBox.Show(this, "Select a media item or timeline clip first.", "Media Intelligence", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Import Media Intelligence Analysis",
            Filter = "Media analysis JSON|media-analysis.json;*.json|JSON files|*.json|All files|*.*",
        };
        if (dialog.ShowDialog(this) != true) return;

        await ApplyMediaIntelligenceToTimelineAsync(dialog.FileName, asset, autoApply: false);
    }

    private async Task ApplyMediaIntelligenceToTimelineAsync(string analysisPath, MediaAsset asset, bool autoApply)
    {
        if (!File.Exists(analysisPath))
        {
            if (!autoApply)
                AddMediaIntelligenceMessage("Run analysis first or choose an existing media-analysis.json file.");
            return;
        }

        var target = ResolveMediaIntelligenceTarget(asset.Id);
        if (target == null)
        {
            AddMediaIntelligenceMessage($"Analysis saved, but '{Path.GetFileName(asset.OriginalPath)}' is not on the timeline yet.");
            return;
        }

        try
        {
            var analysis = await _mediaIntelligenceImportService.ImportAsync(analysisPath, asset);
            MediaIntelligenceImportService.StoreInProject(_project, analysis);
            var command = new ApplyMediaIntelligenceCommand
            {
                TargetItemId = target.Id,
                Analysis = analysis,
                AddSceneMarkers = true,
                AddCaptionClips = true,
            };
            Execute(command);

            if (!string.IsNullOrWhiteSpace(_currentProjectPath))
                _projectRepo.Save(_project, _currentProjectPath);
            else
                _autosave.Save(_project);

            var message = $"Timeline updated: {command.CreatedMarkerCount} scene markers and {command.CreatedCaptionCount} caption clips.";
            AddMediaIntelligenceMessage(message);
            StatusText.Text = message;
            _timeline?.ScrollToTime(target.TimelineStart);
        }
        catch (Exception ex)
        {
            AddMediaIntelligenceMessage($"Could not apply analysis: {ex.Message}");
        }
    }

    private MediaAsset? ResolveMediaIntelligenceAsset()
    {
        if (_timeline?.SelectedItem?.MediaAssetId is MediaAssetId timelineAssetId)
            return _project.MediaLibrary.FirstOrDefault(asset => asset.Id == timelineAssetId);
        return (MediaList.SelectedItem as MediaListItem)?.Asset;
    }

    private TimelineItem? ResolveMediaIntelligenceTarget(MediaAssetId assetId)
    {
        if (_timeline?.SelectedItem is { } selected && selected.MediaAssetId == assetId)
            return selected;

        return _project.MainSequence?.Tracks
            .SelectMany(track => track.Items)
            .FirstOrDefault(item => item.MediaAssetId == assetId);
    }

    private void OpenSelectedMediaAnalysisOutput()
    {
        if (MediaList.SelectedItem is not MediaListItem selected)
        {
            AddMediaIntelligenceMessage("Select a media file first.");
            return;
        }

        var outputDir = GetMediaAnalysisOutputDirectory(selected.Asset);
        Directory.CreateDirectory(outputDir);
        Process.Start(new ProcessStartInfo
        {
            FileName = outputDir,
            UseShellExecute = true,
        });
    }

    private void AddMediaIntelligenceMessage(string message)
    {
        MediaIntelligenceList.Items.Add(message);
        MediaIntelligenceEmptyText.Visibility = Visibility.Collapsed;
        MediaIntelligenceList.ScrollIntoView(message);
    }

    private string GetMediaAnalysisOutputDirectory(MediaAsset asset) =>
        Path.Combine(_appData, "analysis", "media-intelligence", asset.Id.ToString());

    private string WriteMediaIntelligenceRunner()
    {
        var runnerDir = Path.Combine(_appData, "analysis");
        Directory.CreateDirectory(runnerDir);
        var runnerPath = Path.Combine(runnerDir, "run_media_intelligence.py");
        File.WriteAllText(
            runnerPath,
            """
            import sys
            from pathlib import Path

            from rushframe_intelligence.pipeline import MediaIntelligencePipeline

            source = Path(sys.argv[1])
            output = Path(sys.argv[2])
            ffmpeg = Path(sys.argv[3])
            detect_scenes = sys.argv[4] == "1"
            transcribe = sys.argv[5] == "1"
            analyze_audio = sys.argv[6] == "1"
            understand_frames = sys.argv[7] == "1"
            whisper_model = sys.argv[8]

            pipeline = MediaIntelligencePipeline(ffmpeg)
            pipeline.run(
                source,
                output,
                detect_visual_scenes=detect_scenes,
                transcribe_speech=transcribe,
                analyze_audio=analyze_audio,
                understand_frames=understand_frames,
                whisper_model=whisper_model,
            )
            print(output / "media-analysis.json")
            """,
            Encoding.UTF8);
        return runnerPath;
    }

    private static async Task<ProcessResult> RunPythonAsync(IReadOnlyList<string> args, string workingDirectory)
    {
        foreach (var launcher in new[] { "py", "python" })
        {
            var psi = new ProcessStartInfo
            {
                FileName = launcher,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            if (launcher == "py") psi.ArgumentList.Add("-3");
            foreach (var arg in args) psi.ArgumentList.Add(arg);

            try
            {
                using var process = Process.Start(psi) ?? throw new InvalidOperationException("Python did not start.");
                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                return new ProcessResult(process.ExitCode, await stdoutTask, await stderrTask);
            }
            catch (Win32Exception) when (launcher == "py")
            {
                continue;
            }
        }

        throw new InvalidOperationException("Python was not found. Install Python or add it to PATH.");
    }

    private void SummarizeMediaAnalysis(string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            AddMediaIntelligenceMessage("Analysis finished, but media-analysis.json was not created.");
            return;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(jsonPath));
        var root = document.RootElement;
        var scenes = CountArray(root, "scenes");
        var transcript = CountArray(root, "transcript");
        var warnings = CountArray(root, "warnings");
        AddMediaIntelligenceMessage($"Complete: {scenes} scenes, {transcript} transcript segments, {warnings} warnings.");
        AddMediaIntelligenceMessage(jsonPath);
    }

    private string GetSelectedWhisperModel() =>
        WhisperModelCombo.SelectedItem is ComboBoxItem item && item.Content is string value
            ? value
            : "base";

    private static string BoolArg(bool? value) => value == true ? "1" : "0";

    private static int CountArray(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.GetArrayLength()
            : 0;

    private static string ResolveFfmpegPath(string repoRoot)
    {
        var local = Path.Combine(repoRoot, ".tools", "bin", "ffmpeg.exe");
        return File.Exists(local) ? local : "ffmpeg";
    }

    private static string FindRepoRoot()
    {
        foreach (var start in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(start);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "rushframe_intelligence", "pipeline.py")))
                    return directory.FullName;
                directory = directory.Parent;
            }
        }

        return Environment.CurrentDirectory;
    }

    private async void ExtractAudio_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (_selectedInspectorItem?.MediaAssetId == null) return;
        var source = _project.MediaLibrary.FirstOrDefault(a => a.Id == _selectedInspectorItem.MediaAssetId.Value);
        if (source == null || !File.Exists(source.OriginalPath)) return;

        var output = Path.Combine(Path.GetDirectoryName(source.OriginalPath)!, $"{Path.GetFileNameWithoutExtension(source.OriginalPath)}_audio.wav");
        try
        {
            await _mediaService.ExtractAudioAsync(source.OriginalPath, output);
            var asset = new MediaAsset
            {
                Kind = MediaKind.Audio,
                OriginalPath = output,
                RelativeProjectPath = output,
                Duration = source.Duration,
            };
            _project.MediaLibrary.Add(asset);
            RefreshMediaList();
            RenderQueueList.Items.Add($"Extracted audio: {Path.GetFileName(output)}");
        }
        catch (Exception ex)
        {
            RenderQueueList.Items.Add($"Extract audio failed: {ex.Message}");
        }
    }

    private void AddText_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var seq = _project.MainSequence;
        if (seq == null || _timeline == null) return;

        var text = PromptForTextClip();
        if (string.IsNullOrWhiteSpace(text)) return;

        var track = seq.Tracks.FirstOrDefault(t => t.Kind == TrackKind.Text && !t.Locked);
        if (track == null)
        {
            track = new Track { Kind = TrackKind.Text, Name = "T1", Order = seq.Tracks.Count };
            seq.Tracks.Add(track);
        }

        Execute(new AddClipCommand
        {
            TrackId = track.Id,
            Item = new TimelineItem
            {
                Kind = ItemKind.Text,
                TimelineStart = _timeline.PlayheadTime,
                Duration = MediaTime.FromSeconds(5),
                SourceDuration = MediaTime.FromSeconds(5),
                TextContent = text.Trim(),
                FillColor = "white",
                FontSize = 64,
                Transform = { PositionX = 80, PositionY = 120 },
            },
        });
    }

    private string? PromptForTextClip()
    {
        var dialog = new Window
        {
            Title = "Add Text",
            Owner = this,
            Width = 420,
            Height = 190,
            MinWidth = 360,
            MinHeight = 170,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = (Brush)FindResource("PanelBrush"),
            Foreground = (Brush)FindResource("TextBrush"),
            FontFamily = FontFamily,
            FontSize = FontSize,
        };

        var root = new Grid { Margin = new Thickness(14) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var label = new TextBlock
        {
            Text = "Text content",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8),
        };
        Grid.SetRow(label, 0);
        root.Children.Add(label);

        var input = new TextBox
        {
            Text = "Text",
            MinHeight = 36,
            Margin = new Thickness(0, 0, 0, 14),
        };
        Grid.SetRow(input, 1);
        root.Children.Add(input);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        var cancel = new Button
        {
            Content = "Cancel",
            MinWidth = 82,
            Margin = new Thickness(0, 0, 8, 0),
            IsCancel = true,
        };
        var add = new Button
        {
            Content = "Add Text",
            MinWidth = 92,
            IsDefault = true,
            Style = (Style)FindResource("PrimaryButtonStyle"),
        };
        add.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(input.Text))
            {
                input.Focus();
                return;
            }

            dialog.DialogResult = true;
        };
        actions.Children.Add(cancel);
        actions.Children.Add(add);
        Grid.SetRow(actions, 2);
        root.Children.Add(actions);

        dialog.Content = root;
        dialog.Loaded += (_, _) =>
        {
            input.Focus();
            input.SelectAll();
        };

        return dialog.ShowDialog() == true ? input.Text : null;
    }

    private void AddMarker_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (_project.MainSequence == null || _timeline == null) return;
        Execute(new AddMarkerCommand
        {
            Marker = new Marker
            {
                Label = $"Marker {_project.MainSequence.Markers.Count + 1}",
                Time = _timeline.PlayheadTime,
                Color = "#ffcc00",
            },
        });
    }

    private async void Render_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var seq = _project.MainSequence;
        if (seq == null) return;

        var dialog = new SaveFileDialog { Filter = "MP4 Video (*.mp4)|*.mp4", FileName = $"{_project.Name}.mp4" };
        if (dialog.ShowDialog() != true) return;

        try
        {
            await _mediaService.ExportTimelineAsync(_project, seq, dialog.FileName);
            MessageBox.Show("Render complete.", "Render");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Render failed:\n{ex.Message}", "Render Error");
        }
    }

    private void Undo_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (_project.MainSequence != null)
        {
            _undoRedo.Undo(_project.MainSequence);
            _timeline?.InvalidateVisual();
            UpdateInspector(_selectedInspectorItem);
        }
    }

    private void Redo_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (_project.MainSequence != null)
        {
            _undoRedo.Redo(_project.MainSequence);
            _timeline?.InvalidateVisual();
            UpdateInspector(_selectedInspectorItem);
        }
    }

    private void Cut_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (CopySelectedClip()) DeleteSelectedClip();
    }

    private void Copy_Executed(object sender, ExecutedRoutedEventArgs e) => CopySelectedClip();

    private void Paste_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var seq = _project.MainSequence;
        if (seq == null || _timeline == null || _clipboard?.Clipboard == null) return;
        if (_timeline.SelectedTrackIndex < 0 || _timeline.SelectedTrackIndex >= seq.Tracks.Count) return;

        Execute(new PasteClipCommand
        {
            TrackId = seq.Tracks[_timeline.SelectedTrackIndex].Id,
            TimelineStart = _timeline.PlayheadTime,
            CopyCommand = _clipboard,
        });
    }

    private void SplitClip_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var seq = _project.MainSequence;
        var timeline = _timeline;
        var item = timeline?.SelectedItem;
        if (seq == null || timeline == null || item == null) return;

        Execute(new Domain.Editing.SplitClipCommand
        {
            TrackId = seq.Tracks[timeline.SelectedTrackIndex].Id,
            ItemId = item.Id,
            SplitTime = timeline.PlayheadTime,
        });
    }

    private void DeleteClip_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        DeleteSelectedClip();
    }

    private void DeleteSelectedClip()
    {
        var item = _timeline?.SelectedItem;
        if (item == null) return;
        Execute(new Domain.Editing.DeleteClipCommand { ItemId = item.Id });
    }

    private void RippleDelete_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var item = _timeline?.SelectedItem;
        if (item == null) return;
        Execute(new RippleDeleteClipCommand { ItemId = item.Id, Ripple = _rippleState });
    }

    private void Duplicate_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var item = _timeline?.SelectedItem;
        if (item == null) return;
        Execute(new DuplicateClipCommand { ItemId = item.Id });
    }

    private bool CopySelectedClip()
    {
        var item = _timeline?.SelectedItem;
        var seq = _project.MainSequence;
        if (item == null || seq == null) return false;

        var copy = new CopyClipCommand { ItemId = item.Id };
        var result = copy.Execute(seq);
        if (!result.Success) return false;
        _clipboard = copy;
        return true;
    }

    private void RefreshMediaList()
    {
        var selectedAssetId = (MediaList.SelectedItem as MediaListItem)?.Asset.Id;
        var assets = _project.MediaLibrary.AsEnumerable();

        if (_mediaKindFilter.HasValue)
            assets = assets.Where(asset => asset.Kind == _mediaKindFilter.Value);

        if (!string.IsNullOrWhiteSpace(_mediaSearchText))
        {
            assets = assets.Where(asset =>
                Path.GetFileName(asset.OriginalPath).Contains(
                    _mediaSearchText,
                    StringComparison.OrdinalIgnoreCase));
        }

        var visibleAssets = assets.ToList();
        MediaList.Items.Clear();
        foreach (var asset in visibleAssets)
            MediaList.Items.Add(CreateMediaListItem(asset));

        if (selectedAssetId.HasValue)
        {
            MediaList.SelectedItem = MediaList.Items
                .OfType<MediaListItem>()
                .FirstOrDefault(item => item.Asset.Id == selectedAssetId.Value);
        }

        MediaCountText.Text = $"{visibleAssets.Count} item{(visibleAssets.Count == 1 ? string.Empty : "s")}";
        MediaEmptyState.Visibility = Vis(visibleAssets.Count == 0);
        AddToTimelineButton.IsEnabled = MediaList.SelectedItem != null;
        PreviewSelectedMediaButton.IsEnabled = MediaList.SelectedItem != null;
        StatusText.Text = visibleAssets.Count == 0
            ? "Ready"
            : $"{visibleAssets.Count} media item{(visibleAssets.Count == 1 ? string.Empty : "s")} available";
    }

    private void SetMediaFilter(MediaKind? kind)
    {
        _mediaKindFilter = kind;
        UpdateMediaFilterButtons();
        RefreshMediaList();
    }

    private void UpdateMediaFilterButtons()
    {
        SetFilterButtonState(AllMediaFilterButton, !_mediaKindFilter.HasValue);
        SetFilterButtonState(VideoFilterButton, _mediaKindFilter == MediaKind.Video);
        SetFilterButtonState(ImageFilterButton, _mediaKindFilter == MediaKind.Image);
        SetFilterButtonState(AudioFilterButton, _mediaKindFilter == MediaKind.Audio);
    }

    private void SetFilterButtonState(Button button, bool active)
    {
        button.Background = active
            ? (Brush)FindResource("SelectionBrush")
            : Brushes.Transparent;
        button.BorderBrush = active
            ? (Brush)FindResource("AccentBrush")
            : (Brush)FindResource("BorderBrush");
        button.Foreground = active
            ? (Brush)FindResource("TextBrush")
            : (Brush)FindResource("TextSecondaryBrush");
    }

    private void MoveClip(ClipMoveRequestedEventArgs args)
    {
        var seq = _project.MainSequence;
        if (seq == null || args.TargetTrackIndex < 0 || args.TargetTrackIndex >= seq.Tracks.Count) return;

        Execute(new MoveClipCommand
        {
            ItemId = args.Item.Id,
            TargetTrackId = seq.Tracks[args.TargetTrackIndex].Id,
            NewTimelineStart = args.NewStart,
            Ripple = _rippleState,
        });
    }

    private void TrimClip(ClipTrimRequestedEventArgs args)
    {
        var seq = _project.MainSequence;
        if (seq == null || args.TrackIndex < 0 || args.TrackIndex >= seq.Tracks.Count) return;

        Execute(new TrimClipCommand
        {
            TrackId = seq.Tracks[args.TrackIndex].Id,
            ItemId = args.Item.Id,
            NewStart = args.NewStart,
            NewDuration = args.NewDuration,
            NewSourceStart = args.NewSourceStart,
            Ripple = _rippleState,
        });
    }

    private void AddSelectedMediaToTimeline()
    {
        if (MediaList.SelectedItem is not MediaListItem selected) return;
        var seq = _project.MainSequence;
        if (seq == null || _timeline == null) return;

        var trackKind = selected.Asset.Kind switch
        {
            MediaKind.Audio => TrackKind.Audio,
            MediaKind.Image => TrackKind.Overlay,
            _ => TrackKind.Video,
        };
        var itemKind = selected.Asset.Kind == MediaKind.Image ? ItemKind.Image : ItemKind.Clip;

        var track = seq.Tracks.FirstOrDefault(t => t.Kind == trackKind && !t.Locked);
        if (track == null)
        {
            track = new Track { Kind = trackKind, Name = trackKind == TrackKind.Audio ? "A1" : trackKind == TrackKind.Overlay ? "O1" : "V1", Order = seq.Tracks.Count };
            seq.Tracks.Add(track);
        }

        var duration = selected.Asset.Duration.Seconds > 0
            ? selected.Asset.Duration
            : MediaTime.FromSeconds(selected.Asset.Kind == MediaKind.Image ? 5 : 10);

        Execute(new AddClipCommand
        {
            TrackId = track.Id,
            Item = new TimelineItem
            {
                Kind = itemKind,
                MediaAssetId = selected.Asset.Id,
                TimelineStart = _timeline.PlayheadTime,
                Duration = duration,
                SourceDuration = duration,
            },
        });
        PreviewAsset(selected.Asset);
    }

    private void PreviewSelectedMedia()
    {
        if (MediaList.SelectedItem is MediaListItem selected) PreviewAsset(selected.Asset);
    }

    private void PreviewTimelineItem(TimelineItem item)
    {
        if (!item.MediaAssetId.HasValue) return;
        var asset = _project.MediaLibrary.FirstOrDefault(a => a.Id == item.MediaAssetId.Value);
        if (asset != null) PreviewAsset(asset);
    }

    private void PreviewAsset(MediaAsset asset)
    {
        if (!File.Exists(asset.OriginalPath))
        {
            PreviewSourceNameText.Text = "Media file is offline";
            return;
        }

        PreviewSourceNameText.Text = Path.GetFileName(asset.OriginalPath);
        PreviewPlayer.Stop();
        PreviewPlayer.Source = null;
        PreviewPlayer.Position = TimeSpan.Zero;
        PreviewSeekSlider.Value = 0;
        PreviewSeekSlider.Maximum = 1;
        PreviewTimeText.Text = "00:00";
        PreviewDurationText.Text = FormatPreviewTime(TimeSpan.Zero);

        if (asset.Kind == MediaKind.Image)
        {
            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(asset.OriginalPath);
                image.EndInit();
                image.Freeze();
                PreviewImage.Source = image;
                PreviewImage.Visibility = Visibility.Visible;
                PreviewPlayer.Visibility = Visibility.Collapsed;
                PreviewDurationText.Text = "Still";
            }
            catch (Exception ex)
            {
                PreviewImage.Source = null;
                PreviewSourceNameText.Text = $"Image preview failed: {ex.Message}";
            }
            return;
        }

        if (asset.Kind is not (MediaKind.Video or MediaKind.Audio))
        {
            PreviewImage.Source = null;
            PreviewImage.Visibility = Visibility.Collapsed;
            PreviewPlayer.Visibility = Visibility.Collapsed;
            PreviewSourceNameText.Text = "This media type has no source preview";
            return;
        }

        PreviewImage.Source = null;
        PreviewImage.Visibility = Visibility.Collapsed;
        PreviewPlayer.Visibility = Visibility.Visible;
        PreviewPlayer.Source = new Uri(asset.OriginalPath);
    }

    private void OnPreviewMediaOpened()
    {
        if (!PreviewPlayer.NaturalDuration.HasTimeSpan) return;
        var duration = PreviewPlayer.NaturalDuration.TimeSpan;
        PreviewSeekSlider.Maximum = Math.Max(0.001, duration.TotalSeconds);
        PreviewDurationText.Text = FormatPreviewTime(duration);
        UpdatePreviewProgress();
    }

    private void UpdatePreviewProgress()
    {
        if (PreviewPlayer.Visibility != Visibility.Visible) return;

        var position = PreviewPlayer.Position;
        if (!_isPreviewSeeking)
            PreviewSeekSlider.Value = Math.Clamp(position.TotalSeconds, 0, PreviewSeekSlider.Maximum);
        PreviewTimeText.Text = FormatPreviewTime(position);
    }

    private static string FormatPreviewTime(TimeSpan value)
    {
        if (value.TotalHours >= 1)
            return value.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
        return value.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
    }

    private void UpdateInspector(TimelineItem? item)
    {
        InspectorPanel.IsEnabled = item != null;
        InspectorPanel.Visibility = item == null ? Visibility.Collapsed : Visibility.Visible;
        InspectorEmptyState.Visibility = item == null ? Visibility.Visible : Visibility.Collapsed;
        InspectorTitle.Text = item == null ? "No clip selected" : $"{item.Kind} clip";
        StatusText.Text = item == null ? "Ready" : $"Selected {item.Kind.ToString().ToLowerInvariant()} clip";

        PositionXBox.Text = Format(item?.Transform.PositionX ?? 0);
        PositionYBox.Text = Format(item?.Transform.PositionY ?? 0);
        ScaleBox.Text = Format(item?.Transform.ScaleX ?? 1);
        RotationBox.Text = Format(item?.Transform.RotationDegrees ?? 0);
        OpacityBox.Text = Format(item?.Opacity ?? 1);
        SpeedBox.Text = Format(item?.SpeedCurve?.ConstantSpeed ?? item?.Speed ?? 1);
        ReverseToggle.IsChecked = item?.Reversed ?? false;

        var color = item?.ColorCorrection;
        BrightnessBox.Text = Format(color?.Brightness ?? 0);
        ContrastBox.Text = Format(color?.Contrast ?? 0);
        SaturationBox.Text = Format(color?.Saturation ?? 1);
        BlackWhiteToggle.IsChecked = color?.BlackAndWhite ?? false;
        StabilizeToggle.IsChecked = item?.Stabilization?.Enabled ?? false;

        EffectList.Items.Clear();
        if (item == null) return;
        foreach (var effect in item.Effects)
            EffectList.Items.Add($"{(effect.Enabled ? "On" : "Off")} - {effect.EffectTypeId}");
    }

    private void ApplyInspectorSettings()
    {
        var item = _selectedInspectorItem;
        if (item == null) return;

        Execute(new SetPropertyCommand
        {
            ItemId = item.Id,
            PropertyName = nameof(TimelineItem.Transform),
            NewValue = new TransformSnapshot(
                ParseDouble(PositionXBox.Text, item.Transform.PositionX),
                ParseDouble(PositionYBox.Text, item.Transform.PositionY),
                Math.Max(0.01, ParseDouble(ScaleBox.Text, item.Transform.ScaleX)),
                ParseDouble(RotationBox.Text, item.Transform.RotationDegrees)),
            Getter = i => new TransformSnapshot(i.Transform.PositionX, i.Transform.PositionY, i.Transform.ScaleX, i.Transform.RotationDegrees),
            Setter = (i, v) =>
            {
                if (v is not TransformSnapshot t) return;
                i.Transform.PositionX = t.X;
                i.Transform.PositionY = t.Y;
                i.Transform.ScaleX = t.Scale;
                i.Transform.ScaleY = t.Scale;
                i.Transform.RotationDegrees = t.Rotation;
            },
        });

        Execute(SetValue(item, nameof(TimelineItem.Opacity), Math.Clamp(ParseDouble(OpacityBox.Text, item.Opacity), 0, 1), i => i.Opacity, (i, v) => i.Opacity = (double)v!));
        Execute(SetValue(item, nameof(TimelineItem.Reversed), ReverseToggle.IsChecked ?? false, i => i.Reversed, (i, v) => i.Reversed = (bool)v!));

        var speed = Math.Clamp(ParseDouble(SpeedBox.Text, item.SpeedCurve?.ConstantSpeed ?? item.Speed), 0.1, 100);
        Execute(SetValue(item, nameof(TimelineItem.SpeedCurve), new SpeedCurve { ConstantSpeed = speed, PreservePitch = true }, i => i.SpeedCurve, (i, v) => i.SpeedCurve = (SpeedCurve?)v));

        var color = new ColorCorrection
        {
            Brightness = Math.Clamp(ParseDouble(BrightnessBox.Text, item.ColorCorrection?.Brightness ?? 0), -1, 1),
            Contrast = Math.Clamp(ParseDouble(ContrastBox.Text, item.ColorCorrection?.Contrast ?? 0), -1, 3),
            Saturation = Math.Clamp(ParseDouble(SaturationBox.Text, item.ColorCorrection?.Saturation ?? 1), 0, 4),
            BlackAndWhite = BlackWhiteToggle.IsChecked ?? false,
        };
        Execute(SetValue(item, nameof(TimelineItem.ColorCorrection), color, i => i.ColorCorrection, (i, v) => i.ColorCorrection = (ColorCorrection?)v));

        var stabilization = new StabilizationSettings
        {
            Enabled = StabilizeToggle.IsChecked ?? false,
            Strength = item.Stabilization?.Strength ?? 0.5,
            CropZoomCompensation = item.Stabilization?.CropZoomCompensation ?? true,
            AnalysisComplete = item.Stabilization?.AnalysisComplete ?? false,
        };
        Execute(SetValue(item, nameof(TimelineItem.Stabilization), stabilization, i => i.Stabilization, (i, v) => i.Stabilization = (StabilizationSettings?)v));
    }

    private void AddSelectedEffect()
    {
        if (_selectedInspectorItem == null || EffectCombo.SelectedItem is not EffectDefinition effect) return;
        Execute(new AddEffectCommand
        {
            ItemId = _selectedInspectorItem.Id,
            EffectTypeId = effect.EffectTypeId,
            Parameters = effect.Parameters.ToDictionary(p => p.Name, p => p.DefaultValue),
        });
    }

    private async Task AnalyzeSelectedStabilizationAsync()
    {
        var item = _selectedInspectorItem;
        if (item?.MediaAssetId == null) return;
        var asset = _project.MediaLibrary.FirstOrDefault(a => a.Id == item.MediaAssetId.Value);
        if (asset == null) return;

        var settings = item.Stabilization ?? new StabilizationSettings { Enabled = true };
        settings.Enabled = true;
        RenderQueueList.Items.Add($"Stabilization: analyzing {Path.GetFileName(asset.OriginalPath)}");
        try
        {
            await _stabilizationService.AnalyzeAsync(asset, settings);
            Execute(SetValue(item, nameof(TimelineItem.Stabilization), settings, i => i.Stabilization, (i, v) => i.Stabilization = (StabilizationSettings?)v));
            RenderQueueList.Items.Add("Stabilization: analysis complete");
            RenderQueueEmptyText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            RenderQueueList.Items.Add($"Stabilization failed: {ex.Message}");
            RenderQueueEmptyText.Visibility = Visibility.Collapsed;
        }
    }

    private static SetPropertyCommand SetValue<T>(TimelineItem item, string propertyName, T value, Func<TimelineItem, T> getter, Action<TimelineItem, object?> setter) =>
        new()
        {
            ItemId = item.Id,
            PropertyName = propertyName,
            NewValue = value,
            Getter = i => getter(i),
            Setter = setter,
        };

    private static double ParseDouble(string value, double fallback) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;

    private static string Format(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);

    private sealed record TransformSnapshot(double X, double Y, double Scale, double Rotation);

    private static MediaKind GetMediaKind(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".mp3" or ".wav" or ".aac" or ".m4a" or ".flac" => MediaKind.Audio,
        ".png" or ".jpg" or ".jpeg" or ".webp" or ".bmp" => MediaKind.Image,
        ".srt" or ".vtt" => MediaKind.Subtitle,
        ".ttf" or ".otf" => MediaKind.Font,
        ".mp4" or ".mov" or ".avi" or ".mkv" or ".webm" => MediaKind.Video,
        _ => MediaKind.Other,
    };

    private MediaListItem CreateMediaListItem(MediaAsset asset)
    {
        var previewPath = asset.Kind switch
        {
            MediaKind.Image => asset.OriginalPath,
            MediaKind.Video => Path.Combine(_appData, "Cache", "thumbnails", $"{asset.Id}.jpg"),
            MediaKind.Audio => Path.Combine(_appData, "Cache", "waveforms", $"{asset.Id}.png"),
            _ => string.Empty,
        };

        return new MediaListItem(
            asset,
            LoadThumbnail(previewPath),
            asset.Kind switch
            {
                MediaKind.Video => "▶",
                MediaKind.Audio => "♪",
                MediaKind.Image => "▧",
                MediaKind.Subtitle => "CC",
                MediaKind.Font => "Aa",
                _ => "•",
            },
            FormatDuration(asset.Duration));
    }

    private static ImageSource? LoadThumbnail(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 240;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private static string FormatDuration(MediaTime duration)
    {
        if (duration.Seconds <= 0) return string.Empty;
        var value = TimeSpan.FromSeconds(duration.Seconds);
        return value.TotalHours >= 1 ? value.ToString(@"h\:mm\:ss") : value.ToString(@"m\:ss");
    }

    private sealed record MediaListItem(MediaAsset Asset, ImageSource? Thumbnail, string FallbackGlyph, string DurationText)
    {
        public string FileName => Path.GetFileName(Asset.OriginalPath);
        public string KindText => Asset.Kind.ToString().ToUpperInvariant();
        public string FolderPath => Path.GetDirectoryName(Asset.OriginalPath) ?? string.Empty;
        public bool HasDuration => !string.IsNullOrEmpty(DurationText);
    }
}
