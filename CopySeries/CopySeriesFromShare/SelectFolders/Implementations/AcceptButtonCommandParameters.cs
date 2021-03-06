﻿namespace DoenaSoft.CopySeries.SelectFolders.Implementations
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    internal sealed class AcceptButtonCommandParameters : IAcceptButtonCommandParameters
    {
        public IEnumerable<String> SelectedFolders { get; private set; }

        public ICloseable Closeable { get; private set; }

        public AcceptButtonCommandParameters(IEnumerable<String> selectedFolders
            , ICloseable closeable)
        {
            SelectedFolders = selectedFolders;
            Closeable = closeable;
        }
    }
}