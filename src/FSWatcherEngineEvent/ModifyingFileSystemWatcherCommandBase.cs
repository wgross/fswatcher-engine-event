using System.Management.Automation;

namespace FSWatcherEngineEvent
{
    public abstract class ModifyingFileSystemWatcherCommandBase : FileSystemWatcherCommandBase
    {
        [Parameter(
            Position = 0,
            Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "Unique name of the event associated with the file system watcher")]
        [ValidateNotNullOrEmpty]
        public string SourceIdentifier { get; set; }

        protected bool IsParameterBound(string parameterName) => this.MyInvocation.BoundParameters.ContainsKey(parameterName);
    }
}