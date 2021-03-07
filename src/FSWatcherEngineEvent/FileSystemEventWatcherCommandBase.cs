using System.Collections.Generic;
using System.IO;
using System.Management.Automation;

namespace FSWatcherEngineEvent
{
    public abstract class FileSystemEventWatcherCommandBase : PSCmdlet
    {
        protected static Dictionary<string, FileSystemWatcher> FileSystemWatchers { get; } = new();

        protected void StartWatching(string sourcesIdentifier, FileSystemWatcher watcher)
        {
            FileSystemWatchers.Add(sourcesIdentifier, watcher);
            watcher.EnableRaisingEvents = true;
        }

        protected void StopWatching(string sourceIdentifier)
        {
            if (FileSystemWatchers.TryGetValue(sourceIdentifier, out var watcher))
            {
                FileSystemWatchers.Remove(sourceIdentifier);

                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
        }
    }
}