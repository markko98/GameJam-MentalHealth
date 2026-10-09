// Shared by editor tools that need the on-disk paths for EntryPoint's config/catalog
// assets — ConfigureWindow creates them, ChecklistWindow verifies them.
public static class ConfigAssetPaths
{
    public const string ConfigFolder = "Assets/Application/Modules/Config";
    public const string ResourcesFolder = ConfigFolder + "/Resources";

    public const string EnvironmentConfigPath = ResourcesFolder + "/EnvironmentConfig.asset";
    public const string IapCatalogPath = ConfigFolder + "/IAPCatalog.asset";
    public const string AdUnitConfigPath = ConfigFolder + "/AdUnitConfig.asset";
    public const string UnityAdsConfigPath = ConfigFolder + "/UnityAdsConfig.asset";
    public const string TaskCatalogPath = ConfigFolder + "/TaskCatalog.asset";
    public const string TrackingConfigPath = ConfigFolder + "/TrackingConfig.asset";
    public const string SoundCatalogPath = "Assets/Application/Core/AudioNew/Resources/SoundCatalog.asset";
}
