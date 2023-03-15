using HarmonyLib;
using SandBox.Objects.AreaMarkers;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(WorkshopAreaMarker), "GetWorkshop")]
internal class WorkshopAreaMarkerPatch
{
	private static bool Prefix(WorkshopAreaMarker __instance, ref Workshop __result)
	{
		if (Test.followingHero != null)
		{
			__result = null;
			Settlement settlement = Test.followingHero.CurrentSettlement;
			if (settlement != null && settlement.IsTown && settlement.Town.Workshops.Length > __instance.AreaIndex && __instance.AreaIndex > 0)
			{
				__result = settlement.Town.Workshops[__instance.AreaIndex];
			}
			return false;
		}
		return true;
	}
}
