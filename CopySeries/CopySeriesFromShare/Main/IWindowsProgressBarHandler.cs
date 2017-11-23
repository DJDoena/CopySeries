namespace DoenaSoft.CopySeries.Main
{
    using System;

    internal interface IWindowsProgressBarHandler
    {
        void Set(Int32 value
            , Int32 max);
    }
}