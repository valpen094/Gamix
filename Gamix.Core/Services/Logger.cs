using System;
using System.IO;
using System.Text;

namespace Gamix.Core.Services
{
    /// <summary>
    /// アプリケーション全体で使用する統一ロガー。
    /// ログは %AppData%\Gamix\logs に保存され、日次でローテーションされます。
    /// </summary>
    public static class Logger
    {
        private static readonly object _lock = new();
        private static readonly string _logDirectory;
        private static string? _currentLogFile;

        static Logger()
        {
#if DEBUG
            return;
#endif
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _logDirectory = Path.Combine(appData, "Gamix", "logs");
            
            try
            {
                Directory.CreateDirectory(_logDirectory);
                CleanOldLogs();
            }
            catch
            {
                // ログディレクトリ作成に失敗しても継続（ログは出力できない）
            }
        }

        /// <summary>
        /// Debugレベルのログを記録します。
        /// </summary>
        public static void Debug(string message)
        {
#if DEBUG
            WriteLog("DEBUG", message);
#endif
        }

        /// <summary>
        /// Infoレベルのログを記録します。
        /// </summary>
        public static void Info(string message)
        {
            WriteLog("INFO", message);
        }

        /// <summary>
        /// Warningレベルのログを記録します。
        /// </summary>
        public static void Warning(string message)
        {
            WriteLog("WARN", message);
        }

        /// <summary>
        /// Warningレベルのログを記録します（例外付き）。
        /// </summary>
        public static void Warning(string message, Exception ex)
        {
            var fullMessage = $"{message}\n{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
            WriteLog("WARN", fullMessage);
        }

        /// <summary>
        /// Errorレベルのログを記録します。
        /// </summary>
        public static void Error(string message, Exception? ex = null)
        {
            var fullMessage = ex != null 
                ? $"{message}\n{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}"
                : message;
            WriteLog("ERROR", fullMessage);
        }

        /// <summary>
        /// クラッシュ情報を専用ファイルに記録します。
        /// </summary>
        public static void Fatal(string message, Exception ex)
        {
            var crashLog = new StringBuilder();
            crashLog.AppendLine("=== FATAL ERROR ===");
            crashLog.AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            crashLog.AppendLine($"Message: {message}");
            crashLog.AppendLine($"Exception: {ex.GetType().FullName}");
            crashLog.AppendLine($"Message: {ex.Message}");
            crashLog.AppendLine($"StackTrace:\n{ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                crashLog.AppendLine($"\nInner Exception: {ex.InnerException.GetType().FullName}");
                crashLog.AppendLine($"Message: {ex.InnerException.Message}");
                crashLog.AppendLine($"StackTrace:\n{ex.InnerException.StackTrace}");
            }

            WriteLog("FATAL", crashLog.ToString());
            
            // クラッシュログは専用ファイルにも保存
            try
            {
#if DEBUG
                return;
#endif
                var crashFile = Path.Combine(_logDirectory, "crash.log");
                File.AppendAllText(crashFile, crashLog.ToString() + "\n\n");
            }
            catch
            {
                // クラッシュログ保存失敗は無視（どうしようもない）
            }
        }

        private static void WriteLog(string level, string message)
        {
            lock (_lock)
            {
                try
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var logEntry = $"[{timestamp}] [{level}] {message}\n";

#if DEBUG
                    System.Diagnostics.Debug.Write(logEntry);
#else
                    var logFile = GetCurrentLogFile();
                    File.AppendAllText(logFile, logEntry, Encoding.UTF8);
#endif
                }
                catch
                {
                    // ログ書き込み失敗は無視（無限ループを防ぐ）
                }
            }
        }

        private static string GetCurrentLogFile()
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var expectedFile = Path.Combine(_logDirectory, $"gamix_{today}.log");
            
            if (_currentLogFile != expectedFile)
            {
                _currentLogFile = expectedFile;
            }
            
            return _currentLogFile;
        }

        /// <summary>
        /// 30日より古いログファイルを削除します。
        /// </summary>
        private static void CleanOldLogs()
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-30);
                var logFiles = Directory.GetFiles(_logDirectory, "gamix_*.log");
                
                foreach (var file in logFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < cutoffDate)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch
            {
                // クリーンアップ失敗は無視
            }
        }
    }
}
