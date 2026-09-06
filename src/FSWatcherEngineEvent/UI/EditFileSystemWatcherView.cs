using Humanizer;
using System;
using System.IO;
using System.Linq;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.DataGrid;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Styling;

namespace FSWatcherEngineEvent.UI;

internal sealed class EditFileSystemWatcherView(EditFileSystemWatcherViewModel viewModel) : IDisposable
{
    private readonly EditFileSystemWatcherViewModel viewModel = viewModel;
    private DataGridDocumentView dataGridDocumentView;

    public void Dispose() => this.dataGridDocumentView?.Dispose();

    public static implicit operator Visual(EditFileSystemWatcherView view)
    {
        view.viewModel.MakeDatagridDocument();
        view.dataGridDocumentView = new DataGridDocumentView(view.viewModel.FileSystemWatcherSubscriptionDocument);

        return new ScrollViewer(MakeFileSystemWatcherDataGrid(view));
    }

    private static DataGridControl MakeFileSystemWatcherDataGrid(EditFileSystemWatcherView view)
    {
        var dataGridControl = new DataGridControl
        {
            View = view.dataGridDocumentView,
            FrozenColumns = 2,
            ReadOnly = true,
            SelectionMode = DataGridSelectionMode.Row,
            ShowRowAnchor = false,
        };

        dataGridControl.Style(new DataGridStyle()
        {
            ShowVerticalLines = true,
            ShowHeaderSeparator = true,
            Glyphs = LineGlyphs.Single,
            
        });

        dataGridControl.Columns.Add(new DataGridColumn<string>
        {
            Key = Resources.DataGrid_SourceIdentifier,
            TypedValueAccessor = FileSystemWatcherSubscriptionViewModel.Accessor.SourceIdentifier,
            Width = GridLength.Auto,
            CellAlignment = TextAlignment.Left,
            Sortable = true,
        });

        dataGridControl.Columns.Add(new DataGridColumn<string>
        {
            Key = Resources.DataGrid_Path,
            TypedValueAccessor = FileSystemWatcherSubscriptionViewModel.Accessor.Path,
            Width = GridLength.Star(1),
            Sortable = true,
        });

        if (dataGridControl.Commands.FirstOrDefault(c => c.Gesture is { Key: TerminalKey.F2 }) is { } editCommand)
            dataGridControl.Commands.Remove(editCommand);

        dataGridControl.Commands.Add(new Command
        {
            Id = "fileSysteWatcher.Edit",
            LabelMarkup = Resources.DataGrid_Edit,
            Gesture = new KeyGesture(TerminalKey.Enter),
            Execute = visual =>
            {
                if (visual is DataGridControl { CurrentCell: { Row: > -1 } currentCell } dg)
                {
                    if (view.dataGridDocumentView.Document.CurrentSnapshot.GetRowModel(currentCell.Row) is FileSystemWatcherSubscriptionViewModel fileSystemWatcherSubscriptionViewModel)
                    {
                        MakeFileSystemWatcherEditorDialog(fileSystemWatcherSubscriptionViewModel)
                            .Top(dg.Bounds.Top + currentCell.Row + 2).Left(1).Width(dg.Bounds.Width - 2)
                            .Show();
                    }
                }
            }
        });

        return dataGridControl;
    }

    private static Dialog MakeFileSystemWatcherEditorDialog(FileSystemWatcherSubscriptionViewModel viewModel) => new Dialog(MakeFileSystemWatcherEditor(viewModel))
        .Title(Resources.Editor_Title.FormatWith(viewModel.SourceIdentifier))
        .MinWidth(75)
        .Content(new DockLayout().Bottom(new CommandBar()).Content(MakeFileSystemWatcherEditor(viewModel)))
        .IsModal(true)
        .AddCommand(new()
        {
            Id = "fileSystemWatcherEditor.Close",
            LabelMarkup = Resources.Editor_Close,
            Gesture = new KeyGesture(TerminalKey.Escape),
            Execute = visual =>
            {
                if (visual is Dialog dialog)
                    dialog.Close();
            }
        }) as Dialog;

    private static VStack MakeFileSystemWatcherEditor(FileSystemWatcherSubscriptionViewModel viewModel)
    {
        static TextBlock makeSourceIdentifierView(FileSystemWatcherSubscriptionViewModel viewModel)
            => new TextBlock().Text(viewModel.SourceIdentifier).TextAlignment(TextAlignment.Left).Margin(new Thickness(2,0,0,0));
        static TextBox makePathEditEdit(FileSystemWatcherSubscriptionViewModel viewModel)
            => new TextBox().Text(viewModel.Path).TextAlignment(TextAlignment.Left).Margin(new Thickness(1,0,0,0));
        static TextBox makeFilterEdit(FileSystemWatcherSubscriptionViewModel viewModel)
            => new TextBox().Text(viewModel.Filters).TextAlignment(TextAlignment.Left).Margin(new Thickness(1,0,0,0));
        static CheckBox makeEnableRaisingEventsEdit(FileSystemWatcherSubscriptionViewModel viewModel)
            => new CheckBox().IsChecked(viewModel.EnableRaisingEvents).Margin(new Thickness(1, 0, 0, 0));
        static CheckBox makeIncludeSubdirectoriesEdit(FileSystemWatcherSubscriptionViewModel viewModel)
            => new CheckBox().IsChecked(viewModel.IncludeSubdirectories).Margin(new Thickness(1, 0, 0, 0));
        static SelectionList<NotifyFilters> makeNotifyFilterEdit(FileSystemWatcherSubscriptionViewModel viewModel)
            => new SelectionList<NotifyFilters>().Items(viewModel.NotifyFiltersItems).Checked(viewModel.NotifyFiltersSelected).Update(viewModel.UpdateNotifyFilters);

        static HStack makeEditorRow(string title, Visual editor) => new HStack()
            .Add(new TextBlock().Text(title).TextAlignment(TextAlignment.Right).MinWidth(26))
            .Add(editor.Stretch())
            .HorizontalAlignment(Align.Stretch)
            .Spacing(1);

        return new VStack()
            .Add(makeEditorRow(Resources.Editor_SourceIdentifier, makeSourceIdentifierView(viewModel)))
            .Add(makeEditorRow("Enable Raising Events:", makeEnableRaisingEventsEdit(viewModel)))
            .Add(makeEditorRow("Path:", makePathEditEdit(viewModel)))
            .Add(makeEditorRow("Filters:", makeFilterEdit(viewModel)))
            .Add(makeEditorRow("Include Subdirectories:", makeIncludeSubdirectoriesEdit(viewModel)))
            .Add(makeEditorRow("Notify Filters:", makeNotifyFilterEdit(viewModel)))
            .HorizontalAlignment(Align.Stretch);
    }
}