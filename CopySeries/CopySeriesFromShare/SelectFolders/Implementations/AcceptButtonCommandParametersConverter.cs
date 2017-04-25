using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace DoenaSoft.CopySeries.SelectFolders.Implementations
{
    public sealed class AcceptButtonCommandParametersConverter : IMultiValueConverter
    {
        #region IMultiValueConverter

        public Object Convert(Object[] values
            , Type targetType
            , Object parameter
            , CultureInfo culture)
        {
            ICloseable closeable = (ICloseable)(values[0]);

            IEnumerable<String> selectedFolders = ((IList)(values[1])).Cast<String>();

            IAcceptButtonCommandParameters parameters = new AcceptButtonCommandParameters(selectedFolders, closeable);

            return (parameters);
        }

        public Object[] ConvertBack(Object value
            , Type[] targetTypes
            , Object parameter
            , CultureInfo culture)
        {
            throw (new NotSupportedException());
        }

        #endregion
    }
}