using System;
using System.IO;
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
        /// <summary>
        /// 指定されたパスからアイコンを取得します。
        /// </summary>
        /// <param name="path">実行ファイルのパス。</param>
        /// <returns>アイコンの ImageSource、取得できない場合は null。</returns>
        public static ImageSource? GetIconFromPath(string? path)
        {
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
    }
}
