using System;

namespace DoenaSoft.CopySeries
{
    internal interface ICloseable
    {
        Nullable<Boolean> DialogResult { get; set; }

        void Close();
    }
}
