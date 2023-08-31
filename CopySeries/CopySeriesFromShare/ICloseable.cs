namespace DoenaSoft.CopySeries
{
    using System;

    internal interface ICloseable
    {
        Nullable<bool> DialogResult { get; set; }

        void Close();
    }
}