using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ContextMenuManager.Services
{
    /// <summary>Explorer helper (notify shell, restart Explorer).</summary>
    public static class ExplorerHelper
    {
        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private const int SHCNE_ASSOCCHANGED = 0x08000000;
        private const int SHCNF_IDLIST = 0x0000;

        /// <summary>Notify Shell that associations changed (lightweight, no Explorer restart).</summary>
        public static void NotifyShellChange()
        {
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>Restart Explorer (full refresh).</summary>
        public static void RestartExplorer()
        {
            try
            {
                foreach (var process in Process.GetProcessesByName("explorer"))
                {
                    process.Kill();
                    process.WaitForExit(3000);
                }

                Thread.Sleep(500);

                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to restart Explorer: {ex.Message}", ex);
            }
        }
    }
}
