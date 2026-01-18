using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SysDrawing = System.Drawing;

namespace Gamix.UI.Converters
{
    /// <summary>
    /// 実行ファイルのパスからアイコンを取得するヘルパークラス。
    /// </summary>
    public static class IconHelper
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "GetClassLongPtr")]
        private static extern IntPtr GetClassLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetClassLong")]
        private static extern IntPtr GetClassLongPtr32(IntPtr hWnd, int nIndex);

        private static IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex)
        {
            return IntPtr.Size == 8 ? GetClassLongPtr64(hWnd, nIndex) : GetClassLongPtr32(hWnd, nIndex);
        }

        private const uint WM_GETICON = 0x007F;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;
        private const int GCLP_HICON = -14;
        private const int GCLP_HICONSM = -34;

        /// <summary>
        /// 指定されたパスまたはウィンドウハンドルからアイコンを取得します。
        /// </summary>
        public static ImageSource? GetIconFromPath(string? path, bool isMaster = false, string? processName = null, IntPtr? windowHandle = null)
        {
            // Master Volume はスピーカーアイコン (shell32.dll index 168)
            if (isMaster)
            {
                return GetShellIcon(168);
            }

            // System Sounds はシステム音アイコン (shell32.dll index 156)
            if (string.IsNullOrEmpty(path) && processName == "System Sounds")
            {
                return GetShellIcon(156);
            }

            // Try getting icon from window handle first if available
            if (windowHandle.HasValue && windowHandle.Value != IntPtr.Zero)
            {
                var iconSource = GetIconFromWindow(windowHandle.Value);
                if (iconSource != null) return iconSource;
            }

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            try
            {
                using var icon = SysDrawing.Icon.ExtractAssociatedIcon(path);
                if (icon == null) return null;

                return Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle,
                    System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            catch
            {
                return null;
            }
        }

        private static ImageSource? GetIconFromWindow(IntPtr hWnd)
        {
            try
            {
                IntPtr hIcon = SendMessage(hWnd, WM_GETICON, new IntPtr(ICON_BIG), IntPtr.Zero);
                if (hIcon == IntPtr.Zero)
                    hIcon = SendMessage(hWnd, WM_GETICON, new IntPtr(ICON_SMALL), IntPtr.Zero);
                if (hIcon == IntPtr.Zero)
                    hIcon = GetClassLongPtr(hWnd, GCLP_HICON);
                if (hIcon == IntPtr.Zero)
                    hIcon = GetClassLongPtr(hWnd, GCLP_HICONSM);

                if (hIcon != IntPtr.Zero)
                {
                    var bitmap = Imaging.CreateBitmapSourceFromHIcon(
                        hIcon,
                        System.Windows.Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    // Freeze for cross-thread access if needed, though usually created on UI thread
                    if (bitmap.CanFreeze) bitmap.Freeze();
                    return bitmap;
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// shell32.dll からアイコンを取得します。
        /// </summary>
        /// <param name="iconIndex">アイコンインデックス。</param>
        /// <returns>アイコンの ImageSource。</returns>
        private static BitmapSource? GetShellIcon(int iconIndex)
        {
            try
            {
                string shell32Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll");
                IntPtr hIcon = ExtractIcon(IntPtr.Zero, shell32Path, iconIndex);
                
                if (hIcon == IntPtr.Zero) return null;

                try
                {
                    return Imaging.CreateBitmapSourceFromHIcon(
                        hIcon,
                        System.Windows.Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                }
                finally
                {
                    DestroyIcon(hIcon);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}

