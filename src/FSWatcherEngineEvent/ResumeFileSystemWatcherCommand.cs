using System.IO;
using System.Management.Automation;

namespace FSWatcherEngineEvent;

[Cmdlet(VerbsLifecycle.Resume, nameof(FileSystemWatcher))]
[OutputType(typeof(FileSystemWatcherState))]
public sealed class ResumeFileSystemWatcherCommand : ModifyingFileSystemWatcherCommandBase
{
    protected override void ProcessRecord() => this.WriteFileSystemWatcherState(this.ResumeWatching(this.SourceIdentifier));
}