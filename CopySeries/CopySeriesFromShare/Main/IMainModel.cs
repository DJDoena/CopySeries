using System;
using System.Collections.Generic;
using System.Threading;
using DoenaSoft.ToolBox.Generics;

namespace DoenaSoft.CopySeries.Main
{
    internal interface IMainModel
    {
        IEnumerable<string> Entries { get; }

        ulong Size { get; }

        bool PreserveSubFolders { get; set; }

        bool IgnoreResolutionFolders { get; set; }

        ulong ProgressValue { get; }

        event EventHandler<EventArgs<TimeSpan>> CopyPaused;

        event EventHandler ProgressChanged;

        event EventHandler SizeChanged;

        void AddEntry(string entry);

        void RemoveEntry(string entry);

        void ClearEntries();

        void ApplyFilter(IEnumerable<string> filter
            , bool noSubs
            , bool onlyHDs
            , bool onlySDs);

        void ReadXml(string fileName);

        void Copy(string targetLocation
            , string overwrite
            , CancellationToken cancellationToken);
    }
}