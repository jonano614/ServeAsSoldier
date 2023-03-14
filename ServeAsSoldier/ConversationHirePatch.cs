using HarmonyLib;
using SandBox.CampaignBehaviors;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(LordConversationsCampaignBehavior), "conversation_hero_hire_on_condition")]
internal class ConversationHirePatch
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
