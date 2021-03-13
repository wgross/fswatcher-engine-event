using System.IO;
using System.Management.Automation;

namespace FSWatcherEngineEvent
{
    [Cmdlet(VerbsCommon.Remove, nameof(FileSystemWatcher))]
    [OutputType(typeof(FileSystemWatcherState))]
    public sealed class RemoveFileSystemWatcherCommand : ModifyingFileSystemWatcherCommandBase
    {
        protected override void ProcessRecord() => this.WriteFileSystemWatcherState(this.StopWatching(this.SourceIdentifier));
    }
}