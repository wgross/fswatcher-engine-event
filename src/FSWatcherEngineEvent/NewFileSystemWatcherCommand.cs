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
        public string Include { get; set; }

        [Parameter]
        public NotifyFilters Filters { get; set; } = NotifyFilters.LastAccess | NotifyFilters.LastWrite;

        protected override void ProcessRecord()
        {
            var watcher = new FileSystemWatcher();

            watcher.Path = this.Path;
            watcher.NotifyFilter = this.Filters;

            if (this.IsParameterBound(nameof(IncludeSubdirectories)))
                watcher.IncludeSubdirectories = this.IncludeSubdirectories.ToBool();

            if (this.IsParameterBound(nameof(Include)))
                watcher.Filter = this.Include;

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
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            this.Events.GenerateEvent(
                sourceIdentifier: this.SourceIdentifier,
                sender: sender,
                args: new[] { e.FullPath },
                extraData: PSObject.AsPSObject(e));
        }

        // Define the event handlers.
        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            this.Events.GenerateEvent(
                sourceIdentifier: this.SourceIdentifier,
                sender: sender,
                args: new[] { e.FullPath },
                extraData: PSObject.AsPSObject(e));
        }

        protected bool IsParameterBound(string parameterName) => this.MyInvocation.BoundParameters.ContainsKey(parameterName);
    }
}