using System;
using System.IO;

public class AggregatedFileSystemEventArgs : FileSystemEventArgs
{
    public AggregatedFileSystemEventArgs(WatcherChangeTypes changeType, string directory, string name)
        : base(changeType, directory, name)
    {
    }

    public FileSystemEventArgs[] Aggregated { get; set; } = Array.Empty<FileSystemEventArgs>();
}