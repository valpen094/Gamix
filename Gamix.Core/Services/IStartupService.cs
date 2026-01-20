namespace Gamix.Core.Services
{
    public interface IStartupService
    {
        /// <summary>
        /// アプリがスタートアップに登録されているか確認します。
        /// </summary>
        bool IsStartupEnabled();

        /// <summary>
        /// スタートアップの設定を切り替えます。
        /// </summary>
        /// <param name="enable">true: 登録, false: 解除</param>
        void ToggleStartup(bool enable);
    }
}
