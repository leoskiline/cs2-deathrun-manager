using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CounterStrikeSharp.API;

namespace DeathrunManager
{
    internal class PluginLogger
    {
        private PluginSettings? _settings;
        private readonly object _lockObject = new();
        private string _logDirectory = string.Empty;

        public void Initialize(PluginSettings settings)
        {
            _settings = settings;
            _logDirectory = Path.Combine(Server.GameDirectory, "csgo","addons", "counterstrikesharp","logs", "deathrun_manager");

            try
            {
                Directory.CreateDirectory(_logDirectory);
                CleanOldLogs();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DR Manager] Erro ao inicializar sistema de logs: {ex.Message}");
            }
        }

        public void LogInfo(string message)
        {
            WriteLog("INFO", message);
        }

        public void LogWarning(string message)
        {
            WriteLog("WARN", message);
        }

        public void LogError(string message, Exception? exception = null)
        {
            var fullMessage = exception != null ? $"{message} - Exception: {exception}" : message;
            WriteLog("ERROR", fullMessage);
        }

        public void LogDebug(string message)
        {
            if (_settings?.EnableDetailedLogging == true)
            {
                WriteLog("DEBUG", message);
            }
        }

        private void WriteLog(string level, string message)
        {
            if (_settings?.EnableDetailedLogging != true && level == "DEBUG")
                return;

            lock (_lockObject)
            {
                try
                {
                    var logFileName = $"dr_manager_{DateTime.Now:yyyy-MM-dd}.log";
                    var logFilePath = Path.Combine(_logDirectory, logFileName);
                    var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

                    File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DR Manager] Erro ao escrever log: {ex.Message}");
                }
            }
        }

        public void CleanOldLogs()
        {
            if (_settings == null || string.IsNullOrEmpty(_logDirectory))
                return;

            try
            {
                var cutoffDate = DateTime.Now.AddDays(-_settings.LogRetentionDays);
                var logFiles = Directory.GetFiles(_logDirectory, "dr_manager_*.log");

                foreach (var logFile in logFiles)
                {
                    var fileInfo = new FileInfo(logFile);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(logFile);
                        Console.WriteLine($"[DR Manager] Log antigo removido: {fileInfo.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DR Manager] Erro ao limpar logs antigos: {ex.Message}");
            }
        }
    }
}
