namespace DoenaSoft.CopySeries.Main
{
    using System;

    internal interface IRemainingTimeCalculator
    {
        void Start();

        void AddDelay(TimeSpan delay);

        string Get(int progressValue
            , int progressMax);
    }
}