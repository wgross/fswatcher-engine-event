using System;
using System.IO;

public class AggregatedFileSystemEventArgs(WatcherChangeTypes changeType, string directory, string name) : FileSystemEventArgs(changeType, directory, name)
{
    public FileSystemEventArgs[] Aggregated { get; set; } = Array.Empty<FileSystemEventArgs>();
}