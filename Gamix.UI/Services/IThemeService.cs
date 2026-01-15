using System;

namespace Gamix.UI.Services
{
    public interface IThemeService
    {
        void SetTheme(string themeName);
        string CurrentTheme { get; }
    }
}
