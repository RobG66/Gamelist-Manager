using System;
using System.Reflection;
using System.Configuration;

namespace GamelistManager.classes
{
    public class SettingsHelper
    {
        // Method to get the default value of a property
        public static string? GetDefaultSetting(string propertyName)
        {
            // Get the property from the Settings class by its name
            PropertyInfo? property = typeof(Properties.Settings).GetProperty(propertyName);

            if (property != null)
            {
                // Get the DefaultSettingValue attribute if it exists
                var defaultValueAttribute = (DefaultSettingValueAttribute?)property
                    .GetCustomAttributes(typeof(DefaultSettingValueAttribute), false)
                    .FirstOrDefault();

                if (defaultValueAttribute != null)
                {
                    // Return the default value as a string
                    return defaultValueAttribute.Value;
                }
            }

            // Return null if the property or default value is not found
            return null;
        }
    }
}
