using Helpers;
using SandBox;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Arena;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;

namespace ServeAsSoldier;

public class TrainTroopsEvent : CampaignBehaviorBase
{
	private Settlement EventSettlement;

	public static bool trainingDone;

	public static bool success;

	public override void RegisterEvents()
	{
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, Tick);
	}

	private void Tick(float tick)
	{
		if (Test.followingHero == null || !trainingDone)
		{
			return;
		}
		if (success)
		{
			SubModule.ExecuteActionOnNextTick(delegate
			{
				Test.conversation_type = "training_success";
				Campaign.Current.ConversationManager.AddDialogFlow(CreateDialog2());
				CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false, false), new ConversationCharacterData(Test.followingHero.CharacterObject, (PartyBase)null, false, false, false, false, false));
			});
		}
		else
		{
			SubModule.ExecuteActionOnNextTick(delegate
			{
				Test.conversation_type = "training_fail";
				Campaign.Current.ConversationManager.AddDialogFlow(CreateDialog3());
				CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false, false), new ConversationCharacterData(Test.followingHero.CharacterObject, (PartyBase)null, false, false, false, false, false));
			});
		}
		Test.OngoinEvent = false;
		trainingDone = false;
		success = false;
	}

	private DialogFlow CreateDialog2()
	{
		TextObject textObject = new TextObject("{=FLT0000148}{HERO}, You have done well, the recruits can actual hold their own now. [rf:very_positive_hi, rb:very_positive]");
		TextObject textObject2 = new TextObject("{=FLT0000149}Just doing my job!");
		return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject).Condition(delegate
		{
			textObject.SetTextVariable("HERO", Hero.MainHero.EncyclopediaLinkWithName);
			return Test.conversation_type == "training_success";
		})
			.Consequence(delegate
			{
				Test.conversation_type = null;
				ChangeRelationAction.ApplyPlayerRelation(Test.followingHero, 10);
				Test.ChangeFactionRelation(Test.followingHero.MapFaction, 300);
				Test.ChangeLordRelation(Test.followingHero, 300);
			})
			.BeginPlayerOptions()
			.PlayerOption(textObject2)
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog();
	}

	private DialogFlow CreateDialog3()
	{
		TextObject textObject = new TextObject("{=FLT0000150}{HERO}I expected better from you! [rf:very_positive_ag, rb:very_positive]");
		TextObject textObject2 = new TextObject("{=FLT0000151}I am sorry my lord.");
		return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject).Condition(delegate
		{
			textObject.SetTextVariable("HERO", Hero.MainHero.EncyclopediaLinkWithName);
			return Test.conversation_type == "training_fail";
		})
			.Consequence(delegate
			{
				Test.conversation_type = null;
				ChangeRelationAction.ApplyPlayerRelation(Test.followingHero, -5);
			})
			.BeginPlayerOptions()
			.PlayerLine(textObject2)
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog();
	}

	private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
		if (Test.followingHero != null && hero == Test.followingHero && Test.followingHero.PartyBelongedTo != null && settlement.MapFaction == hero.MapFaction && settlement.IsTown && !Hero.MainHero.IsWounded && !Test.OngoinEvent && HasRecruits() && !settlement.MapFaction.IsAtWarWith(Test.followingHero.MapFaction.MapFaction) && Test.print(MBRandom.RandomInt(100)) < 10)
		{
			Test.OngoinEvent = true;
			EventSettlement = settlement;
			EnterSettlementAction.ApplyForParty(MobileParty.MainParty, EventSettlement);
			Test.conversation_type = "train_troops";
			Campaign.Current.ConversationManager.AddDialogFlow(CreateDialog());
			CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false, false), new ConversationCharacterData(Test.followingHero.CharacterObject, (PartyBase)null, false, false, false, false, false));
		}
	}

	private bool HasRecruits()
	{
		int index = Test.followingHero.PartyBelongedTo.MemberRoster.FindIndexOfTroop(Test.followingHero.Culture.BasicTroop);
		if (index != -1)
		{
			return Test.followingHero.PartyBelongedTo.MemberRoster.GetElementNumber(index) >= 5;
		}
		return false;
	}

	private DialogFlow CreateDialog()
	{
		TextObject textObject = new TextObject("{=FLT0000152}{HERO} I have a task for you!  I have a bunch of new recruits that won't be of much use in a battle.  I need you to train them and get them caught up to the rest of the army.  You can make use of the sparring equipment at the arena to train. [rf:very_positive_ag, rb:very_positive]");
		TextObject textObject2 = new TextObject("{=FLT0000153}Of course my lord!");
		TextObject textObject3 = new TextObject("{=FLT0000154}I can't turn a bunch of peasants into soldiers overnight, only the trials of many battles can do that.");
		return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject).Condition(delegate
		{
			textObject.SetTextVariable("HERO", Hero.MainHero.EncyclopediaLinkWithName);
			return Test.conversation_type == "train_troops";
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
					TrainingMission();
				});
			})
			.PlayerOption(textObject3)
			.Consequence(delegate
			{
				Test.OngoinEvent = false;
				ChangeRelationAction.ApplyPlayerRelation(Test.followingHero, -1);
			})
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog();
	}

	private void TrainingMission()
	{
		MobileParty.MainParty.IsActive = true;
		Test.disable_XP = true;
		Settlement town = ((Test.followingHero.CurrentSettlement != null) ? Test.followingHero.CurrentSettlement : SettlementHelper.FindNearestTown());
		string scene = Test.followingHero.CurrentSettlement.LocationComplex.GetLocationWithId("arena").GetSceneName(town.Town.GetWallLevel());
		Location location = Test.followingHero.CurrentSettlement.LocationComplex.GetLocationWithId("arena");
		MissionState.OpenNew("ArenaDuelMission", SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, "", doNotUseLoadingScreen: false, DecalAtlasGroup.Town), (Mission mission) => new MissionBehavior[9]
		{
			new MissionOptionsComponent(),
			new CustomArenaTrainingMissionController(Test.followingHero.PartyBelongedTo.MemberRoster, requireCivilianEquipment: false, spawnBothSideWithHorses: false),
			new MissionFacialAnimationHandler(),
			new MissionDebugHandler(),
			new AgentHumanAILogic(),
			new ArenaAgentStateDeciderLogic(),
			new VisualTrackerMissionBehavior(),
			new CampaignMissionComponent(),
			new MissionAgentHandler(location)
		}, addDefaultMissionBehaviors: true, needsMemoryCleanup: false);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
