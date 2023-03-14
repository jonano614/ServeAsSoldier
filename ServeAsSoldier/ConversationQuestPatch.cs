using HarmonyLib;
using SandBox.CampaignBehaviors;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(LordConversationsCampaignBehavior), "conversation_hero_main_options_have_issue_on_condition")]
internal class ConversationQuestPatch
{
	private static bool Prefix(bool __result)
	{
		if (Test.followingHero != null)
		{
			__result = false;
			return false;
		}
		return true;
	}
}
