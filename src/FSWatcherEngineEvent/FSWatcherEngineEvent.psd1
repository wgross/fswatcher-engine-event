@{
    RootModule = 'FSWatcherEngineEvent.dll'
    ModuleVersion = '1.0'
    GUID = 'cea9a314-3e4c-4080-9a0b-5a867efa61f3'
    Author = 'github.com/wgross'
    Copyright = '(c) github.com/wgross. All rights reserved.'
    Description = 'Provide file system change notifications as powershell engine events'
    PowershellHostVersion="5.1"
    CmdletsToExport = @(
        'New-FileSystemWatcher'
        'Remove-FileSystemWatcher'
        'Suspend-FileSystemWatcher'
        'Resume-FileSystemWatcher'
        'Get-FileSystemWatcher'
    )
    PrivateData = @{
        PSData = @{
            Tags= @("FileSystemWatcher", "EngineEvent")
            LicenseUri = 'https://github.com/wgross/fswatcher-engine-event/blob/97e24092bdf6fc52165c5f118c26cfb3634031af/LICENSE'
            ProjectUri = 'https://github.com/wgross/fswatcher-engine-event'
            ReleaseNotes = @"
            1.1: compatibility with netstandard2.0 (PowerShell 5.x)
"@
        }
    }
}

