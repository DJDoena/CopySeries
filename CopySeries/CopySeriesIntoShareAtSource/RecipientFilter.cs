namespace DoenaSoft.CopySeries
{
    using System.Collections.Generic;
    using System.Linq;

    internal static class RecipientFilter
    {
        public static IEnumerable<string> GetBcc(this IEnumerable<Recipient> recipients, bool newSeries, bool newSeason) => recipients.Filter(newSeries, newSeason).Select(r => r.Value);

        public static IEnumerable<Recipient> Filter(this IEnumerable<Recipient> recipients, bool newSeries, bool newSeason) => recipients.Where(r => IsInFilter(r, newSeries, newSeason));

        private static bool IsInFilter(Recipient recipient, bool isNewSeries, bool isNewSeason)
        {
            var interested = false;

            if (!string.IsNullOrEmpty(recipient.Flags))
            {
                var flags = recipient.Flags.Split(',');

                if (isNewSeries)
                {
                    interested = flags.Any(f => f == "TVShows" || f == "NewSeason" || f == "NewSeries");
                }
                else if (isNewSeason)
                {
                    interested = flags.Any(f => f == "TVShows" || f == "NewSeason");
                }
                else
                {
                    interested = flags.Any(f => f == "TVShows");
                }
            }

            return interested;
        }
    }
}