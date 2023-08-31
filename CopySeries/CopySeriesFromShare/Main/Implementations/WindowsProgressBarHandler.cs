namespace DoenaSoft.CopySeries.Main.Implementations
{
    using System;
    using Microsoft.WindowsAPICodePack.Taskbar;

    internal sealed class WindowsProgressBarHandler : IWindowsProgressBarHandler
    {
        private bool Indeterminate
            => (Max == int.MaxValue);

        private bool IsRunning
            => (Value >= 0);

        private int Value { get; set; }

        private int Max { get; set; }

        public WindowsProgressBarHandler()
        {
            Value = -1;
            Max = int.MaxValue;
        }

        #region IWindowsProgressBarHandler

        public void Set(int value
            , int max)
        {
            Value = value;
            Max = max;

            if (TaskbarManager.IsPlatformSupported)
            {
                CommenceSet();
            }
        }

        #endregion

        private void CommenceSet()
        {
            if (IsRunning)
            {
                SetValue();
            }
            else
            {
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress);
            }
        }

        private void SetValue()
        {
            if (Indeterminate)
            {
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Indeterminate);
            }
            else
            {
                TaskbarManager.Instance.SetProgressValue(Value, Max);
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal);
            }
        }
    }
}