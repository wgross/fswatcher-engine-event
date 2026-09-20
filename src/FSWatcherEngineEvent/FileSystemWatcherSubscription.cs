using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Threading.Tasks;

namespace FSWatcherEngineEvent;

public sealed class FileSystemWatcherSubscription
{
    internal FileSystemWatcher FileSystemWatcher { get; } = null;

    internal string SourceIdentifier { get; }
    internal string Path => this.FileSystemWatcher.Path;
    internal NotifyFilters NotifyFilter => this.FileSystemWatcher.NotifyFilter;
    internal bool EnableRaisingEvents => this.FileSystemWatcher.EnableRaisingEvents;
    internal bool IncludeSubdirectories => this.FileSystemWatcher.IncludeSubdirectories;
    internal string[] Filter => [.. this.FileSystemWatcher.Filters];

    internal int NotificationCount { get; set; } = 0;

    private readonly PSEventManager psEventManager;
    private readonly ICommandRuntime commandRuntime;
    private readonly int throttleMs;
    private readonly int debounceMs;

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
        this.FileSystemWatcher = fileSystemWatcher;

        if (this.throttleMs > 0)
            this.generateEventDelagate = Throttle(this.GenerateEvent, TimeSpan.FromMilliseconds(this.throttleMs));
        else if (this.debounceMs > 0)
            this.generateEventDelagate = Debounce(this.GenerateEvent, TimeSpan.FromMilliseconds(this.debounceMs));
        else
            this.generateEventDelagate = this.GenerateEvent;
    }

    internal void StartWatching()
    {
        this.FileSystemWatcher.Changed += this.OnChanged;
        this.FileSystemWatcher.Created += this.OnChanged;
        this.FileSystemWatcher.Deleted += this.OnChanged;
        this.FileSystemWatcher.Renamed += this.OnRenamed;
        this.FileSystemWatcher.Error += this.OnError;
        this.FileSystemWatcher.EnableRaisingEvents = true;
    }

    internal void SuspendWatching() => this.FileSystemWatcher.EnableRaisingEvents = false;

    internal void StopWatching()
    {
        this.FileSystemWatcher.EnableRaisingEvents = false;
        this.FileSystemWatcher.Changed -= this.OnChanged;
        this.FileSystemWatcher.Created -= this.OnChanged;
        this.FileSystemWatcher.Deleted -= this.OnChanged;
        this.FileSystemWatcher.Renamed -= this.OnRenamed;
        this.FileSystemWatcher.Error -= this.OnError;
        this.FileSystemWatcher.Dispose();
    }

    internal void ResumeWatching() => this.FileSystemWatcher.EnableRaisingEvents = true;

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
        this.NotificationCount++;
        this.generateEventDelagate(e);
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        this.NotificationCount++;
        this.generateEventDelagate(e);
    }

    private void OnDeleted(object sender, FileSystemEventArgs e)
    {
        this.NotificationCount++;
        this.generateEventDelagate(e);
    }

    private void GenerateEvent(List<FileSystemEventArgs> eventArgs)
    {
        this.psEventManager.GenerateEvent(
            sourceIdentifier: this.SourceIdentifier,
            sender: this.FileSystemWatcher,
            args: null,
            extraData: PSObject.AsPSObject(eventArgs.AsReadOnly()));

        this.RaiseGeneratedEvents(eventArgs);
    }

    private void GenerateEvent(FileSystemEventArgs eventArgs)
    {
        this.psEventManager.GenerateEvent(
            sourceIdentifier: this.SourceIdentifier,
            sender: this.FileSystemWatcher,
            args: null,
            extraData: PSObject.AsPSObject(eventArgs));

        this.RaiseGeneratedEvent(eventArgs);
    }

    private static Action<FileSystemEventArgs> Throttle(Action<List<FileSystemEventArgs>> action, TimeSpan interval)
    {
        // captured in closure:
        Task task = null;

        var l = new object();
        var args = new List<FileSystemEventArgs>();

        return e =>
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

                    args = [];
                    task = null;
                });
            }
        };
    }

    private static Action<FileSystemEventArgs> Debounce(Action<List<FileSystemEventArgs>> action, TimeSpan interval)
    {
        ArgumentNullException.ThrowIfNull(action);

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

                    args = [];
                }
            });
        };
    }

    #region Trace the generated events internally

    internal event EventHandler<FileSystemEventArgs> GeneratedEvent;

    private void RaiseGeneratedEvents(List<FileSystemEventArgs> eventArgs)
    {
        if (this.GeneratedEvent is { } generatedEvent)
        {
            foreach (var e in eventArgs)
            {
                generatedEvent(this, e);
            }
        }
    }

    private void RaiseGeneratedEvent(FileSystemEventArgs eventArgs)
    {
        if (this.GeneratedEvent is { } generatedEvent)
        {
            generatedEvent(this, eventArgs);
        }
    }

    #endregion Trace the generated events internally
}