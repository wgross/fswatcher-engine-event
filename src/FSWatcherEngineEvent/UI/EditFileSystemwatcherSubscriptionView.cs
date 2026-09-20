using System.IO;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Templating;

namespace FSWatcherEngineEvent.UI;

public partial class EditFileSystemwatcherSubscriptionView(FileSystemWatcherSubscriptionViewModel viewModel)
{
    private readonly FileSystemWatcherSubscriptionViewModel viewModel = viewModel;

    public VStack CreateEditor()
    {
        static TextBlock makeSourceIdentifierView(FileSystemWatcherSubscriptionViewModel viewModel)
            => new TextBlock().Text(viewModel.SourceIdentifier).TextAlignment(TextAlignment.Left).Margin(new Thickness(2, 0, 0, 0));

        static ValidationPresenter makePathEditEdit(FileSystemWatcherSubscriptionViewModel viewModel)
            => new TextBox().Text(viewModel.Bind.Path).TextAlignment(TextAlignment.Left).Margin(new Thickness(1, 0, 0, 0)).Validate(
                value: viewModel.Bind.Path,
                validator: p => Path.Exists(p) ? null : new ValidationMessage(ValidationSeverity.Error, string.Format(Resources.Error_PathInvalid, p)),
                placement: ValidationPlacement.Below);

        static TextBox makeFilterEdit(FileSystemWatcherSubscriptionViewModel viewModel)
            => new TextBox().Text(viewModel.Bind.Filters).TextAlignment(TextAlignment.Left).Margin(new Thickness(1, 0, 0, 0));
        static CheckBox makeEnableRaisingEventsEdit(FileSystemWatcherSubscriptionViewModel viewModel)
            => new CheckBox().IsChecked(viewModel.Bind.EnableRaisingEvents).Margin(new Thickness(1, 0, 0, 0));
        static CheckBox makeIncludeSubdirectoriesEdit(FileSystemWatcherSubscriptionViewModel viewModel)
            => new CheckBox().IsChecked(viewModel.Bind.IncludeSubdirectories).Margin(new Thickness(1, 0, 0, 0));
        static SelectionList<NotifyFilters> makeNotifyFilterEdit(FileSystemWatcherSubscriptionViewModel viewModel)
            => new SelectionList<NotifyFilters>().Items(viewModel.NotifyFiltersItems).Checked(viewModel.NotifyFiltersSelected).Update(viewModel.UpdateNotifyFilters);

        static HStack makeEditorRow(string title, Visual editor) => new HStack()
            .Add(new TextBlock().Text(title).TextAlignment(TextAlignment.Right).MinWidth(26))
            .Add(editor.Stretch())
            .HorizontalAlignment(Align.Stretch)
            .Spacing(1);

        return new VStack()
            .Add(makeEditorRow(Resources.Editor_SourceIdentifier, makeSourceIdentifierView(this.viewModel)))
            .Add(makeEditorRow(Resources.Editor_EnableRaisingEvents, makeEnableRaisingEventsEdit(this.viewModel)))
            .Add(makeEditorRow(Resources.Editor_Path, makePathEditEdit(this.viewModel)))
            .Add(makeEditorRow(Resources.Editor_Filters, makeFilterEdit(this.viewModel)))
            .Add(makeEditorRow(Resources.Editor_IncludeSubdirectories, makeIncludeSubdirectoriesEdit(this.viewModel)))
            .Add(makeEditorRow(Resources.Editor_NotifyFilters, makeNotifyFilterEdit(this.viewModel)))
            .HorizontalAlignment(Align.Stretch);
    }

    public TextArea CreateCommandPreview() => new(this.viewModel.Bind.CommandText);

    public DockLayout CreateEditorWithCommandPreview() => new DockLayout()
        .Bottom(new VStack(new Rule(), this.CreateCommandPreview()))
        .Content(this.CreateEditor());

    public TabControl CreateEditorCommandPreviewAndTrace()
    {
        var fsTraceListBox = this.CreateTrace();
        var editorCommandPreviewAndTraceTab = new TabControl().HorizontalAlignment(Align.Stretch).VerticalAlignment(Align.Stretch);
        editorCommandPreviewAndTraceTab.AddTab(Resources.Editor_Title, this.CreateEditorWithCommandPreview());
        editorCommandPreviewAndTraceTab.AddTab(Resources.Trace_Title, fsTraceListBox.Scrollable());
        editorCommandPreviewAndTraceTab.AddCommand(new Command
        {
            Id = "fsevent.ClearTrace",
            LabelMarkup = Resources.Trace_Clear,
            Gesture = new KeyGesture(TerminalChar.CtrlX, TerminalModifiers.Ctrl),
            Execute = _ => fsTraceListBox.Items.Clear()
        });

        return editorCommandPreviewAndTraceTab;
    }

    public ListBox<FileSystemEventArgs> CreateTrace()
    {
        var listbox = new ListBox<FileSystemEventArgs>()
            .Items([])
            .ItemTemplate(new DataTemplate<FileSystemEventArgs>(
                    Display: static (item, in _) => new TextBlock().Text(FormatEventArgs(item.GetValue())).TextAlignment(TextAlignment.Left).Margin(new Thickness(1, 0, 0, 0)),
                    Editor: null))
            .HorizontalAlignment(Align.Stretch)
            .VerticalAlignment(Align.Stretch);

        this.viewModel.UpdateEventView = item => listbox.Dispatcher.Post(() => listbox.Items.Insert(0, item));

        return listbox;
    }

    private static string FormatEventArgs(FileSystemEventArgs eventArgs) => $"{eventArgs.ChangeType} {eventArgs.FullPath}";
}