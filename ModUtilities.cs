using MelonLoader.Utils;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using ScheduleOne.Delivery;
using ScheduleOne.ObjectScripts;
using ScheduleOne.Property;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using MelonLoader;

using S1NPC = ScheduleOne.NPCs.NPC;

namespace PropertyUpgrades
{
    public class Limits
    {
        public int MaxEmployeeCount;
        public int MaxLoadingDocks;
        public float MaxAdditionalGrowthRate;
        public int MaxMixTimeReduction;

        public Limits()
        {
            try
            {
                this.MaxEmployeeCount = MelonPreferences.GetEntryValue<int>("PropertyUpgrades", "MaxEmployeeCount");
                this.MaxLoadingDocks = MelonPreferences.GetEntryValue<int>("PropertyUpgrades", "MaxLoadingDocks");
                this.MaxAdditionalGrowthRate = Convert.ToSingle(MelonPreferences.GetEntryValue<double>("PropertyUpgrades", "MaxAdditionalGrowthRate"));
                this.MaxMixTimeReduction = MelonPreferences.GetEntryValue<int>("PropertyUpgrades", "MaxMixTimeReduction");
            } catch (Exception ex)
            {
                MelonLogger.Error($"Failed to load limits: {ex}");
                this.MaxEmployeeCount = 15;
                this.MaxLoadingDocks = 5;
                this.MaxAdditionalGrowthRate = 5.0f;
                this.MaxMixTimeReduction = 4;
            }
        }
    }
    public class ModUtilities
    {
        public static float PlayerYDistanceToSubtractForDecal = 0.9762f; 

        public static T[] GetBuildableItemScriptsFromProperty<T>(Property property) where T : class
        {
            return (from x in property.BuildableItems
                    where x is T
                    select x as T).ToArray();
        }
        public static Texture2D LoadCustomImage(string fileName, int width = 2, int height = 2)
        {
            string path = Path.Combine(MelonEnvironment.UserDataDirectory, fileName);
            if (!File.Exists(path)) return null;
            byte[] array = File.ReadAllBytes(path);
            Texture2D texture2D = new Texture2D(width, height);
            ImageConversion.LoadImage(texture2D, array);
            return texture2D;
        }

        public static void AddExtraDock(Property targetProperty, Vector3 playerPos, Quaternion playerRot, bool addToSaveManager = false, ModSaveManager saveManager = null)
        {
            Vector3 newDockPos = new Vector3(playerPos.x, playerPos.y - PlayerYDistanceToSubtractForDecal, playerPos.z);
            ExtraLoadingDock saveObj = new ExtraLoadingDock
            {
                Position = newDockPos,
                Rotation = playerRot.eulerAngles,
            };

            AddExtraDock(targetProperty, saveObj);

            if (addToSaveManager && saveManager != null)
            {
                PropertyData propertyData = saveManager.saveData[targetProperty.PropertyName];
                propertyData.ExtraLoadingDocks = propertyData.ExtraLoadingDocks.Append(saveObj).ToArray();
            }
        }

        public static void AddExtraDock(Property targetProperty, ExtraLoadingDock loadingDock)
        {
            if (targetProperty.LoadingDockCount < 1) {
                MelonLogger.Msg(1);
                Property baseProperty = Property.Properties.Find((property) => property.PropertyName == "Barn");
                MelonLogger.Msg(2);
                if (baseProperty == null)
                {
                    MelonLogger.Msg(3);
                    MelonLogger.Error($"Failed to find base property for loading dock: Barn");
                    return;
                }
                MelonLogger.Msg(4);
                GameObject baseDockGO = baseProperty.LoadingDocks[0].gameObject;
                MelonLogger.Msg(5);
                GameObject cloneDockGO = UnityEngine.Object.Instantiate(baseDockGO, loadingDock.Position, Quaternion.Euler(loadingDock.Rotation));
                MelonLogger.Msg(6);
                LoadingDock cloneDock = cloneDockGO.GetComponent<LoadingDock>();
                MelonLogger.Msg(7);
                cloneDock.ParentProperty = targetProperty;
                MelonLogger.Msg(8);
                targetProperty.LoadingDocks = targetProperty.LoadingDocks.Append(cloneDockGO.GetComponent<LoadingDock>()).ToArray();
                MelonLogger.Msg(9);
                return;
            }
            MelonLogger.Msg(10);
            MelonLogger.Msg($"Adding extra loading dock to {targetProperty.LoadingDockCount} at {loadingDock.Position}");
            GameObject baseLoadingDockGO = targetProperty.LoadingDocks[0].gameObject;

            GameObject cloneLoadingDockGO = UnityEngine.Object.Instantiate(baseLoadingDockGO, loadingDock.Position, Quaternion.Euler(loadingDock.Rotation));
            targetProperty.LoadingDocks = targetProperty.LoadingDocks.Append(cloneLoadingDockGO.GetComponent<LoadingDock>()).ToArray();
        }

        public static void ApplyPropertyData(Property property, PropertyData propertyData)
        {
            if (property.EmployeeCapacity < propertyData.EmployeeCapacity)
            {
                // Add employees to the property
                int employeesToAdd = propertyData.EmployeeCapacity - property.EmployeeCapacity;
                for (int i = 0; i < employeesToAdd; i++)
                {
                    Transform idlePoint = property.EmployeeIdlePoints[0];
                    Vector3 newPos = idlePoint.position + new Vector3(UnityEngine.Random.Range(0, 2f), 0, UnityEngine.Random.Range(0, 2f));
                    Transform newIdlePoint = UnityEngine.Object.Instantiate(idlePoint, newPos, Quaternion.identity);
                    property.EmployeeIdlePoints = property.EmployeeIdlePoints.Append(newIdlePoint).ToArray();
                }
            }
            // Employee capacity
            property.EmployeeCapacity = propertyData.EmployeeCapacity;

            // Loading docks
            foreach (ExtraLoadingDock extraDock in propertyData.ExtraLoadingDocks)
            {
                AddExtraDock(property, extraDock);
            }
        }

        // Mixtime
        public static void ApplyMixingUpgrade(MixingStation mixingStation, int mixTimeReduction)
        {
            if (mixingStation.MixTimePerItem - mixTimeReduction <= 1)
            {
                mixingStation.MixTimePerItem = 1;
                return;
            }
            mixingStation.MixTimePerItem -= mixTimeReduction;
        }
    }

    public class Vector3Converter : JsonConverter<Vector3>
    {
        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WriteEndObject();
        }

        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            // Load the JSON object
            JObject jsonObject = JObject.Load(reader);

            // Extract the float values, providing default (0f) if properties are missing
            float x = jsonObject["x"]?.Value<float>() ?? 0f;
            float y = jsonObject["y"]?.Value<float>() ?? 0f;
            float z = jsonObject["z"]?.Value<float>() ?? 0f;

            // Create and return the new Vector3
            return new Vector3(x, y, z);
        }
    }
}
