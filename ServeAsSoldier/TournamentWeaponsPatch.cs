using System.Collections.Generic;
using HarmonyLib;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(TournamentFightMissionController), "GetTeamWeaponEquipmentList")]
internal class TournamentWeaponsPatch
{
	private static bool Prefix(int teamSize, ref List<Equipment> __result)
	{
		if (Test.followingHero != null)
		{
			List<Equipment> list = new List<Equipment>();
			CultureObject culture = Test.followingHero.CurrentSettlement.Culture;
			IReadOnlyList<CharacterObject> readOnlyList = teamSize switch
			{
				2 => culture.TournamentTeamTemplatesForTwoParticipant, 
				4 => culture.TournamentTeamTemplatesForFourParticipant, 
				_ => culture.TournamentTeamTemplatesForOneParticipant, 
			};
			CharacterObject characterObject = ((readOnlyList.Count <= 0) ? (teamSize switch
			{
				2 => MBObjectManager.Instance.GetObject<CharacterObject>("tournament_template_empire_two_participant_set_v1"), 
				4 => MBObjectManager.Instance.GetObject<CharacterObject>("tournament_template_empire_four_participant_set_v1"), 
				_ => MBObjectManager.Instance.GetObject<CharacterObject>("tournament_template_empire_one_participant_set_v1"), 
			}) : readOnlyList[MBRandom.RandomInt(readOnlyList.Count)]);
			foreach (Equipment sourceEquipment in characterObject.BattleEquipments)
			{
				Equipment equipment = new Equipment();
				equipment.FillFrom(sourceEquipment);
				list.Add(equipment);
			}
			__result = list;
			return false;
		}
		return true;
	}
}
