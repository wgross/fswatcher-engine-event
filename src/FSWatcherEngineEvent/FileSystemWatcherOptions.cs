using System.IO;

namespace FSWatcherEngineEvent;

public class FileSystemWatcherOptions
{
    public string Path { get; set; }

    public string Filter { get; set; }

    public NotifyFilters NotifyFilter { get; set; }

    public bool IncludeSubdirectories { get; set; }
    public int ThrottleMs { get; internal set; }
    public int DebounceMs { get; internal set; }
}