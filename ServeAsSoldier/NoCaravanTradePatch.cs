using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(CaravansCampaignBehavior), "caravan_buy_products_on_condition")]
internal class NoCaravanTradePatch
{
	private static bool Prefix(ref bool __result)
	{
		if (Test.followingHero != null)
		{
			__result = false;
			return false;
		}
		return true;
	}
}
