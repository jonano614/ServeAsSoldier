using HarmonyLib;
using TaleWorlds.CampaignSystem;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(Hero), "CanHeroEquipmentBeChanged")]
public class HeroEquipmentChangePatch
{
	public static void Postfix(Hero __instance, ref bool __result)
	{
		if (Test.followingHero != null && __instance != Hero.MainHero)
		{
			__result = false;
		}
	}
}
