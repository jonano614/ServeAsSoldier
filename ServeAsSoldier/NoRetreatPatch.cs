using HarmonyLib;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(BasicMissionHandler), "CreateWarningWidgetForResult")]
internal class NoRetreatPatch
{
	private static bool Prefix(BattleEndLogic.ExitResult result)
	{
		if (Test.NoRetreat && Test.followingHero != null && !SubModule.settings.AllowEventBattleSkip)
		{
			InformationManager.DisplayMessage(new InformationMessage("Can not retreat from this mission"));
			return false;
		}
		return true;
	}
}
