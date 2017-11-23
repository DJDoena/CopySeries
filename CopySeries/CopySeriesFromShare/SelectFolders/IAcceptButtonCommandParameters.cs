namespace DoenaSoft.CopySeries.SelectFolders
{
    using System;
    using System.Collections.Generic;

    internal interface IAcceptButtonCommandParameters
    {
        IEnumerable<String> SelectedFolders { get; }

        ICloseable Closeable { get; }
    }
}