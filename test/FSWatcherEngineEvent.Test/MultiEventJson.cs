namespace FSWatcherEngineEvent.Test
{
    //{
    //  "ComputerName": null,
    //  "RunspaceId": "955b0007-8999-4fb9-954b-8d8d2b318b5d",
    //  "EventIdentifier": 4,
    //  "Sender": {
    //    "NotifyFilter": 48,
    //    "Filters": [],
    //    "EnableRaisingEvents": true,
    //    "Filter": "*",
    //    "IncludeSubdirectories": false,
    //    "InternalBufferSize": 8192,
    //    "Path": "D:\\tmp\\watched",
    //    "Site": null,
    //    "SynchronizingObject": null,
    //    "Container": null
    //  },
    //  "SourceEventArgs": null,
    //  "SourceArgs": [
    //    "D:\\tmp\\watched\\test.txt"
    //  ],
    //  "SourceIdentifier": "source",
    //  "TimeGenerated": "2021-03-07T16:00:51.8923011+01:00",
    //  "MessageData": [{
    //     "ChangeType": 4,
    //    "FullPath": "D:\\tmp\\watched\\test.txt",
    //    "Name": "test.txt"
    //  }]
    //}
    public class MultiEventJson
    {
        public string SourceIdentifier { get; set; }

        public EventDumpMessageData[] MessageData { get; set; }
    }
}