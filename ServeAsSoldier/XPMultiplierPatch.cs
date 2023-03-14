using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(HeroDeveloper), "AddSkillXp")]
internal class XPMultiplierPatch
{
	private static bool Prefix(HeroDeveloper __instance, SkillObject skill, ref float rawXp, bool isAffectedByFocusFactor = true, bool shouldNotify = true)
	{
		if (__instance.Hero.IsWanderer && __instance.Hero.CompanionOf != null)
		{
			if (__instance.Hero.CompanionOf != Clan.PlayerClan)
			{
				rawXp *= 100f;
			}
			else if (Test.followingHero != null)
			{
				rawXp *= 2f;
			}
		}
		return true;
	}
}
