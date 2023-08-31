namespace DoenaSoft.CopySeries.Implementations
{
    using Main;

    internal static class SortHelper
    {
        internal static int CompareFileEntries(IFileEntryViewModel left
            , IFileEntryViewModel right)
        {
            var compareLeft = CreateSortPath(left.FullName);

            var compareRight = CreateSortPath(right.FullName);

            var compare = compareLeft.CompareTo(compareRight);

            return (compare);
        }

        internal static int CompareSeries(string left
            , string right)
        {
            var compareLeft = RemoveArticle(left);

            var compareRight = RemoveArticle(right);

            var compare = compareLeft.CompareTo(compareRight);

            return (compare);
        }

        private static string CreateSortPath(string input)
        {
            var split = input.Split('\\');

            for (var i = 0; i < split.Length; i++)
            {
                split[i] = RemoveArticle(split[i]);
            }

            var output = string.Join(@"\", split);

            return (output);
        }

        private static string RemoveArticle(string input)
        {
            var output = input;

            if (input.StartsWith("The "))
            {
                output = input.Substring(4);
            }
            else if (input.StartsWith("A "))
            {
                output = input.Substring(2);
            }
            else if (input.StartsWith("An "))
            {
                output = input.Substring(3);
            }

            return (output);
        }
    }
}