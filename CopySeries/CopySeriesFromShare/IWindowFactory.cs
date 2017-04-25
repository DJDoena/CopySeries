using System;
using System.Collections.Generic;

namespace DoenaSoft.CopySeries
{
    internal interface IWindowFactory
    {
        void OpenMainWindow();

        Boolean OpenFilterWindow();

        Boolean OpenSelectFoldersWindow(out IEnumerable<String> selectedShows);
    }
}
