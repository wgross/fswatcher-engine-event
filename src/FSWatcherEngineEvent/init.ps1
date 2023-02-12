# Load the module to test or debug
Import-Module $PSScriptRoot/FSWatcherEngineEvent.psd1

# Initialize the environment
New-Item -Path $PWD/test-files -ItemType Container -ErrorAction SilentlyContinue

# New-FileSystemWatcher -SourceIdentifier "myevent" -Path .\test-files
New-FileSystemWatcher -SourceIdentifier myevent -Path .\test-files -EditOptions

# Output process id for attaching the debugger
$PID
