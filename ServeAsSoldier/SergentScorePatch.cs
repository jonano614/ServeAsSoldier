using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(DefaultEncounterModel), "GetCharacterSergeantScore")]
internal class SergentScorePatch
{
	private static bool Prefix(Hero hero, ref int __result)
	{
		if (Test.followingHero != null && hero == Hero.MainHero)
		{
			__result = 1000000;
			return false;
		}
		return true;
	}
}
