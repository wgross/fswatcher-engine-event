using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;

namespace FSWatcherEngineEvent
{
    public sealed class FileSystemWatcherSubscription
    {
        internal string SourceIdentifier { get; }
        internal string Path => this.fileSystemWatcher.Path;
        internal NotifyFilters NotifyFilter => this.fileSystemWatcher.NotifyFilter;
        internal bool EnableRaisingEvents => this.fileSystemWatcher.EnableRaisingEvents;
        internal bool IncludeSubdirectories => this.fileSystemWatcher.IncludeSubdirectories;
        internal string Filter => this.fileSystemWatcher.Filter;

        private readonly PSEventManager psEventManager;
        private readonly ICommandRuntime commandRuntime;
        private readonly int throttleMs;
        private readonly FileSystemWatcher fileSystemWatcher;
        private Action<(object sender, PSObject e)> generateEventDelagate;

        public FileSystemWatcherSubscription(string sourceIdentifier, PSEventManager psEventManager, ICommandRuntime commandRuntime, int throttleMs, FileSystemWatcher fileSystemWatcher)
        {
            this.SourceIdentifier = sourceIdentifier;
            this.psEventManager = psEventManager;
            this.commandRuntime = commandRuntime;
            this.throttleMs = throttleMs;
            this.fileSystemWatcher = fileSystemWatcher;
            this.generateEventDelagate = this.GenerateEvent;
        }

        internal void StartWatching()
        {
            if (this.throttleMs > 0)
                this.generateEventDelagate = this.Throttle<(object, FileSystemEventArgs)>(this.GenerateEvent, TimeSpan.FromMilliseconds(this.throttleMs));

            this.fileSystemWatcher.Changed += this.OnChanged;
            this.fileSystemWatcher.Created += this.OnChanged;
            this.fileSystemWatcher.Deleted += this.OnChanged;
            this.fileSystemWatcher.Renamed += this.OnRenamed;
            this.fileSystemWatcher.Error += this.OnError;
            this.fileSystemWatcher.EnableRaisingEvents = true;
        }

        internal void SuspendWatching() => this.fileSystemWatcher.EnableRaisingEvents = false;

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

        internal void ResumeWatching() => this.fileSystemWatcher.EnableRaisingEvents = true;

        private void OnError(object sender, ErrorEventArgs e)
        {
            this.commandRuntime.WriteError(new ErrorRecord(
                exception: e.GetException(),
                errorId: "fswatcher-failed",
                errorCategory: ErrorCategory.InvalidOperation,
                targetObject: sender));
        }

        private void OnRenamed(object sender, RenamedEventArgs e) => this.generateEventDelagate((sender, PSObject.AsPSObject(e)));

        private void OnChanged(object sender, FileSystemEventArgs e) => this.generateEventDelagate((sender, PSObject.AsPSObject(e)));

        private void GenerateEvent((object sender, PSObject extraData) args)
        {
            this.psEventManager.GenerateEvent(
                sourceIdentifier: this.SourceIdentifier,
                sender: args.sender,
                args: null,
                extraData: args.extraData);
        }

        private Action<(object, PSObject)> Throttle<T>(Action<(object sender, PSObject extraData)> action, TimeSpan interval)
        {
            // captured in closure:
            // .. the delivering delayed task
            Task task = null;
            // .. a lock handle
            var l = new object();
            // .. a storage for the calling args
            var args = new List<(object sender, PSObject extraData)>();

            return ((object sender, PSObject extraData) arg) =>
            {
                // the latest calling args are kept for later use
                args.Add(arg);

                // if the delayed delivery is already initialized, return
                if (task != null)
                    return;

                // starts the delayed delivery task, once!
                lock (l)
                {
                    // double locking...
                    if (task != null)
                        return;

                    // after expiry of the interval the latest args are delivered to the receiver
                    task = Task.Delay(interval).ContinueWith(t =>
                    {
                        var firstEvent = args[0];
                        if (args.Count > 1)
                        {
                            // from the first event create an instance of the aggregated event class.
                            // containing all events the happened during the throttle period as an array.
                            // int the future it may be possible to choose the last event as the header event.

                            var firstEventArgs = (FileSystemEventArgs)firstEvent.extraData.BaseObject;

                            var aggregatedEvent = new AggregatedFileSystemEventArgs(
                                firstEventArgs.ChangeType,
                                System.IO.Path.GetDirectoryName(firstEventArgs.FullPath),
                                firstEventArgs.Name)
                            {
                                Aggregated = args.Select(a => (FileSystemEventArgs)a.extraData.BaseObject).ToArray()
                            };

                            action((firstEvent.sender, PSObject.AsPSObject(aggregatedEvent)));
                        }
                        else action((firstEvent.sender, firstEvent.extraData));

                        task = null;
                    });
                }
            };
        }
    }
}