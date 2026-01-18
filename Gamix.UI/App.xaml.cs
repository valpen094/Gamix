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
        private ITrayIconService? _trayIconService;

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

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // トレイアイコンの初期化
            _trayIconService = Services.GetRequiredService<ITrayIconService>();
            _trayIconService.Initialize();
            
            // テーマ画像のプリロード（初回表示時の遅延解消）
            var themes = new[] { Gamix.Core.Constants.Themes.Gaming, Gamix.Core.Constants.Themes.Pastel };
            _ = ViewModels.ThemeSelectionViewModel.PreloadImagesAsync(themes);

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _trayIconService?.Dispose();
            base.OnExit(e);
        }
    }
}
