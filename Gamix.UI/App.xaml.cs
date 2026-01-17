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
        public ServiceProvider Services { get; }
        private Forms.NotifyIcon? _notifyIcon;

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
            // 利用可能なテーマ一覧を取得して非同期で読み込む
            var themes = new[] { "Gaming", "Pastel" };
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

            // アイコンの初期設定（テーマに合わせて動的生成）
            UpdateTrayIcon();

            // システムのテーマ変更等を監視
            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

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

            // プリセットメニューの追加
            var presetsMenuItem = new Forms.ToolStripMenuItem("プリセット");
            contextMenu.Items.Add(presetsMenuItem);

            // メニューが開かれる直前に動的にプリセット一覧を生成
            contextMenu.Opening += (s, e) =>
            {
                try
                {
                    // GDIリソースリークを防ぐため、Clear()前に既存項目をDisposeする
                    // foreachでコレクションを変更しないよう、先に配列にコピー
                    var itemsToDispose = presetsMenuItem.DropDownItems.Cast<Forms.ToolStripItem>().ToArray();
                    presetsMenuItem.DropDownItems.Clear();
                    foreach (var item in itemsToDispose)
                    {
                        item.Dispose();
                    }
                    
                    // ViewModel から現在のプリセット一覧を取得
                    // WPF Dispatcher経由でスレッドセーフにアクセス
                    var viewModel = Services.GetRequiredService<MainViewModel>();
                    List<Gamix.Core.Models.Preset> currentPresets = [];
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        currentPresets = viewModel.Presets.ToList();
                    });

                    if (currentPresets.Count != 0)
                    {
                        // 現在適用中のプリセット名を取得
                        string currentPresetName = "";
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            currentPresetName = viewModel.NewPresetName ?? "";
                        });
                        
                        foreach (var preset in currentPresets)
                        {
                            var item = new Forms.ToolStripMenuItem(preset.Name);
                            
                            // 現在適用中のプリセットにチェックマークを表示
                            item.Checked = preset.Name == currentPresetName;
                            
                            item.Click += (sender, args) =>
                            {
                                // プリセット適用コマンドを実行 (WPF Dispatcher経由)
                                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                {
                                    if (viewModel.ApplyPresetCommand.CanExecute(preset))
                                    {
                                        viewModel.ApplyPresetCommand.Execute(preset);
                                    }
                                });
                            };
                            presetsMenuItem.DropDownItems.Add(item);
                        }
                        presetsMenuItem.Enabled = true;
                    }
                    else
                    {
                        presetsMenuItem.Enabled = false;
                    }
                }
                catch (Exception)
                {
                    // エラーが発生してもメニュー表示は継続（空の状態で表示）
                    presetsMenuItem.Enabled = false;
                }
            };

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
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            System.Windows.Application.Current.Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
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
            if (_notifyIcon == null) return;

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
                // レジストリ読み込み失敗時はデフォルト（ダークモード想定の白）にする
            }

            // ライトモードなら黒、ダークモードなら白
            var color = isLightMode ? Color.Black : Color.White;
            
            // 古いアイコンがあれば破棄
            var oldIcon = _notifyIcon.Icon;
            
            // 新しいアイコンを設定
            // 注意: Icon.FromHandleで作ったアイコンは元のハンドルを所有しないため
            // 呼び出し元が責任を持ってDestroyIconする必要がある。
            // しかし、Iconクラスの仕様としてFromHandleで作成したIconをDisposeしても元のハンドルは消えないため
            // 自分で管理する必要がある。
            
            using var tempBitmap = CreateBitmapFromText("🎵", color);
            IntPtr hIcon = tempBitmap.GetHicon();
            
            try 
            {
                // FromHandleで作成したIconは、内部でハンドルをコピーするわけではなくラップするだけ。
                // ただし、System.Drawing.IconのコンストラクタやCloneを使うことで所有権を移動またはコピーできる。
                // ここでは安全のため、FromHandleで一時的に作成し、それをCloneしてNotifyIconに渡し、
                // 元のハンドルは即座に破棄するパターンを採用する。
                using var tempIcon = Icon.FromHandle(hIcon);
                _notifyIcon.Icon = (Icon)tempIcon.Clone();
            }
            finally
            {
                DestroyIcon(hIcon);
            }
            
            if (oldIcon != null && oldIcon != SystemIcons.Application)
            {
                oldIcon.Dispose();
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
            // トレイアイコン用に 32x32 で描画
            int size = 32;
            var bitmap = new Bitmap(size, size);
            
            using var g = Graphics.FromImage(bitmap);

            // 高品質な描画設定
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // 背景は透過
            g.Clear(Color.Transparent);

            // フォントとブラシの設定
            // "Segoe UI Emoji" があれば絵文字が綺麗に出るが、なければデフォルトでフォールバック
            using var font = new Font("Segoe UI Emoji", 24, System.Drawing.FontStyle.Regular, GraphicsUnit.Pixel);
            using var brush = new SolidBrush(color);

            // 中央揃えで描画
            var textSize = g.MeasureString(text, font);
            float x = (size - textSize.Width) / 2;
            float y = (size - textSize.Height) / 2;

            g.DrawString(text, font, brush, x, y);

            return bitmap;
        }
    }
}
