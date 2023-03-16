using HarmonyLib;
using TaleWorlds.Library;
using TaleWorlds.Localization;
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
			TextObject text = new TextObject("{=FLT0000310}Can not retreat from this mission");
			InformationManager.DisplayMessage(new InformationMessage(text.ToString()));
			return false;
		}
		return true;
	}
}
