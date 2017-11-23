namespace DoenaSoft.CopySeries.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using AbstractionLayer.IOServices;
    using AbstractionLayer.IOServices.Implementations;
    using AbstractionLayer.UIServices;
    using AbstractionLayer.UIServices.Implementations;
    using Filter;
    using Filter.Implementations;
    using Main;
    using Main.Implementations;
    using SelectFolders;
    using SelectFolders.Implementations;

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

        public Boolean OpenFilterWindow()
        {
            IFilterViewModel viewModel = new FilterViewModel(this);

            Window window = new FilterWindow();

            window.DataContext = viewModel;

            Nullable<Boolean> result = window.ShowDialog();

            return (result == true);
        }

        public Boolean OpenSelectFoldersWindow(out IEnumerable<String> selectedShows)
        {
            ISelectFoldersViewModel viewModel = new SelectFoldersViewModel(IOServices);

            Window window = new SelectFoldersWindow();

            window.DataContext = viewModel;

            Nullable<Boolean> result = window.ShowDialog();

            selectedShows = viewModel.SelectedFolders;

            return (result == true);
        }

        #endregion
    }
}