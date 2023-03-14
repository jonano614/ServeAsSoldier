using HarmonyLib;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(DefaultArmyManagementCalculationModel), "CalculatePartyInfluenceCost")]
internal class InfluenceCalculationPatch
{
	private static bool Prefix(MobileParty armyLeaderParty, MobileParty party, ref int __result)
	{
		if (armyLeaderParty == null || armyLeaderParty.LeaderHero == null || armyLeaderParty.LeaderHero.Clan == null || party == null || party.LeaderHero == null || party.LeaderHero.Clan == null)
		{
			__result = int.MaxValue;
			return false;
		}
		return true;
	}
}
