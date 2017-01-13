namespace Api.Helpers.Contracts.ConfigurationManagerHelpers
{
    public interface IConfigurationManagerHelper
    {
        T GetSettingOrDefaultValue<T>(string settingKey, T defaultValue);
    }
}
