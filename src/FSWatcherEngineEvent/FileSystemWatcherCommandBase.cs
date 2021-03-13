using System.Collections.Generic;
using System.Management.Automation;

namespace FSWatcherEngineEvent
{
    public abstract class FileSystemWatcherCommandBase : PSCmdlet
    {
        
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

        protected void ResumeWatching(string sourceIdentifier)
        {
            if (FileSystemWatchers.TryGetValue(sourceIdentifier, out var watcher))
            {
                watcher.ResumeWatching();
            }
        }
    }
}