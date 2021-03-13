using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text.Json;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace FSWatcherEngineEvent.Test
{
    [Collection("powershell")]
    public class FSWatcherEngineEventTest : IDisposable
    {
        public FSWatcherEngineEventTest()
        {
            this.PowerShell = PowerShell.Create();

            this.PowerShell.AddCommand("Import-Module")
                .AddArgument(@".\FSWatcherEngineEvent.psd1")
                .Invoke();
            this.PowerShell.Commands.Clear();

            // run each test with a clean directory
            this.rootDirectory = Directory.CreateDirectory($@".\{this.sourceIdentifier}");
        }

        public void Dispose()
        {
            this.rootDirectory.Delete(recursive: true);
            this.RemoveFileSystemWatcher();
            //this.PowerShell.Dispose();
        }

        private readonly ScriptBlock SpyOnEvent = ScriptBlock.Create("$global:result = $event|ConvertTo-Json");
        private readonly string sourceIdentifier = Guid.NewGuid().ToString();
        private readonly DirectoryInfo rootDirectory;

        private string ArrangeFilePath(string fileName) => Path.Combine(this.rootDirectory.FullName, fileName);

        private void RemoveFileSystemWatcher()
        {
            this.PowerShell.Commands.Clear();
            this.PowerShell.AddCommand("Unregister-Event").AddParameter("SourceIdentifier", this.sourceIdentifier).Invoke();
            this.PowerShell.Commands.Clear();
            this.PowerShell.AddCommand("Remove-FileSystemWatcher").AddParameter("SourceIdentifier", this.sourceIdentifier).Invoke();
        }

        private void Sleep()
        {
            this.PowerShell.Commands.Clear();
            this.PowerShell.AddCommand("Start-Sleep").AddParameter("Seconds", 1).Invoke();
        }

        public PowerShell PowerShell { get; }

        private void ArrangeEngineEvent()
        {
            this.PowerShell.Commands.Clear();
            this.PowerShell
                .AddCommand("Register-EngineEvent")
                .AddParameter("SourceIdentifier", this.sourceIdentifier)
                .AddParameter("Action", this.SpyOnEvent)
                .Invoke();
        }

        private Collection<PSObject> ReadResultVariable()
        {
            this.PowerShell.Commands.Clear();
            return this.PowerShell
                .AddCommand("Get-Variable")
                .AddParameter("Name", "result")
                .Invoke();
        }

        [Fact]
        public void Notifies_on_created_file()
        {
            // ARRANGE
            this.PowerShell.Commands.Clear();
            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", this.rootDirectory.FullName)
                .AddParameter("SourceIdentifier", this.sourceIdentifier)
                .AddParameter("NotifyFilter", NotifyFilters.LastWrite)
                .Invoke();

            this.ArrangeEngineEvent();

            // ACT
            File.WriteAllText(this.ArrangeFilePath("test.txt"), Guid.NewGuid().ToString());

            // ASSERT
            this.Sleep();
            this.RemoveFileSystemWatcher();

            Assert.False(this.PowerShell.HadErrors);

            PSObject result = this.ReadResultVariable().Single();

            Assert.IsType<PSVariable>(result.BaseObject);

            var resultValue = (PSObject)((PSVariable)result.BaseObject).Value;

            Assert.NotNull(resultValue);

            var eventJson = JsonSerializer.Deserialize<EventJson>(resultValue.ToString());

            Assert.Equal(WatcherChangeTypes.Changed, (WatcherChangeTypes)eventJson.MessageData.ChangeType);
            Assert.Equal(this.ArrangeFilePath("test.txt"), eventJson.MessageData.FullPath);
            Assert.Equal("test.txt", eventJson.MessageData.Name);
        }

        [Fact]
        public void Reads_file_system_watcher_state()
        {
            // ARRANGE
            this.PowerShell.Commands.Clear();
            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", this.rootDirectory.FullName)
                .AddParameter("SourceIdentifier", this.sourceIdentifier)
                .AddParameter("NotifyFilter", NotifyFilters.LastWrite)
                .Invoke();

            this.ArrangeEngineEvent();

            // ACT

            this.PowerShell.Commands.Clear();
            var result = this.PowerShell
                .AddCommand("Get-FileSystemWatcher")
                .Invoke()
                .Single();

            // ASSERT
            this.RemoveFileSystemWatcher();

            Assert.False(this.PowerShell.HadErrors);

            Assert.IsType<FileSystemWatcherState>(result.BaseObject);
            Assert.Equal(this.rootDirectory.FullName, result.Property<string>(nameof(FileSystemWatcherState.Path)));
            Assert.Equal(this.sourceIdentifier, result.Property<string>(nameof(FileSystemWatcherState.SourceIdentifier)));
            Assert.Equal(NotifyFilters.LastWrite, result.Property<NotifyFilters>(nameof(FileSystemWatcherState.NotifyFilter)));
            Assert.Equal("*", result.Property<string>(nameof(FileSystemWatcherState.Filter)));
            Assert.True(result.Property<bool>(nameof(FileSystemWatcherState.EnableRaisingEvents)));
            Assert.False(result.Property<bool>(nameof(FileSystemWatcherState.IncludeSubdirectories)));
        }

        [Fact]
        public void Notifies_on_created_file_skipped_because_of_filter()
        {
            // ARRANGE
            this.PowerShell.Commands.Clear();
            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", this.rootDirectory.FullName)
                .AddParameter("SourceIdentifier", this.sourceIdentifier)
                .AddParameter("NotifyFilter", NotifyFilters.LastWrite)
                .AddParameter("Filter", "*.jpg")
                .Invoke();

            this.ArrangeEngineEvent();

            // ACT
            File.WriteAllText(this.ArrangeFilePath("test.txt"), Guid.NewGuid().ToString());

            // ASSERT
            this.Sleep();
            this.RemoveFileSystemWatcher();

            Assert.False(this.PowerShell.HadErrors);

            var result = this.ReadResultVariable().ToArray();

            Assert.Empty(result);
        }

        [Fact]
        public void Notify_of_created_file_can_be_switched_off()
        {
            // ARRANGE
            this.PowerShell.Commands.Clear();
            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", this.rootDirectory.FullName)
                .AddParameter("SourceIdentifier", this.sourceIdentifier)
                .AddParameter("NotifyFilter", NotifyFilters.LastWrite)
                .Invoke();

            this.ArrangeEngineEvent();

            this.PowerShell.Commands.Clear();
            this.PowerShell.AddCommand("Remove-FileSystemWatcher").AddParameter("SourceIdentifier", this.sourceIdentifier).Invoke();

            // ACT
            File.WriteAllText(this.ArrangeFilePath("test.txt"), Guid.NewGuid().ToString());

            // ASSERT
            this.Sleep();

            this.PowerShell.Commands.Clear();
            this.PowerShell.AddCommand("Unregister-Event").AddParameter("SourceIdentifier", this.sourceIdentifier).Invoke();

            Assert.False(this.PowerShell.HadErrors);

            var result = this.ReadResultVariable();

            Assert.Empty(result);
        }

        [Fact]
        public void Notify_of_created_file_can_be_suspended()
        {
            // ARRANGE
            this.PowerShell.Commands.Clear();
            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", this.rootDirectory.FullName)
                .AddParameter("SourceIdentifier", this.sourceIdentifier)
                .AddParameter("NotifyFilter", NotifyFilters.LastWrite)
                .Invoke();

            this.ArrangeEngineEvent();

            // ACT
            this.PowerShell.Commands.Clear();
            this.PowerShell.AddCommand("Suspend-FileSystemWatcher").AddParameter("SourceIdentifier", this.sourceIdentifier).Invoke();

            File.WriteAllText(this.ArrangeFilePath("test.txt"), Guid.NewGuid().ToString());

            // ASSERT
            this.Sleep();

            this.PowerShell.Commands.Clear();
            this.PowerShell.AddCommand("Unregister-Event").AddParameter("SourceIdentifier", this.sourceIdentifier).Invoke();

            Assert.False(this.PowerShell.HadErrors);

            var result = this.ReadResultVariable();

            Assert.Empty(result);
        }

        [Fact]
        public void Notify_of_created_file_can_be_resumed()
        {
            // ARRANGE
            this.PowerShell.Commands.Clear();
            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", this.rootDirectory.FullName)
                .AddParameter("SourceIdentifier", this.sourceIdentifier)
                .AddParameter("NotifyFilter", NotifyFilters.LastWrite)
                .Invoke();

            this.ArrangeEngineEvent();

            this.PowerShell.Commands.Clear();
            this.PowerShell.AddCommand("Suspend-FileSystemWatcher").AddParameter("SourceIdentifier", this.sourceIdentifier).Invoke();

            // ACT
            this.PowerShell.Commands.Clear();
            this.PowerShell.AddCommand("Resume-FileSystemWatcher").AddParameter("SourceIdentifier", this.sourceIdentifier).Invoke();

            File.WriteAllText(this.ArrangeFilePath("test.txt"), Guid.NewGuid().ToString());

            // ASSERT
            this.Sleep();

            this.PowerShell.Commands.Clear();
            this.PowerShell.AddCommand("Unregister-Event").AddParameter("SourceIdentifier", this.sourceIdentifier).Invoke();

            Assert.False(this.PowerShell.HadErrors);

            PSObject result = this.ReadResultVariable().Single();

            Assert.IsType<PSVariable>(result.BaseObject);

            var resultValue = (PSObject)((PSVariable)result.BaseObject).Value;

            Assert.NotNull(resultValue);

            var eventJson = JsonSerializer.Deserialize<EventJson>(resultValue.ToString());

            Assert.Equal(WatcherChangeTypes.Changed, (WatcherChangeTypes)eventJson.MessageData.ChangeType);
            Assert.Equal(this.ArrangeFilePath("test.txt"), eventJson.MessageData.FullPath);
            Assert.Equal("test.txt", eventJson.MessageData.Name);
        }

        [Fact]
        public void Watching_fails_on_invalid_path()
        {
            // ACT
            this.PowerShell.Commands.Clear();
            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", "invalid-path")
                .AddParameter("SourceIdentifier", this.sourceIdentifier)
                .AddParameter("NotifyFilter", NotifyFilters.LastWrite)
                .Invoke();

            // ASSERT
            Assert.True(this.PowerShell.HadErrors);

            var result = this.ReadResultVariable().ToArray();

            Assert.Empty(result);
        }

        [Fact]
        public void Notifies_on_changed_file()
        {
            // ARRANGE
            File.WriteAllText(this.ArrangeFilePath("test.txt"), Guid.NewGuid().ToString());

            this.PowerShell.Commands.Clear();
            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", this.rootDirectory.FullName)
                .AddParameter("SourceIdentifier", this.sourceIdentifier)
                .AddParameter("NotifyFilter", NotifyFilters.LastWrite)
                .Invoke();

            this.ArrangeEngineEvent();

            // ACT
            File.WriteAllText(this.ArrangeFilePath("test.txt"), Guid.NewGuid().ToString());

            // ASSERT
            this.Sleep();
            this.RemoveFileSystemWatcher();

            Assert.False(this.PowerShell.HadErrors);

            var result = this.ReadResultVariable().Single();

            Assert.IsType<PSVariable>(result.BaseObject);

            var resultValue = (PSObject)((PSVariable)result.BaseObject).Value;

            Assert.NotNull(resultValue);

            var eventJson = JsonSerializer.Deserialize<EventJson>(resultValue.ToString());

            Assert.Equal(WatcherChangeTypes.Changed, (WatcherChangeTypes)eventJson.MessageData.ChangeType);
            Assert.Equal(this.ArrangeFilePath("test.txt"), eventJson.MessageData.FullPath);
            Assert.Equal("test.txt", eventJson.MessageData.Name);
        }

        [Fact]
        public void Notifies_on_renamed_file()
        {
            // ARRANGE
            File.WriteAllText(this.ArrangeFilePath("test.txt"), Guid.NewGuid().ToString());

            this.PowerShell.Commands.Clear();
            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", this.rootDirectory.FullName)
                .AddParameter("SourceIdentifier", this.sourceIdentifier)
                .AddParameter("NotifyFilter", NotifyFilters.FileName)
                .Invoke();

            this.ArrangeEngineEvent();

            // ACT
            var file = new FileInfo(this.ArrangeFilePath("test.txt"));
            File.Move(this.ArrangeFilePath("test.txt"), this.ArrangeFilePath("test-changed.txt"));

            // ASSERT
            this.Sleep();
            this.RemoveFileSystemWatcher();

            Assert.False(this.PowerShell.HadErrors);

            var result = this.ReadResultVariable().Single();

            Assert.IsType<PSVariable>(result.BaseObject);

            var resultValue = (PSObject)((PSVariable)result.BaseObject).Value;

            Assert.NotNull(resultValue);

            var eventJson = JsonSerializer.Deserialize<EventJson>(resultValue.ToString());

            Assert.Equal(WatcherChangeTypes.Renamed, (WatcherChangeTypes)eventJson.MessageData.ChangeType);
            Assert.Equal(this.ArrangeFilePath("test-changed.txt"), eventJson.MessageData.FullPath);
            Assert.Equal("test-changed.txt", eventJson.MessageData.Name);
            Assert.Equal(this.ArrangeFilePath("test.txt"), eventJson.MessageData.OldFullPath);
            Assert.Equal("test.txt", eventJson.MessageData.OldName);
        }

        [Fact]
        public void Notifies_on_deleted_file()
        {
            // ARRANGE
            File.WriteAllText(this.ArrangeFilePath("test.txt"), Guid.NewGuid().ToString());

            this.PowerShell.Commands.Clear();
            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", this.rootDirectory.FullName)
                .AddParameter("SourceIdentifier", this.sourceIdentifier)
                .AddParameter("NotifyFilter", NotifyFilters.FileName)
                .Invoke();

            this.ArrangeEngineEvent();

            // ACT
            File.Delete(this.ArrangeFilePath("test.txt"));

            // ASSERT
            this.Sleep();
            this.RemoveFileSystemWatcher();

            Assert.False(this.PowerShell.HadErrors);

            var result = this.ReadResultVariable().Single();

            Assert.IsType<PSVariable>(result.BaseObject);

            var resultValue = (PSObject)((PSVariable)result.BaseObject).Value;

            Assert.NotNull(resultValue);

            var eventJson = JsonSerializer.Deserialize<EventJson>(resultValue.ToString());

            Assert.Equal(WatcherChangeTypes.Deleted, (WatcherChangeTypes)eventJson.MessageData.ChangeType);
            Assert.Equal(this.ArrangeFilePath("test.txt"), eventJson.MessageData.FullPath);
            Assert.Equal("test.txt", eventJson.MessageData.Name);
        }

        [Fact]
        public void Notifies_on_created_directory()
        {
            // ARRANGE
            this.PowerShell.Commands.Clear();
            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", this.rootDirectory.FullName)
                .AddParameter("SourceIdentifier", this.sourceIdentifier)
                .AddParameter("IncludeSubdirectories")
                .AddParameter("NotifyFilter", NotifyFilters.DirectoryName)
                .Invoke();

            this.ArrangeEngineEvent();

            // ACT
            var subdir = Directory.CreateDirectory(this.ArrangeFilePath("subdir"));

            // ASSERT
            this.Sleep();
            this.RemoveFileSystemWatcher();

            Assert.False(this.PowerShell.HadErrors);

            var result = this.ReadResultVariable().Single();

            Assert.IsType<PSVariable>(result.BaseObject);

            var resultValue = (PSObject)((PSVariable)result.BaseObject).Value;

            Assert.NotNull(resultValue);

            var eventJson = JsonSerializer.Deserialize<EventJson>(resultValue.ToString());

            Assert.Equal(WatcherChangeTypes.Created, (WatcherChangeTypes)eventJson.MessageData.ChangeType);
            Assert.Equal(subdir.FullName, eventJson.MessageData.FullPath);
            Assert.Equal(subdir.Name, eventJson.MessageData.Name);
        }

        [Fact]
        public void Notifies_on_deleted_directory()
        {
            // ARRANGE
            var subdir = Directory.CreateDirectory(this.ArrangeFilePath("subdir"));

            this.PowerShell.Commands.Clear();
            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", this.rootDirectory.FullName)
                .AddParameter("SourceIdentifier", this.sourceIdentifier)
                .AddParameter("IncludeSubdirectories")
                .AddParameter("NotifyFilter", NotifyFilters.DirectoryName)
                .Invoke();

            this.ArrangeEngineEvent();

            // ACT
            subdir.Delete();

            // ASSERT
            this.Sleep();
            this.RemoveFileSystemWatcher();

            Assert.False(this.PowerShell.HadErrors);

            var result = this.ReadResultVariable().Single();

            Assert.IsType<PSVariable>(result.BaseObject);

            var resultValue = (PSObject)((PSVariable)result.BaseObject).Value;

            Assert.NotNull(resultValue);

            var eventJson = JsonSerializer.Deserialize<EventJson>(resultValue.ToString());

            Assert.Equal(WatcherChangeTypes.Deleted, (WatcherChangeTypes)eventJson.MessageData.ChangeType);
            Assert.Equal(subdir.FullName, eventJson.MessageData.FullPath);
            Assert.Equal(subdir.Name, eventJson.MessageData.Name);
        }

        [Fact]
        public void Notifies_on_renamed_directory()
        {
            // ARRANGE
            var subdir = Directory.CreateDirectory(this.ArrangeFilePath("subdir"));

            this.PowerShell.Commands.Clear();
            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", this.rootDirectory.FullName)
                .AddParameter("SourceIdentifier", this.sourceIdentifier)
                .AddParameter("IncludeSubdirectories")
                .AddParameter("NotifyFilter", NotifyFilters.DirectoryName)
                .Invoke();

            this.ArrangeEngineEvent();

            // ACT
            Directory.Move(subdir.FullName, this.ArrangeFilePath("subdir-changed"));

            // ASSERT
            this.Sleep();
            this.RemoveFileSystemWatcher();

            Assert.False(this.PowerShell.HadErrors);

            var result = this.ReadResultVariable().Single();

            Assert.IsType<PSVariable>(result.BaseObject);

            var resultValue = (PSObject)((PSVariable)result.BaseObject).Value;

            Assert.NotNull(resultValue);

            var eventJson = JsonSerializer.Deserialize<EventJson>(resultValue.ToString());

            Assert.Equal(WatcherChangeTypes.Renamed, (WatcherChangeTypes)eventJson.MessageData.ChangeType);
            Assert.Equal(this.ArrangeFilePath("subdir-changed"), eventJson.MessageData.FullPath);
            Assert.Equal("subdir-changed", eventJson.MessageData.Name);
            Assert.Equal(this.ArrangeFilePath("subdir"), eventJson.MessageData.OldFullPath);
            Assert.Equal("subdir", eventJson.MessageData.OldName);
        }
    }
}