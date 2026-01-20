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
using System.Threading;

namespace Gamix.UI
{
    public partial class App : System.Windows.Application
    {
        public new static App Current => (App)System.Windows.Application.Current;
        public ServiceProvider Services { get; }
        private ITrayIconService? _trayIconService;
        private static Mutex? _mutex;
        private const string MutexName = "Global\\Gamix_Unique_Mutex_ID";

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
            services.AddSingleton<Gamix.Core.Services.IStartupService, Gamix.Core.Services.StartupService>();
            services.AddSingleton<ITrayIconService, TrayIconService>();

            // ViewModels
            services.AddSingleton<ViewModels.DeviceViewModel>();
            services.AddSingleton<ViewModels.SessionListViewModel>();
            services.AddSingleton<ViewModels.ThemeViewModel>();
            services.AddSingleton<MainViewModel>();

            // Views
            services.AddSingleton<MainWindow>();

            return services.BuildServiceProvider();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            // グローバル例外ハンドラーの登録
            SetupExceptionHandlers();

            Gamix.Core.Services.Logger.Info("========== Application Startup ==========");

            _mutex = new Mutex(true, MutexName, out bool createdNew);
            if (!createdNew)
            {
                Gamix.Core.Services.Logger.Info("Application already running. Shutting down this instance.");
                System.Windows.Application.Current.Shutdown();
                return;
            }

            base.OnStartup(e);

            bool startMinimized = false;
            foreach (var arg in e.Args)
            {
                if (arg.Equals("--minimized", StringComparison.OrdinalIgnoreCase))
                {
                    startMinimized = true;
                    break;
                }
            }
            
            // トレイアイコンの初期化
            _trayIconService = Services.GetRequiredService<ITrayIconService>();
            _trayIconService.Initialize();
            
            // テーマ画像のプリロード（初回表示時の遅延解消）
            var themes = new[] { Gamix.Core.Constants.Themes.Gaming, Gamix.Core.Constants.Themes.Pastel };
            _ = ViewModels.ThemeSelectionViewModel.PreloadImagesAsync(themes);

            // ViewModel の初期化完了を待ってからウィンドウを表示
            var mainViewModel = Services.GetRequiredService<MainViewModel>();
            await mainViewModel.InitializeAsync();

            var mainWindow = Services.GetRequiredService<MainWindow>();
            if (!startMinimized)
            {
                mainWindow.Show();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Gamix.Core.Services.Logger.Info("========== Application Exit ==========");
            _trayIconService?.Dispose();
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            base.OnExit(e);
        }

        private void SetupExceptionHandlers()
        {
            // UIスレッドの未処理例外
            DispatcherUnhandledException += (s, e) =>
            {
                Gamix.Core.Services.Logger.Fatal("UI thread unhandled exception", e.Exception);
                System.Windows.MessageBox.Show(
                    $"予期しないエラーが発生しました。ログファイルを確認してください。\n\n{e.Exception.Message}",
                    "エラー",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                e.Handled = true;
            };

            // バックグラウンドタスクの未処理例外
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Gamix.Core.Services.Logger.Fatal("Background task unhandled exception", e.Exception);
                e.SetObserved();
            };

            // その他の未処理例外
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    Gamix.Core.Services.Logger.Fatal("AppDomain unhandled exception", ex);
                }
                else
                {
                    Gamix.Core.Services.Logger.Error($"AppDomain unhandled non-exception: {e.ExceptionObject}");
                }
            };
        }
    }
}
