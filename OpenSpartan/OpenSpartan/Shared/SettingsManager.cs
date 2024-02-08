using OpenSpartan.Workshop.Core;
using OpenSpartan.Workshop.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace OpenSpartan.Workshop.Shared
{
    internal class SettingsManager
    {
        internal static WorkshopSettings LoadSettings()
        {
            try
            {
                return JsonSerializer.Deserialize<WorkshopSettings>(File.ReadAllText(Path.Combine(Configuration.AppDataDirectory, Configuration.SettingsFileName)));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Could not load settings. {ex.Message}");
                return null;
            }
        }

        internal static bool StoreSettings(WorkshopSettings settings)
        {
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
                Debug.WriteLine($"Could not store settings. {ex.Message}");
                return false;
            }
        }
    }
}
