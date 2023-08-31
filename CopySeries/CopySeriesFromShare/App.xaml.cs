namespace DoenaSoft.CopySeries
{
    using System;
    using System.Windows;
    using DoenaSoft.CopySeries.Implementations;

    public partial class App : Application
    {
        internal static bool WasCopied { private get; set; } = false;

        protected override void OnStartup(StartupEventArgs e)
        {
            IWindowFactory windowFactory = new WindowFactory();

            windowFactory.OpenMainWindow();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (WasCopied)
            {
                CopySeries.Properties.Settings.Default.LastUse = DateTime.Now;
            }

            CopySeries.Properties.Settings.Default.Save();
        }
    }
}