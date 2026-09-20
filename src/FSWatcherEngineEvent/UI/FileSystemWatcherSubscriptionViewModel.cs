using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace FSWatcherEngineEvent.UI;

public sealed partial class FileSystemWatcherSubscriptionViewModel : IDisposable
{
    private readonly FileSystemWatcherSubscription fileSystemWatcherSubscription;

    public FileSystemWatcherSubscriptionViewModel(FileSystemWatcherSubscription fileSystemWatcherSubscription)
    {
        this.fileSystemWatcherSubscription = fileSystemWatcherSubscription;
        this.SourceIdentifier = fileSystemWatcherSubscription.SourceIdentifier;
        this.EnableRaisingEvents = this.fileSystemWatcherSubscription.FileSystemWatcher.EnableRaisingEvents;

        this.Path = fileSystemWatcherSubscription.Path;
        this.Filters = string.Join(", ", this.fileSystemWatcherSubscription.FileSystemWatcher.Filters);
        this.IncludeSubdirectories = this.fileSystemWatcherSubscription.FileSystemWatcher.IncludeSubdirectories;
        this.Events = [];

        this.NotifyFiltersItems = Enum.GetValues<NotifyFilters>();
        this.NotifyFiltersSelected = [.. Enum.GetValues<NotifyFilters>().Select(ev => this.fileSystemWatcherSubscription.FileSystemWatcher.NotifyFilter.HasFlag(ev))];

        this.UpdateCommandText();
        this.fileSystemWatcherSubscription.GeneratedEvent += this.UpdateEvents;
    }

    public void Dispose() => this.fileSystemWatcherSubscription.GeneratedEvent -= this.UpdateEvents;

    [Bindable]
    public partial string SourceIdentifier { get; set; }

    [Bindable]
    public partial string Path { get; set; }

    [Bindable]
    public partial string Filters { get; set; }

    [Bindable]
    public partial bool EnableRaisingEvents { get; set; }

    [Bindable]
    public partial bool IncludeSubdirectories { get; set; }

    [Bindable]
    public partial NotifyFilters[] NotifyFiltersItems { get; set; }

    [Bindable]
    public partial bool[] NotifyFiltersSelected { get; set; }

    [Bindable]
    public partial string CommandText { get; set; }

    [Bindable]
    public partial List<FileSystemEventArgs> Events { get; set; }

    public Action<FileSystemEventArgs> UpdateEventView { get; internal set; }

    partial void OnFiltersChanged(string value)
    {
        this.fileSystemWatcherSubscription.FileSystemWatcher.Filters.Clear();

        foreach (var filter in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            this.fileSystemWatcherSubscription.FileSystemWatcher.Filters.Add(filter);
        }

        this.UpdateCommandText();
    }

    partial void OnEnableRaisingEventsChanged(bool value)
    {
        this.fileSystemWatcherSubscription.FileSystemWatcher.EnableRaisingEvents = value;
        this.UpdateCommandText();
    }

    partial void OnIncludeSubdirectoriesChanged(bool value)
    {
        this.fileSystemWatcherSubscription.FileSystemWatcher.IncludeSubdirectories = value;
        this.UpdateCommandText();
    }

    partial void OnPathChanged(string value)
    {
        if (System.IO.Path.Exists(value))
        {
            this.fileSystemWatcherSubscription.FileSystemWatcher.Path = value;
            this.UpdateCommandText();
        }
    }

    internal void UpdateNotifyFilters(SelectionList<NotifyFilters> list)
    {
        NotifyFilters newValue = default;
        for (int i = 0; i < list.Checked.Count; i++)
        {
            if (list.Checked[i])
                newValue |= this.NotifyFiltersItems[i];
        }

        // update the file system watcher and the view model's selected notify filters to update the UI
        this.fileSystemWatcherSubscription.FileSystemWatcher.NotifyFilter = newValue;
        this.NotifyFiltersSelected = [.. list.Checked];

        this.UpdateCommandText();
    }

    public void UpdateCommandText()
    {
        StringBuilder commandText = new StringBuilder($"New-FileSystemWatcher -{nameof(ModifyingFileSystemWatcherCommandBase.SourceIdentifier)} {this.SourceIdentifier}")
            .Append(' ')
            .Append($"-{nameof(NewFileSystemWatcherCommand.Path)} \"{this.fileSystemWatcherSubscription.FileSystemWatcher.Path}\"")
            .Append(' ')
            .Append($"-{nameof(NewFileSystemWatcherCommand.IncludeSubdirectories)}:${this.fileSystemWatcherSubscription.FileSystemWatcher.IncludeSubdirectories}")
            .Append(' ')
            .Append($"-{nameof(NewFileSystemWatcherCommand.Filters)} \"{string.Join("\",\"", this.fileSystemWatcherSubscription.FileSystemWatcher.Filters)}\"")
            .Append(' ')
            .Append($"-{nameof(NewFileSystemWatcherCommand.NotifyFilter)} {this.fileSystemWatcherSubscription.FileSystemWatcher.NotifyFilter}")
            .Append(' ');

        this.CommandText = commandText.ToString();
    }

    private void UpdateEvents(object sender, FileSystemEventArgs e) => this.UpdateEventView?.Invoke(e);
}