using Api.Helpers.Contracts.ConfigurationManagerHelpers;
using System;
using System.ComponentModel;
using System.Configuration;

namespace Api.Helpers.Core.ConfigurationManagerHelpers
{
    public class ConfigurationManagerHelper : IConfigurationManagerHelper
    {
        public T GetSettingOrDefaultValue<T>(string settingKey, T defaultValue)
        {
            var configValue = ConfigurationManager.AppSettings[settingKey];

            if (!string.IsNullOrWhiteSpace(configValue))
            {
                try
                {
                    return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(configValue);
                }
                catch (Exception e)
                {
                    throw new ConfigurationErrorsException($"Configuration error: {settingKey}", e);
                }
            }

            return defaultValue;
        }
    }
}
