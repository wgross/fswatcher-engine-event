using System.IO;
using System.Management.Automation;

namespace FSWatcherEngineEvent
{
    [Cmdlet(VerbsCommon.Remove, nameof(FileSystemWatcher))]
    public sealed class RemoveFileSystemWatcherCommand : FileSystemWatcherCommandBase
    {
        protected override void ProcessRecord() => this.StopWatching(this.SourceIdentifier);
    }
}