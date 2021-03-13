using System.IO;

namespace FSWatcherEngineEvent
{
    public record FileSystemWatcherState
    {
        public string SourceIdentifier { get; init; }
        public string Path { get; init; }
        public NotifyFilters NotifyFilter { get; init; }
        public bool EnableRaisingEvents { get; init; }
        public bool IncludeSubdirectories { get; init; }
        public string Filter { get; init; }
    }
}