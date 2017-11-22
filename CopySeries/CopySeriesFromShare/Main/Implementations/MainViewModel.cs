using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using DoenaSoft.AbstractionLayer.IOServices;
using DoenaSoft.AbstractionLayer.UIServices;
using DoenaSoft.CopySeries.Implementations;
using DoenaSoft.ToolBox.Commands;
using DoenaSoft.ToolBox.Extensions;
using DoenaSoft.ToolBox.Generics;
using DoenaSoft.ToolBox.Threading;

namespace DoenaSoft.CopySeries.Main.Implementations
{
    internal sealed class MainViewModel : IMainViewModel
    {
        #region Fields

        #region Thread-unsafe Fields

        /// <summary>
        /// for access: only use the property to make sure it's thread-safe
        /// </summary>
        private Boolean m_TaskIsRunning;

        /// <summary>
        /// for access: only use the property to make sure it's thread-safe
        /// </summary>
        private Int32 m_ProgressValue;

        /// <summary>
        /// for access: only use the property to make sure it's thread-safe
        /// </summary>
        private Int32 m_ProgressMax;

        #endregion

        private String m_SelectedOverwriteOption;

        private ICancelableCommand m_CopyCommand;

        #endregion

        #region Properties

        private IMainModel Model { get; }

        private IIOServices IOServices { get; }

        private IUIServices UIServices { get; }

        private IWindowFactory WindowFactory { get; }

        private IRemainingTimeCalculator RemainingTimeCalculator { get; }

        private IWindowsProgressBarHandler WindowsProgressBarHandler { get; }

        private ISynchronizer Synchronizer { get; }

        private String LastFolder { get; set; }

        private Int64 Divider
        {
            get
            {
                Int64 divider = 1;

                if (Model.Size >= Math.Pow(2, 40) * 2)
                {
                    divider = 1000000;
                }
                else if (Model.Size >= Math.Pow(2, 30) * 2)
                {
                    divider = 1000;
                }

                return (divider);
            }
        }

        #endregion

        #region Constructor

        public MainViewModel(IMainModel model
            , IIOServices ioServices
            , IUIServices uiServices
            , IWindowFactory windowFactory)
        {
            Model = model;
            IOServices = ioServices;
            UIServices = uiServices;
            WindowFactory = windowFactory;

            m_TaskIsRunning = false;
            m_SelectedOverwriteOption = OverwriteOptions.First();

            RemainingTimeCalculator = new RemainingTimeCalculator();
            WindowsProgressBarHandler = new WindowsProgressBarHandler();
            Synchronizer = new Synchronizer(Application.Current.Dispatcher);

            ReadLastRecentFile();
        }

        #endregion

        #region IMainViewModel

        public ObservableCollection<IFileEntryViewModel> FileEntries
        {
            get
            {
                Func<ObservableCollection<IFileEntryViewModel>> func = () =>
                    {
                        List<FileEntryViewModel> list = Model.Entries.Select(item => new FileEntryViewModel(item)).ToList();

                        list.Sort(SortHelper.CompareFileEntries);

                        return (new ObservableCollection<IFileEntryViewModel>(list));
                    };

                return (Synchronizer.InvokeOnUIThread(func));
            }
        }

        public String Size
            => ((new FileSize(Model.Size)).ToString());

        #region CheckBoxes

        public Boolean NoSubs
        {
            get
            {
                return (Properties.Settings.Default.NoSubs);
            }
            set
            {
                if (value != Properties.Settings.Default.NoSubs)
                {
                    Properties.Settings.Default.NoSubs = value;

                    TryAutoApplyFilter();

                    RaisePropertyChanged(nameof(NoSubs));
                }
            }
        }

        public Boolean OnlyHDs
        {
            get
            {
                return (Properties.Settings.Default.OnlyHD);
            }
            set
            {
                if (value != Properties.Settings.Default.OnlyHD)
                {
                    Properties.Settings.Default.OnlyHD = value;

                    if (value)
                    {
                        OnlySDs = false;
                    }

                    TryAutoApplyFilter();

                    RaisePropertyChanged(nameof(OnlyHDs));
                }
            }
        }

        public Boolean OnlySDs
        {
            get
            {
                return (Properties.Settings.Default.OnlySD);
            }
            set
            {
                if (value != Properties.Settings.Default.OnlySD)
                {
                    Properties.Settings.Default.OnlySD = value;

                    if (value)
                    {
                        OnlyHDs = false;
                    }

                    TryAutoApplyFilter();

                    RaisePropertyChanged(nameof(OnlySDs));
                }
            }
        }

        public Boolean IgnoreResolutionFolders
        {
            get
            {
                return (Model.IgnoreResolutionFolders);
            }
            set
            {
                if (value != Model.IgnoreResolutionFolders)
                {
                    Model.IgnoreResolutionFolders = value;

                    TryAutoApplyFilter();

                    RaisePropertyChanged(nameof(IgnoreResolutionFolders));
                }
            }
        }

        public Boolean PreserveSubFolders
        {
            get
            {
                return (Model.PreserveSubFolders);
            }
            set
            {
                if (value != Model.PreserveSubFolders)
                {
                    Model.PreserveSubFolders = value;

                    RaisePropertyChanged(nameof(PreserveSubFolders));
                }
            }
        }

        public Boolean AutoApplyFilter
        {
            get
            {
                return (Properties.Settings.Default.AutoApplyFilter);
            }
            set
            {
                if (value != Properties.Settings.Default.AutoApplyFilter)
                {
                    Properties.Settings.Default.AutoApplyFilter = value;

                    TryAutoApplyFilter();

                    RaisePropertyChanged(nameof(AutoApplyFilter));
                }
            }
        }

        #endregion

        public String Filter
        {
            get
            {
                return (Properties.Settings.Default.Filter);
            }
            set
            {
                if (value != Properties.Settings.Default.Filter)
                {
                    Properties.Settings.Default.Filter = value;

                    TryAutoApplyFilter();

                    RaisePropertyChanged(nameof(Filter));
                }
            }
        }

        public DateTime LastUsed
        {
            get
            {
                return (Properties.Settings.Default.LastUse);
            }
            set
            {
                if (value != Properties.Settings.Default.LastUse)
                {
                    Properties.Settings.Default.LastUse = value;

                    SaschasFunction();

                    TryAutoApplyFilter();

                    RaisePropertyChanged(nameof(LastUsed));
                }
            }
        }

        public String TargetPath
        {
            get
            {
                String targetPath = Properties.Settings.Default.TargetPath;

                if (IOServices.Directory.Exists(targetPath) == false)
                {
                    targetPath = @"C:\";
                }

                return (targetPath);
            }
            set
            {
                if (value != Properties.Settings.Default.TargetPath)
                {
                    Properties.Settings.Default.TargetPath = value;

                    RaisePropertyChanged(nameof(TargetPath));
                }
            }
        }

        #region OverwriteOptions

        public IEnumerable<String> OverwriteOptions
        {
            get
            {
                yield return (OverwriteOptionConstants.Ask);
                yield return (OverwriteOptionConstants.Always);
                yield return (OverwriteOptionConstants.Never);
            }
        }

        public String SelectedOverwriteOption
        {
            get
            {
                return (m_SelectedOverwriteOption);
            }
            set
            {
                if (value != m_SelectedOverwriteOption)
                {
                    m_SelectedOverwriteOption = value;

                    RaisePropertyChanged(nameof(SelectedOverwriteOption));
                }
            }
        }

        #endregion

        #region Progress

        public Int32 ProgressMax
        {
            get
            {
                return (Synchronizer.InvokeOnUIThread(() => m_ProgressMax));
            }
            private set
            {
                Action action = () =>
                {
                    if (value != m_ProgressMax)
                    {
                        m_ProgressMax = value;

                        SetWindowsProgressBar();

                        RaisePropertyChanged(nameof(ProgressMax));
                        RaisePropertyChanged(nameof(ProgressText));
                    }
                };

                Synchronizer.InvokeOnUIThread(action);
            }
        }

        public Int32 ProgressValue
        {
            get
            {
                return (Synchronizer.InvokeOnUIThread(() => m_ProgressValue));
            }
            private set
            {
                Action action = () =>
                {
                    if (value != m_ProgressValue)
                    {
                        m_ProgressValue = value;

                        SetWindowsProgressBar();

                        RaisePropertyChanged(nameof(ProgressValue));
                        RaisePropertyChanged(nameof(ProgressText));
                    }
                };

                Synchronizer.InvokeOnUIThread(action);
            }
        }

        public String ProgressText
        {
            get
            {
                Func<String> func = () =>
                {
                    if ((TaskIsNotRunning) || (ProgressMax == Int32.MaxValue))
                    {
                        return (String.Empty);
                    }

                    String remaining = RemainingTimeCalculator.Get(ProgressValue, ProgressMax);

                    FileSize progressSize = new FileSize(Model.ProgressValue);

                    FileSize maxSize = new FileSize(Model.Size);

                    return ($"{progressSize} / {maxSize} {remaining}");
                };

                return (Synchronizer.InvokeOnUIThread(func));
            }
        }

        public Boolean TaskIsRunning
        {
            get
            {
                return (Synchronizer.InvokeOnUIThread(() => m_TaskIsRunning));
            }
            private set
            {
                Action action = () =>
                {
                    if (value != m_TaskIsRunning)
                    {
                        m_TaskIsRunning = value;

                        SetWindowsProgressBar();

                        RaisePropertyChanged(nameof(TaskIsNotRunning));
                        RaisePropertyChanged(nameof(TaskIsRunning));
                        RaisePropertyChanged(nameof(ProgressText));
                    }
                };

                Synchronizer.InvokeOnUIThread(action);
            }
        }

        public Boolean TaskIsNotRunning
            => (TaskIsRunning == false);

        #endregion

        #region Commands

        public ICommand EditFilterCommand
            => (new RelayCommand(EditFilter, CanExecute));

        public ICommand ApplyFilterCommand
            => (new RelayCommand(ApplyFilter, CanExecute));

        public ICommand SelectTargetCommand
            => (new RelayCommand(SelectTarget, CanExecute));

        public ICommand AddFilesCommand
            => (new RelayCommand(AddFiles, CanExecute));

        public ICommand AddFolderCommand
            => (new RelayCommand(AddFolder, CanExecute));

        public ICommand RemoveEntriesCommand
            => (new ParameterizedRelayCommand(RemoveEntries, CanRemoveEntries));

        public ICommand ClearEntriesCommand
         => (new RelayCommand(ClearEntries, CanClearEntries));

        public ICancelableCommand CopyCommand
        {
            get
            {
                if (m_CopyCommand == null)
                {
                    m_CopyCommand = new CancelableCommandAsync(Copy, CanCopy);
                }

                return (m_CopyCommand);
            }
        }

        public ICommand CancelCommand
            => (new RelayCommand(Cancel, CanCancel));

        #endregion

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        //{
        //    add
        //    {
        //        if (m_PropertyChanged == null)
        //        {
        //            Model.FilesChanged += OnModelFilesChanged;
        //        }

        //        m_PropertyChanged += value;
        //    }
        //    remove
        //    {
        //        m_PropertyChanged -= value;

        //        if (m_PropertyChanged == null)
        //        {
        //            Model.FilesChanged -= OnModelFilesChanged;
        //        }
        //    }
        //}

        #endregion

        #region Methods

        #region Commands

        private Boolean CanExecute()
            => (TaskIsNotRunning);

        private void EditFilter()
        {
            if (WindowFactory.OpenFilterWindow())
            {
                RaisePropertyChanged(nameof(Filter));
                RaisePropertyChanged(nameof(NoSubs));
                RaisePropertyChanged(nameof(OnlyHDs));
                RaisePropertyChanged(nameof(OnlySDs));

                TryAutoApplyFilter();
            }
        }

        private void SelectTarget()
        {
            FolderBrowserDialogOptions options = new FolderBrowserDialogOptions();

            options.ShowNewFolderButton = true;
            options.SelectedPath = TargetPath;
            options.Description = "Select Target Folder";

            String targetPath;
            if (UIServices.ShowFolderBrowserDialog(options, out targetPath))
            {
                TargetPath = targetPath;
            }
        }

        private void AddFiles()
        {
            OpenFileDialogOptions options = new OpenFileDialogOptions();

            options.CheckFileExists = true;
            options.Filter = "Film files|*.avi;*.srt;*.mkv;*.mp4;*.flv|Recent files|RecentFiles.*.xml|All files|*.*";
            options.InitialDirectory = LastFolder;
            options.Title = "Select File(s) to Copy";

            String[] fileNames;
            if (UIServices.ShowOpenFileDialog(options, out fileNames))
            {
                LastFolder = IOServices.GetFileInfo(fileNames[0]).DirectoryName;

                foreach (String file in fileNames)
                {
                    if ((file.Contains("RecentFiles")) && (file.EndsWith(".xml")))
                    {
                        Model.ReadXml(file);

                        TryAutoApplyFilter();
                    }
                    else
                    {
                        Model.AddEntry(file);
                    }
                }

                RaiseFileEntriesChanged();
            }
        }

        private void AddFolder()
        {
            FolderBrowserDialogOptions options = new FolderBrowserDialogOptions();

            options.ShowNewFolderButton = false;
            options.SelectedPath = LastFolder;
            options.Description = "Select Folder to Copy";

            String folder;
            if (UIServices.ShowFolderBrowserDialog(options, out folder))
            {
                Model.AddEntry(folder);

                RaiseFileEntriesChanged();
            }
        }

        private Boolean CanRemoveEntries(Object parameter)
            => ((CanExecute()) && (((IList)parameter).Count > 0));

        private void RemoveEntries(Object parameter)
        {
            IEnumerable<IFileEntryViewModel> entries = ((IList)parameter).Cast<IFileEntryViewModel>();

            foreach (IFileEntryViewModel entry in entries)
            {
                Model.RemoveEntry(entry.FullName);
            }

            RaiseFileEntriesChanged();
        }

        private Boolean CanClearEntries()
           => ((CanExecute()) && (FileEntries.HasItems()));

        private void ClearEntries()
        {
            Model.ClearEntries();

            RaiseFileEntriesChanged();
        }

        private Boolean CanCopy()
            => ((CanExecute()) && (Model.Entries.HasItems()) && (String.IsNullOrEmpty(TargetPath) == false));

        private void Copy(CancellationToken cancellationToken)
        {
            RemainingTimeCalculator.Start();

            OnModelProgressMaxChanged(this, EventArgs.Empty);
            OnModelProgressValueChanged(this, EventArgs.Empty);

            Model.SizeChanged += OnModelProgressMaxChanged;
            Model.ProgressChanged += OnModelProgressValueChanged;
            Model.CopyPaused += OnModelCopyPaused;

            TaskIsRunning = true;

            try
            {
                Model.Copy(TargetPath, SelectedOverwriteOption, cancellationToken);
            }
            catch (Exception ex)
            {
                UIServices.ShowMessageBox(ex.Message, "Error", Buttons.OK, Icon.Error);
            }

            TaskIsRunning = false;

            Model.CopyPaused -= OnModelCopyPaused;
            Model.ProgressChanged -= OnModelProgressValueChanged;
            Model.SizeChanged -= OnModelProgressMaxChanged;
        }

        private void OnModelCopyPaused(Object sender
            , EventArgs<TimeSpan> e)
        {
            RemainingTimeCalculator.AddDelay(e.Value);
        }

        private void OnModelProgressMaxChanged(Object sender
            , EventArgs e)
        {
            ProgressMax = (Int32)(Model.Size / Divider);
        }

        private void OnModelProgressValueChanged(Object sender
            , EventArgs e)
        {
            ProgressValue = (Int32)(Model.ProgressValue / Divider);
        }

        private Boolean CanCancel()
            => (TaskIsRunning);

        private void Cancel()
        {
            CopyCommand.CancellationTokenSource.Cancel();
        }

        #endregion

        #region RaiseEvents

        private void RaisePropertyChanged(String attribute)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(attribute));
        }

        private void RaiseFileEntriesChanged()
        {
            RaisePropertyChanged(nameof(FileEntries));
            RaisePropertyChanged(nameof(Size));
        }

        private void RaiseTaskIsRunning()
        {
            RaisePropertyChanged(nameof(TaskIsRunning));
            RaisePropertyChanged(nameof(TaskIsNotRunning));
        }

        #endregion

        private void SetWindowsProgressBar()
        {
            WindowsProgressBarHandler.Set(TaskIsRunning ? ProgressValue : -1, ProgressMax);
        }

        private void SaschasFunction()
        {
            IDirectoryInfo di = IOServices.GetDirectoryInfo(IOServices.Path.Combine(Properties.Settings.Default.SourcePath, "_RecentFiles"));

            IEnumerable<IFileInfo> fis = di.GetFiles("RecentFiles.*.xml");

            DateTime lastUsed = LastUsed.ToUniversalTime();

            DateTime undefined = (new DateTime(2000, 1, 1, 0, 0, 1)).ToUniversalTime();

            if (lastUsed != undefined)
            {
                foreach (IFileInfo fi in fis)
                {
                    DateTime fileTime = fi.LastWriteTime.ToUniversalTime();

                    if (fileTime > lastUsed)
                    {
                        Model.ReadXml(fi.FullName);
                    }
                }
            }

            RaiseFileEntriesChanged();
        }

        private void ReadLastRecentFile()
        {
            String recentFile = IOServices.Path.Combine(Properties.Settings.Default.SourcePath, "_RecentFiles", "RecentFiles.xml");

            if (IOServices.File.Exists(recentFile))
            {
                Model.ReadXml(recentFile);

                LastFolder = IOServices.GetFileInfo(recentFile).DirectoryName;
            }

            SaschasFunction();

            TryAutoApplyFilter();
        }

        private void TryAutoApplyFilter()
        {
            if (AutoApplyFilter)
            {
                ApplyFilter();
            }
        }

        private void ApplyFilter()
        {
            IEnumerable<String> filter = Filter.IsNotEmpty() ? Filter.Split(';') : Enumerable.Empty<String>();

            Model.ApplyFilter(filter, NoSubs, OnlyHDs, OnlySDs);

            RaiseFileEntriesChanged();
        }

        #endregion
    }
}