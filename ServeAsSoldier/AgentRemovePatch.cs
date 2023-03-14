using HarmonyLib;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem.Settlements.Locations;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(MissionAgentHandler), "OnRemoveBehavior")]
internal class AgentRemovePatch
{
	private static bool Prefix()
	{
		if (Test.followingHero != null && Test.followingHero.CurrentSettlement != null)
		{
			foreach (Location location in Test.followingHero.CurrentSettlement.LocationComplex.GetListOfLocations())
			{
				if (location.StringId == "center" || location.StringId == "village_center" || location.StringId == "lordshall" || location.StringId == "prison" || location.StringId == "tavern")
				{
					location.RemoveAllCharacters((LocationCharacter x) => !x.Character.IsHero);
				}
			}
			return false;
		}
		return true;
	}
}
