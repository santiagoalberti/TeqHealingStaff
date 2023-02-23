using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ItemManager;
using JetBrains.Annotations;
using ServerSync;
using System;
using System.IO;
using System.Reflection;

namespace TeqHealingStaff
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class TeqHealingStaffPlugin : BaseUnityPlugin
    {
        internal const string ModName = "TeqHealingStaff";
        internal const string ModVersion = "1.0.0";
        internal const string Author = "Tequila";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

        internal static string ConnectionError = "";

        private readonly Harmony _harmony = new(ModGUID);

        public static readonly ManualLogSource TeqHealingStaffLogger =
            BepInEx.Logging.Logger.CreateLogSource(ModName);

        private static readonly ConfigSync ConfigSync = new(ModGUID)
        { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        public void Awake()
        {
            _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On,
                "If on, the configuration is locked and can be changed by server admins only.");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

            string defaultEnglishName = "Healing Staff";
            Item teqHealingStaff = new("teq_sur_healing_staff_bundle", "teq_sur_healing_staff_new", defaultEnglishName);

            PatchHealingStats(teqHealingStaff);

            teqHealingStaff.Name.English(defaultEnglishName);
            teqHealingStaff.Description.English("A healing staff.");
            teqHealingStaff.Crafting.Add("forge", 1);
            teqHealingStaff.RequiredItems.Add("Iron", 120);
            teqHealingStaff.RequiredItems.Add("WolfFang", 20);
            teqHealingStaff.RequiredItems.Add("Silver", 40);
            teqHealingStaff.RequiredUpgradeItems.Add("Iron", 20);
            teqHealingStaff.RequiredUpgradeItems.Add("Silver", 10);
            teqHealingStaff.CraftAmount = 1;

            ItemManager.PrefabManager.RegisterPrefab(PrefabManager.RegisterAssetBundle("teq_sur_healing_staff_bundle"), "teq_sur_vfx_blocked", false);
            ItemManager.PrefabManager.RegisterPrefab(PrefabManager.RegisterAssetBundle("teq_sur_healing_staff_bundle"), "teq_sur_healing_aoe", false);
            ItemManager.PrefabManager.RegisterPrefab(PrefabManager.RegisterAssetBundle("teq_sur_healing_staff_bundle"), "teq_sur_fx_shield_start", false);

            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }

        // START HEALING STATS //

        private static void PatchHealingStats(Item item)
        {
            var drop = item.Prefab.GetComponent<ItemDrop>();

            if (drop == null)
            {
                return;
            }

            var shared = drop.m_itemData.m_shared;

            if (shared == null)
            {
                return;
            }

            if (shared.m_attackStatusEffect is SE_Stats se_stats)
            {
                var customHeal = SE_ScalingHeal.ConvertSEStatsToCustomHeal(se_stats, ScalingHealFunction);

                shared.m_attackStatusEffect = customHeal;
            }
        }

        private static float ScalingHealFunction(float skillLevel)
        {
            return skillLevel / 5f;
        }

        // END HEALING STATS //

        private void OnDestroy()
        {
            Config.Save();
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                TeqHealingStaffLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                TeqHealingStaffLogger.LogError($"There was an issue loading your {ConfigFileName}");
                TeqHealingStaffLogger.LogError("Please check your config entries for spelling and format!");
            }
        }

        #region ConfigOptions

        private static ConfigEntry<Toggle> _serverConfigLocked = null!;
        private static ConfigEntry<Toggle> _recipeIsActiveConfig = null!;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order;
            [UsedImplicitly] public bool? Browsable;
            [UsedImplicitly] public string? Category;
            [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer;
        }

        #endregion ConfigOptions
    }
}