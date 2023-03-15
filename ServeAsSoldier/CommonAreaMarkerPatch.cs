using HarmonyLib;
using SandBox.Objects.AreaMarkers;
using TaleWorlds.CampaignSystem.Settlements;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(CommonAreaMarker), "GetAlley")]
internal class CommonAreaMarkerPatch
{
	private static bool Prefix(CommonAreaMarker __instance, ref Alley __result)
	{
		if (Test.followingHero != null)
		{
			__result = null;
			Settlement settlement = Test.followingHero.CurrentSettlement;
			if (settlement != null && settlement?.Alleys != null && __instance.AreaIndex > 0 && __instance.AreaIndex <= settlement.Alleys.Count)
			{
				__result = settlement.Alleys[__instance.AreaIndex - 1];
			}
			return false;
		}
		return true;
	}
}
