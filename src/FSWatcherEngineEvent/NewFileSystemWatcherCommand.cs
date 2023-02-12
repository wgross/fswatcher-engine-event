using Microsoft.PowerShell.Commands;
using System.IO;
using System.Management.Automation;

namespace FSWatcherEngineEvent
{
    [Cmdlet(VerbsCommon.New, nameof(FileSystemWatcher), DefaultParameterSetName = nameof(Path))]
    [OutputType(typeof(FileSystemWatcherState))]
    public class NewFileSystemWatcherCommand : ModifyingFileSystemWatcherCommandBase
    {
        private static PSModuleInfo psModuleInfo;

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

        [Parameter(HelpMessage = "Wild card of files and directory names to include")]
        public string Filter { get; set; }

        [Parameter(HelpMessage = "Type of change to watch for")]
        public NotifyFilters NotifyFilter { get; set; } = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;

        [Parameter(HelpMessage = "Script block to handle file system watcher events")]
        [ValidateNotNullOrEmpty()]
        public ScriptBlock Action { get; set; }

        [Parameter(HelpMessage = "From a first event it collects the incoming events for given number of milliseconds and the send all as one event")]
        public int ThrottleMs { get; set; }

        [Parameter(HelpMessage = "Show editor UI for file system watcher options")]
        public SwitchParameter EditOptions { get; set; }

        protected override void BeginProcessing()
        {
            if (psModuleInfo is null)
            {
                // register a 'OnRemove' handler once
                psModuleInfo = this.InvokeCommand.GetCommand("New-FileSystemWatcher", CommandTypes.All).Module;
                psModuleInfo.OnRemove = ScriptBlock.Create($"[{typeof(FileSystemWatcherCommandBase).FullName}]::{nameof(FileSystemWatcherCommandBase.StopAllWatching)}()");

                // also register exit event handler
                this.InvokeCommand
                    .InvokeScript($"Register-EngineEvent -SourceIdentifier([Management.Automation.PsEngineEvent]::Exiting) -Action {{ [{typeof(FileSystemWatcherCommandBase).FullName}]::{nameof(FileSystemWatcherCommandBase.StopAllWatching)}() }}");
            }
        }

        protected override void ProcessRecord()
        {
            string selectPath() => nameof(Path).Equals(this.ParameterSetName) ? this.Path : this.LiteralPath;

            // always expand without wildcards. Wildcards belong into the filter
            // https://stackoverflow.com/questions/8505294/how-do-i-deal-with-paths-when-writing-a-powershell-cmdlet
            var resolvedPath = this.SessionState.Path.GetUnresolvedProviderPathFromPSPath(selectPath(), out var provider, out var drive);

            // break hard if the path isn't pointing to a win32 file system.
            if (provider.ImplementingType != typeof(FileSystemProvider))
                throw new PSNotSupportedException(string.Format(Resources.Error_ProviderNotSupported, provider.ImplementingType));

            // accept only watchers for existing directories or files
            if (!File.Exists(resolvedPath) && !Directory.Exists(resolvedPath))
            {
                this.WriteError(new ErrorRecord(
                    exception: new PSArgumentException(string.Format(Resources.Error_PathInvalid, selectPath()), this.ParameterSetName),
                    errorId: "path-invalid",
                    errorCategory: ErrorCategory.InvalidArgument,
                    targetObject: default));

                return;
            }

            if (this.TryCreateFileSystemWatcher(resolvedPath))  // creation of the file system watcher was successful
                if (this.IsParameterBound(nameof(this.Action))) // the action parameter is bound
                    this.RegisterEngineEvent();                 // register the action as handler
        }

        private void RegisterEngineEvent()
        {
            var newSubscriber = this.Events.SubscribeEvent(
                source: null,
                eventName: null,
                sourceIdentifier: this.SourceIdentifier,
                data: null,
                action: this.Action,
                supportEvent: false,
                forwardEvent: false);

            this.WriteObject(newSubscriber.Action);
        }

        private bool TryCreateFileSystemWatcher(string resolvedPath)
        {
            // a source identifier must be unique
            if (FileSystemWatchers.TryGetValue(this.SourceIdentifier, out var fileSystemWatcher))
            {
                this.WriteError(new ErrorRecord(
                    exception: new PSArgumentException(string.Format(Resources.Error_SourceIdentifierAlreadyInUse, this.SourceIdentifier, fileSystemWatcher.Path)),
                    errorId: "subscriptionidentifier-duplicate",
                    errorCategory: ErrorCategory.InvalidArgument,
                    targetObject: default));

                return false;
            }

            // Show options UI if required
            if (this.EditOptions.IsPresent)
            {
                var fileSystemWatcherOptions = new FileSystemWatcherOptions
                {
                    Path = resolvedPath,
                    Filter = this.Filter ?? string.Empty,
                    NotifyFilter = this.NotifyFilter,
                    IncludeSubdirectories = this.IncludeSubdirectories,
                    ThrottleMs = this.ThrottleMs
                };

                if (new EditFileSystemWatcherOptionsUI().Run(fileSystemWatcherOptions))
                {
                    this.Filter = fileSystemWatcherOptions.Filter;
                    this.NotifyFilter = fileSystemWatcherOptions.NotifyFilter;
                    this.IncludeSubdirectories = fileSystemWatcherOptions.IncludeSubdirectories;
                    this.ThrottleMs = fileSystemWatcherOptions.ThrottleMs;
                }
                else
                {
                    this.WriteWarning(Resources.Message_EditingCanceledByUser);
                    return false;
                }
            }

            var filesystemWatcher = new FileSystemWatcher
            {
                Path = resolvedPath,
                NotifyFilter = this.NotifyFilter
            };

            filesystemWatcher.IncludeSubdirectories = this.IncludeSubdirectories.ToBool();
            filesystemWatcher.Filter = this.Filter;

            this.WriteFileSystemWatcherState(
                this.StartWatching(new FileSystemWatcherSubscription(this.SourceIdentifier, this.Events, this.CommandRuntime, this.ThrottleMs, filesystemWatcher))
            );

            return true;
        }
    }
}