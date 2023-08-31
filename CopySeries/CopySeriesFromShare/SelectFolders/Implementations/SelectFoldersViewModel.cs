using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using DoenaSoft.AbstractionLayer.Commands;
using DoenaSoft.AbstractionLayer.IOServices;

namespace DoenaSoft.CopySeries.SelectFolders.Implementations
{
    internal sealed class SelectFoldersViewModel : ISelectFoldersViewModel
    {
        #region Properties

        private IEnumerable<string> ExcludeList
        {
            get
            {
                yield return ("_RecentFiles");
                yield return ("sonstiges");
            }
        }

        #endregion

        #region Constructor

        public SelectFoldersViewModel(IIOServices ioServices)
        {
            Folders = ioServices.Folder.GetFolderNames(Properties.Settings.Default.SourcePath);

            Folders = Folders.Select(GetFolderName).Where(NotInExcludeList);
        }

        #endregion

        #region ISelectFoldersViewModel

        public IEnumerable<string> Folders { get; private set; }

        public IEnumerable<string> SelectedFolders { get; private set; }

        public ICommand AcceptCommand
            => (new ParameterizedRelayCommand(Accept));

        public ICommand CancelCommand
            => (new ParameterizedRelayCommand(Cancel));

        #endregion

        #region Methods

        private bool NotInExcludeList(string folder)
            => (ExcludeList.Contains(folder) == false);

        private string GetFolderName(string folder)
        {
            var split = folder.Split('\\');

            folder = split[split.Length - 1];

            return (folder);
        }

        private void Accept(object parameter)
        {
            var parameters = (IAcceptButtonCommandParameters)parameter;

            SelectedFolders = parameters.SelectedFolders;

            parameters.Closeable.DialogResult = true;

            parameters.Closeable.Close();
        }

        private void Cancel(object parameter)
        {
            SelectedFolders = null;

            var closeable = (ICloseable)parameter;

            closeable.DialogResult = false;

            closeable.Close();
        }

        #endregion
    }
}