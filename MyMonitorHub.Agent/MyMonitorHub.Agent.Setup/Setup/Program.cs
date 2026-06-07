using System;
using System.Windows.Forms;

namespace MyMonitorHub.Agent.Setup
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Render natively at the real per-monitor DPI instead of letting Windows
            // bitmap-stretch the window (which distorts the layout at 125%/150%).
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmMain());
        }
    }
}