using System;
using System.Collections.Generic;
using System.Windows.Input;
using DoenaSoft.AbstractionLayer.UIServices;
using DoenaSoft.ToolBox.Generics;

namespace DoenaSoft.CopySeries.SelectFolders
{
    internal interface ISelectFoldersViewModel
    {
        IEnumerable<String> Folders { get; }

        IEnumerable<String> SelectedFolders { get; }

        ICommand AcceptCommand { get; }

        ICommand CancelCommand { get; }
    }
}