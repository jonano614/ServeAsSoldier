using HarmonyLib;
using SandBox.Missions.MissionLogics;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(MissionFightHandler), "OnEndMissionRequest")]
internal class MissionFightEndPatch
{
	private static bool Prefix(out bool canPlayerLeave)
	{
		canPlayerLeave = true;
		if (Test.followingHero != null)
		{
			return false;
		}
		return true;
	}
}
