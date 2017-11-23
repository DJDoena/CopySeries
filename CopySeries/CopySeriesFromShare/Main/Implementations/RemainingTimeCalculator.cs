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

        public String Get(Int32 value
            , Int32 max)
        {
            if (value == 0)
            {
                return (String.Empty);
            }

            TimeSpan elapsed = DateTime.Now.Subtract(StartTime);

            Double complete = (elapsed.TotalSeconds / value) * max;

            TimeSpan remaining = TimeSpan.FromSeconds(complete - elapsed.TotalSeconds);

            String text = GetRemainingText(remaining);

            return (text);
        }

        #endregion

        private static String GetRemainingText(TimeSpan remaining)
        {
            Int32 days = remaining.Days;

            Int32 hours = remaining.Hours;

            Int32 minutes = remaining.Minutes;

            Int32 seconds = remaining.Seconds;

            String dayString = (days > 0) ? $"{days}d " : String.Empty;

            String hourString = ((days > 0) || (hours > 0)) ? $"{hours}h " : String.Empty;

            String minuteString = ((days > 0) || (hours > 0) || minutes > 0) ? $"{minutes}m " : String.Empty;

            String text = $"( est. {dayString}{hourString}{minuteString}{seconds}s remaining)";

            return (text);
        }
    }
}