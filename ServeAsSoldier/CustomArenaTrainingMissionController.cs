using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace ServeAsSoldier;

internal class CustomArenaTrainingMissionController : MissionLogic
{
	private bool _requireCivilianEquipment;

	private bool _spawnBothSideWithHorses;

	private List<MatrixFrame> _initialSpawnFrames;

	private static Dictionary<Agent, CharacterObject> dictionary = new Dictionary<Agent, CharacterObject>();

	private TroopRoster _troops;

	private BasicMissionTimer _duelTimer;

	private int _xp = 0;

	private int _recruits_left = 5;

	private bool spawned = false;

	public CustomArenaTrainingMissionController(TroopRoster memberRoster, bool requireCivilianEquipment, bool spawnBothSideWithHorses)
	{
		_requireCivilianEquipment = requireCivilianEquipment;
		_spawnBothSideWithHorses = spawnBothSideWithHorses;
		_troops = TroopRoster.CreateDummyTroopRoster();
		foreach (TroopRosterElement troop in memberRoster.GetTroopRoster())
		{
			if (!troop.Character.IsHero)
			{
				_troops.AddToCounts(troop.Character, troop.Number);
			}
		}
	}

	public override void AfterStart()
	{
		Mission.Current.SetMissionMode(MissionMode.Battle, atStart: true);
		InitializeMissionTeams();
		_initialSpawnFrames = (from e in base.Mission.Scene.FindEntitiesWithTag("sp_arena")
			select e.GetGlobalFrame()).ToList();
		for (int index = 0; index < _initialSpawnFrames.Count; index++)
		{
			MatrixFrame initialSpawnFrame = _initialSpawnFrames[index];
			initialSpawnFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			_initialSpawnFrames[index] = initialSpawnFrame;
		}
		MBInformationManager.AddQuickInformation(new TextObject("{=FLT0000121}I'm here to teach you miserable maggots how to fight, so you better listen up and do everything I say!"), 0, CharacterObject.PlayerCharacter);
		MBInformationManager.AddQuickInformation(new TextObject("{=FLT0000122}Control the recruits and gain enough xp through dueling to be able to upgrade them and complete the mission."));
		MBInformationManager.AddQuickInformation(new TextObject("{=FLT0000123}You will fail the mission if all recruits get knocked out before getting enough xp."));
		_duelTimer = new BasicMissionTimer();
	}

	public override bool MissionEnded(ref MissionResult missionResult)
	{
		return false;
	}

	private void spawnTroop(bool PlayerControled)
	{
		if (PlayerControled && _recruits_left == 0)
		{
			_recruits_left--;
			return;
		}
		TroopRosterElement troop = _troops.GetTroopRoster().GetRandomElement();
		Agent agent = SpawnAgent(PlayerControled ? Test.followingHero.Culture.BasicTroop : troop.Character, _initialSpawnFrames.GetRandomElement(), PlayerControled ? ((ReadOnlyCollection<Team>)(object)base.Mission.Teams)[0] : ((ReadOnlyCollection<Team>)(object)base.Mission.Teams)[1]);
		agent.Defensiveness = 1f;
		dictionary.Add(agent, troop.Character);
		if (PlayerControled)
		{
			TextObject text = new TextObject("{=FLT0000124}Recruits left");
			InformationManager.DisplayMessage(new InformationMessage(text.ToString() + " : " + _recruits_left));
			_recruits_left--;
		}
	}

	private void InitializeMissionTeams()
	{
		base.Mission.Teams.Add(BattleSideEnum.Defender, Hero.MainHero.MapFaction.Color, Hero.MainHero.MapFaction.Color2, Hero.MainHero.ClanBanner);
		base.Mission.Teams.Add(BattleSideEnum.Attacker, uint.MaxValue, uint.MaxValue, Hero.MainHero.ClanBanner);
		base.Mission.PlayerTeam = base.Mission.Teams.Defender;
		((ReadOnlyCollection<Team>)(object)base.Mission.Teams)[0].SetIsEnemyOf(((ReadOnlyCollection<Team>)(object)base.Mission.Teams)[1], isEnemyOf: true);
	}

	private void DeactivateOtherTournamentSets()
	{
		TournamentBehavior.DeleteTournamentSetsExcept(base.Mission.Scene.FindEntityWithTag("tournament_fight"));
	}

	private Agent SpawnAgent(CharacterObject character, MatrixFrame spawnFrame, Team team)
	{
		AgentBuildData agentBuildData = new AgentBuildData(character);
		agentBuildData.BodyProperties(character.GetBodyPropertiesMax());
		Mission mission = base.Mission;
		AgentBuildData agentBuildData2 = agentBuildData.Team(team).InitialPosition(in spawnFrame.origin);
		Vec2 vec = spawnFrame.rotation.f.AsVec2.Normalized();
		Agent agent = mission.SpawnAgent(agentBuildData2.InitialDirection(in vec).NoHorses(!_spawnBothSideWithHorses).Equipment(_requireCivilianEquipment ? WithSparingWeapons(character.FirstCivilianEquipment, character.IsHero) : WithSparingWeapons(character.FirstBattleEquipment, character.IsHero))
			.TroopOrigin(new SimpleAgentOrigin(character)), false);
		agent.FadeIn();
		if (team == ((ReadOnlyCollection<Team>)(object)base.Mission.Teams)[0])
		{
			agent.Controller = Agent.ControllerType.Player;
		}
		if (agent.IsAIControlled)
		{
			agent.SetWatchState(Agent.WatchState.Alarmed);
		}
		agent.Health = character.HitPoints;
		agent.BaseHealthLimit = character.MaxHitPoints();
		agent.HealthLimit = character.MaxHitPoints();
		if (agent.MountAgent != null)
		{
			agent.MountAgent.Health = character.Equipment.Horse.GetModifiedMountHitPoints();
		}
		return agent;
	}

	private Equipment WithSparingWeapons(Equipment equipment, bool all)
	{
		Equipment newEquipment = new Equipment();
		newEquipment.FillFrom(equipment);
		if (all)
		{
			newEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_sword_t1"));
			newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_2hsword_t1"));
			newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("practice_spear_t1"));
			newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("bound_horsemans_kite_shield"));
		}
		else
		{
			switch (MBRandom.RandomInt(8))
			{
			case 0:
				newEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_sword_t1"));
				newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("bound_horsemans_kite_shield"));
				break;
			case 1:
				newEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_2hsword_t1"));
				newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(null);
				break;
			case 2:
				newEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("practice_spear_t1"));
				newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(null);
				break;
			case 3:
				newEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("training_bow"));
				newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("blunt_arrows"));
				newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_sword_t1"));
				newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(null);
				break;
			case 4:
				newEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("training_longbow"));
				newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("blunt_arrows"));
				newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_sword_t1"));
				newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(null);
				break;
			case 5:
				newEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("western_javelin_1_t2_blunt"));
				newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("western_javelin_1_t2_blunt"));
				newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_sword_t1"));
				newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(null);
				break;
			case 6:
				newEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("peasant_maul_t1_2"));
				newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(null);
				break;
			default:
				newEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("practice_spear_t1"));
				newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("bound_horsemans_kite_shield"));
				break;
			}
		}
		return newEquipment;
	}

	public override void OnMissionTick(float dt)
	{
		if (_duelTimer.ElapsedTime > 13f && !spawned)
		{
			spawnTroop(PlayerControled: true);
			spawnTroop(PlayerControled: false);
			spawned = true;
		}
		if (TrainingDone())
		{
			MBInformationManager.AddQuickInformation(new TextObject("{=FLT0000125}Training Completed"));
		}
	}

	public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
	{
		if (affectorAgent == null || affectorAgent.Character == null || affectedAgent.Character == null)
		{
			return;
		}
		if (dictionary.TryGetValue(affectorAgent, out var HitterCharacter))
		{
			int index = Test.followingHero.PartyBelongedTo.MemberRoster.FindIndexOfTroop(HitterCharacter);
			if (index != -1)
			{
				Test.followingHero.PartyBelongedTo.MemberRoster.SetElementXp(index, Test.followingHero.PartyBelongedTo.MemberRoster.GetElementXp(index) + 50);
			}
		}
		if (dictionary.TryGetValue(affectedAgent, out var HitCharacter) && HitCharacter.IsHero)
		{
			HitCharacter.HeroObject.HitPoints = (int)affectedAgent.Health;
		}
		if (affectorAgent.Team == ((ReadOnlyCollection<Team>)(object)base.Mission.Teams)[0])
		{
			_xp += 10;
			Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 20 + Hero.MainHero.GetSkillValue(DefaultSkills.Leadership));
			TextObject text = new TextObject("{=FLT0000126}Training progress");
			InformationManager.DisplayMessage(new InformationMessage(text.ToString() + " : " + _xp + "/1500"));
		}
		float damage = blow.InflictedDamage;
		if (damage > affectedAgent.HealthLimit)
		{
			damage = affectedAgent.HealthLimit;
		}
		if ((double)damage > (double)affectedAgent.HealthLimit)
		{
			damage = affectedAgent.HealthLimit;
		}
		float num = damage / affectedAgent.HealthLimit;
		EnemyHitReward(affectedAgent, affectorAgent, blow.MovementSpeedDamageModifier, shotDifficulty, attackerWeapon, blow.AttackType, 0.5f * num, damage);
	}

	private void EnemyHitReward(Agent affectedAgent, Agent affectorAgent, float lastSpeedBonus, float lastShotDifficulty, WeaponComponentData attackerWeapon, AgentAttackType attackType, float hitpointRatio, float damageAmount)
	{
		if (dictionary.TryGetValue(affectorAgent, out var HitterCharacter))
		{
			int index = Test.followingHero.PartyBelongedTo.MemberRoster.FindIndexOfTroop(HitterCharacter);
			if (index != -1)
			{
				Test.followingHero.PartyBelongedTo.MemberRoster.SetElementXp(index, Test.followingHero.PartyBelongedTo.MemberRoster.GetElementXp(index) + 50);
			}
		}
		CharacterObject character1 = (CharacterObject)affectedAgent.Character;
		CharacterObject character2 = (CharacterObject)affectorAgent.Character;
		if (affectedAgent.Origin != null && affectorAgent != null && affectorAgent.Origin != null)
		{
			SkillLevelingManager.OnCombatHit(character2, character1, null, null, lastSpeedBonus, lastShotDifficulty, attackerWeapon, hitpointRatio, CombatXpModel.MissionTypeEnum.Battle, affectorAgent.MountAgent != null, affectorAgent.Team == affectedAgent.Team, isAffectorUnderCommand: false, damageAmount, affectedAgent.Health < 1f, isSiegeEngineHit: false, affectorAgent.MountAgent != null && attackType == AgentAttackType.Collision);
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		if (dictionary.TryGetValue(affectedAgent, out var killedCharacter))
		{
			if (killedCharacter.IsHero)
			{
				int index = Test.followingHero.PartyBelongedTo.MemberRoster.FindIndexOfTroop(killedCharacter);
				Test.followingHero.PartyBelongedTo.MemberRoster.GetCharacterAtIndex(index).HeroObject.HitPoints = 0;
			}
			else
			{
				Test.followingHero.PartyBelongedTo.MemberRoster.WoundTroop(killedCharacter);
			}
		}
		if (affectorAgent.Team == ((ReadOnlyCollection<Team>)(object)base.Mission.Teams)[0])
		{
			Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 50 + 3 * Hero.MainHero.GetSkillValue(DefaultSkills.Leadership));
			_xp += 40;
			TextObject text = new TextObject("{=FLT0000126}Training progress");
			InformationManager.DisplayMessage(new InformationMessage(text.ToString() + " : " + _xp + "/1500"));
		}
		if (dictionary.TryGetValue(affectorAgent, out var killerCharacter))
		{
			int index2 = Test.followingHero.PartyBelongedTo.MemberRoster.FindIndexOfTroop(killerCharacter);
			if (index2 != -1)
			{
				Test.followingHero.PartyBelongedTo.MemberRoster.SetElementXp(index2, Test.followingHero.PartyBelongedTo.MemberRoster.GetElementXp(index2) + 100);
			}
		}
		if (!TrainingDone())
		{
			spawnTroop(affectedAgent.Team == ((ReadOnlyCollection<Team>)(object)base.Mission.Teams)[0]);
		}
	}

	private bool TrainingDone()
	{
		return _xp >= 1500;
	}

	public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
	{
		canPlayerLeave = true;
		TrainTroopsEvent.trainingDone = true;
		TrainTroopsEvent.success = TrainingDone();
		return new InquiryData(new TextObject("{=FLT0000269}Train recruits mission").ToString(), (TrainingDone() || _recruits_left < 0) ? new TextObject("{=FLT0000270}Leave Arena?").ToString() : new TextObject("{=FLT0000271}Leaving before training is complete will cause you fail the mission.\nLeave Arena?").ToString(), true, true, GameTexts.FindText("str_ok").ToString(), GameTexts.FindText("str_cancel").ToString(), (Action)Mission.Current.EndMission, (Action)null, "", 0f, (Action)null);
	}
}
