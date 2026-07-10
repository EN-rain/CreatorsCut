using System.Windows.Input;

namespace CreatorCut.Desktop.Commands;

public static class EditorCommands
{
    public static readonly RoutedUICommand OpenProject = New("Open Project", "OpenProject", ModifierKeys.Control, Key.O);
    public static readonly RoutedUICommand SaveProject = New("Save Project", "SaveProject", ModifierKeys.Control, Key.S);
    public static readonly RoutedUICommand ImportMedia = New("Import Media...", "ImportMedia", ModifierKeys.Control, Key.I);
    public static readonly RoutedUICommand Render = New("Render...", "Render", ModifierKeys.Control, Key.R);

    public static readonly RoutedUICommand Cut = New("Cut", "Cut", ModifierKeys.Control, Key.X);
    public static readonly RoutedUICommand Copy = New("Copy", "Copy", ModifierKeys.Control, Key.C);
    public static readonly RoutedUICommand Paste = New("Paste", "Paste", ModifierKeys.Control, Key.V);

    public static readonly RoutedUICommand SplitClip = New("Split Clip", "SplitClip", ModifierKeys.Control, Key.B);
    public static readonly RoutedUICommand DeleteClip = New("Delete", "DeleteClip");
    public static readonly RoutedUICommand Duplicate = New("Duplicate", "Duplicate", ModifierKeys.Control, Key.D);
    public static readonly RoutedUICommand RippleDelete = New("Ripple Delete", "RippleDelete");

    public static readonly RoutedUICommand Undo = New("Undo", "Undo", ModifierKeys.Control, Key.Z);
    public static readonly RoutedUICommand Redo = New("Redo", "Redo", ModifierKeys.Control, Key.Y);

    public static readonly RoutedUICommand TogglePanel = New("Toggle Panel", "TogglePanel");

    private static RoutedUICommand New(string text, string name, ModifierKeys mods, Key key) =>
        new(text, name, typeof(EditorCommands), new InputGestureCollection { new KeyGesture(key, mods) });

    private static RoutedUICommand New(string text, string name) =>
        new(text, name, typeof(EditorCommands));
}
