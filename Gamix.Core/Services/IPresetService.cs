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
        /// <param name="currentSessions">現在のオーディオセッション一覧。</param>
        Task SavePresetAsync(string name, List<AudioSession> currentSessions);

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
        Task DeletePresetAsync(string name);
    }
}
