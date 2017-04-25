using System;
using System.ComponentModel;
using System.Windows.Input;
using DoenaSoft.AbstractionLayer.UIServices;
using DoenaSoft.ToolBox.Generics;

namespace DoenaSoft.CopySeries.Filter
{
    internal interface IFilterViewModel : INotifyPropertyChanged
    {
        Boolean NoSubs { get; set; }

        Boolean OnlyHDs { get; set; }

        Boolean OnlySDs { get; set; }

        String Filter { get; set; }

        ICommand SelectFoldersCommand { get; }

        ICommand AcceptCommand { get; }

        ICommand CancelCommand { get; }
    }
}