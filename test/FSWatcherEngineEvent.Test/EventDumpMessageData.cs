namespace FSWatcherEngineEvent.Test;

public class EventDumpMessageData
{
    public int ChangeType { get; set; }

    public string FullPath { get; set; }

    public string Name { get; set; }

    public string OldFullPath { get; set; }

    public string OldName { get; set; }
}