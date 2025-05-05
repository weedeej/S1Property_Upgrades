using FluffyUnderware.DevTools.Extensions;
using MelonLoader;
using S1API.Entities;
using S1API.Messaging;
using ScheduleOne.AvatarFramework;
using ScheduleOne.Delivery;
using ScheduleOne.DevUtilities;
using ScheduleOne.Money;
using ScheduleOne.ObjectScripts;
using ScheduleOne.Property;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using Player = ScheduleOne.PlayerScripts.Player;
using S1NPC = ScheduleOne.NPCs.NPC;
using S1NPCManager = ScheduleOne.NPCs.NPCManager;

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

        public enum PropertyUpgrade {
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
            catch (Exception ex){
                MelonLogger.Error(ex);
            }
            this.s1npc = S1NPCManager.GetNPC(npc.ID);
            this.player = Player.Local;
            this.Reset();
        }

        public ResponsesLogic SendAction(Action action, Property newTargetProperty = null, bool isRemoveLoadingDock = false)
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
                this.npc.SendTextMessage("I don't know which property you want to upgrade.");
                yield break;
            }
            PropertyData propertyData = this.saveManager.saveData[this.targetProperty.PropertyName];
            float addLoadingBayPrice = (float)propertyData.ExtraLoadingDocks.Length == 0 ? 10000 : (float)propertyData.ExtraLoadingDocks.Length * 10000;
            List<Response> responses = new List<Response>() {
                new Response { Text = "I'm in place", OnTriggered = () => MelonCoroutines.Start(this.UpgradeProperty(PropertyUpgrade.AddLoadingDock, addLoadingBayPrice)) },
                new Response { Text = "Go back", OnTriggered = () => this.Reset() },
            };

            this.ShowResponses(responses, "Move to the location you want the new loading bay to be placed");
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
                    Response response = new Response { Text = property.PropertyName, OnTriggered = () => SendAction(Action.SetTargetProperty, property) };
                    responses.Add(response);
                }
            }
            this.ShowResponses(responses, "I can help you with that. Which property would you like to upgrade?");
            yield break;
        }

        private IEnumerator UpdateProperties(bool save = false)
        {
            this.ownedProperties = Property.OwnedProperties.Where((property) => {
                if (property.PropertyName == "RV") return false;
                if (saveManager.saveData.ContainsKey(property.PropertyName)) return true;
                // Initial property data
                PropertyData propertyData = new PropertyData
                {
                    EmployeeCapacity = property.EmployeeCapacity,
                    MixTimePerItemReduction = 0,
                    ExtraGrowSpeedMultiplier = 1f,
                    ExtraLoadingDocks = []
                };
                saveManager.saveData.Add(property.PropertyName, propertyData);
                return true;
            }).ToList();
            if (!save) yield break;
            saveManager.Save();
        }

        private IEnumerator ShowRemoveBayResponses()
        {
            List<Response> responses = new List<Response>() {
                new Response { Text = "Go back", OnTriggered = () => this.Reset("What do you want me to do?") },
                new Response { Text = "I'm in place ($1000)", OnTriggered = () => MelonCoroutines.Start(this.RemoveLoadingBay()) }
            };
            this.ShowResponses(responses, "Stand on top of the of the extra loading dock you want to remove.");
            yield break;

        }

        private IEnumerator RemoveLoadingBay()
        {
            GroundDetector groundDetector = this.player.GetComponent<GroundDetector>();
            if (groundDetector == null)
            {
                groundDetector = this.player.gameObject.AddComponent<GroundDetector>();
            }
            yield return new WaitUntil(() => groundDetector.ObjectCurrentlyUnderneath != null);
            if (groundDetector.ObjectCurrentlyUnderneath.name != "Collider")
            {
                this.Reset("You need to stand on top of the of the extra loading dock.");
                MelonCoroutines.Start(RemoveLoadingBay());
                yield break;
            }
            MoneyManager moneyManager = NetworkSingleton<MoneyManager>.Instance;
            if (moneyManager.onlineBalance < 1000)
            {
                this.Reset("You don't have enough money in the bank for this upgrade.");
                yield break;
            }
            LoadingDock loadingDock = groundDetector.ObjectCurrentlyUnderneath.GetComponentInParent<LoadingDock>();
            yield return new WaitUntil(() => loadingDock != null);
            string propertyName = loadingDock.ParentProperty.PropertyName.Trim();
            PropertyData propertyData = this.saveManager.saveData[propertyName];
            propertyData.ExtraLoadingDocks = propertyData.ExtraLoadingDocks.Where((dock) => dock.Position != loadingDock.transform.position).ToArray();
            loadingDock.ParentProperty.LoadingDocks = loadingDock.ParentProperty.LoadingDocks.Remove(loadingDock);
            this.Reset($"Removed loading dock at ({propertyName})");
            GameObject.Destroy(loadingDock.gameObject);
            moneyManager.CreateOnlineTransaction("Remove Loading Dock", -1000, 1, $"Loading dock removal ({propertyName})");

        }

        private IEnumerator ShowAvailableUpgradesResponse()
        {
            PropertyData propertyData = this.saveManager.saveData[this.targetProperty.PropertyName];
            int employeeCap = propertyData.EmployeeCapacity;
            float addEmployeePrice = (employeeCap * 1250);
            float mixTimeReductionPrice = (float)propertyData.MixTimePerItemReduction == 0 ? 5000 : (float)propertyData.MixTimePerItemReduction * 5000;
            float extraGrowSpeedPrice = (float)propertyData.ExtraGrowSpeedMultiplier * 5000;
            float addLoadingBayPrice = (float)propertyData.ExtraLoadingDocks.Length == 0 ? 10000 : (float)propertyData.ExtraLoadingDocks.Length * 10000;

            List<Response> upgradeOptions = new List<Response> {
                new Response { Text = "Go back", OnTriggered = () => this.Reset() },
            };

            Limits limits = new Limits();
            if (this.targetProperty.EmployeeCapacity < limits.MaxEmployeeCount && this.targetProperty.PropertyName != "Motel Room")
                upgradeOptions.Add(new Response { Text = $"+1 Employee (${addEmployeePrice})", OnTriggered = () => MelonCoroutines.Start(this.UpgradeProperty(PropertyUpgrade.AddEmployee, addEmployeePrice)) });
            if (this.targetProperty.LoadingDocks.Length < limits.MaxLoadingDocks)
                upgradeOptions.Add(new Response { Text = $"+1 Loading Bay (${addLoadingBayPrice})", OnTriggered = () => this.SendAction(Action.PositionLoadingBay) });
            if (propertyData.ExtraGrowSpeedMultiplier < limits.MaxAdditionalGrowthRate)
                upgradeOptions.Add(new Response { Text = $"+0.25 Pots Grow Rate (${extraGrowSpeedPrice})", OnTriggered = () => MelonCoroutines.Start(this.UpgradeProperty(PropertyUpgrade.AddPlantGrowthMultipler, extraGrowSpeedPrice)) });
            if (propertyData.MixTimePerItemReduction < limits.MaxMixTimeReduction)
                upgradeOptions.Add(new Response { Text = $"-1s Mix Time per Item (${mixTimeReductionPrice})", OnTriggered = () => MelonCoroutines.Start(this.UpgradeProperty(PropertyUpgrade.ReduceMixingTime, mixTimeReductionPrice)) });
            if (upgradeOptions.Count == 1)
            {
                this.Reset($"{this.targetProperty.PropertyName} no longer have available upgrades");
                yield break;
            }
            this.ShowResponses(upgradeOptions, $"{this.targetProperty.PropertyName} Stats:\nEmployee Capacity: {employeeCap}\nAddtional Growth Rate: {propertyData.ExtraGrowSpeedMultiplier}\nMix time reduction: -{propertyData.MixTimePerItemReduction}s\nExtra Docks: {propertyData.ExtraLoadingDocks.Length}\n\nWhat would you like to do?");
            yield break;
        }
        private IEnumerator UpgradeProperty(PropertyUpgrade upgrade, float price)
        {
            MoneyManager moneyManager = NetworkSingleton<MoneyManager>.Instance;
            if (moneyManager.onlineBalance < price)
            {
                this.npc.SendTextMessage("You don't have enough money in the bank for this upgrade.");
                this.SendAction(Action.SetTargetProperty, this.targetProperty);
                yield break;
            }
            PropertyData propertyData = this.saveManager.saveData[this.targetProperty.PropertyName];
            switch (upgrade)
            {
                case PropertyUpgrade.AddEmployee:
                    moneyManager.CreateOnlineTransaction("Property Upgrade", -price, 1, $"Employee upgrade ({targetProperty.PropertyName})");
                    this.targetProperty.EmployeeCapacity++;

                    Transform firstIdlePoint = this.targetProperty.EmployeeIdlePoints[0];
                    Vector3 newPos = firstIdlePoint.position + new Vector3(UnityEngine.Random.Range(0, 2f), 0, UnityEngine.Random.Range(0, 2f));
                    Transform newIdlePoint = UnityEngine.Object.Instantiate(firstIdlePoint, newPos, Quaternion.identity);
                    this.targetProperty.EmployeeIdlePoints = this.targetProperty.EmployeeIdlePoints.Append(newIdlePoint).ToArray();

                    propertyData.EmployeeCapacity = this.targetProperty.EmployeeCapacity;
                    this.npc.SendTextMessage($"Employee upgrade at ({targetProperty.PropertyName}) completed");
                    break;
                case PropertyUpgrade.AddPlantGrowthMultipler:
                    moneyManager.CreateOnlineTransaction("Property Upgrade", -price, 1, $"Plant growth upgrade ({targetProperty.PropertyName})");
                    Pot[] pots = ModUtilities.GetBuildableItemScriptsFromProperty<Pot>(this.targetProperty);
                    foreach (Pot pot in pots)
                    {
                        pot.GrowSpeedMultiplier += 0.25f;
                    }
                    propertyData.ExtraGrowSpeedMultiplier += 0.25f;
                    this.npc.SendTextMessage($"Plant growth upgrade at ({targetProperty.PropertyName}) completed");
                    break;
                case PropertyUpgrade.ReduceMixingTime:
                    moneyManager.CreateOnlineTransaction("Property Upgrade", -price, 1, $"Mixing time upgrade ({targetProperty.PropertyName})");
                    
                    MixingStation[] mixingStations = ModUtilities.GetBuildableItemScriptsFromProperty<MixingStation>(this.targetProperty);
                    MixingStationMk2[] mixingStationsMk2 = ModUtilities.GetBuildableItemScriptsFromProperty<MixingStationMk2>(this.targetProperty);
                    foreach (MixingStation mixingStation in mixingStations)
                    {
                        mixingStation.MixTimePerItem -= 1;
                    }
                    foreach (MixingStationMk2 mixingStationMk2 in mixingStationsMk2)
                    {
                        mixingStationMk2.MixTimePerItem -= 1;
                    }
                    propertyData.MixTimePerItemReduction += 1;
                    this.npc.SendTextMessage($"Mixing time upgrade at ({targetProperty.PropertyName}) completed");
                    break;
                case PropertyUpgrade.AddLoadingDock:
                    moneyManager.CreateOnlineTransaction("Property Upgrade", -price, 1, $"Loading dock upgrade ({targetProperty.PropertyName})");

                    Vector3 playerPos = this.player.transform.position;
                    Quaternion playerRot = this.player.transform.rotation;

                    ModUtilities.AddExtraDock(this.targetProperty, playerPos, playerRot, true, saveManager);

                    this.npc.SendTextMessage($"Loading dock upgrade at ({targetProperty.PropertyName}) completed");
                    break;

            }
            this.Reset("What else do you want to do?");
        }

        private void ShowResponses(List<Response> responses, string message)
        {
            this.npc.SendTextMessage(message, responses.ToArray());
        }

        private void Reset(string message = "Hello! I'm the Builder. I can help you with property upgrades.")
        {
            this.targetProperty = null;
            this.ShowResponses(this.GenerateResponses(), message);
        }
        private List<Response> GenerateResponses()
        {
            List<Response> responses = new List<Response> () {
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
