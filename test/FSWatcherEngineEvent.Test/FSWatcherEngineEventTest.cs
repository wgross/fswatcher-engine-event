using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text.Json;
using Xunit;

namespace FSWatcherEngineEvent.Test
{
    public class FSWatcherEngineEventTest
    {
        public FSWatcherEngineEventTest()
        {
            this.PowerShell = PowerShell.Create();

            this.PowerShell.AddCommand("Import-Module")
                .AddArgument(@".\FSWatcher.dll")
                .Invoke();
            this.PowerShell.Commands.Clear();

            // run each test with a clean directory
            if (Directory.Exists(@".\watched"))
                Directory.Delete(@".\watched", recursive: true);
            if (Directory.Exists(@".\watched"))
                Directory.CreateDirectory(@".\watched");
        }

        private readonly ScriptBlock SpyOnEvent = ScriptBlock.Create("$global:result = $event|ConvertTo-Json");

        public PowerShell PowerShell { get; }

        [Fact]
        public void Notifies_on_created_file()
        {
            // ARRANGE
            var directory = Directory.CreateDirectory(@".\watched");

            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", directory.FullName)
                .AddParameter("SourceIdentifier", "sourceIdentifier")
                .AddParameter("Filters", NotifyFilters.LastWrite)
                .Invoke();
            this.PowerShell.Commands.Clear();
            this.PowerShell
                .AddCommand("Register-EngineEvent")
                .AddParameter("SourceIdentifier", "sourceIdentifier")
                .AddParameter("Action", this.SpyOnEvent)
                .Invoke();
            this.PowerShell.Commands.Clear();

            // ACT
            File.WriteAllText(@".\watched\test.txt", Guid.NewGuid().ToString());

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);

            this.PowerShell
                .AddCommand("Start-Sleep").AddParameter("Seconds", 1)
                .Invoke();
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
            Assert.Equal(Path.Join(directory.FullName, "test.txt"), eventJson.MessageData.FullPath);
            Assert.Equal("test.txt", eventJson.MessageData.Name);
        }

        [Fact]
        public void Notifies_on_changed_file()
        {
            // ARRANGE
            var directory = Directory.CreateDirectory(@".\watched");
            File.WriteAllText(@".\watched\test.txt", Guid.NewGuid().ToString());

            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", directory.FullName)
                .AddParameter("SourceIdentifier", "sourceIdentifier")
                .AddParameter("Filters", NotifyFilters.LastWrite)
                .Invoke();
            this.PowerShell.Commands.Clear();
            this.PowerShell
                .AddCommand("Register-EngineEvent")
                .AddParameter("SourceIdentifier", "sourceIdentifier")
                .AddParameter("Action", this.SpyOnEvent)
                .Invoke();
            this.PowerShell.Commands.Clear();

            // ACT
            File.WriteAllText(@".\watched\test.txt", Guid.NewGuid().ToString());

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);

            this.PowerShell
                .AddCommand("Start-Sleep").AddParameter("Seconds", 1)
                .Invoke();
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
            Assert.Equal(Path.Join(directory.FullName, "test.txt"), eventJson.MessageData.FullPath);
            Assert.Equal("test.txt", eventJson.MessageData.Name);
        }

        [Fact]
        public void Notifies_on_renamed_file()
        {
            // ARRANGE
            var directory = Directory.CreateDirectory(@".\watched");
            File.WriteAllText(@".\watched\test.txt", Guid.NewGuid().ToString());

            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", directory.FullName)
                .AddParameter("SourceIdentifier", "sourceIdentifier")
                .AddParameter("Filters", NotifyFilters.FileName)
                .Invoke();
            this.PowerShell.Commands.Clear();
            this.PowerShell
                .AddCommand("Register-EngineEvent")
                .AddParameter("SourceIdentifier", "sourceIdentifier")
                .AddParameter("Action", this.SpyOnEvent)
                .Invoke();
            this.PowerShell.Commands.Clear();

            // ACT
            var file = new FileInfo(@".\watched\test.txt");
            File.Move(@".\watched\test.txt", @".\watched\test-changed.txt");

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);

            this.PowerShell
                .AddCommand("Start-Sleep").AddParameter("Seconds", 1)
                .Invoke();
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
            Assert.Equal(Path.Join(directory.FullName, "test-changed.txt"), eventJson.MessageData.FullPath);
            Assert.Equal("test-changed.txt", eventJson.MessageData.Name);
            Assert.Equal(Path.Join(directory.FullName, "test.txt"), eventJson.MessageData.OldFullPath);
            Assert.Equal("test.txt", eventJson.MessageData.OldName);
        }

        [Fact]
        public void Notifies_on_deleted_file()
        {
            // ARRANGE
            var directory = Directory.CreateDirectory(@".\watched");
            File.WriteAllText(@".\watched\test.txt", Guid.NewGuid().ToString());

            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", directory.FullName)
                .AddParameter("SourceIdentifier", "sourceIdentifier")
                .AddParameter("Filters", NotifyFilters.FileName)
                .Invoke();
            this.PowerShell.Commands.Clear();
            this.PowerShell
                .AddCommand("Register-EngineEvent")
                .AddParameter("SourceIdentifier", "sourceIdentifier")
                .AddParameter("Action", this.SpyOnEvent)
                .Invoke();
            this.PowerShell.Commands.Clear();

            // ACT
            File.Delete(@".\watched\test.txt");

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);

            this.PowerShell
                .AddCommand("Start-Sleep").AddParameter("Seconds", 1)
                .Invoke();
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

            Assert.Equal(WatcherChangeTypes.Deleted, (WatcherChangeTypes)eventJson.MessageData.ChangeType);
            Assert.Equal(Path.Join(directory.FullName, "test.txt"), eventJson.MessageData.FullPath);
            Assert.Equal("test.txt", eventJson.MessageData.Name);
        }

        [Fact]
        public void Notifies_on_created_directory()
        {
            // ARRANGE
            var directory = Directory.CreateDirectory(@".\watched");

            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", directory.FullName)
                .AddParameter("SourceIdentifier", "sourceIdentifier")
                .AddParameter("IncludeSubdirectories")
                .AddParameter("Filters", NotifyFilters.DirectoryName)
                .Invoke();
            this.PowerShell.Commands.Clear();
            this.PowerShell
                .AddCommand("Register-EngineEvent")
                .AddParameter("SourceIdentifier", "sourceIdentifier")
                .AddParameter("Action", this.SpyOnEvent)
                .Invoke();
            this.PowerShell.Commands.Clear();

            // ACT
            var subdir = Directory.CreateDirectory(Path.Join(directory.FullName, "subdir"));

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);

            this.PowerShell
                .AddCommand("Start-Sleep").AddParameter("Seconds", 1)
                .Invoke();
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

            Assert.Equal(WatcherChangeTypes.Created, (WatcherChangeTypes)eventJson.MessageData.ChangeType);
            Assert.Equal(subdir.FullName, eventJson.MessageData.FullPath);
            Assert.Equal(subdir.Name, eventJson.MessageData.Name);
        }

        [Fact]
        public void Notifies_on_deleted_directory()
        {
            // ARRANGE
            var directory = Directory.CreateDirectory(@".\watched");
            var subdir = Directory.CreateDirectory(Path.Join(directory.FullName, "subdir"));

            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", directory.FullName)
                .AddParameter("SourceIdentifier", "sourceIdentifier")
                .AddParameter("IncludeSubdirectories")
                .AddParameter("Filters", NotifyFilters.DirectoryName)
                .Invoke();
            this.PowerShell.Commands.Clear();
            this.PowerShell
                .AddCommand("Register-EngineEvent")
                .AddParameter("SourceIdentifier", "sourceIdentifier")
                .AddParameter("Action", this.SpyOnEvent)
                .Invoke();
            this.PowerShell.Commands.Clear();

            // ACT
            subdir.Delete();

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);

            this.PowerShell
                .AddCommand("Start-Sleep").AddParameter("Seconds", 1)
                .Invoke();
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

            Assert.Equal(WatcherChangeTypes.Deleted, (WatcherChangeTypes)eventJson.MessageData.ChangeType);
            Assert.Equal(subdir.FullName, eventJson.MessageData.FullPath);
            Assert.Equal(subdir.Name, eventJson.MessageData.Name);
        }

        [Fact]
        public void Notifies_on_renamed_directory()
        {
            // ARRANGE
            var directory = Directory.CreateDirectory(@".\watched");
            var subdir = Directory.CreateDirectory(Path.Join(directory.FullName, "subdir"));

            this.PowerShell
                .AddCommand("New-FileSystemWatcher")
                .AddParameter("Path", directory.FullName)
                .AddParameter("SourceIdentifier", "sourceIdentifier")
                .AddParameter("IncludeSubdirectories")
                .AddParameter("Filters", NotifyFilters.DirectoryName)
                .Invoke();
            this.PowerShell.Commands.Clear();
            this.PowerShell
                .AddCommand("Register-EngineEvent")
                .AddParameter("SourceIdentifier", "sourceIdentifier")
                .AddParameter("Action", this.SpyOnEvent)
                .Invoke();
            this.PowerShell.Commands.Clear();

            // ACT
            Directory.Move(subdir.FullName, Path.Join(directory.FullName, "subdir-changed"));

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);

            this.PowerShell
                .AddCommand("Start-Sleep").AddParameter("Seconds", 1)
                .Invoke();
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
            Assert.Equal(Path.Join(directory.FullName, "subdir-changed"), eventJson.MessageData.FullPath);
            Assert.Equal("subdir-changed", eventJson.MessageData.Name);
            Assert.Equal(Path.Join(directory.FullName, "subdir"), eventJson.MessageData.OldFullPath);
            Assert.Equal("subdir", eventJson.MessageData.OldName);
        }
    }
}