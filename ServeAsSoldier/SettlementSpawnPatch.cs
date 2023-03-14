using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(HeroAgentSpawnCampaignBehavior), "AddCompanionsAndClanMembersToSettlement")]
internal class SettlementSpawnPatch
{
	private static bool Prefix(Settlement settlement)
	{
		if (Test.followingHero != null)
		{
			return false;
		}
		return true;
	}
}
