using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Gamix.Core;
using Gamix.UI.ViewModels;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using CommunityToolkit.Mvvm.Input;
using Gamix.Core.Models;

namespace Gamix.UI.Services
{
    public interface ITrayIconService : IDisposable
    {
        void Initialize();
    }

    public class TrayIconService : ITrayIconService
    {
        private readonly IServiceProvider _serviceProvider;
        private TaskbarIcon? _taskbarIcon;

        public TrayIconService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Initialize()
        {
            SetupTrayIcon();
        }

        private void SetupTrayIcon()
        {
            _taskbarIcon = new TaskbarIcon
            {
                ToolTipText = Constants.AppName,
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

        private ContextMenu CreateTrayContextMenu()
        {
            var contextMenu = new ContextMenu();
            
            // App.xaml で定義したスタイルを適用
            if (System.Windows.Application.Current.TryFindResource("TrayMenuStyle") is Style menuStyle)
            {
                contextMenu.Style = menuStyle;
            }

            var menuItemStyle = System.Windows.Application.Current.TryFindResource("TrayMenuItemStyle") as Style;
            var separatorStyle = System.Windows.Application.Current.TryFindResource("TraySeparatorStyle") as Style;

            // 開く
            var openItem = new MenuItem { Header = Constants.AppName + "!", Tag = Constants.TrayIcons.Open };
            if (menuItemStyle != null) openItem.Style = menuItemStyle;
            openItem.Click += (s, e) => ShowMainWindow();
            contextMenu.Items.Add(openItem);

            // セパレーター (開く と プリセットの間)
            var separator1 = new Separator();
            if (separatorStyle != null) separator1.Style = separatorStyle;
            contextMenu.Items.Add(separator1);

            // 音量ミキサー (サブメニュー: 専用スタイル)
            var volumeMixerItem = new MenuItem { Header = "音量ミキサー", Tag = "🎚️" };
            var volumeMenuParentStyle = System.Windows.Application.Current.TryFindResource("VolumeMenuParentStyle") as Style;
            volumeMixerItem.Style = volumeMenuParentStyle ?? menuItemStyle;
            contextMenu.Items.Add(volumeMixerItem);

            // 出力デバイス (サブメニュー)
            var outputDevicesItem = new MenuItem { Header = "出力デバイス", Tag = Constants.TrayIcons.OutputDevice };
            if (menuItemStyle != null) outputDevicesItem.Style = menuItemStyle;
            contextMenu.Items.Add(outputDevicesItem);

            // 入力デバイス (サブメニュー)
            var inputDevicesItem = new MenuItem { Header = "入力デバイス", Tag = Constants.TrayIcons.InputDevice };
            if (menuItemStyle != null) inputDevicesItem.Style = menuItemStyle;
            contextMenu.Items.Add(inputDevicesItem);

            // セパレーター
            var separator2 = new Separator();
            if (separatorStyle != null) separator2.Style = separatorStyle;
            contextMenu.Items.Add(separator2);

            // プリセット (サブメニュー)
            var presetsItem = new MenuItem { Header = "プリセット", Tag = Constants.TrayIcons.Preset };
            if (menuItemStyle != null) presetsItem.Style = menuItemStyle;
            contextMenu.Items.Add(presetsItem);

            // 設定 (サブメニュー)
            var settingsItem = new MenuItem { Header = "設定", Tag = "⚙" };
            if (menuItemStyle != null) settingsItem.Style = menuItemStyle;
            contextMenu.Items.Add(settingsItem);

            // セパレーター
            var separator = new Separator();
            if (separatorStyle != null) separator.Style = separatorStyle;
            contextMenu.Items.Add(separator);

            // 終了
            var exitItem = new MenuItem { Header = "終了", Tag = Constants.TrayIcons.Exit };
            if (menuItemStyle != null) exitItem.Style = menuItemStyle;
            exitItem.Click += (s, e) => ShutdownApp();
            contextMenu.Items.Add(exitItem);

            // メニューが開かれる直前に動的に一覧を生成
            contextMenu.Opened += (s, e) =>
            {
                PopulatePresetsMenu(presetsItem, menuItemStyle);
                PopulateOutputDevicesMenu(outputDevicesItem, menuItemStyle);
                PopulateInputDevicesMenu(inputDevicesItem, menuItemStyle);
                PopulateSettingsMenu(settingsItem, menuItemStyle);

                var volumeMenuParentStyle = System.Windows.Application.Current.TryFindResource("VolumeMenuParentStyle") as Style;
                PopulateVolumeMixerMenu(volumeMixerItem, volumeMenuParentStyle ?? menuItemStyle);
            };

            return contextMenu;
        }

        private void PopulateSettingsMenu(MenuItem settingsItem, Style? menuItemStyle)
        {
            settingsItem.Items.Clear();
            var viewModel = _serviceProvider.GetRequiredService<MainViewModel>();

            var startupItem = new MenuItem
            {
                Header = "Windows起動時に実行",
                IsCheckable = false, // カスタムチェックマークを使うのでfalse
                Tag = viewModel.IsStartupEnabled ? Constants.TrayIcons.Checkmark : "",
                StaysOpenOnClick = true
            };
            if (menuItemStyle != null) startupItem.Style = menuItemStyle;

            startupItem.Click += (s, e) =>
            {
                viewModel.IsStartupEnabled = !viewModel.IsStartupEnabled;
                startupItem.Tag = viewModel.IsStartupEnabled ? Constants.TrayIcons.Checkmark : "";
            };
            settingsItem.Items.Add(startupItem);
        }

        private void PopulatePresetsMenu(MenuItem presetsItem, Style? menuItemStyle)
        {
            try
            {
                presetsItem.Items.Clear();

                var viewModel = _serviceProvider.GetRequiredService<MainViewModel>();
                var currentPresets = viewModel.Presets.ToList();

                if (currentPresets.Count != 0)
                {
                    string currentPresetName = viewModel.CurrentPresetName ?? "";

                    foreach (var preset in currentPresets)
                    {
                        var item = new MenuItem
                        {
                            Header = preset.Name,
                            IsCheckable = false,
                            Tag = preset.Name == currentPresetName ? Constants.TrayIcons.Checkmark : ""
                        };
                        if (menuItemStyle != null) item.Style = menuItemStyle;

                        item.Click += async (sender, args) =>
                        {
                            var capturedPreset = preset;
                            if (viewModel.ApplyPresetCommand.CanExecute(capturedPreset))
                            {
                                if (viewModel.ApplyPresetCommand is IAsyncRelayCommand<Preset> asyncCommand)
                                {
                                    await asyncCommand.ExecuteAsync(capturedPreset);
                                }
                                else
                                {
                                    viewModel.ApplyPresetCommand.Execute(capturedPreset);
                                }
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

        private void PopulateOutputDevicesMenu(MenuItem devicesItem, Style? menuItemStyle)
        {
            try
            {
                devicesItem.Items.Clear();

                var viewModel = _serviceProvider.GetRequiredService<MainViewModel>();
                var devices = viewModel.Devices.OutputDevices.ToList();
                var selectedDevice = viewModel.Devices.SelectedOutputDevice;

                if (devices.Count != 0)
                {
                    foreach (var device in devices)
                    {
                        var item = new MenuItem
                        {
                            Header = device.Name,
                            Tag = device.Id == selectedDevice?.Id ? Constants.TrayIcons.Checkmark : "",
                            StaysOpenOnClick = true
                        };
                        if (menuItemStyle != null) item.Style = menuItemStyle;

                        item.Click += (sender, args) =>
                        {
                            viewModel.Devices.SelectedOutputDevice = device;
                            PopulateOutputDevicesMenu(devicesItem, menuItemStyle);
                        };
                        devicesItem.Items.Add(item);
                    }
                    devicesItem.IsEnabled = true;
                }
                else
                {
                    devicesItem.IsEnabled = false;
                }
            }
            catch
            {
                devicesItem.IsEnabled = false;
            }
        }

        private void PopulateInputDevicesMenu(MenuItem devicesItem, Style? menuItemStyle)
        {
            try
            {
                devicesItem.Items.Clear();

                var viewModel = _serviceProvider.GetRequiredService<MainViewModel>();
                var devices = viewModel.Devices.InputDevices.ToList();
                var selectedDevice = viewModel.Devices.SelectedInputDevice;

                if (devices.Count != 0)
                {
                    foreach (var device in devices)
                    {
                        var item = new MenuItem
                        {
                            Header = device.Name,
                            Tag = device.Id == selectedDevice?.Id ? Constants.TrayIcons.Checkmark : "",
                            StaysOpenOnClick = true
                        };
                        if (menuItemStyle != null) item.Style = menuItemStyle;

                        item.Click += (sender, args) =>
                        {
                            viewModel.Devices.SelectedInputDevice = device;
                            PopulateInputDevicesMenu(devicesItem, menuItemStyle);
                        };
                        devicesItem.Items.Add(item);
                    }
                    devicesItem.IsEnabled = true;
                }
                else
                {
                    devicesItem.IsEnabled = false;
                }
            }
            catch
            {
                devicesItem.IsEnabled = false;
            }
        }

        private void PopulateVolumeMixerMenu(MenuItem volumeMixerItem, Style? menuItemStyle)
        {
            try
            {
                volumeMixerItem.Items.Clear();

                var viewModel = _serviceProvider.GetRequiredService<MainViewModel>();
                var visibleSessions = viewModel.Sessions.Sessions.ToList(); // コピーを作成

                // Master Volume
                var masterVolumeTemplate = System.Windows.Application.Current.TryFindResource("MasterVolumeItemTemplate") as DataTemplate;
                var volumeMenuItemStyle = System.Windows.Application.Current.TryFindResource("VolumeMenuItemStyle") as Style;
                
                var masterItem = new MenuItem
                {
                    Header = viewModel.Devices, // DeviceViewModel
                    HeaderTemplate = masterVolumeTemplate,
                    StaysOpenOnClick = true,
                    IsCheckable = false
                };
                
                if (volumeMenuItemStyle != null) masterItem.Style = volumeMenuItemStyle;
                else if (menuItemStyle != null) masterItem.Style = menuItemStyle;
                
                volumeMixerItem.Items.Add(masterItem);

                // App Sessions
                if (visibleSessions.Count != 0)
                {
                    var volumeItemTemplate = System.Windows.Application.Current.TryFindResource("VolumeItemTemplate") as DataTemplate;
                    // volumeMenuItemStyle already retrieved above

                    foreach (var session in visibleSessions)
                    {
                        var item = new MenuItem
                        {
                            Header = session,
                            HeaderTemplate = volumeItemTemplate,
                            StaysOpenOnClick = true,
                            IsCheckable = false
                        };
                        
                        // Default to passed style, but override if specific style found
                        if (menuItemStyle != null) item.Style = menuItemStyle;
                        if (volumeMenuItemStyle != null) item.Style = volumeMenuItemStyle;
                        
                        volumeMixerItem.Items.Add(item);
                    }
                    volumeMixerItem.IsEnabled = true;
                }
                else
                {
                    // No sessions, but we have Master Volume so it should be enabled
                    volumeMixerItem.IsEnabled = true; 
                }
            }
            catch
            {
                volumeMixerItem.IsEnabled = false;
            }
        }

        private void ShowMainWindow()
        {
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
            if (mainWindow.WindowState == WindowState.Minimized)
            {
                mainWindow.WindowState = WindowState.Normal;
            }
            mainWindow.Activate();
        }

        private void ShutdownApp()
        {
            Dispose();
            System.Windows.Application.Current.Shutdown();
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
            
            using var tempBitmap = CreateBitmapFromText(Constants.TrayIcons.Open, color);
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

        private static void TryPromoteIcon()
        {
            try
            {
                string exeName = Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty);
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

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr handle);

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

        public void Dispose()
        {
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
            _taskbarIcon?.Dispose();
            _taskbarIcon = null; // Ensure we don't dispose again or use it
        }
    }
}
