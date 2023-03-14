using HarmonyLib;
using TaleWorlds.CampaignSystem.MapEvents;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(MapEvent), "IsPlayerSergeant")]
internal class SergentAssignmentPatch
{
	private static bool Prefix(ref bool __result)
	{
		if (Test.followingHero != null && Test.EnlistTier < 6)
		{
			__result = false;
			return false;
		}
		return true;
	}
}
