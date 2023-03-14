using HarmonyLib;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Towns;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(TownCenterMissionController), "AfterStart")]
internal class TownArmourPatch
{
	private static bool Prefix(TownCenterMissionController __instance)
	{
		if (Test.followingHero != null)
		{
			bool isNight = Campaign.Current.IsNight;
			__instance.Mission.SetMissionMode(MissionMode.StartUp, atStart: true);
			__instance.Mission.IsInventoryAccessible = !Campaign.Current.IsMainHeroDisguised;
			__instance.Mission.IsQuestScreenAccessible = true;
			__instance.Mission.DoesMissionRequireCivilianEquipment = false;
			MissionAgentHandler missionBehavior = __instance.Mission.GetMissionBehavior<MissionAgentHandler>();
			missionBehavior.SpawnPlayer(__instance.Mission.DoesMissionRequireCivilianEquipment, noHorses: true);
			missionBehavior.SpawnLocationCharacters();
			MissionAgentHandler.SpawnHorses();
			MissionAgentHandler.SpawnCats();
			MissionAgentHandler.SpawnDogs();
			if (!isNight)
			{
				MissionAgentHandler.SpawnSheeps();
				MissionAgentHandler.SpawnCows();
				MissionAgentHandler.SpawnHogs();
				MissionAgentHandler.SpawnGeese();
				MissionAgentHandler.SpawnChicken();
			}
			return false;
		}
		return true;
	}
}
