using System.Threading.Tasks;

namespace Gamix.Core.Services
{
    /// <summary>
    /// デバイス設定の保存・読み込みを行うサービスのインターフェース。
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// 選択されたスピーカーのデバイスIDを取得します。
        /// </summary>
        /// <returns>デバイスID。未設定の場合は null。</returns>
        Task<string?> GetSelectedOutputDeviceIdAsync();

        /// <summary>
        /// 選択されたマイクのデバイスIDを取得します。
        /// </summary>
        /// <returns>デバイスID。未設定の場合は null。</returns>
        Task<string?> GetSelectedInputDeviceIdAsync();

        /// <summary>
        /// 選択されたスピーカーのデバイスIDを保存します。
        /// </summary>
        /// <param name="deviceId">デバイスID。</param>
        Task SetSelectedOutputDeviceIdAsync(string deviceId);

        /// <summary>
        /// 選択されたマイクのデバイスIDを保存します。
        /// </summary>
        /// <param name="deviceId">デバイスID。</param>
        Task SetSelectedInputDeviceIdAsync(string deviceId);

        /// <summary>
        /// 現在のテーマ名を取得します。
        /// </summary>
        /// <returns>テーマ名（例: "Pastel"）。未設定の場合は null。</returns>
        Task<string?> GetThemeAsync();

        /// <summary>
        /// 現在のテーマ名を保存します。
        /// </summary>
        /// <param name="themeName">テーマ名。</param>
        Task SetThemeAsync(string themeName);
    }
}
