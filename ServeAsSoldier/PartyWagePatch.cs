using HarmonyLib;
using TaleWorlds.CampaignSystem.Party;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(MobileParty), "TotalWage", MethodType.Getter)]
internal class PartyWagePatch
{
	private static bool Prefix(ref int __result, ref MobileParty __instance)
	{
		if (Test.followingHero != null && Test.followingHero.PartyBelongedTo == __instance)
		{
			__result = 0;
			return false;
		}
		if (__instance.LeaderHero != null && __instance.LeaderHero.Culture == null)
		{
			__result = 0;
			return false;
		}
		return true;
	}
}
