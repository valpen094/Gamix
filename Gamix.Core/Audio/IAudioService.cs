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
        /// 指定したデバイスのオーディオセッション一覧を非同期で取得します。
        /// </summary>
        /// <param name="deviceId">デバイスID。null の場合はデフォルトデバイスを使用。</param>
        /// <returns>AudioSession のリスト。</returns>
        Task<List<AudioSession>> GetActiveSessionsAsync(string? deviceId);

        /// <summary>
        /// 利用可能なオーディオデバイスの一覧を取得します。
        /// </summary>
        /// <param name="isOutput">true の場合は出力デバイス、false の場合は入力デバイスを取得。</param>
        /// <returns>AudioDevice のリスト。</returns>
        Task<List<AudioDevice>> GetAudioDevicesAsync(bool isOutput);

        /// <summary>
        /// 指定したデバイスのマスター音量を取得します。
        /// </summary>
        /// <param name="deviceId">デバイスID。</param>
        /// <returns>マスター音量 (0.0 〜 1.0) とミュート状態。</returns>
        Task<(float Volume, bool IsMuted)> GetDeviceMasterVolumeAsync(string deviceId);

        /// <summary>
        /// 指定したデバイスのマスター音量を設定します。
        /// </summary>
        /// <param name="deviceId">デバイスID。</param>
        /// <param name="volume">音量 (0.0 〜 1.0)。</param>
        void SetDeviceMasterVolume(string deviceId, float volume);

        /// <summary>
        /// 指定したデバイスのマスターミュート状態を設定します。
        /// </summary>
        /// <param name="deviceId">デバイスID。</param>
        /// <param name="isMuted">ミュートするかどうか。</param>
        void SetDeviceMasterMute(string deviceId, bool isMuted);

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
