using System.Reflection;
using HarmonyLib;
using SandBox.Missions.MissionLogics.Towns;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(AlleyFightSpawnHandler), "AfterStart")]
internal class AlleyArmourPatch
{
	private static bool Prefix(AlleyFightSpawnHandler __instance)
	{
		if (Test.followingHero != null)
		{
			MapEvent _mapEvent = (MapEvent)typeof(AlleyFightSpawnHandler).GetField("_mapEvent", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(__instance);
			int num = _mapEvent.GetNumberOfInvolvedMen(BattleSideEnum.Defender);
			int num2 = _mapEvent.GetNumberOfInvolvedMen(BattleSideEnum.Attacker);
			int defenderInitialSpawn = num;
			int attackerInitialSpawn = num2;
			((MissionBehavior)(object)__instance).Mission.DoesMissionRequireCivilianEquipment = false;
			MissionAgentSpawnLogic _missionAgentSpawnLogic = (MissionAgentSpawnLogic)typeof(AlleyFightSpawnHandler).GetField("_missionAgentSpawnLogic", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(__instance);
			_missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Defender, spawnHorses: false);
			_missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Attacker, spawnHorses: false);
			MissionSpawnSettings missionSpawnSettings = MissionSpawnSettings.CreateDefaultSpawnSettings();
			_missionAgentSpawnLogic.InitWithSinglePhase(num, num2, defenderInitialSpawn, attackerInitialSpawn, spawnDefenders: true, spawnAttackers: true, in missionSpawnSettings);
			return false;
		}
		return true;
	}
}
