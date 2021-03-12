using System.IO;
using System.Management.Automation;

namespace FSWatcherEngineEvent
{
    public sealed class FileSystemWatcherSubscription
    {
        internal string SourceIdentifier { get; }
        private readonly PSEventManager psEventManager;
        private readonly ICommandRuntime commandRuntime;
        private readonly FileSystemWatcher fileSystemWatcher;

        public FileSystemWatcherSubscription(string sourceIdentifier, PSEventManager psEventManager, ICommandRuntime commandRuntime, FileSystemWatcher fileSystemWatcher)
        {
            this.SourceIdentifier = sourceIdentifier;
            this.psEventManager = psEventManager;
            this.commandRuntime = commandRuntime;
            this.fileSystemWatcher = fileSystemWatcher;
        }

        internal void StartWatching()
        {
            this.fileSystemWatcher.Changed += this.OnChanged;
            this.fileSystemWatcher.Created += this.OnChanged;
            this.fileSystemWatcher.Deleted += this.OnChanged;
            this.fileSystemWatcher.Renamed += this.OnRenamed;
            this.fileSystemWatcher.Error += this.OnError;
            this.fileSystemWatcher.EnableRaisingEvents = true;
        }

        internal void SuspendWatching()
        {
            this.fileSystemWatcher.EnableRaisingEvents = false;
        }

        internal void StopWatching()
        {
            this.fileSystemWatcher.EnableRaisingEvents = false;
            this.fileSystemWatcher.Changed -= this.OnChanged;
            this.fileSystemWatcher.Created -= this.OnChanged;
            this.fileSystemWatcher.Deleted -= this.OnChanged;
            this.fileSystemWatcher.Renamed -= this.OnRenamed;
            this.fileSystemWatcher.Error -= this.OnError;
            this.fileSystemWatcher.Dispose();
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            this.commandRuntime.WriteError(new ErrorRecord(
                exception: e.GetException(),
                errorId: "fswatcher-failed",
                errorCategory: ErrorCategory.InvalidOperation,
                targetObject: sender));
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            this.psEventManager.GenerateEvent(
                sourceIdentifier: this.SourceIdentifier,
                sender: sender,
                args: null,
                extraData: PSObject.AsPSObject(e));
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            this.psEventManager.GenerateEvent(
                sourceIdentifier: this.SourceIdentifier,
                sender: sender,
                args: null,
                extraData: PSObject.AsPSObject(e));
        }
    }
}