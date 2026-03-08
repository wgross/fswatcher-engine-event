# AGENTS.md - FSWatcherEngineEvent Development Guide

## Project Overview

FSWatcherEngineEvent is a PowerShell module that provides file system change notifications as PowerShell engine events. It wraps .NET's `FileSystemWatcher` class and exposes cmdlets for creating, managing, and monitoring file system watchers.

- **Language**: C# (.NET 10)
- **Test Framework**: xUnit
- **Target SDK**: Microsoft.PowerShell.SDK 7.6.0-preview.5
- **Module Version**: 2.0.0

## Build Commands

### Build the project
```powershell
dotnet build src/FSWatcherEngineEvent/FSWatcherEngineEvent.csproj
```

### Build test project
```powershell
dotnet build test/FSWatcherEngineEvent.Test/FSWatcherEngineEvent.Test.csproj
```

### Run all tests
```powershell
dotnet test test/FSWatcherEngineEvent.Test/FSWatcherEngineEvent.Test.csproj
```

### Run a single test
```powershell
dotnet test test/FSWatcherEngineEvent.Test/FSWatcherEngineEvent.Test.csproj --filter "FullyQualifiedName~FSWatcherEngineEventTest.Notifies_on_created_file"
```

### Run tests with verbose output
```powershell
dotnet test test/FSWatcherEngineEvent.Test/FSWatcherEngineEvent.Test.csproj -v n
```

## Project Structure

```
src/FSWatcherEngineEvent/           # Main module source
  FSWatcherEngineEvent.csproj       # Project file
  FSWatcherEngineEvent.psd1         # Module manifest
  *.cs                              # Cmdlet implementations
  Resources.resx                    # Localized strings

test/FSWatcherEngineEvent.Test/    # xUnit test project
  FSWatcherEngineEventTest.cs      # Main test file
```

## Code Style Guidelines

### Naming Conventions

- **Classes**: PascalCase (e.g., `FileSystemWatcherSubscription`)
- **Properties**: PascalCase (e.g., `SourceIdentifier`)
- **Private fields**: camelCase with `this.` prefix (e.g., `this.fileSystemWatcher`)
- **Parameters**: PascalCase (e.g., `SourceIdentifier`)
- **Methods**: PascalCase (e.g., `StartWatching`)
- **Constants**: PascalCase (e.g., `Error_CantConbineThrottleAndDebounce`)
- **Cmdlets**: Follow PowerShell verb-noun pattern with `Command` suffix in code (e.g., `NewFileSystemWatcherCommand`)
- Use this. prefix for all member access

### File Organization

- One public class per file
- File name matches class name
- Related classes in same file (e.g., base class + derived class)
- Properties with getters/setters on single lines when simple

### Imports

- System namespaces first, then project namespaces
- No unnecessary imports
- Full namespace for external types (e.g., `System.Management.Automation.PSCmdlet`)
- Use implicit usings where possible

### Code Formatting

- 4 spaces for indentation (no tabs)
- Opening brace on next line
- Single space after comma in parameter lists
- No trailing whitespace
- Line length not strictly enforced but keep reasonable (~120 chars)

### Type Usage

- Use explicit types for public APIs
- Target-typed `new()` where clear
- Use `string[]` for arrays, not generic `List<T>` for simple cases
- Use collection expressions where possible
- Use nullable reference types appropriately

### Error Handling

- Use `WriteError()` with `ErrorRecord` for cmdlet errors
- Use `PSArgumentException` for invalid arguments
- Use `PSNotSupportedException` for unsupported operations
- Use `WriteWarning()` for non-critical issues
- Include meaningful error messages with context (use string.Format)

### PowerShell Cmdlet Patterns

Inherit from `PSCmdlet` for cmdlet base class:
```csharp
[Cmdlet(VerbsCommon.New, nameof(FileSystemWatcher), DefaultParameterSetName = nameof(Path))]
[OutputType(typeof(FileSystemWatcherState))]
public class NewFileSystemWatcherCommand : ModifyingFileSystemWatcherCommandBase
```

- Use `[Parameter]` attributes for parameter definition
- Use `WriteObject()` to return results
- Use `this.SessionState.Path` for path resolution
- Use `this.Events` for event subscription management
- Register cleanup in `BeginProcessing()` using module's `OnRemove` handler

### Resource Strings

- All user-facing strings go in `Resources.resx`
- Generated `Resources.Designer.cs` provides typed accessors
- Keys follow pattern: `Error_<MessageId>`, `Message_<MessageId>`
- Use `string.Format()` for parameterized messages

### Testing Patterns

- Tests use PowerShell SDK via `System.Management.Automation.PowerShell`
- Each test gets a clean directory via `Directory.CreateDirectory()`
- Cleanup in `Dispose()` method
- Use ARRANGE/ACT/ASSERT comments for clarity
- Use `Guid.NewGuid().ToString()` for unique source identifiers
- Tests must handle async events (use `Start-Sleep` or equivalent)

Example test structure:
```csharp
[Fact]
public void Notifies_on_created_file()
{
    // ARRANGE
    this.PowerShell.Commands.Clear();
    this.PowerShell.AddCommand("New-FileSystemWatcher")
        .AddParameter("Path", this.rootDirectory.FullName)
        .AddParameter("SourceIdentifier", this.sourceIdentifier)
        .Invoke();

    // ACT
    File.WriteAllText(this.ArrangeFilePath("test.txt"), "content");

    // ASSERT
    this.Sleep();
    False(this.PowerShell.HadErrors);
}
```

### Common Patterns

**Dictionary for global state:**
```csharp
protected static Dictionary<string, FileSystemWatcherSubscription> FileSystemWatchers { get; } = new Dictionary<string, FileSystemWatcherSubscription>();
```

**Throttle/Debounce delegates:**
```csharp
// Throttle: batches events at fixed intervals
// Debounce: waits for quiet period before delivering
private Action<FileSystemEventArgs> Throttle(Action<List<FileSystemEventArgs>> action, TimeSpan interval)
```

## Key Cmdlets

| Cmdlet | Description |
|--------|-------------|
| `New-FileSystemWatcher` | Creates a file system watcher |
| `Get-FileSystemWatcher` | Lists active watchers |
| `Suspend-FileSystemWatcher` | Pauses watching |
| `Resume-FileSystemWatcher` | Resumes watching |
| `Remove-FileSystemWatcher` | Stops and removes watcher |

## Important Notes

- The module automatically cleans up watchers on module removal and PowerShell exit
- `ThrottleMs` and `DebounceMs` cannot be combined
- Each watcher needs a unique `SourceIdentifier`
- Filters use .NET wildcard patterns (*, ?)
