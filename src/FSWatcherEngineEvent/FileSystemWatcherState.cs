using System.IO;

namespace FSWatcherEngineEvent;

public record FileSystemWatcherState(
    string SourceIdentifier,
    string Path,
    NotifyFilters NotifyFilter,
    bool EnableRaisingEvents,
    bool IncludeSubdirectories,
    string[] Filter
);
