using System.Reflection;
using HarmonyLib;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.TournamentGames;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(TournamentBehavior), "OnPlayerWinTournament")]
internal class AddTournamentPrizePatch
{
	private static void Postfix(TournamentBehavior __instance)
	{
		if (Test.followingHero != null)
		{
			TournamentGame game = (TournamentGame)typeof(TournamentBehavior).GetField("_tournamentGame", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(__instance);
			if (Test.tournamentPrizes == null)
			{
				Test.tournamentPrizes = new ItemRoster();
			}
			Test.tournamentPrizes.AddToCounts(game.Prize, 1);
			Test.ChangeFactionRelation(Test.followingHero.MapFaction, Hero.MainHero.GetPerkValue(DefaultPerks.OneHanded.Duelist) ? 300 : 200);
			Test.xp += 200;
		}
	}
}
