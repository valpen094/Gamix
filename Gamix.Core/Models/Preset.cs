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
        /// 各プロセスの音量設定リスト。
        /// </summary>
        public List<ProcessVolumeSetting> Settings { get; set; } = new();
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
