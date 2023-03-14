using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(EncounterGameMenuBehavior), "AddGameMenus")]
internal class EncounterMenuPatch
{
	private static void Postfix(CampaignGameStarter gameSystemInitializer)
	{
		TextObject textObject219 = new TextObject("{=FLT0000219}Wait in reserve");
		TextObject textObject220 = new TextObject("{=FLT0000221}You cant wait in reserve if there are less than 100 healthy troops in the army");
		gameSystemInitializer.AddGameMenuOption("encounter", "join_encounter_wait", textObject219.ToString(), (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			if (Test.followingHero != null && Test.followingHero.PartyBelongedTo != null && Test.followingHero.PartyBelongedTo.MapEvent != null && (ContainsParty(Test.followingHero.PartyBelongedTo.MapEvent.PartiesOnSide(BattleSideEnum.Attacker), Test.followingHero.PartyBelongedTo) ? Test.followingHero.PartyBelongedTo.MapEvent.AttackerSide.TroopCount : Test.followingHero.PartyBelongedTo.MapEvent.DefenderSide.TroopCount) < 100)
			{
				args.IsEnabled = false;
				args.Tooltip = textObject220;
			}
			args.optionLeaveType = GameMenuOption.LeaveType.LeaveTroopsAndFlee;
			return Test.followingHero != null && Test.followingHero.PartyBelongedTo != null && Test.followingHero.PartyBelongedTo.Army != null;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			PlayerEncounter.Finish();
			Test.waitingInReserve = true;
			GameMenu.ActivateGameMenu("battle_wait");
		}, true, -1, false);
		TextObject textObject221 = new TextObject("{=FLT0000231}Defect to other side");
		TextObject textObject222 = new TextObject("{=FLT0000232}Can only serve lords part of a kingdom");
		TextObject textObject223 = new TextObject("{=FLT0000233}This will harm your relations with your current lord and faction");
		TextObject textObject224 = new TextObject("{=FLT0000234}The enemy lord hates you");
		gameSystemInitializer.AddGameMenuOption("encounter", "encounter_defect", textObject221.ToString(), (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			if (Test.followingHero == null || Test.followingHero.PartyBelongedTo == null || Test.followingHero.PartyBelongedTo.MapEvent == null)
			{
				return false;
			}
			MobileParty mobileParty2 = (ContainsParty(Test.followingHero.PartyBelongedTo.MapEvent.PartiesOnSide(BattleSideEnum.Attacker), Test.followingHero.PartyBelongedTo) ? Test.followingHero.PartyBelongedTo.MapEvent.DefenderSide.LeaderParty.MobileParty : Test.followingHero.PartyBelongedTo.MapEvent.AttackerSide.LeaderParty.MobileParty);
			if (Test.followingHero != null && Test.followingHero.PartyBelongedTo != null && Test.followingHero.PartyBelongedTo.MapEvent != null && mobileParty2 != null && mobileParty2.LeaderHero != null && mobileParty2.LeaderHero.Clan != null && mobileParty2.LeaderHero.Clan.Kingdom != null && mobileParty2.LeaderHero.Occupation == Occupation.Lord && !mobileParty2.LeaderHero.Clan.IsMinorFaction)
			{
				if (mobileParty2.LeaderHero.GetRelation(Hero.MainHero) <= -10)
				{
					args.IsEnabled = false;
					args.Tooltip = textObject224;
				}
				else
				{
					args.IsEnabled = true;
					args.Tooltip = textObject223;
				}
			}
			else
			{
				args.IsEnabled = false;
				args.Tooltip = textObject222;
			}
			args.optionLeaveType = GameMenuOption.LeaveType.BribeAndEscape;
			return Test.followingHero != null && Test.followingHero.PartyBelongedTo != null && Test.followingHero.PartyBelongedTo.Army != null;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			MobileParty mobileParty = (ContainsParty(Test.followingHero.PartyBelongedTo.MapEvent.PartiesOnSide(BattleSideEnum.Attacker), Test.followingHero.PartyBelongedTo) ? Test.followingHero.PartyBelongedTo.MapEvent.DefenderSide.LeaderParty.MobileParty : Test.followingHero.PartyBelongedTo.MapEvent.AttackerSide.LeaderParty.MobileParty);
			PlayerEncounter.Finish();
			ChangeRelationAction.ApplyPlayerRelation(Test.followingHero, -25);
			Test.ChangeFactionRelation(Test.followingHero.MapFaction, -2000);
			Test.ChangeLordRelation(Test.followingHero, -5000);
			Test.followingHero = mobileParty.LeaderHero;
		}, true, -1, false);
	}

	public static bool ContainsParty(IReadOnlyList<MapEventParty> parties, MobileParty party)
	{
		foreach (MapEventParty p in parties)
		{
			if (p.Party.MobileParty == party)
			{
				return true;
			}
		}
		return false;
	}
}
