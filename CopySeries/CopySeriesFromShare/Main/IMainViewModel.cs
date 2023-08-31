using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using DoenaSoft.AbstractionLayer.Commands;

namespace DoenaSoft.CopySeries.Main
{
    internal interface IMainViewModel : INotifyPropertyChanged
    {
        string Filter { get; set; }

        #region CheckBoxes

        bool NoSubs { get; set; }

        bool OnlyHDs { get; set; }

        bool OnlySDs { get; set; }

        bool AutoApplyFilter { get; set; }

        bool PreserveSubFolders { get; set; }

        bool IgnoreResolutionFolders { get; set; }

        #endregion

        string TargetPath { get; set; }

        ObservableCollection<IFileEntryViewModel> FileEntries { get; }

        DateTime LastUsed { get; set; }

        string Size { get; }

        #region OverwriteOptions

        IEnumerable<string> OverwriteOptions { get; }

        string SelectedOverwriteOption { get; set; }

        #endregion

        #region Progress

        bool TaskIsRunning { get; }

        bool TaskIsNotRunning { get; }

        int ProgressMax { get; }

        int ProgressValue { get; }

        string ProgressText { get; }

        #endregion

        #region Commands

        ICommand EditFilterCommand { get; }

        ICommand ApplyFilterCommand { get; }

        ICommand SelectTargetCommand { get; }

        ICommand AddFilesCommand { get; }

        ICommand AddFolderCommand { get; }

        ICommand RemoveEntriesCommand { get; }

        ICommand ClearEntriesCommand { get; }

        ICancelableCommand CopyCommand { get; }

        ICommand CancelCommand { get; }

        #endregion
    }
}