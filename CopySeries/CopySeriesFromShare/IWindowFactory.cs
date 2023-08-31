namespace DoenaSoft.CopySeries
{
    using System;
    using System.Collections.Generic;

    internal interface IWindowFactory
    {
        void OpenMainWindow();

        bool OpenFilterWindow();

        bool OpenSelectFoldersWindow(out IEnumerable<string> selectedShows);
    }
}