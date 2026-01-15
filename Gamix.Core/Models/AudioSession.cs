using System;

namespace Gamix.Core.Models
{
    /// <summary>
    /// アプリケーションのオーディオセッションを表すモデル。
    /// </summary>
    public class AudioSession
    {
        /// <summary>
        /// セッションの一意識別子。
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// プロセス名（例: "chrome", "Spotify"）。
        /// </summary>
        public string ProcessName { get; set; } = string.Empty;

        /// <summary>
        /// プロセスID。
        /// </summary>
        public int ProcessId { get; set; }

        /// <summary>
        /// アイコン取得用の実行ファイルパス。
        /// </summary>
        public string IconPath { get; set; } = string.Empty;

        /// <summary>
        /// 音量レベル (0.0 〜 1.0)。
        /// </summary>
        public float Volume { get; set; }

        /// <summary>
        /// ミュート状態かどうか。
        /// </summary>
        public bool IsMuted { get; set; }

        /// <summary>
        /// システムマスター音量を表すかどうか。
        /// </summary>
        public bool IsMaster { get; set; }
    }
}
