using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.Property;
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
        private string saveFilePath = null;
        private string tempSaveFilePath = null;
        public Dictionary<string, PropertyData> saveData = new Dictionary<string, PropertyData>();

        public ModSaveManager()
        {
            string saveDirectory = LoadManager.Instance.LoadedGameFolderPath;
            int separatorIndex = saveDirectory.IndexOf("Saves\\") + 5;
            string idSlotPath = saveDirectory.Substring(separatorIndex);
            string[] idSlotArr = idSlotPath.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            this.saveFilePath = Path.Combine(MelonEnvironment.UserDataDirectory, "Property Upgrades", $"{idSlotArr[0]}_{idSlotArr[1]}.json");
            this.tempSaveFilePath = Path.Combine(MelonEnvironment.UserDataDirectory, "Property Upgrades", $"_temp_{idSlotArr[0]}_{idSlotArr[1]}.json");
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

        public void SaveTemp()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this.saveData, Formatting.Indented);
                File.WriteAllText(this.tempSaveFilePath, json);
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
            return this;
        }

        public ModSaveManager LoadTemp()
        {
            if (!File.Exists(this.tempSaveFilePath) && !File.Exists(this.saveFilePath))
            {
                return this;
            }
            string json = File.ReadAllText(File.Exists(this.tempSaveFilePath) ? this.tempSaveFilePath : this.saveFilePath);
            this.saveData = JsonConvert.DeserializeObject<Dictionary<string, PropertyData>>(json);
            return this;
        }

        public static void ClearTemp()
        {
            if (!Directory.Exists(Path.Combine(MelonEnvironment.UserDataDirectory, "Property Upgrades")))
            {
                Directory.CreateDirectory(Path.Combine(MelonEnvironment.UserDataDirectory, "Property Upgrades"));
            }
            // Delete all temp files
            string[] tempFiles = Directory.GetFiles(Path.Combine(MelonEnvironment.UserDataDirectory, "Property Upgrades"), "_temp_*.json");
            foreach (string file in tempFiles)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Failed to delete temp file: {ex.Message}");
                }
            }
        }
    }
}
