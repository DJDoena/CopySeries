namespace DoenaSoft.CopySeries
{
    using System.Collections.Generic;
    using System.Linq;

    internal static class RecipientFilter
    {
        public static IEnumerable<string> GetBcc(this IEnumerable<Recipient> recipients, bool newSeries, bool newSeason) => recipients.Filter(newSeries, newSeason).Select(r => r.Value);

        public static IEnumerable<Recipient> Filter(this IEnumerable<Recipient> recipients, bool newSeries, bool newSeason) => recipients.Where(r => IsInFilter(r, newSeries, newSeason));

        private static bool IsInFilter(Recipient recipient, bool newSeries, bool newSeason)
        {
            if (string.IsNullOrEmpty(recipient.Flags))
            {
                //not interested in TV
                return false;
            }
            else
            {
                var flags = recipient.Flags.Split(',');

                if (!flags.Any(f => f == "TVShows"))
                {
                    //not interested in TV
                    return false;
                }
                else
                {
                    if (!newSeries)
                    {
                        if (!newSeason)
                        {
                            if (flags.Any(f => f == "NewSeries" || f == "NewSeason"))
                            {
                                //only interested in new shows or new seasons
                                return false;
                            }
                            else
                            {
                                //interested in TV
                                return true;
                            }
                        }
                        else
                        {
                            if (flags.Any(f => f == "NewSeason"))
                            {
                                //interested in new seasons
                                return true;
                            }
                            else if (flags.Any(f => f == "NewSeries"))
                            {
                                //only interested in new shows
                                return false;
                            }
                            else
                            {
                                //interested in new seasons
                                return true;
                            }
                        }
                    }
                    else
                    {
                        //interested in new shows or new seasons
                        return true;
                    }
                }
            }
        }

    }
}
