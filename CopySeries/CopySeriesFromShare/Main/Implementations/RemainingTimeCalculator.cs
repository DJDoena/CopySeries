namespace DoenaSoft.CopySeries.Main.Implementations
{
    using System;

    internal sealed class RemainingTimeCalculator : IRemainingTimeCalculator
    {
        private DateTime StartTime { get; set; }

        public RemainingTimeCalculator()
        {
            StartTime = new DateTime(0);
        }

        #region IRemainingTimeCalculator

        public void Start()
        {
            StartTime = DateTime.Now;
        }

        public void AddDelay(TimeSpan delay)
        {
            StartTime.Add(delay);
        }

        public string Get(int value
            , int max)
        {
            if (value == 0)
            {
                return (string.Empty);
            }

            var elapsed = DateTime.Now.Subtract(StartTime);

            var complete = (elapsed.TotalSeconds / value) * max;

            var remaining = TimeSpan.FromSeconds(complete - elapsed.TotalSeconds);

            var text = GetRemainingText(remaining);

            return (text);
        }

        #endregion

        private static string GetRemainingText(TimeSpan remaining)
        {
            var days = remaining.Days;

            var hours = remaining.Hours;

            var minutes = remaining.Minutes;

            var seconds = remaining.Seconds;

            var dayString = (days > 0) ? $"{days}d " : string.Empty;

            var hourString = ((days > 0) || (hours > 0)) ? $"{hours}h " : string.Empty;

            var minuteString = ((days > 0) || (hours > 0) || minutes > 0) ? $"{minutes}m " : string.Empty;

            var text = $"( est. {dayString}{hourString}{minuteString}{seconds}s remaining)";

            return (text);
        }
    }
}