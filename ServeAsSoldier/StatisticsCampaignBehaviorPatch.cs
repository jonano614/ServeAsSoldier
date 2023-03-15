using HarmonyLib;
using SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(StatisticsCampaignBehavior), "OnPartyAttachedAnotherParty")]
internal class StatisticsCampaignBehaviorPatch
{
	private static bool Prefix(MobileParty mobileParty)
	{
        if (Test.followingHero != null)
        {
            return false;
        }
        return true;
    }
}
