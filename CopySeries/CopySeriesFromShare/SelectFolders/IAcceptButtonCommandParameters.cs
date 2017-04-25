using System;
using System.Collections.Generic;

namespace DoenaSoft.CopySeries.SelectFolders
{
    internal interface IAcceptButtonCommandParameters
    {
        IEnumerable<String> SelectedFolders { get; }

        ICloseable Closeable { get; }
    }
}