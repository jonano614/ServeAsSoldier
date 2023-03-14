using System.Linq;
using HarmonyLib;
using Helpers;
using SandBox.Missions.MissionLogics;
using SandBox.Objects.AreaMarkers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(VisualTrackerMissionBehavior), "RefreshCommonAreas")]
internal class CommonAreaPatch
{
	private static bool Prefix(VisualTrackerMissionBehavior __instance)
	{
		if (Test.followingHero != null)
		{
			Settlement settlement = Test.followingHero.CurrentSettlement;
			if (settlement == null)
			{
				settlement = SettlementHelper.FindNearestVillage();
			}
			foreach (CommonAreaMarker commonAreaMarker in __instance.Mission.ActiveMissionObjects.FindAllWithType<CommonAreaMarker>().ToList())
			{
				if (settlement.Alleys.Count >= commonAreaMarker.AreaIndex && Campaign.Current.VisualTrackerManager.CheckTracked((ITrackableBase)settlement.Alleys[commonAreaMarker.AreaIndex - 1]))
				{
					__instance.RegisterLocalOnlyObject(commonAreaMarker);
				}
			}
			return false;
		}
		return true;
	}
}
