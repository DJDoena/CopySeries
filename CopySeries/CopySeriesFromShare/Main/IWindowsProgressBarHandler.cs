using System;

namespace DoenaSoft.CopySeries.Main
{
    internal interface IWindowsProgressBarHandler
    {
        void Set(Int32 value
            , Int32 max);
    }
}