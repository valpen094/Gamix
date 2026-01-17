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

        /// <summary>
        /// 指定されたパスからアイコンを取得します。
        /// Master Volume や System Sounds の場合はシェルアイコンを返します。
        /// </summary>
        /// <param name="path">実行ファイルのパス。</param>
        /// <param name="isMaster">マスター音量かどうか。</param>
        /// <param name="processName">プロセス名。</param>
        /// <returns>アイコンの ImageSource、取得できない場合は null。</returns>
        public static ImageSource? GetIconFromPath(string? path, bool isMaster = false, string? processName = null)
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

