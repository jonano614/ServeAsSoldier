using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;

namespace ServeAsSoldier;

internal class BanditAmbushEvent : CampaignBehaviorBase
{
	private MobileParty BanditParty;

	public override void RegisterEvents()
	{
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, Tick);
		CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, MenuItems);
	}

	private void Tick()
	{
		if (Test.followingHero != null && Test.followingHero.PartyBelongedTo != null && Test.followingHero.PartyBelongedTo.MapEvent == null && Test.followingHero.PartyBelongedTo.MemberRoster.TotalHealthyCount > 50 && !Hero.MainHero.IsWounded && !Test.OngoinEvent && Test.followingHero.PartyBelongedTo.Army == null && Test.followingHero.PartyBelongedTo.CurrentSettlement == null && MBRandom.RandomInt(2500) == 0)
		{
			Test.OngoinEvent = true;
			Test.conversation_type = "bandit_ambush";
			Campaign.Current.ConversationManager.AddDialogFlow(CreateDialog());
			SoundEvent.PlaySound2D(SoundEvent.GetEventIdFromString("event:/ui/mission/horns/attack"));
			SoundEvent.PlaySound2D(SoundEvent.GetEventIdFromString("event:/ui/mission/horns/attack"));
			SoundEvent.PlaySound2D(SoundEvent.GetEventIdFromString("event:/ui/mission/horns/attack"));
			CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false, false), new ConversationCharacterData(Test.followingHero.PartyBelongedTo.MemberRoster.GetTroopRoster().GetRandomElement().Character, (PartyBase)null, false, false, false, false, false));
		}
	}

	private void MenuItems(CampaignGameStarter campaignStarter)
	{
		campaignStarter.AddWaitGameMenu("bandit_ambush", "", wait_on_init, wait_on_condition, null, wait_on_tick, GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);
	}

	private void wait_on_init(MenuCallbackArgs args)
	{
	}

	private void wait_on_tick(MenuCallbackArgs args, CampaignTime dt)
	{
	}

	private bool wait_on_condition(MenuCallbackArgs args)
	{
		return true;
	}

	private void OnGameMenuOpened(MenuCallbackArgs args)
	{
		if (Campaign.Current.GameMenuManager.NextLocation == null && GameStateManager.Current.ActiveState is MapState && args.MenuContext.GameMenu.StringId == "bandit_ambush" && PlayerEncounter.Battle != null)
		{
			Test.OngoinEvent = false;
			PlayerEncounter.Current.FinalizeBattle();
			PlayerEncounter.LeaveEncounter = true;
			GameMenu.ActivateGameMenu("party_wait");
		}
	}

	private DialogFlow CreateDialog()
	{
		TextObject textObject = new TextObject("{=FLT0000172}Sound the alarm!  We are under attack!  To arms men!  [rf:very_negative_hi, rb:very_negative]");
		TextObject textObject2 = new TextObject("{=FLT0000173}Where did they come from?");
		return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject).Condition(() => Test.conversation_type == "bandit_ambush")
			.Consequence(delegate
			{
				Test.conversation_type = null;
			})
			.PlayerLine(textObject2)
			.Consequence(delegate
			{
				SubModule.ExecuteActionOnNextTick(delegate
				{
					startbattle();
				});
			})
			.CloseDialog();
	}

	private void startbattle()
	{
		BanditParty = CreateAmbushParty();
		MobileParty.MainParty.IsActive = true;
		PlayerEncounter.RestartPlayerEncounter(PartyBase.MainParty, BanditParty.Party, forcePlayerOutFromSettlement: false);
		PlayerEncounter.StartBattle();
		Test.NoRetreat = true;
		MapEvent _mapEvent = (MapEvent)typeof(PlayerEncounter).GetField("_mapEvent", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(PlayerEncounter.Current);
		typeof(MapEventSide).GetMethod("AddNearbyPartyToPlayerMapEvent", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(_mapEvent.DefenderSide, new object[1] { Test.followingHero.PartyBelongedTo });
		CampaignMission.OpenBattleMission(PlayerEncounter.GetBattleSceneForMapPatch(Campaign.Current.MapSceneWrapper.GetMapPatchAtPosition(MobileParty.MainParty.Position2D)));
		GameMenu.ActivateGameMenu("bandit_ambush");
	}

	private MobileParty CreateAmbushParty()
	{
		Settlement settlement = SettlementHelper.FindNearestHideout();
		Clan clan = null;
		if (settlement != null)
		{
			CultureObject banditCulture = settlement.Culture;
			clan = Clan.BanditFactions.FirstOrDefault((Clan x) => x.Culture == banditCulture);
		}
		if (clan == null)
		{
			clan = ((IReadOnlyList<Clan>)Clan.All).GetRandomElementWithPredicate((Func<Clan, bool>)((Clan x) => x.IsBanditFaction));
		}
		MobileParty _ambushParty = BanditPartyComponent.CreateBanditParty("bandit_ambush_party", clan, settlement.Hideout, isBossParty: false);
		TextObject customName = new TextObject("{=FLT0000174}Bandit Ambush Party");
		_ambushParty.InitializeMobilePartyAroundPosition(new TroopRoster(_ambushParty.Party), new TroopRoster(_ambushParty.Party), Test.followingHero.PartyBelongedTo.Position2D, 1f, 0.5f);
		_ambushParty.SetCustomName(customName);
		CharacterObject character1 = CharacterObject.All.FirstOrDefault((CharacterObject t) => t.StringId == "mounted_ransacker");
		CharacterObject character2 = CharacterObject.All.FirstOrDefault((CharacterObject t) => t.StringId == "mounted_pillager");
		CharacterObject character3 = CharacterObject.All.FirstOrDefault((CharacterObject t) => t.StringId == "looter");
		_ambushParty.MemberRoster.AddToCounts(character1, 5 * Test.followingHero.PartyBelongedTo.MemberRoster.TotalHealthyCount / 20);
		_ambushParty.MemberRoster.AddToCounts(character2, 5 * Test.followingHero.PartyBelongedTo.MemberRoster.TotalHealthyCount / 20);
		_ambushParty.MemberRoster.AddToCounts(character3, 30 * Test.followingHero.PartyBelongedTo.MemberRoster.TotalHealthyCount / 20);
		return _ambushParty;
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
