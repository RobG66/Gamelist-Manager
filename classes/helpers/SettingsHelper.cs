using System.Configuration;
using System.Reflection;

namespace GamelistManager.classes.helpers
{
    public class SettingsHelper
    {
        // Method to get the default Value of a property
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
                    // Return the default Value as a string
                    return defaultValueAttribute.Value;
                }
            }

            // Return null if the property or default Value is not found
            return null;
        }
    }
}
