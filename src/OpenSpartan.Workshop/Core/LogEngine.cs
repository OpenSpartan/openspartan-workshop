using NLog;
using OpenSpartan.Workshop.Models;
using OpenSpartan.Workshop.ViewModels;
using System.Runtime.CompilerServices;

namespace OpenSpartan.Workshop.Core
{
    internal class LogEngine
    {
        private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

        internal static void Log(string message, LogSeverity severity = LogSeverity.Info, [CallerMemberName] string caller = null)
        {
            if (SettingsViewModel.Instance.EnableLogging)
            {
                switch (severity)
                {
                    case LogSeverity.Warning:
                        Logger.Warn($"[{caller}] {message}");
                        break;
                    case LogSeverity.Error:
                        Logger.Error($"[{caller}] {message}");
                        break;
                    case LogSeverity.Info:
                        Logger.Info($"[{caller}] {message}");
                        break;
                    default:
                        Logger.Info($"[{caller}] {message}");
                        break;
                }
            }
        }
    }
}
