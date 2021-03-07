using System.Management.Automation;

namespace FSWatcherEngineEvent
{
    [Cmdlet(VerbsCommon.Remove, "FileSystemWatcher")]
    public sealed class RemoveFileSystemWatcherCommand : FileSystemEventWatcherCommandBase
    {
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public string SourceIdentifier { get; set; }

        protected override void ProcessRecord()
        {
            this.StopWatching(this.SourceIdentifier);
        }
    }
}