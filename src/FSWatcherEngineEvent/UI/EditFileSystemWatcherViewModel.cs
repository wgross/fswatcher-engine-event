using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.DataGrid;

namespace FSWatcherEngineEvent.UI;

#pragma warning disable IDE0290 // bindable partials can't be initialized in primary ctor

internal sealed partial class EditFileSystemWatcherViewModel
{
    public EditFileSystemWatcherViewModel(IEnumerable<FileSystemWatcherSubscription> fileSystemWatchers)
        => this.FileSystemWatchers = [.. fileSystemWatchers.Select(v => new FileSystemWatcherSubscriptionViewModel(v))];

    public void MakeDatagridDocument()
    {
        this.FileSystemWatcherSubscriptionDocument = new DataGridListDocument<FileSystemWatcherSubscriptionViewModel>()
            .AddColumn(FileSystemWatcherSubscriptionViewModel.Accessor.SourceIdentifier)
            .AddColumn(FileSystemWatcherSubscriptionViewModel.Accessor.Path);

        foreach (var row in this.FileSystemWatchers)
            this.FileSystemWatcherSubscriptionDocument.AddRow(row);
    }

    [Bindable]
    public partial DataGridListDocument<FileSystemWatcherSubscriptionViewModel> FileSystemWatcherSubscriptionDocument { get; set; }

    [Bindable]
    public partial FileSystemWatcherSubscriptionViewModel[] FileSystemWatchers { get; set; }
}

internal sealed partial class FileSystemWatcherSubscriptionViewModel
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

        this.NotifyFiltersItems = Enum.GetValues<NotifyFilters>();
        this.NotifyFiltersSelected = Enum.GetValues<NotifyFilters>().Select(ev => this.fileSystemWatcherSubscription.FileSystemWatcher.NotifyFilter.HasFlag(ev)).ToArray();
    }

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

    partial void OnFiltersChanged(string value)
    {
        this.fileSystemWatcherSubscription.FileSystemWatcher.Filters.Clear();

        foreach (var filter in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            this.fileSystemWatcherSubscription.FileSystemWatcher.Filters.Add(filter);
        }
    }

    partial void OnEnableRaisingEventsChanged(bool value)
        => this.fileSystemWatcherSubscription.FileSystemWatcher.EnableRaisingEvents = value;

    partial void OnIncludeSubdirectoriesChanged(bool value)
        => this.fileSystemWatcherSubscription.FileSystemWatcher.IncludeSubdirectories = value;

    partial void OnPathChanged(string value)
        => this.fileSystemWatcherSubscription.FileSystemWatcher.Path = value;

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
    }
}