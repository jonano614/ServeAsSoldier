using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(HeroDeveloper), "AddSkillXp")]
internal static class SkillsFromPartyPatch
{
	private static void Postfix(HeroDeveloper __instance, SkillObject skill, float rawXp, bool isAffectedByFocusFactor, bool shouldNotify)
	{
		if (Test.followingHero != null && Test.followingHero.PartyBelongedTo != null && __instance.Hero == Test.followingHero)
		{
			if (Test.currentAssignment == Test.Assignment.Strategist && skill == DefaultSkills.Tactics)
			{
				Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, rawXp);
			}
			else if (Test.currentAssignment == Test.Assignment.Engineer && skill == DefaultSkills.Engineering)
			{
				Hero.MainHero.AddSkillXp(DefaultSkills.Engineering, rawXp);
			}
			else if (Test.currentAssignment == Test.Assignment.Quartermaster && skill == DefaultSkills.Steward)
			{
				Hero.MainHero.AddSkillXp(DefaultSkills.Steward, rawXp);
			}
			else if (Test.currentAssignment == Test.Assignment.Scout && skill == DefaultSkills.Scouting)
			{
				Hero.MainHero.AddSkillXp(DefaultSkills.Scouting, rawXp);
			}
			else if (Test.currentAssignment == Test.Assignment.Surgeon && skill == DefaultSkills.Medicine)
			{
				Hero.MainHero.AddSkillXp(DefaultSkills.Medicine, rawXp);
			}
		}
	}
}
