using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using CreatorCut.Desktop.Commands;
using CreatorCut.Desktop.Timeline;
using CreatorCut.Domain;
using CreatorCut.Domain.Editing;
using CreatorCut.Desktop.Panels;
using CreatorCut.Desktop.Workspace;
using CreatorCut.Infrastructure;
using CreatorCut.Application;
using Microsoft.Win32;

namespace CreatorCut.Desktop;

public partial class MainWindow : Window
{
    private readonly WorkspaceLayoutService _workspaceService;
    private WorkspaceLayout _layout;

    private Project _project = new();
    private readonly UndoRedoStack _undoRedo = new();
    private readonly AutosaveService _autosave;
    private readonly ProjectRepository _projectRepo = new();
    private readonly MigrationService _migrationService;
    private readonly RippleState _rippleState = new();
    private TimelineControl? _timeline;
    public MainWindow()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CreatorCut");
        _workspaceService = new WorkspaceLayoutService(appData);
        _layout = _workspaceService.Load();
        _autosave = new AutosaveService(Path.Combine(appData, "autosave"));
        _migrationService = new MigrationService(Path.Combine(appData, "backups"));

        InitializeComponent();

        RippleToggle.Click += (_, _) => _rippleState.Enabled = RippleToggle.IsChecked ?? false;
        ZoomSlider.ValueChanged += (_, _) => { _timeline?.InvalidateVisual(); };

        BuildPanelsMenu();
        ApplyLayout();
        InitTimeline();

        _autosave.StartBackground(_project, TimeSpan.FromSeconds(30));

        CommandBindings.Add(new CommandBinding(EditorCommands.OpenProject, OpenProject_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.SaveProject, SaveProject_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.ImportMedia, ImportMedia_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.Undo, Undo_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.Redo, Redo_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.Cut, Cut_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.Copy, Copy_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.Paste, Paste_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.SplitClip, SplitClip_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.DeleteClip, DeleteClip_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.Duplicate, Duplicate_Executed));
        CommandBindings.Add(new CommandBinding(EditorCommands.RippleDelete, RippleDelete_Executed));

        Closed += (_, _) => { _autosave.StopBackground(); SaveLayout(); };
    }

    private void InitTimeline()
    {
        _timeline = new TimelineControl();
        _timeline.Sequence = _project.MainSequence;

        var defaultTrack = new Track { Kind = TrackKind.Video, Name = "V1" };
        defaultTrack.Items.Add(new TimelineItem
        {
            Duration = MediaTime.FromSeconds(10),
            SourceDuration = MediaTime.FromSeconds(30),
            Kind = ItemKind.Clip,
        });
        _project.MainSequence!.Tracks.Add(defaultTrack);

        _timeline.ClipSelected += (_, item) =>
        {
            _contextTrackIndex = item != null ? _timeline.SelectedTrackIndex : -1;
            if (item == null) _contextTrackIndex = -1;
        };

        TimelineHost.Content = _timeline;
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
                IsChecked = _layout.IsPanelOpen(panel.Id),
                Tag = panel.Id,
            };
            item.Click += PanelMenuItem_Click;
            PanelsMenu.Items.Add(item);
        }
    }

    private void PanelMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem item || item.Tag is not PanelId panelId) return;
        var current = _layout.IsPanelOpen(panelId);
        _layout = _layout.WithPanelToggled(panelId, !current);
        item.IsChecked = !current;
        ApplyLayout();
    }

    private void TogglePanel_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is not string panelKey) return;
        var panelId = new PanelId(panelKey);
        var current = _layout.IsPanelOpen(panelId);
        _layout = _layout.WithPanelToggled(panelId, !current);
        foreach (MenuItem item in PanelsMenu.Items)
            if (item.Tag is PanelId id && id == panelId) item.IsChecked = !current;
        ApplyLayout();
    }

    private void ApplyLayout()
    {
        MediaBorder.Visibility = Vis(_layout.IsPanelOpen(PanelId.Media));
        MediaSplitter.Visibility = Vis(_layout.IsPanelOpen(PanelId.Media));
        PreviewBorder.Visibility = Vis(_layout.IsPanelOpen(PanelId.Preview));
        TimelineBorder.Visibility = Vis(_layout.IsPanelOpen(PanelId.Timeline));
        PreviewTimelineSplitter.Visibility = Vis(_layout.IsPanelOpen(PanelId.Preview) && _layout.IsPanelOpen(PanelId.Timeline));
        TasksBorder.Visibility = Vis(_layout.IsPanelOpen(PanelId.Tasks));
        TimelineTasksSplitter.Visibility = Vis(_layout.IsPanelOpen(PanelId.Timeline) && _layout.IsPanelOpen(PanelId.Tasks));
        InspectorBorder.Visibility = Vis(_layout.IsPanelOpen(PanelId.Inspector));
        InspectorSplitter.Visibility = Vis(_layout.IsPanelOpen(PanelId.Inspector));
    }

    private static Visibility Vis(bool open) => open ? Visibility.Visible : Visibility.Collapsed;
    private void SaveLayout() => _workspaceService.Save(_layout);

    private void Execute(IEditCommand cmd)
    {
        if (_project.MainSequence == null) return;
        _undoRedo.Execute(_project.MainSequence, cmd);
        _timeline?.InvalidateVisual();
    }

    private void OpenProject_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "CreatorCut Project (*.creatorcut)|*.creatorcut|Legacy Project (*/project.json)|project.json" };
        if (dialog.ShowDialog() != true) return;

        if (dialog.FileName.EndsWith("project.json"))
        {
            var legacyDir = Path.GetDirectoryName(dialog.FileName)!;
            var result = _migrationService.MigrateLegacyProject(legacyDir);
            if (result.Success && result.Project != null)
            {
                _project = result.Project;
                _undoRedo.Clear();
                _timeline!.Sequence = _project.MainSequence;
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
                _undoRedo.Clear();
                _timeline!.Sequence = _project.MainSequence;
            }
        }
    }

    private void SaveProject_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "CreatorCut Project (*.creatorcut)|*.creatorcut", FileName = _project.Name + ".creatorcut" };
        if (dialog.ShowDialog() == true)
            _projectRepo.Save(_project, dialog.FileName);
    }

    private void ImportMedia_Executed(object sender, ExecutedRoutedEventArgs e)
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
                _project.MediaLibrary.Add(new MediaAsset
                {
                    Kind = MediaKind.Video,
                    OriginalPath = file,
                    RelativeProjectPath = file,
                });
            }
        }
    }

    private void Undo_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (_project.MainSequence != null)
        {
            _undoRedo.Undo(_project.MainSequence);
            _timeline?.InvalidateVisual();
        }
    }

    private void Redo_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (_project.MainSequence != null)
        {
            _undoRedo.Redo(_project.MainSequence);
            _timeline?.InvalidateVisual();
        }
    }

    private void Cut_Executed(object sender, ExecutedRoutedEventArgs e) { }
    private void Copy_Executed(object sender, ExecutedRoutedEventArgs e) { }
    private void Paste_Executed(object sender, ExecutedRoutedEventArgs e) { }

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
}
