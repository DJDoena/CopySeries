using System;
using Microsoft.WindowsAPICodePack.Taskbar;

namespace DoenaSoft.CopySeries.Main.Implementations
{
    internal sealed class WindowsProgressBarHandler : IWindowsProgressBarHandler
    {
        private Boolean Indeterminate
            => (Max == Int32.MaxValue);

        private Boolean IsRunning
            => (Value >= 0);

        private Int32 Value { get; set; }

        private Int32 Max { get; set; }

        public WindowsProgressBarHandler()
        {
            Value = -1;
            Max = Int32.MaxValue;
        }

        #region IWindowsProgressBarHandler

        public void Set(Int32 value
            , Int32 max)
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