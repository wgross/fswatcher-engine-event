using FSWatcherEngineEvent.UI;
using System.Management.Automation;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

namespace FSWatcherEngineEvent;

[Cmdlet(VerbsData.Edit, nameof(FileSystemWatcher))]
public sealed class EditFileSystemWatcherCommand : ModifyingFileSystemWatcherCommandBase
{
    [Parameter(ValueFromPipeline = true)]
    public FileSystemWatcherState FileSystemWatcher { get; set; }

    protected override void ProcessRecord()
    {
        if (this.SourceIdentifier is { } sourceIdentifier && FileSystemWatchers.TryGetValue(sourceIdentifier, out var _1))
        {
            EditFileSystemWatcher(_1);
        }
        else if (FileSystemWatchers.TryGetValue(this.FileSystemWatcher.SourceIdentifier, out var _2))
        {
            EditFileSystemWatcher(_2);
        }
    }

    private static void EditFileSystemWatcher(FileSystemWatcherSubscription fileSystemWatcherSubscription)
    {
        using var fileSystemWatcherViewModel = new FileSystemWatcherSubscriptionViewModel(fileSystemWatcherSubscription);
        var fileSystemWatcherView = new EditFileSystemwatcherSubscriptionView(fileSystemWatcherViewModel);

        using var terminal = Terminal.Open();

        Terminal.Run(
            visual: MakeFullScreenUi(fileSystemWatcherView.CreateEditorCommandPreviewAndTrace()),
            onUpdate: () => TerminalLoopResult.Continue);
    }

    private static DockLayout MakeFullScreenUi(Visual visual) => new DockLayout().Bottom(new VStack(new Rule(), new CommandBar())).Content(visual).Style(Theme.Terminal);
}