using System;
using System.Collections.Generic;
using System.Threading;
using DoenaSoft.ToolBox.Generics;

namespace DoenaSoft.CopySeries.Main
{
    internal interface IMainModel
    {
        IEnumerable<String> Entries { get; }

        Int64 Size { get; }

        Boolean PreserveSubFolders { get; set; }

        Boolean IgnoreResolutionFolders { get; set; }

        Int64 ProgressValue { get; }

        event EventHandler<EventArgs<TimeSpan>> CopyPaused;

        event EventHandler ProgressChanged;

        event EventHandler SizeChanged;

        void AddEntry(String entry);

        void RemoveEntry(String entry);

        void ClearEntries();

        void ApplyFilter(IEnumerable<String> filter
            , Boolean noSubs
            , Boolean onlyHDs
            , Boolean onlySDs);

        void ReadXml(String fileName);

        void Copy(String targetLocation
            , String overwrite
            , CancellationToken cancellationToken);
    }
}