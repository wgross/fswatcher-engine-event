using System.IO;
using System.Management.Automation;

namespace FSWatcherEngineEvent;

[Cmdlet(VerbsCommon.Remove, nameof(FileSystemWatcher))]
[OutputType(typeof(FileSystemWatcherState))]
public sealed class RemoveFileSystemWatcherCommand : ModifyingFileSystemWatcherCommandBase
{
    [Parameter()]
    public SwitchParameter UnregisterAll { get; set; }

    protected override void ProcessRecord()
    {
        var fileSystemWatcher = this.StopWatching(this.SourceIdentifier);
        if (fileSystemWatcher is null)
            return;

        if (this.IsParameterBound(nameof(this.UnregisterAll)))
        {
            foreach (var subscriber in this.Events.GetEventSubscribers(this.SourceIdentifier))
            {
                Events.UnsubscribeEvent(subscriber);
            }
        }

        this.WriteFileSystemWatcherState(fileSystemWatcher);
    }
}