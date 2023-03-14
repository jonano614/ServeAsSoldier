using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(VassalAndMercenaryOfferCampaignBehavior), "DailyTick")]
internal class VassalAndMercenaryOfferPatch
{
	private static bool Prefix()
	{
		if (Test.followingHero != null)
		{
			return false;
		}
		return true;
	}
}
