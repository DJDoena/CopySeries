using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using DoenaSoft.ToolBox.Commands;

namespace DoenaSoft.CopySeries.Main
{
    internal interface IMainViewModel : INotifyPropertyChanged
    {
        String Filter { get; set; }

        #region CheckBoxes

        Boolean NoSubs { get; set; }

        Boolean OnlyHDs { get; set; }

        Boolean OnlySDs { get; set; }

        Boolean AutoApplyFilter { get; set; }

        Boolean PreserveSubFolders { get; set; }

        Boolean IgnoreResolutionFolders { get; set; }

        #endregion

        String TargetPath { get; set; }

        ObservableCollection<IFileEntryViewModel> FileEntries { get; }

        DateTime LastUsed { get; set; }

        String Size { get; }

        #region OverwriteOptions

        IEnumerable<String> OverwriteOptions { get; }

        String SelectedOverwriteOption { get; set; }

        #endregion

        #region Progress

        Boolean TaskIsRunning { get; }

        Boolean TaskIsNotRunning { get; }        

        Int32 ProgressMax { get; }

        Int32 ProgressValue { get; }

        String ProgressText { get; }

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