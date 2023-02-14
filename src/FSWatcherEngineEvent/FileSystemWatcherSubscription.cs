using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly int debounceMs;
        private readonly FileSystemWatcher fileSystemWatcher;
        private readonly Action<FileSystemEventArgs> generateEventDelagate;

        public FileSystemWatcherSubscription(
            string sourceIdentifier,
            PSEventManager psEventManager,
            ICommandRuntime commandRuntime,
            int throttleMs,
            int debounceMs,
            FileSystemWatcher fileSystemWatcher)
        {
            this.SourceIdentifier = sourceIdentifier;
            this.psEventManager = psEventManager;
            this.commandRuntime = commandRuntime;
            this.throttleMs = throttleMs;
            this.debounceMs = debounceMs;
            this.fileSystemWatcher = fileSystemWatcher;

            if (this.throttleMs > 0)
                this.generateEventDelagate = this.Throttle(this.GenerateEvent, TimeSpan.FromMilliseconds(this.throttleMs));
            else if (this.debounceMs > 0)
                this.generateEventDelagate = this.Debounce(this.GenerateEvent, TimeSpan.FromMilliseconds(this.debounceMs));
            else
                this.generateEventDelagate = this.GenerateEvent;
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

        private void OnRenamed(object sender, RenamedEventArgs e) => this.generateEventDelagate(e);

        private void OnChanged(object sender, FileSystemEventArgs e) => this.generateEventDelagate(e);

        private void GenerateEvent(List<FileSystemEventArgs> eventArgs)
        {
            this.psEventManager.GenerateEvent(
                sourceIdentifier: this.SourceIdentifier,
                sender: this.fileSystemWatcher,
                args: null,
                extraData: PSObject.AsPSObject(eventArgs.AsReadOnly()));
        }

        private void GenerateEvent(FileSystemEventArgs eventArgs)
        {
            this.psEventManager.GenerateEvent(
                sourceIdentifier: this.SourceIdentifier,
                sender: this.fileSystemWatcher,
                args: null,
                extraData: PSObject.AsPSObject(eventArgs));
        }

        private Action<FileSystemEventArgs> Throttle(Action<List<FileSystemEventArgs>> action, TimeSpan interval)
        {
            // captured in closure:
            Task task = null;

            var l = new object();
            var args = new List<FileSystemEventArgs>();

            return (FileSystemEventArgs e) =>
            {
                // the latest calling args are kept for later use
                args.Add(e);

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
                        var tmp = args;

                        action(tmp);

                        args = new List<FileSystemEventArgs>();
                        task = null;
                    });
                }
            };
        }

        private Action<FileSystemEventArgs> Debounce(Action<List<FileSystemEventArgs>> action, TimeSpan interval)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));

            var last = 0;
            var args = new List<FileSystemEventArgs>();

            return arg =>
            {
                args.Add(arg);

                // increment while calls of the event are coming
                var current = System.Threading.Interlocked.Increment(ref last);

                // first incoming event starts the delayed invocation of the action
                Task.Delay(interval).ContinueWith(task =>
                {
                    // excute action after a period of time where no changes happen
                    if (current == last)
                    {
                        var tmp = args;

                        action(tmp);

                        args = new List<FileSystemEventArgs>();
                    }
                });
            };
        }
    }
}