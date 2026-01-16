using System.Collections.Generic;
using System.Threading.Tasks;
using Gamix.Core.Models;

namespace Gamix.Core.Services
{
    /// <summary>
    /// プリセットの保存・読み込み・適用を行うサービスのインターフェース。
    /// </summary>
    public interface IPresetService
    {
        /// <summary>
        /// 現在のセッション状態を名前付きプリセットとして保存します。
        /// </summary>
        /// <param name="name">プリセット名。</param>
        /// <param name="deviceId">対象の出力デバイスID。</param>
        /// <param name="masterVolume">マスター音量 (0.0 〜 1.0)。</param>
        /// <param name="isMasterMuted">マスターがミュート状態かどうか。</param>
        /// <param name="currentSessions">現在のオーディオセッション一覧。</param>
        Task SavePresetAsync(string name, string deviceId, float masterVolume, bool isMasterMuted, List<AudioSession> currentSessions);

        /// <summary>
        /// 保存されたプリセット一覧を読み込みます。
        /// </summary>
        /// <returns>Preset のリスト。</returns>
        Task<List<Preset>> LoadPresetsAsync();

        /// <summary>
        /// プリセットを現在のセッションに適用します。
        /// </summary>
        /// <param name="preset">適用するプリセット。</param>
        /// <param name="currentSessions">現在のオーディオセッション一覧。</param>
        Task ApplyPresetAsync(Preset preset, List<AudioSession> currentSessions);

        /// <summary>
        /// 指定した名前のプリセットを削除します。
        /// </summary>
        /// <param name="name">削除するプリセット名。</param>
        /// <param name="deviceId">対象デバイスID。</param>
        Task DeletePresetAsync(string name, string deviceId);

        /// <summary>
        /// プリセットの名前を変更します。
        /// </summary>
        /// <param name="oldName">現在の名前。</param>
        /// <param name="deviceId">対象デバイスID。</param>
        /// <param name="newName">新しい名前。</param>
        Task RenamePresetAsync(string oldName, string deviceId, string newName);

        /// <summary>
        /// プリセットのお気に入り状態を設定します。
        /// 同じデバイスでは1つのプリセットのみお気に入りに設定できます。
        /// </summary>
        /// <param name="name">プリセット名。</param>
        /// <param name="deviceId">対象デバイスID。</param>
        /// <param name="isFavorite">お気に入り状態。</param>
        Task SetFavoriteAsync(string name, string deviceId, bool isFavorite);

        /// <summary>
        /// 指定したデバイスのお気に入りプリセットを取得します。
        /// </summary>
        /// <param name="deviceId">対象デバイスID。</param>
        /// <returns>お気に入りプリセット。存在しない場合は null。</returns>
        Task<Preset?> GetFavoritePresetAsync(string deviceId);
    }
}
