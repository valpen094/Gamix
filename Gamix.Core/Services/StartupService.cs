using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Gamix.Core.Services
{
    public class StartupService : IStartupService
    {
        private const string ShortcutName = "Gamix.lnk";

        private static string GetStartupPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), ShortcutName);
        }

        public bool IsStartupEnabled()
        {
            var path = GetStartupPath();
            return File.Exists(path);
        }

        public void ToggleStartup(bool enable)
        {
            var path = GetStartupPath();

            if (enable)
            {
                CreateShortcut(path);
            }
            else
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        private static void CreateShortcut(string shortcutPath)
        {
            try
            {
                var exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath))
                {
                    Logger.Warning("StartupService: Unable to get current process path");
                    return;
                }

                // Use WScript.Shell to create shortcut
                Type? t = Type.GetTypeFromProgID("WScript.Shell");
                if (t == null)
                {
                    Logger.Warning("StartupService: WScript.Shell COM component not available");
                    return;
                }

                dynamic? shell = Activator.CreateInstance(t);
                if (shell == null)
                {
                    Logger.Warning("StartupService: Failed to create WScript.Shell instance");
                    return;
                }

                var shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = exePath;
                shortcut.Arguments = "--minimized";
                shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
                shortcut.Description = "Gamix Audio Controller";
                shortcut.Save();
                
                Logger.Info($"StartupService: Created startup shortcut at {shortcutPath}");
            }
            catch (Exception ex)
            {
                Logger.Error("StartupService: Failed to create startup shortcut", ex);
            }
        }
    }
}
