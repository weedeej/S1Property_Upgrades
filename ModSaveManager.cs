using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;
using ScheduleOne.Persistence;
using ScheduleOne.Property;
using UnityEngine;

namespace PropertyUpgrades
{
    public class ExtraLoadingDock
    {
        [JsonProperty("Position"), JsonConverter(typeof(Vector3Converter))]
        public Vector3 Position { get; set; }
        [JsonProperty("Rotation"), JsonConverter(typeof(Vector3Converter))]
        public Vector3 Rotation { get; set; }
    }
    public class PropertyData
    {
        [JsonProperty("EmployeeCapacity")]
        public int EmployeeCapacity { get; set; }

        [JsonProperty("MixTimePerItemReduction")]
        public int MixTimePerItemReduction { get; set; }

        [JsonProperty("ExtraGrowSpeedMultiplier")]
        public float ExtraGrowSpeedMultiplier { get; set; }
        [JsonProperty("ExtraLoadingDocks")]
        public ExtraLoadingDock[] ExtraLoadingDocks { get; set; }
    }
    public class ModSaveManager
    {
        private string saveFilePath;
        public Dictionary<string, PropertyData> saveData = new Dictionary<string, PropertyData>();

        public ModSaveManager()
        {
            string saveDirectory = LoadManager.Instance.LoadedGameFolderPath;
            int separatorIndex = saveDirectory.IndexOf("Saves\\") + 5;
            string idSlotPath = saveDirectory.Substring(separatorIndex);
            string[] idSlotArr = idSlotPath.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            this.saveFilePath = Path.Combine(MelonEnvironment.UserDataDirectory, "Property Upgrades", $"{idSlotArr[0]}_{idSlotArr[1]}.json");
            // Initialize the save manager
            if (!Directory.Exists(Path.Combine(MelonEnvironment.UserDataDirectory, "Property Upgrades")))
            {
                Directory.CreateDirectory(Path.Combine(MelonEnvironment.UserDataDirectory, "Property Upgrades"));
            }

        }
        public void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this.saveData, Formatting.Indented);
                File.WriteAllText(this.saveFilePath, json);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to save data: {ex.Message}");
            }
        }

        public ModSaveManager Load()
        {
            if (!File.Exists(this.saveFilePath))
            {
                return this;
            }
            string json = File.ReadAllText(this.saveFilePath);
            this.saveData = JsonConvert.DeserializeObject<Dictionary<string, PropertyData>>(json);
            foreach (var property in this.saveData)
            {
                ModUtilities.ApplyPropertyData(Property.Properties.Find((p) => p.PropertyName == property.Key), property.Value);
            }
            return this;
        }
    }
}
