namespace DoenaSoft.CopySeries.SelectFolders
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Input;

    internal interface ISelectFoldersViewModel
    {
        IEnumerable<string> Folders { get; }

        IEnumerable<string> SelectedFolders { get; }

        ICommand AcceptCommand { get; }

        ICommand CancelCommand { get; }
    }
}