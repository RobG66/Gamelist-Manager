using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Gamelist_Manager.Views;

namespace Gamelist_Manager.Classes.Helpers
{
    public static class UwpAppHelper
    {
        // COM interface for IApplicationActivationManager
        [ComImport]
        [Guid("2e941141-7f97-4756-ba1d-9decde894a3d")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IApplicationActivationManager
        {
            int ActivateApplication(
                [In] string appUserModelId,
                [In] string arguments,
                [In] ActivateOptions options,
                [Out] out uint processId);

            int ActivateForFile(
                [In] string appUserModelId,
                [In] IShellItemArray itemArray,
                [In] string verb,
                [Out] out uint processId);

            int ActivateForProtocol(
                [In] string appUserModelId,
                [In] IShellItemArray itemArray,
                [Out] out uint processId);
        }

        [Flags]
        private enum ActivateOptions
        {
            None = 0x00000000,
            DesignMode = 0x00000001,
            NoErrorUI = 0x00000002,
            NoSplashScreen = 0x00000004,
        }

        [ComImport]
        [Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")]
        private class ApplicationActivationManager { }

        [ComImport]
        [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem { }

        [ComImport]
        [Guid("b63ea76d-1f85-456f-a19c-48159efa858b")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItemArray { }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHCreateShellItemArrayFromShellItem(
            IShellItem psi,
            [In] ref Guid riid,
            [Out, MarshalAs(UnmanagedType.Interface)] out IShellItemArray ppv);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHCreateItemFromParsingName(
            [In] string pszPath,
            IntPtr pbc,
            [In] ref Guid riid,
            [Out, MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);

        // Helper to check if the OS supports UWP (Windows 8 or newer)
        private static bool IsUwpSupported()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return false;

            Version osVersion = Environment.OSVersion.Version;
            return osVersion.Major > 6 || (osVersion.Major == 6 && osVersion.Minor >= 2);
        }

        // Helper to create a shell item array from a single file path
        private static IShellItemArray CreateShellItemArrayFromPath(string filePath)
        {
            Guid shellItemGuid = typeof(IShellItem).GUID;
            SHCreateItemFromParsingName(filePath, IntPtr.Zero, ref shellItemGuid, out IShellItem shellItem);

            Guid shellItemArrayGuid = typeof(IShellItemArray).GUID;
            SHCreateShellItemArrayFromShellItem(shellItem, ref shellItemArrayGuid, out IShellItemArray shellItemArray);

            return shellItemArray;
        }

        /// <summary>
        /// Launches a UWP application by AppUserModelId, or falls back to Process.Start if it's Windows Photos.
        /// Windows-only. Returns false on non-Windows platforms.
        /// </summary>
        public static async Task<bool> LaunchAppWithFileAsync(string appUserModelId, string filePath, string? verb = null)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return false;

            // Fallback to Process.Start if it's Windows Photos (often has issues with COM activation)
            if (appUserModelId.Contains("Microsoft.Windows.Photos", StringComparison.OrdinalIgnoreCase))
            {
                return LaunchWithFallback(filePath);
            }

            return await LaunchAppWithFileInternalAsync(appUserModelId, filePath, verb);
        }

        private static async Task<bool> LaunchAppWithFileInternalAsync(string appUserModelId, string filePath, string? verb)
        {
            if (!IsUwpSupported())
            {
                await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
                {
                    Title = "Error",
                    Message = "UWP apps are not supported on this version of Windows.",
                    IconTheme = DialogIconTheme.Error,
                    Button1Text = "",
                    Button2Text = "",
                    Button3Text = "OK"
                });
                return false;
            }

            if (string.IsNullOrWhiteSpace(appUserModelId) || string.IsNullOrWhiteSpace(filePath))
            {
                await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
                {
                    Title = "Error",
                    Message = "Invalid appUserModelId or file path.",
                    IconTheme = DialogIconTheme.Error,
                    Button1Text = "",
                    Button2Text = "",
                    Button3Text = "OK"
                });
                return false;
            }

            try
            {
                var appActivator = new ApplicationActivationManager() as IApplicationActivationManager;
                IShellItemArray shellItemArray = CreateShellItemArrayFromPath(filePath);

                int hr = appActivator!.ActivateForFile(
                    appUserModelId,
                    shellItemArray,
                    verb ?? "open",
                    out _);  // Discard the processId

                return hr >= 0; // S_OK or similar
            }
            catch (Exception ex)
            {
                await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
                {
                    Title = "Error",
                    Message = $"Failed to launch UWP app with file: {ex.Message}",
                    IconTheme = DialogIconTheme.Error,
                    Button1Text = "",
                    Button2Text = "",
                    Button3Text = "OK"
                });
            }

            return false;
        }

        /// <summary>
        /// Launch a file with the default system handler (e.g., Windows Photos) using Process.Start().
        /// </summary>
        private static bool LaunchWithFallback(string filePath)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });

                return true;
            }
            catch (Exception ex)
            {
                // Just log, don't show dialog for fallback failures
                Debug.WriteLine($"Failed to launch file with default handler: {ex.Message}");
                return false;
            }
        }
    }
}
