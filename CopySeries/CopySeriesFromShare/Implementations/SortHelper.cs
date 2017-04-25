using System;
using DoenaSoft.CopySeries.Main;

namespace DoenaSoft.CopySeries.Implementations
{
    internal static class SortHelper
    {
        internal static Int32 CompareFileEntries(IFileEntryViewModel left
            , IFileEntryViewModel right)
        {
            String compareLeft = CreateSortPath(left.FullName);

            String compareRight = CreateSortPath(right.FullName);

            Int32 compare = compareLeft.CompareTo(compareRight);

            return (compare);
        }

        internal static Int32 CompareSeries(String left
            , String right)
        {
            String compareLeft = RemoveArticle(left);

            String compareRight = RemoveArticle(right);

            Int32 compare = compareLeft.CompareTo(compareRight);

            return (compare);
        }

        private static String CreateSortPath(String input)
        {
            String[] split = input.Split('\\');

            for (Int32 i = 0; i < split.Length; i++)
            {
                split[i] = RemoveArticle(split[i]);
            }

            String output = String.Join(@"\", split);

            return (output);
        }

        private static String RemoveArticle(String input)
        {
            String output = input;

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