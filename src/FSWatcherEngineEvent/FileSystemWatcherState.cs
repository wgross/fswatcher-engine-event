using System.IO;

namespace FSWatcherEngineEvent;

public class FileSystemWatcherState
{
    public FileSystemWatcherState(
        string sourceIdentifier,
        string path,
        NotifyFilters notifyFilters,
        bool enableRaisingEvents,
        bool includeSubdirectories,
        string filter
    )
    {
        this.SourceIdentifier = sourceIdentifier;
        this.Path = path;
        this.NotifyFilter = notifyFilters;
        this.EnableRaisingEvents = enableRaisingEvents;
        this.IncludeSubdirectories = includeSubdirectories;
        this.Filter = filter;
    }

    public string SourceIdentifier { get; }
    public string Path { get; }
    public NotifyFilters NotifyFilter { get; }
    public bool EnableRaisingEvents { get; }
    public bool IncludeSubdirectories { get; }
    public string Filter { get; }
}