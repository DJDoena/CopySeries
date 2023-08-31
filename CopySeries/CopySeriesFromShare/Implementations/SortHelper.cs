namespace DoenaSoft.CopySeries.Implementations
{
    using System;
    using Main;

    internal static class SortHelper
    {
        internal static int CompareFileEntries(IFileEntryViewModel left
            , IFileEntryViewModel right)
        {
            string compareLeft = CreateSortPath(left.FullName);

            string compareRight = CreateSortPath(right.FullName);

            int compare = compareLeft.CompareTo(compareRight);

            return (compare);
        }

        internal static int CompareSeries(string left
            , string right)
        {
            string compareLeft = RemoveArticle(left);

            string compareRight = RemoveArticle(right);

            int compare = compareLeft.CompareTo(compareRight);

            return (compare);
        }

        private static string CreateSortPath(string input)
        {
            string[] split = input.Split('\\');

            for (int i = 0; i < split.Length; i++)
            {
                split[i] = RemoveArticle(split[i]);
            }

            string output = string.Join(@"\", split);

            return (output);
        }

        private static string RemoveArticle(string input)
        {
            string output = input;

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