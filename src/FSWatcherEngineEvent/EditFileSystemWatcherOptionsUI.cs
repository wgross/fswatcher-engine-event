using NStack;
using System.Globalization;
using System.IO;
using Terminal.Gui;

namespace FSWatcherEngineEvent
{
    public class FileSystemWatcherOptions
    {
        public string Path { get; set; }

        public string Filter { get; set; }

        public NotifyFilters NotifyFilter { get; set; }

        public bool IncludeSubdirectories { get; set; }
        public int ThrottleMs { get; internal set; }
        public int DebounceMs { get; internal set; }
    }

    public class EditFileSystemWatcherOptionsUI
    {
        private TextField filterText;
        private CheckBox recurse;
        private CheckBox fileName;
        private CheckBox directory;
        private CheckBox attributes;
        private CheckBox size;
        private CheckBox lastWrite;
        private CheckBox lastAccess;
        private CheckBox creationTime;
        private CheckBox security;
        private FileSystemWatcherOptions options;
        private bool isOk;
        private TextField pathText;
        private Window win;
        private Label labelPath;
        private Label labelFilter;
        private Label labelNotifyOn;
        private TextField throttleMs;
        private Button ok;
        private Button cancel;
        private RadioGroup delayEventRadio;
        private Label labelThrottleMs;
        private TextField debounceMs;
        private Label labelDebounceMs;

        public bool Run(FileSystemWatcherOptions options)
        {
            this.options = options;

            Application.Init();
            Application.Run(this.CreateTopLevel());
            Application.Shutdown();

            return this.isOk;
        }

        private Toplevel CreateTopLevel()
        {
            var top = new Toplevel()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            top.Add(CreateWindow());
            return top;
        }

        private Window CreateWindow()
        {
            this.win = new Window(Resources.OptionsUI_Title)
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill() - 1
            };

            this.labelPath = new Label(Resources.OptionsUI_PathLabel);
            this.labelPath.X = Pos.Left(this.win) + 2;
            this.labelPath.Y = Pos.Top(this.win) + 1;

            this.pathText = new TextField(this.options.Path);
            this.pathText.Enabled = false;
            this.pathText.X = Pos.Left(this.labelPath) + 30;
            this.pathText.Y = Pos.Top(this.labelPath);
            this.pathText.Width = Dim.Sized(30);

            this.labelFilter = new Label(Resources.OptionsUI_FilterWildCardLabel);
            this.labelFilter.X = Pos.Left(this.win) + 2;
            this.labelFilter.Y = Pos.Top(this.labelPath) + 2;

            this.filterText = new TextField(this.options.Filter);
            this.filterText.X = Pos.Left(this.labelFilter) + 30;
            this.filterText.Y = Pos.Top(this.labelFilter);
            this.filterText.Width = Dim.Sized(30);

            this.recurse = new CheckBox(Resources.OptionsUI_IncludeSubDirs, this.options.IncludeSubdirectories);
            this.recurse.X = Pos.Left(this.win) + 2;
            this.recurse.Y = Pos.Top(this.labelFilter) + 2;

            this.labelNotifyOn = new Label(Resources.OptionsUI_NotifyOnHeader);
            this.labelNotifyOn.X = Pos.Left(this.win) + 2;
            this.labelNotifyOn.Y = Pos.Top(this.recurse) + 2;

            this.fileName = new CheckBox(Resources.OptionsUI_NotifyOnFileName, this.options.NotifyFilter.HasFlag(NotifyFilters.FileName));
            this.fileName.X = Pos.Left(this.win) + 2;
            this.fileName.Y = Pos.Top(this.labelNotifyOn) + 1;

            this.directory = new CheckBox(Resources.OptionsUI_NotifyOnDirectory, this.options.NotifyFilter.HasFlag(NotifyFilters.DirectoryName));
            this.directory.X = Pos.Left(this.win) + 2;
            this.directory.Y = Pos.Top(this.fileName) + 1;

            this.attributes = new CheckBox(Resources.OptionsUI_NotifyOnAttributes, this.options.NotifyFilter.HasFlag(NotifyFilters.Attributes));
            this.attributes.X = Pos.Left(this.win) + 2;
            this.attributes.Y = Pos.Top(this.directory) + 1;

            this.size = new CheckBox(Resources.OptionsUI_NotifyOnSize, this.options.NotifyFilter.HasFlag(NotifyFilters.Size));
            this.size.X = Pos.Left(this.win) + 2;
            this.size.Y = Pos.Top(this.attributes) + 1;

            this.lastWrite = new CheckBox(Resources.OptionsUI_NotifyOnLastWrite, this.options.NotifyFilter.HasFlag(NotifyFilters.LastWrite));
            this.lastWrite.X = Pos.Left(this.win) + 2;
            this.lastWrite.Y = Pos.Top(this.size) + 1;

            this.lastAccess = new CheckBox(Resources.OptionsUI_NotifyOnLastAccess, this.options.NotifyFilter.HasFlag(NotifyFilters.LastAccess));
            this.lastAccess.X = Pos.Left(this.win) + 2;
            this.lastAccess.Y = Pos.Top(this.lastWrite) + 1;

            this.creationTime = new CheckBox(Resources.OptionsUI_NotifyOnCreationTime, this.options.NotifyFilter.HasFlag(NotifyFilters.CreationTime));
            this.creationTime.X = Pos.Left(this.win) + 2;
            creationTime.Y = Pos.Top(this.lastAccess) + 1;

            this.security = new CheckBox(Resources.OptionsUI_NotifyOnSecurity, this.options.NotifyFilter.HasFlag(NotifyFilters.Security));
            this.security.X = Pos.Left(this.win) + 2;
            this.security.Y = Pos.Top(this.creationTime) + 1;

            this.delayEventRadio = new RadioGroup(new ustring[]
            {
                Resources.OptionsUI_ThrottleMs,
                Resources.OptionsUI_DebounceMs
            });
            this.delayEventRadio.X = Pos.Left(this.win) + 2;
            this.delayEventRadio.Y = Pos.Top(this.security) + 2;

            this.throttleMs = new TextField("");
            this.throttleMs.X = Pos.Right(this.delayEventRadio) + 1;
            this.throttleMs.Y = Pos.Top(this.delayEventRadio);
            this.throttleMs.Width = 6;
            this.throttleMs.KeyPress += this.ThrottleMs_KeyPress;

            this.debounceMs = new TextField("");
            this.debounceMs.X = Pos.Right(this.delayEventRadio) + 1;
            this.debounceMs.Y = Pos.Top(this.delayEventRadio) + 1;
            this.debounceMs.Width = 6;
            this.debounceMs.KeyPress += this.DebounceMs_KeyPress;

            this.ok = new Button(Resources.OptionsUI_Ok);
            this.ok.X = 10;
            this.ok.Y = Pos.Bottom(this.delayEventRadio) + 2;
            this.ok.Clicked += this.OnOk;

            this.cancel = new Button(Resources.OptionsUI_Cancel);
            this.cancel.X = 25;
            this.cancel.Y = Pos.Bottom(this.delayEventRadio) + 2;
            this.cancel.Clicked += this.OnCancel;

            win.Add(
                this.labelPath, this.pathText,
                this.labelFilter, this.filterText,
                this.recurse,
                this.labelNotifyOn, this.fileName, this.directory, this.attributes, this.size, this.lastWrite, this.lastAccess, this.creationTime, this.security,
                this.delayEventRadio, this.debounceMs, this.throttleMs,
                this.ok, this.cancel);
            return win;
        }

        private void ThrottleMs_KeyPress(View.KeyEventEventArgs obj)
        {
            if (int.TryParse(this.throttleMs.Text.ToString(), out var value))
            {
                this.options.ThrottleMs = value;
            }
            else this.throttleMs.Text = this.options.ThrottleMs.ToString(CultureInfo.InvariantCulture);
        }

        private void DebounceMs_KeyPress(View.KeyEventEventArgs obj)
        {
            if (int.TryParse(this.debounceMs.Text.ToString(), out var value))
            {
                this.options.DebounceMs = value;
            }
            else this.debounceMs.Text = this.options.ThrottleMs.ToString(CultureInfo.InvariantCulture);
        }

        private void OnOk()
        {
            this.options.Filter = this.filterText.Text.ToString();

            int filters = 0;
            filters |= this.directory.Checked ? (int)NotifyFilters.DirectoryName : filters;
            filters |= this.fileName.Checked ? (int)NotifyFilters.FileName : filters;
            filters |= this.lastAccess.Checked ? (int)NotifyFilters.LastAccess : filters;
            filters |= this.attributes.Checked ? (int)NotifyFilters.Attributes : filters;
            filters |= this.security.Checked ? (int)NotifyFilters.Security : filters;
            filters |= this.creationTime.Checked ? (int)NotifyFilters.CreationTime : filters;
            filters |= this.lastWrite.Checked ? (int)NotifyFilters.LastWrite : filters;
            this.options.NotifyFilter = (NotifyFilters)filters;

            this.options.IncludeSubdirectories = this.recurse.Checked;
            this.options.ThrottleMs = this.delayEventRadio.SelectedItem == 0 ? this.options.ThrottleMs : 0;
            this.options.DebounceMs = this.delayEventRadio.SelectedItem == 1 ? this.options.DebounceMs : 0;

            this.isOk = true;

            Application.RequestStop();
        }

        private void OnCancel()
        {
            this.isOk = false;
            Application.RequestStop();
        }
    }
}