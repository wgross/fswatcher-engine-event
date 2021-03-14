using Microsoft.PowerShell.Commands;
using System.IO;
using System.Management.Automation;

namespace FSWatcherEngineEvent
{
    [Cmdlet(VerbsCommon.New, nameof(FileSystemWatcher), DefaultParameterSetName = nameof(Path))]
    [OutputType(typeof(FileSystemWatcherState))]
    public class NewFileSystemWatcherCommand : ModifyingFileSystemWatcherCommandBase
    {
        [Parameter(
            Mandatory = true,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = nameof(LiteralPath),
            HelpMessage = "File or directory to watch")]
        [Alias("PSPath")]
        [ValidateNotNullOrEmpty]
        public string LiteralPath { get; set; }

        [Parameter(
            Mandatory = true,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            Position = 1,
            ParameterSetName = nameof(Path),
            HelpMessage = "File or directory to watch")]
        public string Path { get; set; }

        [Parameter(HelpMessage = "Watch in subdirectories of $Path as well")]
        public SwitchParameter IncludeSubdirectories { get; set; }

        [Parameter(HelpMessage = "Wildcard of files and directory names to include")]
        public string Filter { get; set; }

        [Parameter(HelpMessage = "Type of change to watch for")]
        public NotifyFilters NotifyFilter { get; set; } = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;

        protected override void ProcessRecord()
        {
            string selectPath() => nameof(Path).Equals(this.ParameterSetName) ? this.Path : this.LiteralPath;

            // always expand without wildcards. Wildcards belong into the filter
            // https://stackoverflow.com/questions/8505294/how-do-i-deal-with-paths-when-writing-a-powershell-cmdlet
            var resolvedPath = this.SessionState.Path.GetUnresolvedProviderPathFromPSPath(selectPath(), out var provider, out var drive);

            // break hard if the path isn't pointing to a win32 file system.
            if (provider.ImplementingType != typeof(FileSystemProvider))
                throw new PSNotSupportedException($"FileSystemWatcher doesn't work for cmdlet providers of type {provider.ImplementingType}");

            if (!File.Exists(resolvedPath) && !Directory.Exists(resolvedPath))
            {
                this.WriteError(new ErrorRecord(
                    exception: new PSArgumentException($"Path: {selectPath()} is invalid", this.ParameterSetName),
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

            this.WriteFileSystemWatcherState(
                this.StartWatching(new FileSystemWatcherSubscription(this.SourceIdentifier, this.Events, this.CommandRuntime, filesystemWatcher))
            );
        }

        protected bool IsParameterBound(string parameterName) => this.MyInvocation.BoundParameters.ContainsKey(parameterName);
    }
}