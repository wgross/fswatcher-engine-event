using System.IO;
using System.Management.Automation;

namespace FSWatcherEngineEvent
{
    [Cmdlet(VerbsCommon.New, "FileSystemWatcher")]
    public class NewFileSystemWatcherCommand : FileSystemEventWatcherCommandBase
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
        public string Path { get; set; }

        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public string SourceIdentifier { get; set; }

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

            var watcher = new FileSystemWatcher
            {
                Path = resolvedPath,
                NotifyFilter = this.NotifyFilter
            };

            if (this.IsParameterBound(nameof(IncludeSubdirectories)))
                watcher.IncludeSubdirectories = this.IncludeSubdirectories.ToBool();

            if (this.IsParameterBound(nameof(Filter)))
                watcher.Filter = this.Filter;

            watcher.Changed += this.OnChanged;
            watcher.Created += this.OnChanged;
            watcher.Deleted += this.OnChanged;
            watcher.Renamed += this.OnRenamed;
            watcher.Error += this.OnError;

            // Begin watching.
            this.StartWatching(this.SourceIdentifier, watcher);
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            this.WriteError(new ErrorRecord(
                exception: e.GetException(),
                errorId: "fswatcher-failed",
                errorCategory: ErrorCategory.InvalidOperation,
                targetObject: sender));
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            this.Events.GenerateEvent(
                sourceIdentifier: this.SourceIdentifier,
                sender: sender,
                args: null,
                extraData: PSObject.AsPSObject(e));
        }

        // Define the event handlers.
        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            this.Events.GenerateEvent(
                sourceIdentifier: this.SourceIdentifier,
                sender: sender,
                args: null,
                extraData: PSObject.AsPSObject(e));
        }

        protected bool IsParameterBound(string parameterName) => this.MyInvocation.BoundParameters.ContainsKey(parameterName);
    }
}