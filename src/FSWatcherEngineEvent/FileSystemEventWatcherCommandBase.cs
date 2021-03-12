using System.Collections.Generic;
using System.Management.Automation;

namespace FSWatcherEngineEvent
{
    public abstract class FileSystemEventWatcherCommandBase : PSCmdlet
    {
        [Parameter(
            Position = 0,
            Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "Unique name of the event associated with the file system watcher")]
        [ValidateNotNullOrEmpty]
        public string SourceIdentifier { get; set; }

        protected static Dictionary<string, FileSystemWatcherSubscription> FileSystemWatchers { get; } = new();

        protected void StartWatching(FileSystemWatcherSubscription fileSystemWatcherSubscription)
        {
            FileSystemWatchers.Add(fileSystemWatcherSubscription.SourceIdentifier, fileSystemWatcherSubscription);
            fileSystemWatcherSubscription.StartWatching();
        }

        protected void StopWatching(string sourceIdentifier)
        {
            if (FileSystemWatchers.TryGetValue(sourceIdentifier, out var watcher))
            {
                FileSystemWatchers.Remove(sourceIdentifier);

                watcher.StopWatching();
            }
        }

        protected void SuspendWatching(string sourceIdentifier)
        {
            if (FileSystemWatchers.TryGetValue(sourceIdentifier, out var watcher))
            {
                watcher.SuspendWatching();
            }
        }
    }
}