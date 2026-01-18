using System.Collections.Generic;

namespace Gamix.Core.Models
{
    /// <summary>
    /// 音量プリセットを表すモデル。
    /// </summary>
    public class Preset
    {
        /// <summary>
        /// プリセット名。
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// このプリセットが関連付けられている出力デバイスID。
        /// </summary>
        public string? DeviceId { get; set; }

        /// <summary>
        /// マスター音量 (0.0 〜 1.0)。
        /// </summary>
        public float? MasterVolume { get; set; }

        /// <summary>
        /// マスターがミュート状態かどうか。
        /// </summary>
        public bool? IsMasterMuted { get; set; }

        /// <summary>
        /// お気に入りプリセットかどうか。起動時に自動適用される。
        /// </summary>
        public bool IsFavorite { get; set; }

        /// <summary>
        /// 各プロセスの音量設定リスト。
        /// </summary>
        public List<ProcessVolumeSetting> Settings { get; set; } = [];
    }

    /// <summary>
    /// プロセスごとの音量設定を表すモデル。
    /// </summary>
    public class ProcessVolumeSetting
    {
        /// <summary>
        /// プロセス名。
        /// </summary>
        public string ProcessName { get; set; } = string.Empty;

        /// <summary>
        /// 音量レベル (0.0 〜 1.0)。
        /// </summary>
        public float Volume { get; set; }

        /// <summary>
        /// ミュート状態かどうか。
        /// </summary>
        public bool IsMuted { get; set; }
    }
}
