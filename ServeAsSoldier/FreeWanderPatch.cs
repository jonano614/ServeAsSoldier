using HarmonyLib;
using Helpers;
using SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(LordConversationsCampaignBehavior), "conversation_talk_lord_defeat_to_lord_release_on_consequence")]
internal class FreeWanderPatch
{
	private static bool Prefix()
	{
		if (Hero.OneToOneConversationHero.IsWanderer && Hero.OneToOneConversationHero.CompanionOf != Hero.MainHero.Clan)
		{
			Hero.OneToOneConversationHero.CompanionOf = null;
			if (Hero.OneToOneConversationHero.IsPrisoner)
			{
				EndCaptivityAction.ApplyByReleasedAfterBattle(Hero.OneToOneConversationHero);
			}
			ChangeRelationAction.ApplyPlayerRelation(CharacterObject.OneToOneConversationCharacter.HeroObject, 4);
			DialogHelper.SetDialogString("DEFEAT_LORD_ANSWER", "str_prisoner_released");
			return false;
		}
		return true;
	}
}
