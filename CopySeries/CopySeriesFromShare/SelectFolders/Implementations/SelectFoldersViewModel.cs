namespace DoenaSoft.CopySeries.SelectFolders.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;
    using AbstractionLayer.IOServices;
    using ToolBox.Commands;

    internal sealed class SelectFoldersViewModel : ISelectFoldersViewModel
    {
        #region Properties

        private IEnumerable<String> ExcludeList
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

        public IEnumerable<String> Folders { get; private set; }

        public IEnumerable<String> SelectedFolders { get; private set; }

        public ICommand AcceptCommand
            => (new ParameterizedRelayCommand(Accept));

        public ICommand CancelCommand
            => (new ParameterizedRelayCommand(Cancel));

        #endregion

        #region Methods

        private Boolean NotInExcludeList(String folder)
            => (ExcludeList.Contains(folder) == false);

        private String GetFolderName(String folder)
        {
            String[] split = folder.Split('\\');

            folder = split[split.Length - 1];

            return (folder);
        }

        private void Accept(Object parameter)
        {
            IAcceptButtonCommandParameters parameters = (IAcceptButtonCommandParameters)parameter;

            SelectedFolders = parameters.SelectedFolders;

            parameters.Closeable.DialogResult = true;

            parameters.Closeable.Close();
        }

        private void Cancel(Object parameter)
        {
            SelectedFolders = null;

            ICloseable closeable = (ICloseable)parameter;

            closeable.DialogResult = false;

            closeable.Close();
        }

        #endregion
    }
}