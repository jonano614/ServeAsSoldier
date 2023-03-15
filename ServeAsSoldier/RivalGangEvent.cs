using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace ServeAsSoldier;

public class RivalGangEvent : CampaignBehaviorBase
{
	private Hero GangLeader1;

	private Hero GangLeader2;

	private Settlement EventSettlement;

	private MobileParty gangParty;

	public override void RegisterEvents()
	{
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, MenuItems);
	}

	private void MenuItems(CampaignGameStarter campaignStarter)
	{
		campaignStarter.AddWaitGameMenu("gang_war", "", wait_on_init, wait_on_condition, null, wait_on_tick, GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);
	}

	private void wait_on_tick(MenuCallbackArgs args, CampaignTime dt)
	{
	}

	private bool wait_on_condition(MenuCallbackArgs args)
	{
		return true;
	}

	private void wait_on_init(MenuCallbackArgs args)
	{
	}

	private void OnGameMenuOpened(MenuCallbackArgs args)
	{
		if (Campaign.Current.GameMenuManager.NextLocation == null && GameStateManager.Current.ActiveState is MapState && args.MenuContext.GameMenu.StringId == "gang_war" && PlayerEncounter.Battle != null)
		{
			if (gangParty != null)
			{
				DestroyPartyAction.Apply(PartyBase.MainParty, gangParty);
			}
			MobileParty.MainParty.MemberRoster.AddToCounts(EventSettlement.Culture.GangleaderBodyguard, -1 * MobileParty.MainParty.MemberRoster.GetElementNumber(MobileParty.MainParty.MemberRoster.FindIndexOfTroop(EventSettlement.Culture.GangleaderBodyguard)));
			Test.OngoinEvent = false;
			if (PlayerEncounter.Battle.WinningSide == PlayerEncounter.Battle.PlayerSide)
			{
				GangLeader1.AddPower(20f);
				GangLeader2.AddPower(-10f);
				Test.conversation_type = "gang_war_win";
				Hero.MainHero.AddSkillXp(DefaultSkills.Roguery, 500f);
				Campaign.Current.ConversationManager.AddDialogFlow(CreateDialog2());
				CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false, false), new ConversationCharacterData(GangLeader1.CharacterObject, (PartyBase)null, false, false, false, false, false));
			}
			else
			{
				GangLeader1.AddPower(-10f);
				GangLeader2.AddPower(20f);
				TextObject text = new TextObject("{=FLT0000163}A few of the men from your party found your body and brought it back to the camp.");
				InformationManager.DisplayMessage(new InformationMessage(text.ToString()));
				Hero.MainHero.AddSkillXp(DefaultSkills.Roguery, 200f);
			}
			PlayerEncounter.Current.FinalizeBattle();
			PlayerEncounter.LeaveEncounter = true;
			GameMenu.ActivateGameMenu("party_wait");
		}
	}

	private DialogFlow CreateDialog2()
	{
		TextObject textObject = new TextObject("{=FLT0000164}Well done {HERO}, here is the money I promised you![rf:very_positive_hi, rb:very_positive]");
		TextObject textObject2 = new TextObject("{=FLT0000159}Glad I could help");
		return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject).Condition(delegate
		{
			textObject.SetTextVariable("HERO", Hero.MainHero.EncyclopediaLinkWithName);
			return Test.conversation_type == "gang_war_win";
		})
			.Consequence(delegate
			{
				Test.conversation_type = null;
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, MBRandom.RandomInt(1000, 3000));
				ChangeRelationAction.ApplyPlayerRelation(GangLeader1, 10);
				ChangeRelationAction.ApplyPlayerRelation(GangLeader2, -5);
			})
			.BeginPlayerOptions()
			.PlayerOption(textObject2)
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog();
	}

	private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
		if (Test.followingHero != null && hero == Test.followingHero && settlement.IsTown && gangLeaders(settlement).Count > 1 && !Hero.MainHero.IsWounded && !Test.OngoinEvent && Test.print(MBRandom.RandomInt(100)) < 5)
		{
			Test.OngoinEvent = true;
			EventSettlement = settlement;
			EnterSettlementAction.ApplyForParty(MobileParty.MainParty, EventSettlement);
			Test.conversation_type = "gang_war";
			Campaign.Current.ConversationManager.AddDialogFlow(CreateDialog());
			List<Hero> leaders = gangLeaders(settlement);
			GangLeader1 = leaders.GetRandomElement();
			leaders.Remove(GangLeader1);
			GangLeader2 = leaders.GetRandomElement();
			CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false, false), new ConversationCharacterData(GangLeader1.CharacterObject, (PartyBase)null, false, false, false, false, false));
		}
	}

	private DialogFlow CreateDialog()
	{
		TextObject textObject = new TextObject("{=FLT0000165}You want to make some quick gold?  I got a problem with a upstart by the name of {RIVAL}.  {GENDER_PRONOUN} has been bothering shop owners under our protection, demanding money and making threats. Let me tell you something - those shop owners are my cows, and no one else gets to milk them.  Me and my boys are eager to teach them a lesson but I figure some extra muscle wouldn't hurt");
		TextObject textObject2 = new TextObject("{=FLT0000166}Quick gold you say, sign me up!");
		TextObject textObject3 = new TextObject("{=FLT0000167}I don't want any part in this.");
		return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject).Condition(delegate
		{
			textObject.SetTextVariable("RIVAL", GangLeader2.EncyclopediaLinkWithName);
			textObject.SetTextVariable("GENDER_PRONOUN", GangLeader2.IsFemale ? "She" : "He");
			return Test.conversation_type == "gang_war";
		})
			.Consequence(delegate
			{
				Test.conversation_type = null;
			})
			.BeginPlayerOptions()
			.PlayerOption(textObject2)
			.CloseDialog()
			.Consequence(delegate
			{
				SubModule.ExecuteActionOnNextTick(delegate
				{
					GangFight();
				});
			})
			.PlayerOption(textObject3)
			.Consequence(delegate
			{
				Test.OngoinEvent = false;
			})
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog();
	}

	private void GangFight()
	{
		int upgradeLevel = EventSettlement.Town.GetWallLevel();
		int size = MBRandom.RandomInt(15, 25);
		MobileParty.MainParty.MemberRoster.AddToCounts(EventSettlement.Culture.GangleaderBodyguard, size);
		gangParty = CreateGangParty(GangLeader2, size + 5 + MobileParty.MainParty.MemberRoster.TotalHeroes);
		MobileParty.MainParty.IsActive = true;
		Test.disable_XP = true;
		PlayerEncounter.RestartPlayerEncounter(gangParty.Party, PartyBase.MainParty, forcePlayerOutFromSettlement: false);
		//PlayerEncounter.Current.ForceAlleyFight = true;
		PlayerEncounter.StartBattle();
        Location locationWithId = EventSettlement.LocationComplex.GetLocationWithId("center");
        CampaignMission.OpenAlleyFightMission(locationWithId.GetSceneName(upgradeLevel), upgradeLevel, locationWithId, 
			MobileParty.MainParty.MemberRoster, gangParty.MemberRoster);
		GameMenu.ActivateGameMenu("gang_war");
	}

	private MobileParty CreateGangParty(Hero owner, int size)
	{
		MobileParty gangParty = MobileParty.CreateParty("gang_party", null);
		TextObject textObject = new TextObject("{=FLT0000162}{RIVAL_GANG_LEADER}'s Party");
		textObject.SetTextVariable("RIVAL_GANG_LEADER", owner.Name);
		gangParty.InitializeMobilePartyAroundPosition(new TroopRoster(gangParty.Party), new TroopRoster(gangParty.Party), EventSettlement.GatePosition, 1f, 0.5f);
		gangParty.SetCustomName(textObject);
		EnterSettlementAction.ApplyForParty(gangParty, EventSettlement);
		gangParty.MemberRoster.AddToCounts(EventSettlement.Culture.GangleaderBodyguard, size);
		EnterSettlementAction.ApplyForParty(gangParty, EventSettlement);
		return gangParty;
	}

	private List<Hero> gangLeaders(Settlement town)
	{
		List<Hero> list = new List<Hero>();
		foreach (Hero notable in town.Notables)
		{
			if (notable.IsGangLeader)
			{
				list.Add(notable);
			}
		}
		return list;
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
