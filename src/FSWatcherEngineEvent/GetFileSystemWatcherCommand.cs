using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace FSWatcherEngineEvent
{
    [Cmdlet(VerbsCommon.Get, nameof(FileSystemWatcher))]
    [OutputType(typeof(FileSystemWatcherState))]
    public sealed class GetFileSystemWatcherCommand : FileSystemWatcherCommandBase
    {
        protected override void ProcessRecord() => FileSystemWatchers
            .Select(ConvertToFileSystemWatcherInfo)
            .ToList()
            .ForEach(this.WriteObject);

        private static FileSystemWatcherState ConvertToFileSystemWatcherInfo(KeyValuePair<string, FileSystemWatcherSubscription> fsw)
        {
            return new FileSystemWatcherState
            {
                SourceIdentifier = fsw.Key,
                Path = fsw.Value.Path,
                NotifyFilter = fsw.Value.NotifyFilter,
                EnableRaisingEvents = fsw.Value.EnableRaisingEvents,
                IncludeSubdirectories = fsw.Value.IncludeSubdirectories,
                Filter = fsw.Value.Filter
            };
        }
    }

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