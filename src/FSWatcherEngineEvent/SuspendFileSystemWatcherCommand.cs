using System.IO;
using System.Management.Automation;

namespace FSWatcherEngineEvent
{
    [Cmdlet(VerbsLifecycle.Suspend, nameof(FileSystemWatcher))]
    [OutputType(typeof(FileSystemWatcherState))]
    public sealed class SuspendFileSystemWatcherCommand : ModifyingFileSystemWatcherCommandBase
    {
        protected override void ProcessRecord() => this.WriteFileSystemWatcherState(this.SuspendWatching(this.SourceIdentifier));
    }
}