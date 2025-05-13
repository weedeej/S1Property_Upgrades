using HarmonyLib;
using MelonLoader;
using MelonLoader.Utils;
using PropertyUpgrades.BuilderNPC;
using ScheduleOne.Delivery;
using ScheduleOne.EntityFramework;
using ScheduleOne.ObjectScripts;
using ScheduleOne.Persistence;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Property;
using System.Collections;
using UnityEngine;

[assembly: MelonInfo(typeof(PropertyUpgrades.Core), "PropertyUpgrades", "1.0.2", "weedeej", null)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace PropertyUpgrades
{
    [HarmonyPatch(typeof(Property), nameof(Property.Awake))]
    public static class PropertyAwakePatch
    {
        public static void Postfix(Property __instance)
        {
            if (__instance.PropertyName == "RV")
            {
                return;
            }

            ModSaveManager modSaveManager = new ModSaveManager().Load();
            if (modSaveManager.saveData.ContainsKey(__instance.PropertyName))
            {
                PropertyData propertyData = modSaveManager.saveData[__instance.PropertyName];
                ModUtilities.ApplyPropertyData(__instance, propertyData);
            }
        }
    }

    [HarmonyPatch(typeof(MixingStation), nameof(MixingStation.Start))]
    public static class MixtimePatch
    {
        public static void Postfix(MixingStation __instance)
        {
            string propertyName = __instance.ParentProperty.PropertyName;
            if (propertyName == "RV") return;
            ModSaveManager modSaveManager = new ModSaveManager().LoadTemp();
            int reduction = modSaveManager.saveData[propertyName].MixTimePerItemReduction;
            if (modSaveManager.saveData.ContainsKey(propertyName) && reduction > 0)
                ModUtilities.ApplyMixingUpgrade(__instance, reduction);
        }
    }
    public class Core : MelonMod
    {
        public override void OnInitializeMelon()
        {
            MelonPreferences.Load();
            MelonPreferences.CreateCategory("PropertyUpgrades", "Property Upgrades");
            MelonPreferences.CreateEntry("PropertyUpgrades", "MaxEmployeeCount", 15, "Max Employee Count", "Maximum employee count a property can have");
            MelonPreferences.CreateEntry("PropertyUpgrades", "MaxLoadingDocks", 5, "Max Loading Docks", "Maximum loading docks a player can buy for a property");
            MelonPreferences.CreateEntry("PropertyUpgrades", "MaxAdditionalGrowthRate", 5.0, "Max Add Growth Rate", "Maximum additional growth rate a player can get");
            MelonPreferences.CreateEntry("PropertyUpgrades", "MaxMixTimeReduction", 4, "Max Mix Time Reduction", "Maximum time(seconds) PER ITEM reduction a mixer can have (mk1 & 2)");
            MelonPreferences_Category translation = MelonPreferences.CreateCategory("PropertyUpgrades_Translation", "Property Upgrades Translation");
            translation.SetFilePath(Path.Combine(MelonEnvironment.UserDataDirectory, "PropertyUpgrades_Translation.cfg"));
            MelonPreferences.CreateEntry("PropertyUpgrades_Translation", "BuilderNPC_Name", "Builder");
            MelonPreferences.CreateEntry("PropertyUpgrades_Translation", "ResetMessage", "Hello! I'm the Builder. I can help you with property upgrades.");
            MelonPreferences.CreateEntry("PropertyUpgrades_Translation", "WhatElse", "What else do you want to do?");
            MelonPreferences.CreateEntry("PropertyUpgrades_Translation", "EmployeeUpgrade", "Employee upgrade at ({{PropertyName}}) completed", "EmployeeUpgrade", "Do not remove '{{PropertyName}}'");
            MelonPreferences.CreateEntry("PropertyUpgrades_Translation", "PotUpgrade", "Plant Growth upgrade at ({{PropertyName}}) completed", "PotUpgrade", "Do not remove '{{PropertyName}}'");
            MelonPreferences.CreateEntry("PropertyUpgrades_Translation", "MixerUpgrade", "Mixing time upgrade at ({{PropertyName}}) completed", "MixerUpgrade", "Do not remove '{PropertyName}}'");
            MelonPreferences.CreateEntry("PropertyUpgrades_Translation", "LoadingDockUpgrade", "Loading Dock upgrade at ({{PropertyName}}) completed", "LoadingDockUpgrade", "Do not remove '{{PropertyName}}'");
            MelonPreferences.CreateEntry("PropertyUpgrades_Translation", "NoBankBalance", "You don't have enough money in the bank for this upgrade.");
            MelonPreferences.CreateEntry("PropertyUpgrades_Translation", "GoBack", "Go back");
            MelonPreferences.CreateEntry("PropertyUpgrades_Translation", "AddEmployee", "+1 Employee (${{Price}})", "AddEmployee", "Do not remove '{{Price}}'");
            MelonPreferences.CreateEntry("PropertyUpgrades_Translation", "AddPlantGrowth", "+0.25 Plant Growth (${{Price}})", "AddPlantGrowth", "Do not remove '{{Price}}'");
            MelonPreferences.CreateEntry("PropertyUpgrades_Translation", "AddLoadingDock", "+1 Loading Dock (${{Price}})", "AddLoadingDock", "Do not remove '{{Price}}'");
            MelonPreferences.CreateEntry("PropertyUpgrades_Translation", "ReduceMixTime", "-1s MixTime Per Item (${{Price}})", "ReduceMixTime", "Do not remove '{{Price}}'");
            MelonPreferences.CreateEntry("PropertyUpgrades_Translation", "NoUpgrades", "{{PropertyName}} no longer have available upgrades.", "Do not remove '{{PropertyName}}'");
            MelonPreferences.CreateEntry("PropertyUpgrades_Translation", "PropertyStats", "{{PropertyName}} Stats:\nEmployee Capacity: {{employeeCap}}\nAddtional Growth Rate: {{ExtraGrowSpeedMultiplier}}\nMix time reduction: -{{MixTimePerItemReduction}}s\nExtra Docks: {{ExtraLoadingDocksCount}}\n\nWhat would you like to do?", "PropertyStats", "Do not remove/change strings enclosed in {{}}");
            MelonPreferences.CreateEntry("PropertyUpgrades_Translation", "StandOnTopOfDock", "You need to stand on top of the of the extra loading dock.");
            MelonPreferences.CreateEntry("PropertyUpgrades_Translation", "DockRemoved", "Loading dock removed.");
            MelonPreferences.CreateEntry("PropertyUpgrades_Translation", "InPlace", "I'm in place");
            MelonPreferences.CreateEntry("PropertyUpgrades_Translation", "WhichProperty", "I can help you with that. Which property would you like to upgrade?");
            MelonPreferences.CreateEntry("PropertyUpgrades_Translation", "IdkWhichProperty", "I don't know which property you want to upgrade.");
            MelonPreferences.CreateEntry("PropertyUpgrades_Translation", "MoveToWhere", "Move to the location you want {{entity}} placed.", "MoveToWhere", "Do not remove/change strings enclosed in {{}}");
            MelonPreferences.CreateEntry("PropertyUpgrades_Translation", "UpgradeProperty", "Upgrade Property");
            MelonPreferences.CreateEntry("PropertyUpgrades_Translation", "RemoveDock", "Remove Loading Dock");
            MelonPreferences.Save();
            LoggerInstance.Msg("Initialized.");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            base.OnSceneWasLoaded(buildIndex, sceneName);
            if (sceneName != "Main") return;
            MelonCoroutines.Start(DelayedStart());

        }


        private IEnumerator DelayedStart()
        {
            yield return new WaitForSeconds(3f);
            ModSaveManager modSaveManager = new ModSaveManager();
            SaveManager.Instance.onSaveStart.AddListener(modSaveManager.Save);
            Builder.InitBuilder(modSaveManager);
        }

        public override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            ModSaveManager.ClearTemp();
        }
    }
}