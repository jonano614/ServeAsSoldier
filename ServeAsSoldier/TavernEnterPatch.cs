using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameMenus;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(PlayerTownVisitCampaignBehavior), "visit_the_tavern_on_condition")]
internal class TavernEnterPatch
{
	private static bool Prefix(ref bool __result, MenuCallbackArgs args)
	{
		if (Test.followingHero != null)
		{
			__result = false;
			return false;
		}
		return true;
	}
}
