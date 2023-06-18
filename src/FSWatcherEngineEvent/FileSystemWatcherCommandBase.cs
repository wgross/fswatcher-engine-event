using System.Collections.Generic;
using System.Management.Automation;

namespace FSWatcherEngineEvent;

public abstract class FileSystemWatcherCommandBase : PSCmdlet
{
    protected static Dictionary<string, FileSystemWatcherSubscription> FileSystemWatchers { get; } = new Dictionary<string, FileSystemWatcherSubscription>();

    protected FileSystemWatcherState StartWatching(FileSystemWatcherSubscription fileSystemWatcherSubscription)
    {
        FileSystemWatchers.Add(fileSystemWatcherSubscription.SourceIdentifier, fileSystemWatcherSubscription);
        fileSystemWatcherSubscription.StartWatching();
        return ConvertToFileSystemWatcherInfo(fileSystemWatcherSubscription);
    }

    protected FileSystemWatcherState StopWatching(string sourceIdentifier)
    {
        if (FileSystemWatchers.TryGetValue(sourceIdentifier, out var watcher))
        {
            FileSystemWatchers.Remove(sourceIdentifier);

            watcher.StopWatching();
            return ConvertToFileSystemWatcherInfo(watcher);
        }
        return null;
    }

    public static void StopAllWatching()
    {
        foreach (var watcher in FileSystemWatchers.Values)
        {
            watcher.StopWatching();
        }

        FileSystemWatchers.Clear();
    }

    protected FileSystemWatcherState SuspendWatching(string sourceIdentifier)
    {
        if (FileSystemWatchers.TryGetValue(sourceIdentifier, out var watcher))
        {
            watcher.SuspendWatching();
            return ConvertToFileSystemWatcherInfo(watcher);
        }
        return null;
    }

    protected FileSystemWatcherState ResumeWatching(string sourceIdentifier)
    {
        if (FileSystemWatchers.TryGetValue(sourceIdentifier, out var watcher))
        {
            watcher.ResumeWatching();
            return ConvertToFileSystemWatcherInfo(watcher);
        }
        return null;
    }

    protected void WriteFileSystemWatcherState(FileSystemWatcherState fileSystemWatcherState)
    {
        if (fileSystemWatcherState is null)
            this.WriteObject(fileSystemWatcherState);
    }

    protected static FileSystemWatcherState ConvertToFileSystemWatcherInfo(KeyValuePair<string, FileSystemWatcherSubscription> fsw) => ConvertToFileSystemWatcherInfo(fsw.Value);

    protected static FileSystemWatcherState ConvertToFileSystemWatcherInfo(FileSystemWatcherSubscription fsw)
    {
        return new FileSystemWatcherState(
            fsw.SourceIdentifier,
            fsw.Path,
            fsw.NotifyFilter,
            fsw.EnableRaisingEvents,
            fsw.IncludeSubdirectories,
            fsw.Filter
        );
    }
}