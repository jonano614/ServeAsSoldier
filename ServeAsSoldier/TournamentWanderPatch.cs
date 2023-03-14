using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(HeroAgentSpawnCampaignBehavior), "AddWandererLocationCharacter")]
internal class TournamentWanderPatch
{
	private static bool Prefix(Hero wanderer, Settlement settlement)
	{
		if (Test.followingHero != null)
		{
			Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(wanderer.CharacterObject.Race, "_settlement");
			string actionSetCode = ((!(settlement.Culture.StringId.ToLower() == "aserai") && !(settlement.Culture.StringId.ToLower() == "khuzait")) ? (wanderer.IsFemale ? "as_human_female_warrior_in_tavern" : "as_human_warrior_in_tavern") : (wanderer.IsFemale ? "as_human_female_warrior_in_aserai_tavern" : "as_human_warrior_in_aserai_tavern"));
			LocationCharacter locationCharacter = new LocationCharacter(new AgentData(new PartyAgentOrigin(null, wanderer.CharacterObject)).Monster(monsterWithSuffix).NoHorses(noHorses: true), SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors, "npc_common", fixedLocation: true, LocationCharacter.CharacterRelations.Neutral, actionSetCode, useCivilianEquipment: true);
			if (settlement.IsTown)
			{
				settlement.LocationComplex.GetLocationWithId("tavern").AddCharacter(locationCharacter);
			}
			return false;
		}
		return true;
	}
}
