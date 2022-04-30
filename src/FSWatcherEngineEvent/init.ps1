# Load the module to test or debug
Import-Module $PSScriptRoot/FSWatcherEngineEvent.psd1

# Initialize the environment
New-FileSystemWatcher -SourceIdentifier "myevent" -Path d:\tmp\files

# Output process id for attaching the debugger
$PID
