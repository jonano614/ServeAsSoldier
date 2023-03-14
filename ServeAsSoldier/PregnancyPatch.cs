using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(PregnancyCampaignBehavior), "CheckAreNearby")]
internal class PregnancyPatch
{
	private static bool Prefix(Hero hero, Hero spouse, ref bool __result)
	{
		if (Test.followingHero != null && hero.Clan == Clan.PlayerClan && hero != null && spouse != null && hero.PartyBelongedTo != null && spouse.PartyBelongedTo != null && hero.PartyBelongedTo == spouse.PartyBelongedTo)
		{
			__result = true;
			return false;
		}
		return true;
	}
}
