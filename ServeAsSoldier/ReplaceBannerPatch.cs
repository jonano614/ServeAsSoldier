using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(Clan), "Banner", MethodType.Getter)]
internal class ReplaceBannerPatch
{
	private static bool Prefix(ref Banner __result, ref Clan __instance)
	{
		if (Test.followingHero != null && __instance == Hero.MainHero.Clan)
		{
			__result = Test.followingHero.ClanBanner;
			return false;
		}
		return true;
	}
}
