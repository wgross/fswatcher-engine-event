using System.IO;
using System.Management.Automation;

namespace FSWatcherEngineEvent
{
    [Cmdlet(VerbsLifecycle.Resume, nameof(FileSystemWatcher))]
    public sealed class ResumeFileSystemWatcherCommand : ModifyingFileSystemWatcherCommandBase
    {
        protected override void ProcessRecord() => this.ResumeWatching(this.SourceIdentifier);
    }
}