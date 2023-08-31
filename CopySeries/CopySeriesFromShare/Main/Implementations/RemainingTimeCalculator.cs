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

            TimeSpan elapsed = DateTime.Now.Subtract(StartTime);

            double complete = (elapsed.TotalSeconds / value) * max;

            TimeSpan remaining = TimeSpan.FromSeconds(complete - elapsed.TotalSeconds);

            string text = GetRemainingText(remaining);

            return (text);
        }

        #endregion

        private static string GetRemainingText(TimeSpan remaining)
        {
            int days = remaining.Days;

            int hours = remaining.Hours;

            int minutes = remaining.Minutes;

            int seconds = remaining.Seconds;

            string dayString = (days > 0) ? $"{days}d " : string.Empty;

            string hourString = ((days > 0) || (hours > 0)) ? $"{hours}h " : string.Empty;

            string minuteString = ((days > 0) || (hours > 0) || minutes > 0) ? $"{minutes}m " : string.Empty;

            string text = $"( est. {dayString}{hourString}{minuteString}{seconds}s remaining)";

            return (text);
        }
    }
}