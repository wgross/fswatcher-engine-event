# FSWatcherEngineEvent

is a powershell module which notifies of filesystem changes using a Powershell engine events.

A new event handler can be created with the cmdlet New-FileSystemWatcher:

```powershell
PS> New-FileSystemWatcher -SourceIdentifier "myevent" -Path c:\temp\files
```

The parameter 'SourceIdentifier' is the unique name of the powershell engine event wich is generated if the FileSystemWatcher notifies of a change in the given path. The other parameters of the cmdlet correspond to the properies of the .Net [FileSystemWatcher](https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher) class.

The notifications can be suspendend and resumed without disabling the FileSystemWatcher:

```powershell
PS> Suspend-FileSystemWatcher -SourceIdentifier "myevent"

PS> Resume-FileSystemWatcher -SourceIdentifier "myevent"
```

If no longer needed the FileSystemWatcher can be disposed:

```powershell
PS> Remove-FileSystemWatcher -SourceIdentifier "myevent"
```
