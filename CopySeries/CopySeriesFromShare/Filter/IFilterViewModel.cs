namespace DoenaSoft.CopySeries.Filter
{
    using System.ComponentModel;
    using System.Windows.Input;

    internal interface IFilterViewModel : INotifyPropertyChanged
    {
        bool NoSubs { get; set; }

        bool OnlyHDs { get; set; }

        bool OnlySDs { get; set; }

        string Filter { get; set; }

        ICommand SelectFoldersCommand { get; }

        ICommand AcceptCommand { get; }

        ICommand CancelCommand { get; }
    }
}