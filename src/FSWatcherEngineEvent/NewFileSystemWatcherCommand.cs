using System.IO;
using System.Management.Automation;

namespace FSWatcherEngineEvent
{
    [Cmdlet(VerbsCommon.New, nameof(FileSystemWatcher))]
    public class NewFileSystemWatcherCommand : FileSystemEventWatcherCommandBase
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 1)]
        public string Path { get; set; }

        [Parameter()]
        public SwitchParameter IncludeSubdirectories { get; set; }

        [Parameter]
        public string Filter { get; set; }

        [Parameter]
        public NotifyFilters NotifyFilter { get; set; } = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;

        protected override void ProcessRecord()
        {
            var resolvedPath = this.GetUnresolvedProviderPathFromPSPath(this.Path);

            if (!File.Exists(resolvedPath) && !Directory.Exists(resolvedPath))
            {
                this.WriteError(new ErrorRecord(
                    exception: new PSArgumentException($"Path:{this.Path} is invalid", nameof(Path)),
                    errorId: "path-invalid",
                    errorCategory: ErrorCategory.InvalidArgument,
                    targetObject: null));
                return;
            }

            var filesystemWatcher = new FileSystemWatcher
            {
                Path = resolvedPath,
                NotifyFilter = this.NotifyFilter
            };

            if (this.IsParameterBound(nameof(IncludeSubdirectories)))
                filesystemWatcher.IncludeSubdirectories = this.IncludeSubdirectories.ToBool();

            if (this.IsParameterBound(nameof(Filter)))
                filesystemWatcher.Filter = this.Filter;

            this.StartWatching(new FileSystemWatcherSubscription(this.SourceIdentifier, this.Events, this.CommandRuntime, filesystemWatcher));
        }

        protected bool IsParameterBound(string parameterName) => this.MyInvocation.BoundParameters.ContainsKey(parameterName);
    }
}