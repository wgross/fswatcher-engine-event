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
    }
}