namespace Gamix.Core.Models
{
    /// <summary>
    /// オーディオデバイス（スピーカー/マイク）を表すモデル。
    /// </summary>
    public class AudioDevice
    {
        /// <summary>
        /// デバイスの一意識別子。
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// デバイスの表示名。
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// デフォルトデバイスかどうか。
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// 出力デバイス（スピーカー）かどうか。false の場合は入力デバイス（マイク）。
        /// </summary>
        public bool IsOutput { get; set; }
    }
}
