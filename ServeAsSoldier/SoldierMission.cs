using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace ServeAsSoldier;

internal class SoldierMission : MissionBehavior
{
	public static List<Agent> allies = new List<Agent>();

	public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

	public override void OnRenderingStarted()
	{
	}

	public override void AfterStart()
	{
	}

	public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
	{
		if (Test.followingHero == null || Test.disable_XP || !affectorAgent.IsPlayerControlled || affectedAgent.Character == null || !isenemey(affectedAgent, affectorAgent))
		{
			return;
		}
		float damage = blow.InflictedDamage;
		if (damage > affectedAgent.HealthLimit)
		{
			damage = affectedAgent.HealthLimit;
		}
		if (Hero.MainHero.GetPerkValue(DefaultPerks.Bow.BullsEye) && blow.VictimBodyPart == BoneBodyPartType.Head)
		{
			SkillLevelingManager.OnCombatHit((CharacterObject)affectorAgent.Character, (CharacterObject)affectedAgent.Character, null, null, blow.MovementSpeedDamageModifier, shotDifficulty, attackerWeapon, 0.5f * damage / affectedAgent.HealthLimit, CombatXpModel.MissionTypeEnum.Battle, affectorAgent.MountAgent != null, affectorAgent.Team == affectedAgent.Team, isAffectorUnderCommand: false, damage, isFatal: true, isSiegeEngineHit: false, affectorAgent.MountAgent != null && blow.AttackType == AgentAttackType.Collision);
		}
		if (attackerWeapon != null && attackerWeapon.IsRangedWeapon && attackerWeapon.WeaponClass == WeaponClass.Crossbow)
		{
			if (Hero.MainHero.GetPerkValue(DefaultPerks.Crossbow.Sniper) && hitDistance > 50f)
			{
				SkillLevelingManager.OnCombatHit((CharacterObject)affectorAgent.Character, (CharacterObject)affectedAgent.Character, null, null, blow.MovementSpeedDamageModifier, shotDifficulty, attackerWeapon, 0.5f * damage / affectedAgent.HealthLimit, CombatXpModel.MissionTypeEnum.Battle, affectorAgent.MountAgent != null, affectorAgent.Team == affectedAgent.Team, isAffectorUnderCommand: false, damage, isFatal: true, isSiegeEngineHit: false, affectorAgent.MountAgent != null && blow.AttackType == AgentAttackType.Collision);
			}
			if (Hero.MainHero.GetPerkValue(DefaultPerks.Crossbow.Steady) && affectedAgent.HasMount)
			{
				SkillLevelingManager.OnCombatHit((CharacterObject)affectorAgent.Character, (CharacterObject)affectedAgent.Character, null, null, blow.MovementSpeedDamageModifier, shotDifficulty, attackerWeapon, 0.5f * damage / affectedAgent.HealthLimit, CombatXpModel.MissionTypeEnum.Battle, affectorAgent.MountAgent != null, affectorAgent.Team == affectedAgent.Team, isAffectorUnderCommand: false, damage, isFatal: true, isSiegeEngineHit: false, affectorAgent.MountAgent != null && blow.AttackType == AgentAttackType.Collision);
			}
		}
		if (affectedAgent.Character.IsHero)
		{
			Test.xp += 25;
			Test.ChangeFactionRelation(Test.followingHero.MapFaction, 25);
			Test.ChangeLordRelation(Test.followingHero, 25);
		}
		else
		{
			int xpgain = affectedAgent.Character.Level / 5 + 4;
			Test.xp += xpgain;
			Test.ChangeFactionRelation(Test.followingHero.MapFaction, xpgain);
			Test.ChangeLordRelation(Test.followingHero, xpgain);
		}
	}

	private bool isenemey(Agent affectedAgent, Agent affectorAgent)
	{
		if (affectedAgent.Team == null || affectorAgent.Team == null)
		{
			return false;
		}
		return affectedAgent.Team.IsEnemyOf(affectorAgent.Team);
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		if (affectedAgent == null || affectorAgent == null || affectedAgent.Character == null || Test.followingHero == null || Test.disable_XP || !affectorAgent.IsPlayerControlled || !isenemey(affectedAgent, affectorAgent))
		{
			return;
		}
		if (Hero.MainHero.GetPerkValue(DefaultPerks.TwoHanded.BaptisedInBlood))
		{
			Clan.PlayerClan.AddRenown(0.3f);
		}
		if (Hero.MainHero.GetPerkValue(DefaultPerks.Polearm.Phalanx) && !affectedAgent.HasMount && !killingBlow.IsMissile)
		{
			Clan.PlayerClan.AddRenown(0.3f);
		}
		if (Hero.MainHero.GetPerkValue(DefaultPerks.Bow.RenownedArcher) && killingBlow.IsMissile)
		{
			Clan.PlayerClan.AddRenown(0.3f);
		}
		if (affectedAgent.Character.IsHero)
		{
			if (Hero.MainHero.GetPerkValue(DefaultPerks.Throwing.HeadHunter))
			{
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 1000);
			}
			Test.xp += 100;
			Test.ChangeFactionRelation(Test.followingHero.MapFaction, 100);
			Test.ChangeLordRelation(Test.followingHero, 100);
		}
		else
		{
			int xpgain = 4 * (affectedAgent.Character.Level / 5 + 4);
			Test.xp += xpgain;
			Test.ChangeFactionRelation(Test.followingHero.MapFaction, xpgain);
			Test.ChangeLordRelation(Test.followingHero, xpgain);
		}
	}
}
