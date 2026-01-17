using System;
using System.Windows;
using System.Windows.Controls;
using System.Drawing;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;
using System.Threading.Tasks;
using Gamix.UI.Services;
using Gamix.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Hardcodet.Wpf.TaskbarNotification;

namespace Gamix.UI
{
    public partial class App : System.Windows.Application
    {
        public new static App Current => (App)System.Windows.Application.Current;
        public ServiceProvider Services { get; }
        private TaskbarIcon? _taskbarIcon;

        public App()
        {
            Services = ConfigureServices();
        }

        private static ServiceProvider ConfigureServices()
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
            var themes = new[] { "Gaming", "Pastel" };
            _ = Views.ThemeSelectionDialog.PreloadImagesAsync(themes);

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        /// <summary>
        /// システムトレイにアイコンを設定する (Hardcodet.NotifyIcon.Wpf 使用)
        /// </summary>
        private void SetupTrayIcon()
        {
            _taskbarIcon = new TaskbarIcon
            {
                ToolTipText = "Gamix",
                // メニュー位置をタスクバー上に表示（デフォルト動作を利用）
                MenuActivation = PopupActivationMode.RightClick
            };

            // アイコンの初期設定（テーマに合わせて動的生成）
            UpdateTrayIcon();

            // システムのテーマ変更等を監視
            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

            // 左クリックでウィンドウを表示
            _taskbarIcon.TrayLeftMouseDown += (s, e) => ShowMainWindow();

            // 右クリックでコンテキストメニューを表示（毎回最新のテーマで再作成）
            _taskbarIcon.TrayRightMouseUp += (s, e) =>
            {
                _taskbarIcon.ContextMenu = CreateTrayContextMenu();
                _taskbarIcon.ContextMenu.IsOpen = true;
            };

            // 通知領域の「常に表示」設定を試みる（Windows のレジストリに依存）
            Task.Delay(3000).ContinueWith(_ => TryPromoteIcon(), TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        /// WPF スタイルのコンテキストメニューを作成
        /// </summary>
        private ContextMenu CreateTrayContextMenu()
        {
            var contextMenu = new ContextMenu
            {
                // タスクバーと重ならないように上に配置
                Placement = System.Windows.Controls.Primitives.PlacementMode.Top,
                VerticalOffset = -8  // タスクバーとの隙間
            };
            
            // App.xaml で定義したスタイルを適用
            if (TryFindResource("TrayMenuStyle") is Style menuStyle)
            {
                contextMenu.Style = menuStyle;
            }

            var menuItemStyle = TryFindResource("TrayMenuItemStyle") as Style;
            var separatorStyle = TryFindResource("TraySeparatorStyle") as Style;

            // 開く
            var openItem = new MenuItem { Header = "Gamix!", Tag = "🎵" };
            if (menuItemStyle != null) openItem.Style = menuItemStyle;
            openItem.Click += (s, e) => ShowMainWindow();
            contextMenu.Items.Add(openItem);

            // セパレーター (開く と プリセットの間)
            var separator1 = new Separator();
            if (separatorStyle != null) separator1.Style = separatorStyle;
            contextMenu.Items.Add(separator1);

            // プリセット (サブメニュー)
            var presetsItem = new MenuItem { Header = "プリセット", Tag = "📁" };
            if (menuItemStyle != null) presetsItem.Style = menuItemStyle;
            contextMenu.Items.Add(presetsItem);

            // メニューが開かれる直前に動的にプリセット一覧を生成
            contextMenu.Opened += (s, e) => PopulatePresetsMenu(presetsItem, menuItemStyle);

            // セパレーター
            var separator = new Separator();
            if (separatorStyle != null) separator.Style = separatorStyle;
            contextMenu.Items.Add(separator);

            // 終了
            var exitItem = new MenuItem { Header = "終了", Tag = "✕" };
            if (menuItemStyle != null) exitItem.Style = menuItemStyle;
            exitItem.Click += (s, e) => ShutdownApp();
            contextMenu.Items.Add(exitItem);

            return contextMenu;
        }

        /// <summary>
        /// プリセットサブメニューを動的に生成
        /// </summary>
        private void PopulatePresetsMenu(MenuItem presetsItem, Style? menuItemStyle)
        {
            try
            {
                presetsItem.Items.Clear();

                var viewModel = Services.GetRequiredService<MainViewModel>();
                var currentPresets = viewModel.Presets.ToList();

                if (currentPresets.Count != 0)
                {
                    string currentPresetName = viewModel.NewPresetName ?? "";

                    foreach (var preset in currentPresets)
                    {
                        var item = new MenuItem
                        {
                            Header = preset.Name,
                            IsCheckable = false,
                            Tag = preset.Name == currentPresetName ? "✓" : ""
                        };
                        if (menuItemStyle != null) item.Style = menuItemStyle;

                        item.Click += (sender, args) =>
                        {
                            if (viewModel.ApplyPresetCommand.CanExecute(preset))
                            {
                                viewModel.ApplyPresetCommand.Execute(preset);
                            }
                        };
                        presetsItem.Items.Add(item);
                    }
                    presetsItem.IsEnabled = true;
                }
                else
                {
                    presetsItem.IsEnabled = false;
                }
            }
            catch
            {
                presetsItem.IsEnabled = false;
            }
        }

        /// <summary>
        /// 通知領域でアイコンを「常に表示」に設定することを試みる
        /// </summary>
        private static void TryPromoteIcon()
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
            _taskbarIcon?.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
            _taskbarIcon?.Dispose();
            base.OnExit(e);
        }

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                UpdateTrayIcon();
            }
        }

        private void UpdateTrayIcon()
        {
            if (_taskbarIcon == null) return;

            bool isLightMode = false;
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key != null)
                {
                    object? val = key.GetValue("AppsUseLightTheme");
                    if (val is int i)
                    {
                        isLightMode = (i == 1);
                    }
                }
            }
            catch
            {
                // レジストリ読み込み失敗時はデフォルト（ダークモード想定の白）
            }

            // ライトモードなら黒、ダークモードなら白
            var color = isLightMode ? Color.Black : Color.White;
            
            using var tempBitmap = CreateBitmapFromText("🎵", color);
            IntPtr hIcon = tempBitmap.GetHicon();
            
            try 
            {
                using var tempIcon = Icon.FromHandle(hIcon);
                _taskbarIcon.Icon = (Icon)tempIcon.Clone();
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr handle);

        /// <summary>
        /// テキストからビットマップを生成する
        /// </summary>
        private static Bitmap CreateBitmapFromText(string text, Color color)
        {
            int size = 32;
            var bitmap = new Bitmap(size, size);
            
            using var g = Graphics.FromImage(bitmap);

            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var font = new Font("Segoe UI Emoji", 24, System.Drawing.FontStyle.Regular, GraphicsUnit.Pixel);
            using var brush = new SolidBrush(color);

            var textSize = g.MeasureString(text, font);
            float x = (size - textSize.Width) / 2;
            float y = (size - textSize.Height) / 2;

            g.DrawString(text, font, brush, x, y);

            return bitmap;
        }
    }
}
