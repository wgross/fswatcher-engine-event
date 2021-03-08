using System;
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
                .AddArgument(@".\FSWatcherEngineEvent.dll")
                .Invoke();
            this.PowerShell.Commands.Clear();

            // run each test with a clean directory
            this.rootDirectory = Directory.CreateDirectory($@".\{this.sourceIdentifier}");
        }

        public void Dispose()
        {
            this.rootDirectory.Delete(recursive: true);
            this.PowerShell.Commands.Clear();
            this.PowerShell.AddCommand("Remove-FileSystemWatcher").AddParameter("SourceIdentifier", this.sourceIdentifier).Invoke();
            //this.PowerShell.Dispose();
        }

        private readonly ScriptBlock SpyOnEvent = ScriptBlock.Create("$global:result = $event|ConvertTo-Json");
        private readonly string sourceIdentifier = Guid.NewGuid().ToString();
        private readonly DirectoryInfo rootDirectory;

        private string ArrangeFilePath(string fileName) => Path.Combine(this.rootDirectory.FullName, fileName);

        private void Sleep()
        {
            this.PowerShell.AddCommand("Start-Sleep").AddParameter("Seconds", 1).Invoke();
            this.PowerShell.Commands.Clear();
        }

        public PowerShell PowerShell { get; }

        private void ArrangeEngineEvent()
        {
            this.PowerShell
                .AddCommand("Register-EngineEvent")
                .AddParameter("SourceIdentifier", this.sourceIdentifier)
                .AddParameter("Action", this.SpyOnEvent)
                .Invoke();
            this.PowerShell.Commands.Clear();
        }

        [Fact]
        public void Notifies_on_created_file()
        {
            // ARRANGE
            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", this.rootDirectory.FullName)
                .AddParameter("SourceIdentifier", this.sourceIdentifier)
                .AddParameter("NotifyFilter", NotifyFilters.LastWrite)
                .Invoke();
            this.PowerShell.Commands.Clear();
            this.ArrangeEngineEvent();

            // ACT1
            File.WriteAllText(this.ArrangeFilePath("test.txt"), Guid.NewGuid().ToString());
            this.Sleep();

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);

            this.PowerShell.Commands.Clear();
            var result = this.PowerShell
                .AddCommand("Get-Variable")
                .AddParameter("Name", "result")
                .Invoke()
                .Single();

            Assert.IsType<PSVariable>(result.BaseObject);

            var resultValue = (PSObject)((PSVariable)result.BaseObject).Value;

            Assert.NotNull(resultValue);

            var eventJson = JsonSerializer.Deserialize<EventJson>(resultValue.ToString());

            Assert.Equal(WatcherChangeTypes.Changed, (WatcherChangeTypes)eventJson.MessageData.ChangeType);
            Assert.Equal(this.ArrangeFilePath("test.txt"), eventJson.MessageData.FullPath);
            Assert.Equal("test.txt", eventJson.MessageData.Name);
        }

        [Fact]
        public void Notifies_on_created_file_skipped_because_of_filter()
        {
            // ARRANGE
            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", this.rootDirectory.FullName)
                .AddParameter("SourceIdentifier", this.sourceIdentifier)
                .AddParameter("NotifyFilter", NotifyFilters.LastWrite)
                .AddParameter("Filter", "*.jpg")
                .Invoke();
            this.PowerShell.Commands.Clear();
            this.ArrangeEngineEvent();

            // ACT
            File.WriteAllText(this.ArrangeFilePath("test.txt"), Guid.NewGuid().ToString());
            this.Sleep();

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);

            this.PowerShell.Commands.Clear();
            var result = this.PowerShell
                .AddCommand("Get-Variable")
                .AddParameter("Name", "result")
                .Invoke()
                .ToArray();

            Assert.Empty(result);
        }

        [Fact]
        public void Notify_of_created_file_can_be_switched_off()
        {
            // ARRANGE
            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", this.rootDirectory.FullName)
                .AddParameter("SourceIdentifier", this.sourceIdentifier)
                .AddParameter("NotifyFilter", NotifyFilters.LastWrite)
                .Invoke();
            this.PowerShell.Commands.Clear();
            this.ArrangeEngineEvent();
            this.PowerShell
                .AddCommand("Remove-FileSystemWatcher")
                .AddParameter("SourceIdentifier", this.sourceIdentifier)
                .Invoke();
            this.PowerShell.Commands.Clear();

            // ACT
            File.WriteAllText(this.ArrangeFilePath("test.txt"), Guid.NewGuid().ToString());
            this.Sleep();

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);

            this.PowerShell.Commands.Clear();
            var result = this.PowerShell
                .AddCommand("Get-Variable")
                .AddParameter("Name", "result")
                .Invoke()
                .ToArray();

            Assert.Empty(result);
        }

        [Fact]
        public void Watching_fails_on_invalid_path()
        {
            // ACT
            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", "invalid-path")
                .AddParameter("SourceIdentifier", this.sourceIdentifier)
                .AddParameter("NotifyFilter", NotifyFilters.LastWrite)
                .Invoke();

            // ASSERT
            Assert.True(this.PowerShell.HadErrors);

            this.PowerShell.Commands.Clear();
            var result = this.PowerShell
                .AddCommand("Get-Variable")
                .AddParameter("Name", "result")
                .Invoke()
                .ToArray();

            Assert.Empty(result);
        }

        [Fact]
        public void Notifies_on_changed_file()
        {
            // ARRANGE
            File.WriteAllText(this.ArrangeFilePath("test.txt"), Guid.NewGuid().ToString());

            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", this.rootDirectory.FullName)
                .AddParameter("SourceIdentifier", this.sourceIdentifier)
                .AddParameter("NotifyFilter", NotifyFilters.LastWrite)
                .Invoke();
            this.PowerShell.Commands.Clear();
            this.ArrangeEngineEvent();

            // ACT
            File.WriteAllText(this.ArrangeFilePath("test.txt"), Guid.NewGuid().ToString());
            this.Sleep();

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);

            var result = this.PowerShell
                .AddCommand("Get-Variable")
                .AddParameter("Name", "result")
                .Invoke()
                .Single();

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

            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", this.rootDirectory.FullName)
                .AddParameter("SourceIdentifier", this.sourceIdentifier)
                .AddParameter("NotifyFilter", NotifyFilters.FileName)
                .Invoke();
            this.PowerShell.Commands.Clear();
            this.ArrangeEngineEvent();

            // ACT
            var file = new FileInfo(this.ArrangeFilePath("test.txt"));
            File.Move(this.ArrangeFilePath("test.txt"), this.ArrangeFilePath("test-changed.txt"));
            this.Sleep();

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);

            this.PowerShell.Commands.Clear();
            var result = this.PowerShell
                .AddCommand("Get-Variable")
                .AddParameter("Name", "result")
                .Invoke()
                .Single();

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

            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", this.rootDirectory.FullName)
                .AddParameter("SourceIdentifier", this.sourceIdentifier)
                .AddParameter("NotifyFilter", NotifyFilters.FileName)
                .Invoke();
            this.PowerShell.Commands.Clear();
            this.ArrangeEngineEvent();

            // ACT
            File.Delete(this.ArrangeFilePath("test.txt"));
            this.Sleep();

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);

            var result = this.PowerShell
                .AddCommand("Get-Variable")
                .AddParameter("Name", "result")
                .Invoke()
                .Single();

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
            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", this.rootDirectory.FullName)
                .AddParameter("SourceIdentifier", this.sourceIdentifier)
                .AddParameter("IncludeSubdirectories")
                .AddParameter("NotifyFilter", NotifyFilters.DirectoryName)
                .Invoke();
            this.PowerShell.Commands.Clear();
            this.ArrangeEngineEvent();

            // ACT
            var subdir = Directory.CreateDirectory(this.ArrangeFilePath("subdir"));
            this.Sleep();

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);

            var result = this.PowerShell
                .AddCommand("Get-Variable")
                .AddParameter("Name", "result")
                .Invoke()
                .Single();

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

            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", this.rootDirectory.FullName)
                .AddParameter("SourceIdentifier", this.sourceIdentifier)
                .AddParameter("IncludeSubdirectories")
                .AddParameter("NotifyFilter", NotifyFilters.DirectoryName)
                .Invoke();
            this.PowerShell.Commands.Clear();
            this.ArrangeEngineEvent();

            // ACT
            subdir.Delete();
            this.Sleep();

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);

            var result = this.PowerShell
                .AddCommand("Get-Variable")
                .AddParameter("Name", "result")
                .Invoke()
                .Single();

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

            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", this.rootDirectory.FullName)
                .AddParameter("SourceIdentifier", this.sourceIdentifier)
                .AddParameter("IncludeSubdirectories")
                .AddParameter("NotifyFilter", NotifyFilters.DirectoryName)
                .Invoke();
            this.PowerShell.Commands.Clear();
            this.ArrangeEngineEvent();

            // ACT
            Directory.Move(subdir.FullName, this.ArrangeFilePath("subdir-changed"));
            this.Sleep();

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);

            var result = this.PowerShell
                .AddCommand("Get-Variable")
                .AddParameter("Name", "result")
                .Invoke()
                .Single();

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