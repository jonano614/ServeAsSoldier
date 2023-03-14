using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(PlayerArmyWaitBehavior), "OnArmyDispersed")]
internal class NoDisperseMessagePatch
{
	private static bool Prefix(Army army, Army.ArmyDispersionReason reason, bool isPlayersArmy)
	{
		if (Test.followingHero != null)
		{
			return false;
		}
		return true;
	}
}
