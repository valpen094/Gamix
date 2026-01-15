using System;
using System.Windows;
using Gamix.UI.Services;
using Gamix.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Gamix.UI
{
    public partial class App : Application
    {
        public new static App Current => (App)Application.Current;
        public IServiceProvider Services { get; }

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

            // ViewModels
            services.AddSingleton<MainViewModel>();

            // Views
            services.AddSingleton<MainWindow>();

            return services.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var themeService = Services.GetRequiredService<IThemeService>();
            themeService.SetTheme("Pastel");

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
    }
}
