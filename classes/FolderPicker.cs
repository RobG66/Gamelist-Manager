using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace GamelistManager.classes
{
    /// <summary>
    /// FolderPicker class to open a folder selection dialog
    /// </summary>
    public static class FolderPicker
    {
        public static string PickFolder(Window owner, string title)
        {
            IFileOpenDialog dialog = null;
            IShellItem item = null;
            IntPtr pszString = IntPtr.Zero;

            try
            {
                dialog = (IFileOpenDialog)new FileOpenDialog();

                // Set options (preserve existing ones, add our flags)
                dialog.GetOptions(out uint options);
                options |= FOS_PICKFOLDERS | FOS_FORCEFILESYSTEM;
                dialog.SetOptions(options);

                dialog.SetTitle(title);

                var hwnd = owner != null ? new WindowInteropHelper(owner).Handle : IntPtr.Zero;

                // Show dialog
                int hr = dialog.Show(hwnd);
                if (hr != 0) // cancelled or error
                    return null;

                dialog.GetResult(out item);
                item.GetDisplayName(SIGDN.FILESYSPATH, out pszString);

                if (pszString == IntPtr.Zero)
                    return null;

                return Marshal.PtrToStringUni(pszString);
            }
            catch
            {
                return null;
            }
            finally
            {
                if (pszString != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(pszString);
                if (item != null)
                    Marshal.ReleaseComObject(item);
                if (dialog != null)
                    Marshal.ReleaseComObject(dialog);
            }
        }

        // COM interfaces and constants
        [ComImport]
        [Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
        private class FileOpenDialog { }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("D57C7288-D4AD-4768-BE02-9D969532D960")]
        private interface IFileOpenDialog
        {
            // Only declaring what we use (order matters)
            [PreserveSig] int Show(IntPtr parent);
            void SetFileTypes(); // not used
            void SetFileTypeIndex(); // not used
            void GetFileTypeIndex(); // not used
            void Advise(); // not used
            void Unadvise(); // not used
            void SetOptions(uint options);
            void GetOptions(out uint options);
            void SetDefaultFolder(); // not used
            void SetFolder(); // not used
            void GetFolder(); // not used
            void GetCurrentSelection(); // not used
            void SetFileName(); // not used
            void GetFileName(); // not used
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string title);
            void SetOkButtonLabel(); // not used
            void SetFileNameLabel(); // not used
            void GetResult(out IShellItem item);
            // others omitted for brevity
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        private interface IShellItem
        {
            void BindToHandler(); // not used
            void GetParent(); // not used
            void GetDisplayName(SIGDN sigdnName, out IntPtr ppszName);
            void GetAttributes(); // not used
            void Compare(); // not used
        }

        private enum SIGDN : uint
        {
            FILESYSPATH = 0x80058000
        }

        private const uint FOS_PICKFOLDERS = 0x00000020;
        private const uint FOS_FORCEFILESYSTEM = 0x00000040;
    }
}
