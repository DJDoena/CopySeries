namespace DoenaSoft.CopySeries.SelectFolders
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Input;

    internal interface ISelectFoldersViewModel
    {
        IEnumerable<String> Folders { get; }

        IEnumerable<String> SelectedFolders { get; }

        ICommand AcceptCommand { get; }

        ICommand CancelCommand { get; }
    }
}