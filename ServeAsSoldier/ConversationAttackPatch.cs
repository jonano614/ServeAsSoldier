using HarmonyLib;
using SandBox.CampaignBehaviors;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(LordConversationsCampaignBehavior), "conversation_lord_is_threated_neutral_on_condition")]
internal class ConversationAttackPatch
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
