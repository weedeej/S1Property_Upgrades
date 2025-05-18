using MelonLoader;
using S1API.Entities;
using System.Collections;
using UnityEngine;
using S1NPC = Il2CppScheduleOne.NPCs.NPC;
using S1NPCManager = Il2CppScheduleOne.NPCs.NPCManager;

namespace PropertyUpgrades.BuilderNPC
{
    public class Builder : NPC
    {
        public Builder() : base("builder_npc", MelonPreferences.GetEntryValue<string>("PropertyUpgrades_Translation", "BuilderNPC_Name"), "")
        {}

        public static Builder InitBuilder(ModSaveManager saveManager)
        {
            Builder builder = Builder.GetBuilder();
            S1NPC s1NPC = S1NPCManager.GetNPC("builder_npc");
            s1NPC.ConversationCanBeHidden = false;
            ResetConversation(saveManager);
            return builder;
        }

        public static Builder GetBuilder()
        {
            Builder builder = (Builder)NPC.Get<Builder>();
            return builder;
        }

        public static void ResetConversation(ModSaveManager saveManager)
        {
            MelonCoroutines.Start(ResetCoro(saveManager));
        }

        private static IEnumerator ResetCoro(ModSaveManager saveManager)
        {
            yield return new WaitUntil(new Func<bool>(() => NPC.Get<Builder>() != null));
            NPC builder = NPC.Get<Builder>();
            ResponsesLogic responsesLogic = new ResponsesLogic(builder, saveManager);
        }
    }
}
