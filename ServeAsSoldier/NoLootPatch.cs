using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Encounters;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(PlayerEncounter), "DoLootParty")]
internal class NoLootPatch
{
	private static bool Prefix(PlayerEncounter __instance)
	{
		if (Test.followingHero != null)
		{
			typeof(PlayerEncounter).GetField("_mapEventState", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SetValue(__instance, PlayerEncounterState.End);
			return false;
		}
		return true;
	}
}
