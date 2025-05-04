using HarmonyLib;
using MelonLoader;
using PropertyUpgrades.BuilderNPC;
using ScheduleOne.Delivery;
using ScheduleOne.Persistence;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Property;
using System.Collections;
using UnityEngine;

[assembly: MelonInfo(typeof(PropertyUpgrades.Core), "PropertyUpgrades", "1.0.0", "Dixie", null)]
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
    }
}