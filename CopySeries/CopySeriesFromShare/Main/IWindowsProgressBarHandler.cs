namespace DoenaSoft.CopySeries.Main
{
    using System;

    internal interface IWindowsProgressBarHandler
    {
        void Set(int value
            , int max);
    }
}