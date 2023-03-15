using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace ServeAsSoldier;

public class TownRobberEvent : CampaignBehaviorBase
{
	private Hero RobbedNotable;

	private Hero RobberNotable;

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
		campaignStarter.AddWaitGameMenu("robbery_aftermath", "", wait_on_init, wait_on_condition, null, wait_on_tick, GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);
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
		if (Campaign.Current.GameMenuManager.NextLocation == null && GameStateManager.Current.ActiveState is MapState && args.MenuContext.GameMenu.StringId == "robbery_aftermath" && PlayerEncounter.Battle != null)
		{
			Campaign.Current.PlayerTraitDeveloper.AddTraitXp(DefaultTraits.Honor, 50);
			if (gangParty != null)
			{
				DestroyPartyAction.Apply(PartyBase.MainParty, gangParty);
			}
			Test.OngoinEvent = false;
			if (PlayerEncounter.Battle.WinningSide == PlayerEncounter.Battle.PlayerSide)
			{
				RobbedNotable.AddPower(20f);
				RobberNotable.AddPower(-10f);
				EventSettlement.Town.Security += 10f;
				Test.conversation_type = "town_robbery_win";
				Campaign.Current.ConversationManager.AddDialogFlow(CreateDialog2());
				CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false, false), new ConversationCharacterData(RobbedNotable.CharacterObject, (PartyBase)null, false, false, false, false, false));
			}
			else
			{
				RobbedNotable.AddPower(-10f);
				RobberNotable.AddPower(20f);
				EventSettlement.Town.Security += 5f;
				Test.conversation_type = "town_robbery_lose";
				Campaign.Current.ConversationManager.AddDialogFlow(CreateDialog3());
				CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false, false), new ConversationCharacterData(RobbedNotable.CharacterObject, (PartyBase)null, false, false, false, false, false));
			}
			PlayerEncounter.Current.FinalizeBattle();
			PlayerEncounter.LeaveEncounter = true;
			GameMenu.ActivateGameMenu("party_wait");
		}
	}

	private DialogFlow CreateDialog2()
	{
		TextObject textObject = new TextObject("{=FLT0000155}Thank you {HERO}, You saved my bussiness![rf:very_positive_hi, rb:very_positive]");
		TextObject textObject2 = new TextObject("{=FLT0000149}Just doing my job!");
		return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject).Condition(delegate
		{
			textObject.SetTextVariable("HERO", Hero.MainHero.EncyclopediaLinkWithName);
			return Test.conversation_type == "town_robbery_win";
		})
			.Consequence(delegate
			{
				Test.conversation_type = null;
				ChangeRelationAction.ApplyPlayerRelation(RobbedNotable, 10);
			})
			.BeginPlayerOptions()
			.PlayerOption(textObject2)
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog();
	}

	private DialogFlow CreateDialog3()
	{
		TextObject textObject = new TextObject("{=FLT0000156}{HERO} are you okay?[rf:unsure_hi, rb:unsure]");
		TextObject textObject2 = new TextObject("{=FLT0000157}I'm fine. I'm sorry I let the thugs escape");
		TextObject textObject3 = new TextObject("{=FLT0000158}Don't feel bad, the thugs had you outnumbered.  You had no chance against that many.  The commotion from that fight drew the attention of every guard in town.  The thugs had to flee so fast that the weren't able to take any of my merchandise with them.");
		TextObject textObject4 = new TextObject("{=FLT0000159}Glad I could help.");
		return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject).Condition(delegate
		{
			textObject.SetTextVariable("HERO", Hero.MainHero.EncyclopediaLinkWithName);
			return Test.conversation_type == "town_robbery_lose";
		})
			.Consequence(delegate
			{
				Test.conversation_type = null;
				ChangeRelationAction.ApplyPlayerRelation(RobbedNotable, 5);
			})
			.BeginPlayerOptions()
			.PlayerLine(textObject2)
			.NpcLine(textObject3)
			.PlayerLine(textObject4)
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog();
	}

	private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
		if (Test.followingHero != null && hero == Test.followingHero && settlement.MapFaction == hero.MapFaction && settlement.IsTown && hasNotables(settlement) && !Hero.MainHero.IsWounded && !Test.OngoinEvent && Test.print(MBRandom.RandomInt(100)) < 5)
		{
			Test.OngoinEvent = true;
			EventSettlement = settlement;
			EnterSettlementAction.ApplyForParty(MobileParty.MainParty, EventSettlement);
			Test.conversation_type = "town_robbery";
			Campaign.Current.ConversationManager.AddDialogFlow(CreateDialog());
			RobbedNotable = GetRobbedNotable(settlement);
			RobberNotable = GetRobberNotable(settlement);
			CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false, false), new ConversationCharacterData(RobbedNotable.CharacterObject, (PartyBase)null, false, false, false, false, false));
		}
	}

	private DialogFlow CreateDialog()
	{
		TextObject textObject = new TextObject("{=FLT0000160}Guards! Guards!  {ROBBER}'s thugs are stealing all of the goods from my shop!  Come quick before they get away![rf:very_negative_hi, rb:very_negative]");
		TextObject textObject2 = new TextObject("{=FLT0000161}I am a soldier, not a guard!  This is none of my business!");
		return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject).Condition(delegate
		{
			textObject.SetTextVariable("ROBBER", RobberNotable.EncyclopediaLinkWithName);
			return Test.conversation_type == "town_robbery";
		})
			.Consequence(delegate
			{
				Test.conversation_type = null;
			})
			.BeginPlayerOptions()
			.PlayerOption("{=FLT0000303}Lead the way!")
			.CloseDialog()
			.Consequence(delegate
			{
				SubModule.ExecuteActionOnNextTick(delegate
				{
					GangFight();
				});
			})
			.PlayerOption(textObject2)
			.Consequence(delegate
			{
				Test.OngoinEvent = false;
				ChangeRelationAction.ApplyPlayerRelation(RobbedNotable, -1);
			})
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog();
	}

	private void GangFight()
	{
		int upgradeLevel = EventSettlement.Town.GetWallLevel();
		MobileParty GangParty = CreateGangParty();
		MobileParty.MainParty.IsActive = true;
		gangParty = GangParty;
		PlayerEncounter.RestartPlayerEncounter(GangParty.Party, PartyBase.MainParty, forcePlayerOutFromSettlement: false);
		//PlayerEncounter.Current.ForceAlleyFight = true;
		PlayerEncounter.StartBattle();
        Location locationWithId = LocationComplex.Current.GetLocationWithId("center");
        CampaignMission.OpenAlleyFightMission(EventSettlement.LocationComplex.GetLocationWithId("center").GetSceneName(upgradeLevel), upgradeLevel, locationWithId,
            MobileParty.MainParty.MemberRoster, gangParty.MemberRoster);
        GameMenu.ActivateGameMenu("robbery_aftermath");
	}

	private MobileParty CreateGangParty()
	{
		MobileParty gangParty = MobileParty.CreateParty("gang_party", null);
		TextObject textObject = new TextObject("{=FLT0000162}{RIVAL_GANG_LEADER}'s Party");
		textObject.SetTextVariable("RIVAL_GANG_LEADER", RobberNotable.Name);
		gangParty.InitializeMobilePartyAroundPosition(new TroopRoster(gangParty.Party), new TroopRoster(gangParty.Party), EventSettlement.GatePosition, 1f, 0.5f);
		gangParty.SetCustomName(textObject);
		EnterSettlementAction.ApplyForParty(gangParty, EventSettlement);
		gangParty.MemberRoster.AddToCounts(EventSettlement.Culture.GangleaderBodyguard, MBRandom.RandomInt(5) + 3 + MobileParty.MainParty.MemberRoster.TotalHealthyCount);
		return gangParty;
	}

	private bool hasNotables(Settlement town)
	{
		int num1 = 0;
		int num2 = 0;
		foreach (Hero notable in town.Notables)
		{
			if (notable.IsGangLeader)
			{
				num1++;
			}
			else
			{
				num2++;
			}
		}
		return num1 > 0 && num2 > 0;
	}

	private Hero GetRobbedNotable(Settlement town)
	{
		List<Hero> list = new List<Hero>();
		foreach (Hero notable in town.Notables)
		{
			if (!notable.IsGangLeader)
			{
				list.Add(notable);
			}
		}
		return list.GetRandomElement();
	}

	private Hero GetRobberNotable(Settlement town)
	{
		List<Hero> list = new List<Hero>();
		foreach (Hero notable in town.Notables)
		{
			if (notable.IsGangLeader)
			{
				list.Add(notable);
			}
		}
		return list.GetRandomElement();
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
