using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Gamix.Core.Utils
{
    public static class StringExtensions
    {
        /// <summary>
        /// 文字列を指定した「見た目の文字数」（サロゲートペアや結合文字を考慮した書記素クラスタ数）で切り詰めます。
        /// </summary>
        public static string TruncateVisual(this string text, int maxVisibleChars)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var si = new StringInfo(text);
            if (si.LengthInTextElements <= maxVisibleChars)
            {
                return text;
            }

            return si.SubstringByTextElements(0, maxVisibleChars);
        }

        /// <summary>
        /// 文字列の「見た目の文字数」を取得します。
        /// </summary>
        public static int GetVisualLength(this string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return new StringInfo(text).LengthInTextElements;
        }
    }
}
