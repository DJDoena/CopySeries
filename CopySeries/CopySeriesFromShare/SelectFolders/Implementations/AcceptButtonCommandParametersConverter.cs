﻿namespace DoenaSoft.CopySeries.SelectFolders.Implementations
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Linq;
    using System.Windows.Data;

    public sealed class AcceptButtonCommandParametersConverter : IMultiValueConverter
    {
        #region IMultiValueConverter

        public object Convert(object[] values
            , Type targetType
            , object parameter
            , CultureInfo culture)
        {
            var closeable = (ICloseable)(values[0]);

            var selectedFolders = ((IList)(values[1])).Cast<string>();

            IAcceptButtonCommandParameters parameters = new AcceptButtonCommandParameters(selectedFolders, closeable);

            return (parameters);
        }

        public object[] ConvertBack(object value
            , Type[] targetTypes
            , object parameter
            , CultureInfo culture)
        {
            throw (new NotSupportedException());
        }

        #endregion
    }
}