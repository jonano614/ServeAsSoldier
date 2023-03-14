using HarmonyLib;
using SandBox.CampaignBehaviors;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(LordConversationsCampaignBehavior), "conversation_hero_main_options_discussions")]
internal class ConversationDiscussPatch
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
