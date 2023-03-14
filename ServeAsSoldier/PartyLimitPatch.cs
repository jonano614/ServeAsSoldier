using HarmonyLib;
using TaleWorlds.CampaignSystem.Party;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(MobileParty), "LimitedPartySize", MethodType.Getter)]
internal class PartyLimitPatch
{
	private static bool Prefix(ref int __result, ref MobileParty __instance)
	{
		if (Test.followingHero != null && Test.followingHero.PartyBelongedTo == __instance)
		{
			__result = __instance.Party.PartySizeLimit;
			return false;
		}
		return true;
	}
}
