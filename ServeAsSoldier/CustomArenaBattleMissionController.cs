using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace ServeAsSoldier;

internal class CustomArenaBattleMissionController : MissionLogic
{
	private Agent _playerAgent;

	private List<MatrixFrame> _initialSpawnFrames;

	private static Dictionary<Agent, CharacterObject> dictionary = new Dictionary<Agent, CharacterObject>();

	private TroopRoster _troops;

	private string _training_type;

	private bool _isFastForwarding;

	private int _kill_count = 0;

	public CustomArenaBattleMissionController(TroopRoster memberRoster)
	{
		_troops = TroopRoster.CreateDummyTroopRoster();
		foreach (TroopRosterElement troop in memberRoster.GetTroopRoster())
		{
			if (troop.Number - troop.WoundedNumber > 0)
			{
				_troops.AddToCounts(troop.Character, troop.Number - troop.WoundedNumber);
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
		TrainingType();
	}

	public override bool MissionEnded(ref MissionResult missionResult)
	{
		return false;
	}

	public void TrainingType()
	{
		List<InquiryElement> inquiryElements = new List<InquiryElement>();
		TextObject text2 = new TextObject("{=FLT0000180}Melee");
		TextObject text3 = new TextObject("{=FLT0000181}Bow");
		TextObject text4 = new TextObject("{=FLT0000181}Crossbow");
		TextObject text5 = new TextObject("{=FLT0000182}Cavalry");
		TextObject text6 = new TextObject("{=FLT0000204}Throwing");
		inquiryElements.Add(new InquiryElement("m", text2.ToString(), null, isEnabled: true, null));
		inquiryElements.Add(new InquiryElement("b", text3.ToString(), null, isEnabled: true, null));
		inquiryElements.Add(new InquiryElement("cb", text4.ToString(), null, isEnabled: true, null));
		inquiryElements.Add(new InquiryElement("c", text5.ToString(), null, isEnabled: true, null));
		inquiryElements.Add(new InquiryElement("t", text6.ToString(), null, isEnabled: true, null));
		TextObject text = new TextObject("{=FLT0000183}Select training type");
		TextObject affrimativetext = new TextObject("{=FLT0000077}Continue");
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(text.ToString(), "", inquiryElements, isExitShown: false, 1, affrimativetext.ToString(), null, delegate(List<InquiryElement> args)
		{
			if (args == null || args.Any())
			{
				InformationManager.HideInquiry();
				_training_type = args.Select((InquiryElement element) => element.Identifier as string).First();
				MatrixFrame randomElement = _initialSpawnFrames.GetRandomElement();
				_playerAgent = SpawnAgent(CharacterObject.PlayerCharacter, randomElement, base.Mission.PlayerTeam);
				foreach (TroopRosterElement current in MobileParty.MainParty.MemberRoster.GetTroopRoster())
				{
					if (current.Character.IsHero && !current.Character.HeroObject.IsWounded && current.Character.HeroObject != Hero.MainHero)
					{
						Agent key = SpawnAgent(current.Character, randomElement, base.Mission.PlayerTeam);
						dictionary.Add(key, current.Character);
					}
				}
				if (base.Mission.Teams.Player.ActiveAgents.Count > 1)
				{
					TextObject message = new TextObject("{=FLT0000225}Press U to switch characters");
					MBInformationManager.AddQuickInformation(message);
				}
				TextObject message2 = new TextObject("{=FLT0000227}Press H to toggle auto-train");
				MBInformationManager.AddQuickInformation(message2);
				dictionary.Add(_playerAgent, Hero.MainHero.CharacterObject);
				spawnTroop();
			}
		}, null));
	}

	private void spawnTroop()
	{
		while (_troops.TotalManCount > 0 && (((ReadOnlyCollection<Team>)(object)base.Mission.Teams)[1].ActiveAgents.Count == 0 || ((ReadOnlyCollection<Team>)(object)base.Mission.Teams)[1].ActiveAgents.Count <= base.Mission.Teams.Player.ActiveAgents.Count / 2 + _kill_count / 10))
		{
			TroopRosterElement troop = _troops.GetTroopRoster().GetRandomElement();
			Agent agent = SpawnAgent(troop.Character, _initialSpawnFrames.GetRandomElement(), ((ReadOnlyCollection<Team>)(object)base.Mission.Teams)[1]);
			agent.Defensiveness = 1f;
			dictionary.Add(agent, troop.Character);
			_troops.AddToCounts(troop.Character, -1);
		}
		if (((ReadOnlyCollection<Team>)(object)base.Mission.Teams)[1].ActiveAgents.Count == 0 && _troops.TotalHealthyCount == 0)
		{
			MBInformationManager.AddQuickInformation(new TextObject("{=FLT0000128}The men need time to rest and recover.  There is no one left to fight"), 0, Test.followingHero.CharacterObject);
		}
	}

	private void InitializeMissionTeams()
	{
		base.Mission.Teams.Add(BattleSideEnum.Defender, Hero.MainHero.MapFaction.Color, Hero.MainHero.MapFaction.Color2, Hero.MainHero.ClanBanner);
		base.Mission.Teams.Add(BattleSideEnum.Attacker, uint.MaxValue, uint.MaxValue, Hero.MainHero.ClanBanner);
		base.Mission.PlayerTeam = base.Mission.Teams.Defender;
		((ReadOnlyCollection<Team>)(object)base.Mission.Teams)[0].SetIsEnemyOf(((ReadOnlyCollection<Team>)(object)base.Mission.Teams)[1], isEnemyOf: true);
	}

	private Agent SpawnAgent(CharacterObject character, MatrixFrame spawnFrame, Team team)
	{
		AgentBuildData agentBuildData = new AgentBuildData(character);
		agentBuildData.BodyProperties(character.GetBodyPropertiesMax());
		AgentBuildData agentBuildData2 = agentBuildData.Team(team).InitialPosition(in spawnFrame.origin);
		Vec2 vec = spawnFrame.rotation.f.AsVec2.Normalized();
		Agent agent = Mission.Current.SpawnAgent(agentBuildData2.InitialDirection(in vec).NoHorses(noHorses: false).Equipment(WithSparingWeapons(character.FirstBattleEquipment, character.IsPlayerCharacter))
			.TroopOrigin(new SimpleAgentOrigin(character)), false);
		agent.FadeIn();
		if (character == CharacterObject.PlayerCharacter)
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
			agent.MountAgent.Health = 1E+09f;
		}
		return agent;
	}

	private Equipment WithSparingWeapons(Equipment equipment, bool all)
	{
		Equipment newEquipment = new Equipment();
		newEquipment.FillFrom(equipment);
		if (all)
		{
			if (_training_type == "m")
			{
				newEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_sword_t1"));
				newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_2hsword_t1"));
				newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("practice_spear_t1"));
				newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("bound_horsemans_kite_shield"));
				newEquipment[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(null);
			}
			else if (_training_type == "b")
			{
				newEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("training_bow"));
				newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("blunt_arrows"));
				newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(null);
			}
			else if (_training_type == "cb")
			{
				newEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("crossbow_a"));
				newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("tournament_bolts"));
				newEquipment[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(null);
			}
			else if (_training_type == "c")
			{
				newEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_sword_t1"));
				newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("practice_spear_t1"));
				newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("bound_horsemans_kite_shield"));
				newEquipment[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("noble_horse"));
				newEquipment[EquipmentIndex.HorseHarness] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("saddle_of_aeneas"));
			}
			else if (_training_type == "t")
			{
				newEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("western_javelin_1_t2_blunt"));
				newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(null);
			}
		}
		else if (_training_type == "m")
		{
			switch (MBRandom.RandomInt(6))
			{
			case 0:
				newEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_sword_t1"));
				newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("bound_horsemans_kite_shield"));
				newEquipment[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(null);
				break;
			case 1:
				newEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_2hsword_t1"));
				newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(null);
				break;
			case 2:
				newEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("practice_spear_t1"));
				newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(null);
				break;
			case 3:
				newEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("western_javelin_1_t2_blunt"));
				newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("western_javelin_1_t2_blunt"));
				newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_sword_t1"));
				newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(null);
				break;
			case 4:
				newEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("peasant_maul_t1_2"));
				newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(null);
				break;
			default:
				newEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("practice_spear_t1"));
				newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("bound_horsemans_kite_shield"));
				newEquipment[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(null);
				break;
			}
		}
		else if (_training_type == "b")
		{
			if (MBRandom.RandomInt(2) == 0)
			{
				newEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("training_bow"));
				newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("blunt_arrows"));
				newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("blunt_arrows"));
				newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("blunt_arrows"));
				newEquipment[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(null);
			}
			else
			{
				newEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("training_longbow"));
				newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("blunt_arrows"));
				newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("blunt_arrows"));
				newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("blunt_arrows"));
				newEquipment[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(null);
			}
		}
		else if (_training_type == "cb")
		{
			newEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("crossbow_a"));
			newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("tournament_bolts"));
			newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("tournament_bolts"));
			newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("tournament_bolts"));
			newEquipment[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(null);
		}
		else if (_training_type == "c")
		{
			switch (MBRandom.RandomInt(4))
			{
			case 0:
				newEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("practice_spear_t1"));
				newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("bound_horsemans_kite_shield"));
				newEquipment[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("noble_horse"));
				newEquipment[EquipmentIndex.HorseHarness] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("saddle_of_aeneas"));
				break;
			case 1:
				newEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("practice_spear_t1"));
				newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("noble_horse"));
				newEquipment[EquipmentIndex.HorseHarness] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("saddle_of_aeneas"));
				break;
			case 2:
				newEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_sword_t1"));
				newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("western_javelin_1_t2_blunt"));
				newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("western_javelin_1_t2_blunt"));
				newEquipment[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("noble_horse"));
				newEquipment[EquipmentIndex.HorseHarness] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("saddle_of_aeneas"));
				break;
			default:
				newEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_sword_t1"));
				newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(null);
				newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("bound_horsemans_kite_shield"));
				newEquipment[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("noble_horse"));
				newEquipment[EquipmentIndex.HorseHarness] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("saddle_of_aeneas"));
				break;
			}
		}
		else if (_training_type == "t")
		{
			newEquipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("western_javelin_1_t2_blunt"));
			newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("western_javelin_1_t2_blunt"));
			newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("western_javelin_1_t2_blunt"));
			newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("western_javelin_1_t2_blunt"));
			newEquipment[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(null);
		}
		return newEquipment;
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
		if (HitterCharacter != null && HitterCharacter.IsHero && attackerWeapon != null)
		{
			if (MBRandom.RandomInt(250) == 0)
			{
				if (HitterCharacter.HeroObject.HeroDeveloper.GetFocus(attackerWeapon.RelevantSkill) < 5)
				{
					HitterCharacter.HeroObject.HeroDeveloper.AddFocus(attackerWeapon.RelevantSkill, 1, checkUnspentFocusPoints: false);
					TextObject text4 = new TextObject("{=FLT0000217}One focus point gained in {SKILL} from training.");
					text4.SetTextVariable("SKILL", attackerWeapon.RelevantSkill.Name.ToString());
					MBInformationManager.AddQuickInformation(text4, 0, HitterCharacter);
					SoundEvent.PlaySound2D(SoundEvent.GetEventIdFromString("event:/ui/notification/levelup"));
				}
				else
				{
					HitterCharacter.HeroObject.HeroDeveloper.AddAttribute(attackerWeapon.RelevantSkill.CharacterAttribute, 1, checkUnspentPoints: false);
					TextObject text3 = new TextObject("{=FLT0000218}One attribute point gained in {ATTRIBUTE} from training.");
					text3.SetTextVariable("ATTRIBUTE", attackerWeapon.RelevantSkill.CharacterAttribute.Name.ToString());
					MBInformationManager.AddQuickInformation(text3, 0, HitterCharacter);
					SoundEvent.PlaySound2D(SoundEvent.GetEventIdFromString("event:/ui/notification/levelup"));
				}
			}
			else if (MBRandom.RandomInt(500) == 0)
			{
				SkillObject skill = (affectorAgent.HasMount ? DefaultSkills.Riding : DefaultSkills.Athletics);
				if (HitterCharacter.HeroObject.HeroDeveloper.GetFocus(skill) < 5)
				{
					HitterCharacter.HeroObject.HeroDeveloper.AddFocus(skill, 1, checkUnspentFocusPoints: false);
					if (HitterCharacter.HeroObject.Clan == Clan.PlayerClan)
					{
						TextObject text2 = new TextObject("{=FLT0000217}One focus point gained in {SKILL} from training");
						text2.SetTextVariable("SKILL", skill.Name.ToString());
						MBInformationManager.AddQuickInformation(text2, 0, HitterCharacter);
						SoundEvent.PlaySound2D(SoundEvent.GetEventIdFromString("event:/ui/notification/levelup"));
					}
				}
				else
				{
					HitterCharacter.HeroObject.HeroDeveloper.AddAttribute(skill.CharacterAttribute, 1, checkUnspentPoints: false);
					if (HitterCharacter.HeroObject.Clan == Clan.PlayerClan)
					{
						TextObject text = new TextObject("{=FLT0000218}One attribute point gained in {ATTRIBUTE} from training");
						text.SetTextVariable("ATTRIBUTE", skill.CharacterAttribute.Name.ToString());
						MBInformationManager.AddQuickInformation(text, 0, HitterCharacter);
						SoundEvent.PlaySound2D(SoundEvent.GetEventIdFromString("event:/ui/notification/levelup"));
					}
				}
			}
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

	public override void OnMissionTick(float dt)
	{
		if (Input.IsKeyPressed(InputKey.U) && ((!_playerAgent.IsActive() && base.Mission.Teams.Player.ActiveAgents.Count > 0) || base.Mission.Teams.Player.ActiveAgents.Count > 1))
		{
			switchAgent();
		}
		if (Input.IsKeyPressed(InputKey.H) && base.Mission.Teams.Player.ActiveAgents.Count > 0 && ((ReadOnlyCollection<Team>)(object)base.Mission.Teams)[1].ActiveAgents.Count > 0)
		{
			AutoTrain();
		}
		if (!(_training_type == "t") && !(_training_type == "b") && !(_training_type == "cb"))
		{
			return;
		}
		foreach (Agent agent in base.Mission.Agents)
		{
			for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
			{
				MissionWeapon missionWeapon = agent.Equipment[equipmentIndex];
				if (!missionWeapon.IsEmpty && (missionWeapon.Item.PrimaryWeapon.WeaponClass == WeaponClass.Javelin || missionWeapon.Item.PrimaryWeapon.WeaponClass == WeaponClass.Bolt || missionWeapon.Item.PrimaryWeapon.WeaponClass == WeaponClass.Arrow))
				{
					agent.SetWeaponAmountInSlot(equipmentIndex, agent.Equipment[equipmentIndex].ModifiedMaxAmount, enforcePrimaryItem: true);
				}
			}
		}
	}

	private void AutoTrain()
	{
		if (_isFastForwarding)
		{
			if (base.Mission.Teams.Player.ActiveAgents.Count > 0)
			{
				_playerAgent = ((IReadOnlyList<Agent>)base.Mission.Teams.Player.ActiveAgents).GetRandomElement();
				_playerAgent.Controller = Agent.ControllerType.Player;
			}
			Mission.Current.SetFastForwardingFromUI(fastForwarding: false);
			_isFastForwarding = false;
		}
		else
		{
			_playerAgent.Controller = Agent.ControllerType.AI;
			_playerAgent.SetWatchState(Agent.WatchState.Alarmed);
			Mission.Current.SetFastForwardingFromUI(fastForwarding: true);
			_isFastForwarding = true;
		}
	}

	private void switchAgent()
	{
		int index = 0;
		Agent[] agents = Enumerable.ToArray(base.Mission.Teams.Player.ActiveAgents);
		if (agents.Length <= 1)
		{
			return;
		}
		for (int i = 0; i < agents.Length; i++)
		{
			if (agents[i] == _playerAgent)
			{
				index = i;
			}
		}
		Agent next = agents[(index != agents.Length - 1) ? (index + 1) : 0];
		_playerAgent.Controller = Agent.ControllerType.AI;
		_playerAgent.SetWatchState(Agent.WatchState.Alarmed);
		next.Controller = Agent.ControllerType.Player;
		Mission.Current.MainAgent = next;
		_playerAgent = next;
		TextObject text226 = new TextObject("{=FLT0000226}Playing as {CHARACTER}");
		text226.SetTextVariable("CHARACTER", next.Character.GetName());
		InformationManager.DisplayMessage(new InformationMessage(text226.ToString()));
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
			SkillLevelingManager.OnCombatHit(character2, character1, null, null, lastSpeedBonus, lastShotDifficulty, attackerWeapon, hitpointRatio, CombatXpModel.MissionTypeEnum.Battle, affectorAgent.MountAgent != null, affectorAgent.Team == affectedAgent.Team, isAffectorUnderCommand: false, damageAmount, affectedAgent.Health < 1f || MBRandom.RandomInt(100) > (Hero.MainHero.GetPerkValue(DefaultPerks.OneHanded.Trainer) ? 33 : 67), isSiegeEngineHit: false, affectorAgent.MountAgent != null && attackType == AgentAttackType.Collision);
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		if (dictionary.TryGetValue(affectedAgent, out var killedCharacter))
		{
			if (killedCharacter.IsHero)
			{
				if (killedCharacter == CharacterObject.PlayerCharacter)
				{
					MBInformationManager.AddQuickInformation(new TextObject("{=FLT0000129}I should stop before I seriously injury myself"), 0, CharacterObject.PlayerCharacter);
					return;
				}
				if (!_isFastForwarding)
				{
					MBInformationManager.AddQuickInformation(getDeafeatedMessage(), 0, killedCharacter);
				}
				int index = killedCharacter.HeroObject.PartyBelongedTo.MemberRoster.FindIndexOfTroop(killedCharacter);
				killedCharacter.HeroObject.PartyBelongedTo.MemberRoster.GetCharacterAtIndex(index).HeroObject.HitPoints = 0;
			}
			else
			{
				if (!_isFastForwarding)
				{
					MBInformationManager.AddQuickInformation(getDeafeatedMessage(), 0, killedCharacter);
				}
				Test.followingHero.PartyBelongedTo.MemberRoster.WoundTroop(killedCharacter);
			}
			TextObject text = new TextObject("{=FLT0000228}Opponets defeated : {KILLS}");
			text.SetTextVariable("KILLS", (_kill_count + 1).ToString());
			MBInformationManager.AddQuickInformation(new TextObject(text.ToString()));
		}
		if (dictionary.TryGetValue(affectorAgent, out var killerCharacter))
		{
			int index2 = Test.followingHero.PartyBelongedTo.MemberRoster.FindIndexOfTroop(killerCharacter);
			if (index2 != -1)
			{
				Test.followingHero.PartyBelongedTo.MemberRoster.SetElementXp(index2, Test.followingHero.PartyBelongedTo.MemberRoster.GetElementXp(index2) + 100);
			}
		}
		if (affectedAgent.Team != _playerAgent.Team)
		{
			if (Hero.MainHero.GetPerkValue(DefaultPerks.Throwing.ThrowingCompetitions) && (killingBlow.WeaponClass == 19 || killingBlow.WeaponClass == 21 || killingBlow.WeaponClass == 20))
			{
				Clan.PlayerClan.AddRenown(0.2f);
			}
			if (Hero.MainHero.GetPerkValue(DefaultPerks.TwoHanded.ShowOfStrength))
			{
				Clan.PlayerClan.AddRenown(0.1f);
			}
			_kill_count++;
			spawnTroop();
		}
	}

	private TextObject getDeafeatedMessage()
	{
		List<TextObject> message = new List<TextObject>();
		message.Add(new TextObject("{=FLT0000130}You win, I give up!"));
		message.Add(new TextObject("{=FLT0000131}Ouch that really hurt, I'm done!"));
		message.Add(new TextObject("{=FLT0000132}You just got lucky, I will beat you next time!"));
		message.Add(new TextObject("{=FLT0000133}I am going to be sore in the morning!"));
		message.Add(new TextObject("{=FLT0000134}Well fought, friend!"));
		message.Add(new TextObject("{=FLT0000135}I shouldn't have agreed to this!"));
		message.Add(new TextObject("{=FLT0000136}Ahhh! I think that broke a bone!"));
		message.Add(new TextObject("{=FLT0000137}I didn't expect training weapons to hurt this much!"));
		message.Add(new TextObject("{=FLT0000138}No fair, you cheated!"));
		message.Add(new TextObject("{=FLT0000139}I yield!"));
		SoundEvent.PlaySound2D(SoundEvent.GetEventIdFromString("event:/mission/ambient/detail/arena/cheer_big"));
		return message.GetRandomElement();
	}

	public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
	{
		canPlayerLeave = true;
		return new InquiryData(new TextObject("{=FLT0000267}Train with troops").ToString(), new TextObject("{=FLT0000268}Do you want to end your training?").ToString(), true, true, GameTexts.FindText("str_ok").ToString(), GameTexts.FindText("str_cancel").ToString(), (Action)Mission.Current.EndMission, (Action)null, "", 0f, (Action)null);
	}
}
