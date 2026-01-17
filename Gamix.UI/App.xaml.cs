using System;
using System.Windows;
using System.Drawing;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;
using System.Threading.Tasks;
using Gamix.UI.Services;
using Gamix.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Forms = System.Windows.Forms;

namespace Gamix.UI
{
    public partial class App : System.Windows.Application
    {
        public new static App Current => (App)System.Windows.Application.Current;
        public IServiceProvider Services { get; }
        private Forms.NotifyIcon? _notifyIcon;

        public App()
        {
            Services = ConfigureServices();
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Services
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<Gamix.Core.Audio.IAudioService, Gamix.Core.Audio.WasapiAudioService>();
            services.AddSingleton<Gamix.Core.Services.IPresetService, Gamix.Core.Services.PresetService>();
            services.AddSingleton<Gamix.Core.Services.ISettingsService, Gamix.Core.Services.SettingsService>();

            // ViewModels
            services.AddSingleton<MainViewModel>();

            // Views
            services.AddSingleton<MainWindow>();

            return services.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            SetupTrayIcon();
            
            // テーマ画像のプリロード（初回表示時の遅延解消）
            // 利用可能なテーマ一覧を取得して非同期で読み込む
            var themes = new[] { "Default", "Gaming", "Pastel" };
            _ = Views.ThemeSelectionDialog.PreloadImagesAsync(themes);

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        /// <summary>
        /// システムトレイにアイコンを設定する
        /// </summary>
        private void SetupTrayIcon()
        {
            _notifyIcon = new Forms.NotifyIcon { Text = "Gamix" };

            // アイコンの設定（PNG から Icon に変換）
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Icons", "tray_icon.png");
            if (File.Exists(iconPath))
            {
                using var bitmap = new Bitmap(iconPath);
                _notifyIcon.Icon = Icon.FromHandle(bitmap.GetHicon());
            }
            else
            {
                _notifyIcon.Icon = SystemIcons.Application;
            }

            // アイコン設定後に表示
            _notifyIcon.Visible = true;

            // 左クリックでウィンドウを表示
            _notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == Forms.MouseButtons.Left)
                    ShowMainWindow();
            };

            // 右クリックメニュー
            var contextMenu = new Forms.ContextMenuStrip();
            contextMenu.Items.Add("開く", null, (s, e) => ShowMainWindow());
            contextMenu.Items.Add(new Forms.ToolStripSeparator());
            contextMenu.Items.Add("終了", null, (s, e) => ShutdownApp());
            _notifyIcon.ContextMenuStrip = contextMenu;

            // 通知領域の「常に表示」設定を試みる（Windows のレジストリに依存）
            Task.Delay(3000).ContinueWith(_ => TryPromoteIcon(), TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        /// 通知領域でアイコンを「常に表示」に設定することを試みる
        /// Windows がレジストリにエントリを作成している場合のみ有効
        /// </summary>
        private void TryPromoteIcon()
        {
            try
            {
                string exeName = Path.GetFileName(Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty);
                if (string.IsNullOrEmpty(exeName)) return;

                using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\NotifyIconSettings", true);
                if (key == null) return;

                foreach (string subKeyName in key.GetSubKeyNames())
                {
                    using var subKey = key.OpenSubKey(subKeyName, true);
                    if (subKey?.GetValue("ExecutablePath") is string exePath &&
                        exePath.EndsWith(exeName, StringComparison.OrdinalIgnoreCase))
                    {
                        subKey.SetValue("IsPromoted", 1, RegistryValueKind.DWord);
                    }
                }
            }
            catch
            {
                // レジストリ操作に失敗しても、アイコン自体は表示されるので無視
            }
        }

        private void ShowMainWindow()
        {
            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
            if (mainWindow.WindowState == System.Windows.WindowState.Minimized)
            {
                mainWindow.WindowState = System.Windows.WindowState.Normal;
            }
            mainWindow.Activate();
        }

        private void ShutdownApp()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            System.Windows.Application.Current.Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            base.OnExit(e);
        }
    }
}
