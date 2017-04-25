using System;

namespace DoenaSoft.CopySeries.Main
{
    internal interface IRemainingTimeCalculator
    {
        void Start();

        void AddDelay(TimeSpan delay);

        String Get(Int32 progressValue
            , Int32 progressMax);
    }
}