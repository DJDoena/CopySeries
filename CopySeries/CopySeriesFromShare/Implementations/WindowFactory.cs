using System.Collections.Generic;
using System.Windows;
using DoenaSoft.AbstractionLayer.IOServices;
using DoenaSoft.AbstractionLayer.UIServices;
using DoenaSoft.CopySeries.Filter;
using DoenaSoft.CopySeries.Filter.Implementations;
using DoenaSoft.CopySeries.Main;
using DoenaSoft.CopySeries.Main.Implementations;
using DoenaSoft.CopySeries.SelectFolders;
using DoenaSoft.CopySeries.SelectFolders.Implementations;

namespace DoenaSoft.CopySeries.Implementations
{
    internal sealed class WindowFactory : IWindowFactory
    {
        private IUIServices UIServices { get; }

        private IIOServices IOServices { get; }

        public WindowFactory()
        {
            IOServices = new IOServices();
            UIServices = new WindowUIServices();

            //For the old forms
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
        }

        #region IWindowFactory

        public void OpenMainWindow()
        {
            IMainModel model = new MainModel(UIServices, IOServices);

            IMainViewModel viewModel = new MainViewModel(model, IOServices, UIServices, this);

            Window window = new MainWindow();

            window.DataContext = viewModel;

            window.Show();
        }

        public bool OpenFilterWindow()
        {
            IFilterViewModel viewModel = new FilterViewModel(this);

            Window window = new FilterWindow();

            window.DataContext = viewModel;

            var result = window.ShowDialog();

            return (result == true);
        }

        public bool OpenSelectFoldersWindow(out IEnumerable<string> selectedShows)
        {
            ISelectFoldersViewModel viewModel = new SelectFoldersViewModel(IOServices);

            Window window = new SelectFoldersWindow();

            window.DataContext = viewModel;

            var result = window.ShowDialog();

            selectedShows = viewModel.SelectedFolders;

            return (result == true);
        }

        #endregion
    }
}