using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party.PartyComponents;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(BanditPartyComponent), "PartyOwner", MethodType.Getter)]
internal class banditComponentPatch
{
	private static bool Prefix(BanditPartyComponent __instance, ref Hero __result)
	{
		if (__instance == null || __instance.MobileParty == null || __instance.MobileParty.ActualClan == null)
		{
			__result = null;
			return false;
		}
		return true;
	}
}
