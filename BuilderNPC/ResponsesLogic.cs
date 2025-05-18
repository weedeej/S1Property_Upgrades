using Il2CppFluffyUnderware.DevTools.Extensions;
using MelonLoader;
using S1API.Entities;
using S1API.Messaging;
using Il2CppScheduleOne.AvatarFramework;
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.Property;
using System.Collections;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.UIElements;
using Player = Il2CppScheduleOne.PlayerScripts.Player;
using S1NPC = Il2CppScheduleOne.NPCs.NPC;
using S1NPCManager = Il2CppScheduleOne.NPCs.NPCManager;

namespace PropertyUpgrades.BuilderNPC
{
    public class ResponsesLogic
    {
        public enum Action
        {
            UpgradeProperty,
            SetTargetProperty,
            PositionLoadingBay,
            RemoveLoadingDock
        };

        public enum PropertyUpgrade
        {
            AddEmployee,
            AddPlantGrowthMultipler,
            ReduceMixingTime,
            AddLoadingDock
        };

        // Class Properties
        private NPC npc;
        private S1NPC s1npc;

        private Property targetProperty = null;
        private List<Property> ownedProperties;
        private ModSaveManager saveManager;
        private Player player;

        public ResponsesLogic(NPC npc, ModSaveManager saveManager)
        {
            this.npc = npc;
            this.saveManager = saveManager;
            try
            {
                saveManager.Load();
                MelonCoroutines.Start(UpdateProperties(true));
            }
            catch (Exception ex)
            {
                MelonLogger.Error(ex);
            }

            this.s1npc = S1NPCManager.GetNPC(npc.ID);
            this.player = Player.Local;
            this.Reset();
        }

        public ResponsesLogic SendAction(Action action, Property newTargetProperty = null,
            bool isRemoveLoadingDock = false)
        {
            switch (action)
            {
                case Action.UpgradeProperty:
                    MelonCoroutines.Start(ShowPropertiesResponse());
                    break;
                case Action.SetTargetProperty:
                    this.targetProperty = newTargetProperty;
                    if (!isRemoveLoadingDock)
                    {
                        MelonCoroutines.Start(ShowAvailableUpgradesResponse());
                        break;
                    }

                    break;
                case Action.PositionLoadingBay:
                    MelonCoroutines.Start(ShowPositionLoadingBayResponse());
                    break;
                case Action.RemoveLoadingDock:
                    MelonCoroutines.Start(ShowRemoveBayResponses());
                    break;
            }

            return this;
        }

        private IEnumerator ShowPositionLoadingBayResponse()
        {
            if (this.targetProperty == null)
            {
                this.Reset(MelonPreferences.GetEntryValue<string>("PropertyUpgrades_Translation", "IdkWhichProperty"));
                yield break;
            }

            PropertyData propertyData = this.saveManager.saveData[this.targetProperty.PropertyName];
            float addLoadingBayPrice = (float)propertyData.ExtraLoadingDocks.Length == 0
                ? 10000
                : (float)propertyData.ExtraLoadingDocks.Length * 10000;
            List<Response> responses = new List<Response>()
            {
                new Response
                {
                    Text = MelonPreferences.GetEntryValue<string>("PropertyUpgrades_Translation", "InPlace"),
                    OnTriggered = () =>
                        MelonCoroutines.Start(this.UpgradeProperty(PropertyUpgrade.AddLoadingDock, addLoadingBayPrice))
                },
                new Response
                {
                    Text = MelonPreferences.GetEntryValue<string>("PropertyUpgrades_Translation", "GoBack"),
                    OnTriggered = () => this.Reset()
                },
            };

            this.ShowResponses(responses,
                MelonPreferences.GetEntryValue<string>("PropertyUpgrades_Translation", "MoveToWhere")
                    .Replace("{{entity}}", "loading bay"));
            yield break;
        }

        // Is remove dock?
        private IEnumerator ShowPropertiesResponse()
        {
            MelonCoroutines.Start(UpdateProperties());
            List<Response> responses = new List<Response>();
            foreach (Property property in ownedProperties)
            {
                if (property != null)
                {
                    Response response = new Response
                    {
                        Text = property.PropertyName, OnTriggered = () => SendAction(Action.SetTargetProperty, property)
                    };
                    responses.Add(response);
                }
            }

            this.ShowResponses(responses,
                MelonPreferences.GetEntryValue<string>("PropertyUpgrades_Translation", "WhichProperty"));
            yield break;
        }

        private IEnumerator UpdateProperties(bool save = false)
        {
            this.ownedProperties = Property.OwnedProperties._items
                .Where(property =>
                    property != null &&
                    !string.IsNullOrEmpty(property.PropertyName)
                )
                .Where(property =>
                {
                    if (property.PropertyName == "RV")
                        return false;

                    if (!saveManager.saveData.ContainsKey(property.PropertyName))
                    {
                        // Initial property data
                        PropertyData propertyData = new PropertyData
                        {
                            EmployeeCapacity = property.EmployeeCapacity,
                            MixTimePerItemReduction = 0,
                            ExtraGrowSpeedMultiplier = 1f,
                            ExtraLoadingDocks = []
                        };
                        saveManager.saveData.Add(property.PropertyName, propertyData);
                    }

                    return true;
                })
                .ToList();

            if (save)
                saveManager.Save();

            yield break;
        }


        private IEnumerator ShowRemoveBayResponses()
        {
            List<Response> responses = new List<Response>()
            {
                new Response
                {
                    Text = MelonPreferences.GetEntryValue<string>("PropertyUpgrades_Translation", "GoBack"),
                    OnTriggered = () =>
                        this.Reset(MelonPreferences.GetEntryValue<string>("PropertyUpgrades_Translation", "WhatElse"))
                },
                new Response
                {
                    Text = MelonPreferences.GetEntryValue<string>("PropertyUpgrades_Translation", "InPlace") +
                           "($1000)",
                    OnTriggered = () => MelonCoroutines.Start(this.RemoveLoadingBay())
                }
            };
            this.ShowResponses(responses,
                MelonPreferences.GetEntryValue<string>("PropertyUpgrades_Translation", "StandOnTopOfDock"));
            yield break;
        }

        private IEnumerator RemoveLoadingBay()
        {
            GroundDetector groundDetector = this.player.GetComponent<GroundDetector>();
            if (groundDetector == null)
            {
                groundDetector = this.player.gameObject.AddComponent<GroundDetector>();
            }

            yield return new WaitUntil(new Func<bool>(() => groundDetector.ObjectCurrentlyUnderneath != null));
            if (groundDetector.ObjectCurrentlyUnderneath.name != "Collider")
            {
                this.Reset(MelonPreferences.GetEntryValue<string>("PropertyUpgrades_Translation", "StandOnTopOfDock"));
                MelonCoroutines.Start(RemoveLoadingBay());
                yield break;
            }

            MoneyManager moneyManager = NetworkSingleton<MoneyManager>.Instance;
            if (moneyManager.onlineBalance < 1000)
            {
                this.Reset(MelonPreferences.GetEntryValue<string>("PropertyUpgrades_Translation", "NoBankBalance"));
                yield break;
            }

            LoadingDock loadingDock = groundDetector.ObjectCurrentlyUnderneath.GetComponentInParent<LoadingDock>();
            yield return new WaitUntil(new Func<bool>(() => loadingDock != null));
            string propertyName = loadingDock.ParentProperty.PropertyName.Trim();
            PropertyData propertyData = this.saveManager.saveData[propertyName];
            propertyData.ExtraLoadingDocks = propertyData.ExtraLoadingDocks
                .Where((dock) => dock.Position != loadingDock.transform.position).ToArray();
            var currentDocks = loadingDock.ParentProperty.LoadingDocks.ToList();
            currentDocks.Remove(loadingDock);
            loadingDock.ParentProperty.LoadingDocks = new Il2CppReferenceArray<LoadingDock>(currentDocks.ToArray());
            this.Reset(MelonPreferences.GetEntryValue<string>("PropertyUpgrades_Translation", "DockRemoved"));
            GameObject.Destroy(loadingDock.gameObject);
            moneyManager.CreateOnlineTransaction("Remove Loading Dock", -1000, 1,
                $"Loading dock removal ({propertyName})");
        }

        private IEnumerator ShowAvailableUpgradesResponse()
        {
            PropertyData propertyData = this.saveManager.saveData[this.targetProperty.PropertyName];
            int employeeCap = propertyData.EmployeeCapacity;
            float addEmployeePrice = (employeeCap * 1250);
            float mixTimeReductionPrice = (float)propertyData.MixTimePerItemReduction == 0
                ? 5000
                : (float)propertyData.MixTimePerItemReduction * 5000;
            float extraGrowSpeedPrice = (float)propertyData.ExtraGrowSpeedMultiplier * 5000;
            float addLoadingBayPrice = (float)propertyData.ExtraLoadingDocks.Length == 0
                ? 10000
                : (float)propertyData.ExtraLoadingDocks.Length * 10000;

            List<Response> upgradeOptions = new List<Response>
            {
                new Response
                {
                    Text = MelonPreferences.GetEntryValue<string>("PropertyUpgrades_Translation", "GoBack"),
                    OnTriggered = () => this.Reset()
                },
            };

            Limits limits = new Limits();
            if (this.targetProperty.EmployeeCapacity < limits.MaxEmployeeCount &&
                this.targetProperty.PropertyName != "Motel Room")
                upgradeOptions.Add(new Response
                {
                    Text = MelonPreferences.GetEntryValue<string>("PropertyUpgrades_Translation", "AddEmployee")
                        .Replace("{{Price}}", addEmployeePrice.ToString()),
                    OnTriggered = () =>
                        MelonCoroutines.Start(this.UpgradeProperty(PropertyUpgrade.AddEmployee, addEmployeePrice))
                });
            if (this.targetProperty.LoadingDocks.Length < limits.MaxLoadingDocks)
                upgradeOptions.Add(new Response
                {
                    Text = MelonPreferences.GetEntryValue<string>("PropertyUpgrades_Translation", "AddLoadingDock")
                        .Replace("{{Price}}", addLoadingBayPrice.ToString()),
                    OnTriggered = () => this.SendAction(Action.PositionLoadingBay)
                });
            if (propertyData.ExtraGrowSpeedMultiplier < limits.MaxAdditionalGrowthRate)
                upgradeOptions.Add(new Response
                {
                    Text = MelonPreferences.GetEntryValue<string>("PropertyUpgrades_Translation", "AddPlantGrowth")
                        .Replace("{{Price}}", extraGrowSpeedPrice.ToString()),
                    OnTriggered = () =>
                        MelonCoroutines.Start(this.UpgradeProperty(PropertyUpgrade.AddPlantGrowthMultipler,
                            extraGrowSpeedPrice))
                });
            if (propertyData.MixTimePerItemReduction < limits.MaxMixTimeReduction)
                upgradeOptions.Add(new Response
                {
                    Text = MelonPreferences.GetEntryValue<string>("PropertyUpgrades_Translation", "ReduceMixTime")
                        .Replace("{{Price}}", mixTimeReductionPrice.ToString()),
                    OnTriggered = () =>
                        MelonCoroutines.Start(this.UpgradeProperty(PropertyUpgrade.ReduceMixingTime,
                            mixTimeReductionPrice))
                });
            if (upgradeOptions.Count == 1)
            {
                this.Reset(MelonPreferences.GetEntryValue<string>("PropertyUpgrades_Translation", "NoUpgrades")
                    .Replace("{{PropertyName}}", this.targetProperty.PropertyName));
                yield break;
            }

            string stats = MelonPreferences.GetEntryValue<string>("PropertyUpgrades_Translation", "PropertyStats")
                .Replace("{{PropertyName}}", this.targetProperty.PropertyName)
                .Replace("{{employeeCap}}", employeeCap.ToString())
                .Replace("{{ExtraGrowSpeedMultiplier}}", propertyData.ExtraGrowSpeedMultiplier.ToString())
                .Replace("{{MixTimePerItemReduction}}", propertyData.MixTimePerItemReduction.ToString())
                .Replace("{{ExtraLoadingDocksCount}}", propertyData.ExtraLoadingDocks.Length.ToString());
            this.ShowResponses(upgradeOptions, stats);
            yield break;
        }

        private IEnumerator UpgradeProperty(PropertyUpgrade upgrade, float price)
        {
            MoneyManager moneyManager = NetworkSingleton<MoneyManager>.Instance;
            if (moneyManager.onlineBalance < price)
            {
                this.Reset(MelonPreferences.GetEntryValue<string>("PropertyUpgrades_Translation", "NoBankBalance"));
                yield break;
            }

            PropertyData propertyData = this.saveManager.saveData[this.targetProperty.PropertyName];
            switch (upgrade)
            {
                case PropertyUpgrade.AddEmployee:
                    moneyManager.CreateOnlineTransaction("Property Upgrade", -price, 1,
                        $"Employee upgrade ({targetProperty.PropertyName})");
                    this.targetProperty.EmployeeCapacity++;

                    Transform firstIdlePoint = this.targetProperty.EmployeeIdlePoints[0];
                    Vector3 newPos = firstIdlePoint.position + new Vector3(UnityEngine.Random.Range(0, 2f), 0,
                        UnityEngine.Random.Range(0, 2f));
                    Transform newIdlePoint =
                        UnityEngine.Object.Instantiate(firstIdlePoint, newPos, Quaternion.identity);
                    this.targetProperty.EmployeeIdlePoints =
                        this.targetProperty.EmployeeIdlePoints.Append(newIdlePoint).ToArray();

                    propertyData.EmployeeCapacity = this.targetProperty.EmployeeCapacity;
                    this.npc.SendTextMessage(MelonPreferences
                        .GetEntryValue<string>("PropertyUpgrades_Translation", "EmployeeUpgrade")
                        .Replace("{{PropertyName}}", this.targetProperty.PropertyName));
                    break;
                case PropertyUpgrade.AddPlantGrowthMultipler:
                    moneyManager.CreateOnlineTransaction("Property Upgrade", -price, 1,
                        $"Plant growth upgrade ({targetProperty.PropertyName})");
                    Pot[] pots = ModUtilities.GetBuildableItemScriptsFromProperty<Pot>(this.targetProperty);
                    foreach (Pot pot in pots)
                    {
                        pot.GrowSpeedMultiplier += 0.25f;
                    }

                    propertyData.ExtraGrowSpeedMultiplier += 0.25f;
                    this.npc.SendTextMessage(MelonPreferences
                        .GetEntryValue<string>("PropertyUpgrades_Translation", "PotUpgrade")
                        .Replace("{{PropertyName}}", this.targetProperty.PropertyName));
                    break;
                case PropertyUpgrade.ReduceMixingTime:
                    moneyManager.CreateOnlineTransaction("Property Upgrade", -price, 1,
                        $"Mixing time upgrade ({targetProperty.PropertyName})");

                    MixingStation[] mixingStations =
                        ModUtilities.GetBuildableItemScriptsFromProperty<MixingStation>(this.targetProperty);
                    propertyData.MixTimePerItemReduction += 1;
                    foreach (MixingStation mixingStation in mixingStations)
                    {
                        ModUtilities.ApplyMixingUpgrade(mixingStation, propertyData.MixTimePerItemReduction);
                    }

                    this.npc.SendTextMessage(MelonPreferences
                        .GetEntryValue<string>("PropertyUpgrades_Translation", "MixerUpgrade")
                        .Replace("{{PropertyName}}", this.targetProperty.PropertyName));
                    break;
                case PropertyUpgrade.AddLoadingDock:
                    moneyManager.CreateOnlineTransaction("Property Upgrade", -price, 1,
                        $"Loading dock upgrade ({targetProperty.PropertyName})");

                    Vector3 playerPos = this.player.transform.position;
                    Quaternion playerRot = this.player.transform.rotation;

                    ModUtilities.AddExtraDock(this.targetProperty, playerPos, playerRot, true, saveManager);

                    this.npc.SendTextMessage(MelonPreferences
                        .GetEntryValue<string>("PropertyUpgrades_Translation", "LoadingDockUpgrade")
                        .Replace("{{PropertyName}}", this.targetProperty.PropertyName));
                    break;
            }

            saveManager.SaveTemp();
            this.Reset(MelonPreferences.GetEntryValue<string>("PropertyUpgrades_Translation", "WhatElse"));
        }

        private void ShowResponses(List<Response> responses, string message)
        {
            this.npc.SendTextMessage(message, responses.ToArray());
        }

        private void Reset(string message = null)
        {
            if (message == null)
            {
                message = MelonPreferences.GetEntryValue<string>("PropertyUpgrades_Translation", "ResetMessage");
            }

            this.targetProperty = null;
            this.ShowResponses(this.GenerateResponses(), message);
        }

        private List<Response> GenerateResponses()
        {
            List<Response> responses = new List<Response>()
            {
                new Response
                {
                    Text = "Upgrade Property",
                    OnTriggered = () => SendAction(Action.UpgradeProperty)
                },
                new Response
                {
                    Text = "Remove Loading Dock",
                    OnTriggered = () => SendAction(Action.RemoveLoadingDock)
                },
            };
            return responses;
        }
    }
}