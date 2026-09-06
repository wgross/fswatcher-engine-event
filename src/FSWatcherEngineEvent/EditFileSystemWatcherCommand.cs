using FSWatcherEngineEvent.UI;
using System.IO;
using System.Management.Automation;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

namespace FSWatcherEngineEvent;

[Cmdlet(VerbsData.Edit, nameof(FileSystemWatcher))]
public sealed class EditFileSystemWatcherCommand : FileSystemWatcherCommandBase
{
    protected override void ProcessRecord()
    {
        using var terminal = Terminal.Open();
        using var fileSystemWatcherUi = new EditFileSystemWatcherView(new EditFileSystemWatcherViewModel(FileSystemWatchers.Values));

        Terminal.Run(
            visual: MakeFullScreenUi(fileSystemWatcherUi),
            onUpdate: () => TerminalLoopResult.Continue);
    }

    private static DockLayout MakeFullScreenUi(Visual visual) => new DockLayout().Bottom(new CommandBar()).Content(visual).Style(Theme.Terminal);
}