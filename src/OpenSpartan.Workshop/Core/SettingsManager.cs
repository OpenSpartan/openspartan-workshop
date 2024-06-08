using NLog;
using OpenSpartan.Workshop.Models;
using System;
using System.IO;
using System.Text.Json;

namespace OpenSpartan.Workshop.Core
{
    internal sealed class SettingsManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        internal static WorkshopSettings? LoadSettings()
        {
            try
            {
                return JsonSerializer.Deserialize<WorkshopSettings>(File.ReadAllText(Path.Combine(Configuration.AppDataDirectory, Configuration.SettingsFileName)), UserContextManager.SerializerOptions);
            }
            catch (Exception ex)
            {
                // Assume that here we cannot check for settings yet and we need to
                // always log an error. We are also intentionally not using the log
                // wrapper class here because we don't have the settings initialized yet.
                Logger.Error($"Could not load settings. {ex.Message}");
                return null;
            }
        }

        internal static bool StoreSettings(WorkshopSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            try
            {
                if (settings != null)
                {
                    UserContextManager.EnsureDirectoryExists(Path.Combine(Configuration.AppDataDirectory, Configuration.SettingsFileName));
                    File.WriteAllText(Path.Combine(Configuration.AppDataDirectory, Configuration.SettingsFileName), JsonSerializer.Serialize(settings));
                    return true;
                }
                else
                {
                    throw new ArgumentNullException(nameof(settings));
                }
            }
            catch (Exception ex)
            {
                LogEngine.Log($"Could not store settings. {ex.Message}", LogSeverity.Error);
                return false;
            }
        }
    }
}
