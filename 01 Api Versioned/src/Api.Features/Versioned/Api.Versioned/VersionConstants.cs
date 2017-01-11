using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;

namespace Api.Versioned
{
    public static class VersionConstants
    {
        static Dictionary<string, object> _defaultValues = new Dictionary<string, object>()
        {
            { ConfVersionHeader, VersionConstants.VersionHeader },
            { ConfVersionDefault, VersionConstants.VersionDefault }
        };

        //
        // Configuration Settings
        //
        public const string ConfVersionHeader = "configuration:api-header";
        public const string ConfVersionDefault = "configuration:api-version-default";

        //
        // Versioned Constants
        //
        public const string VersionHeader = "api-header";
        public const int VersionDefault = 1;

        public static T GetSettingOrDefaultValue<T>(string settingKey)
        {
            var configValue = ConfigurationManager.AppSettings[settingKey];

            if (!string.IsNullOrWhiteSpace(configValue))
            {
                try
                {
                    return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(configValue);
                }
                catch (Exception)
                {

                }
            }

            object value;

            _defaultValues.TryGetValue(settingKey, out value);

            if (value != null)
                return (T)Convert.ChangeType(value, typeof(T));
            throw new ConfigurationErrorsException($"Configuration not found: {settingKey}");
        }
    }
}
