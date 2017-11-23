namespace DoenaSoft.CopySeries
{
    using System;
    using System.Collections.Generic;

    internal interface IWindowFactory
    {
        void OpenMainWindow();

        Boolean OpenFilterWindow();

        Boolean OpenSelectFoldersWindow(out IEnumerable<String> selectedShows);
    }
}