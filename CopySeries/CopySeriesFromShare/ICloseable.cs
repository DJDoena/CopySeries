namespace DoenaSoft.CopySeries
{
    using System;

    internal interface ICloseable
    {
        Nullable<Boolean> DialogResult { get; set; }

        void Close();
    }
}