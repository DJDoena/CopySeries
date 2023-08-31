namespace DoenaSoft.CopySeries.SelectFolders
{
    using System;
    using System.Collections.Generic;

    internal interface IAcceptButtonCommandParameters
    {
        IEnumerable<string> SelectedFolders { get; }

        ICloseable Closeable { get; }
    }
}