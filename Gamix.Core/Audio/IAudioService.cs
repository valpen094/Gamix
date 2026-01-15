using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gamix.Core.Models;

namespace Gamix.Core.Audio
{
    /// <summary>
    /// オーディオセッションの取得・音量操作を行うサービスのインターフェース。
    /// </summary>
    public interface IAudioService
    {
        /// <summary>
        /// アクティブなオーディオセッションの一覧を非同期で取得します。
        /// </summary>
        /// <returns>AudioSession のリスト。</returns>
        Task<List<AudioSession>> GetActiveSessionsAsync();

        /// <summary>
        /// 指定したセッションの音量を設定します。
        /// </summary>
        /// <param name="sessionId">セッションID。</param>
        /// <param name="volume">音量 (0.0 〜 1.0)。</param>
        void SetVolume(string sessionId, float volume);

        /// <summary>
        /// 指定したセッションのミュート状態を設定します。
        /// </summary>
        /// <param name="sessionId">セッションID。</param>
        /// <param name="isMuted">ミュートするかどうか。</param>
        void SetMute(string sessionId, bool isMuted);

        /// <summary>
        /// セッション構成が変更されたときに発火するイベント。
        /// </summary>
        event Action? SessionsChanged;
    }
}
