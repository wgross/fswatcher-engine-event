using System.IO;
using System.Management.Automation;

namespace FSWatcherEngineEvent
{
    [Cmdlet(VerbsLifecycle.Suspend, nameof(FileSystemWatcher))]
    public sealed class SuspendFileSystemWatcherCommand : ModifyingFileSystemWatcherCommandBase
    {
        protected override void ProcessRecord() => this.SuspendWatching(this.SourceIdentifier);
    }
}