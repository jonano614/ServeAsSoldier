using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace ServeAsSoldier;

internal class model : DefaultTargetScoreCalculatingModel
{
	private float ReasonableDistanceForDefendingVillage => (80f + 1.42f * Campaign.AverageDistanceBetweenTwoFortifications) / 2f;

	private float ReasonableDistanceForDefendingTownOrCastle => (160f + 2.84f * Campaign.AverageDistanceBetweenTwoFortifications) / 2f;

	private float ReasonableDistanceForBesiegingTown => (127f + 2.27f * Campaign.AverageDistanceBetweenTwoFortifications) / 2f;

	private float ReasonableDistanceForBesiegingCastle => (106f + 1.89f * Campaign.AverageDistanceBetweenTwoFortifications) / 2f;

	private float GiveUpDistanceLimit => (127f + 2.27f * Campaign.AverageDistanceBetweenTwoFortifications) / 2f;

	private float RaidDistanceLimit => (318f + 5.68f * Campaign.AverageDistanceBetweenTwoFortifications) / 2f;

	private float DistanceOfMobilePartyDivider => (254f + 4.54f * Campaign.AverageDistanceBetweenTwoFortifications) / 2f;

	private float ReasonableDistanceForRaiding => (106f + 1.89f * Campaign.AverageDistanceBetweenTwoFortifications) / 2f;

	public override float GetTargetScoreForFaction(Settlement targetSettlement, Army.ArmyTypes missionType, MobileParty mobileParty, float ourStrength, int numberOfEnemyFactionSettlements = -1, float totalEnemyMobilePartyStrength = -1f)
	{
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		return GetTargetScoreForFaction(targetSettlement, missionType, mobileParty, ourStrength, out num, out num2, out num3, numberOfEnemyFactionSettlements, totalEnemyMobilePartyStrength);
	}

	private float GetTargetScoreForFaction(Settlement targetSettlement, Army.ArmyTypes missionType, MobileParty mobileParty, float ourStrength, out float powerScore, out float distanceScore, out float settlementImportanceScore, int numberOfEnemyFactionSettlements = -1, float totalEnemyMobilePartyStrength = -1f)
	{
		IFaction mapFaction = mobileParty.MapFaction;
		if (((missionType == Army.ArmyTypes.Besieger || missionType == Army.ArmyTypes.Raider) && !FactionManager.IsAtWarAgainstFaction(targetSettlement.MapFaction, mapFaction)) || (missionType == Army.ArmyTypes.Raider && (targetSettlement.Village.VillageState != 0 || targetSettlement.Party.MapEvent != null) && (mobileParty.MapEvent == null || mobileParty.MapEvent.MapEventSettlement != targetSettlement)) || (missionType == Army.ArmyTypes.Besieger && (targetSettlement.Party.MapEvent != null || targetSettlement.SiegeEvent != null) && (targetSettlement.SiegeEvent == null || targetSettlement.SiegeEvent.BesiegerCamp.BesiegerParty.MapFaction != mobileParty.MapFaction) && (mobileParty.MapEvent == null || mobileParty.MapEvent.MapEventSettlement != targetSettlement)) || (missionType == Army.ArmyTypes.Defender && (targetSettlement.LastAttackerParty == null || !targetSettlement.LastAttackerParty.IsActive || targetSettlement.LastAttackerParty.MapFaction == mobileParty.MapFaction || targetSettlement.MapFaction != mobileParty.MapFaction)))
		{
			powerScore = 0f;
			distanceScore = 0f;
			settlementImportanceScore = 0f;
			return 0f;
		}
		if (mobileParty.Objective == MobileParty.PartyObjective.Defensive && (missionType == Army.ArmyTypes.Besieger || missionType == Army.ArmyTypes.Raider))
		{
			powerScore = 0f;
			distanceScore = 0f;
			settlementImportanceScore = 0f;
			return 0f;
		}
		if (mobileParty.Objective == MobileParty.PartyObjective.Aggressive && (missionType == Army.ArmyTypes.Defender || missionType == Army.ArmyTypes.Patrolling))
		{
			powerScore = 0f;
			distanceScore = 0f;
			settlementImportanceScore = 0f;
			return 0f;
		}
		if (missionType == Army.ArmyTypes.Defender)
		{
			MobileParty lastAttackerParty = targetSettlement.LastAttackerParty;
			if (lastAttackerParty == null || !mobileParty.MapFaction.IsAtWarWith(lastAttackerParty.MapFaction))
			{
				powerScore = 0f;
				distanceScore = 0f;
				settlementImportanceScore = 0f;
				return 0f;
			}
		}
		if (mobileParty.Army == null && missionType == Army.ArmyTypes.Besieger && ((targetSettlement.Party.MapEvent != null && targetSettlement.Party.MapEvent.AttackerSide.LeaderParty != mobileParty.Party) || (targetSettlement.Party.SiegeEvent != null && mobileParty.BesiegedSettlement != targetSettlement)))
		{
			powerScore = 0f;
			distanceScore = 0f;
			settlementImportanceScore = 0f;
			return 0f;
		}
		float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(mapFaction.FactionMidSettlement, targetSettlement);
		float distance2 = Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty, targetSettlement);
		float num = Campaign.MapDiagonalSquared;
		float num12 = Campaign.MapDiagonalSquared;
		int num23 = 0;
		int num34 = 0;
		Settlement settlement = null;
		Settlement settlement2 = null;
		foreach (Settlement settlement3 in mobileParty.MapFaction.Settlements)
		{
			if (settlement3.IsTown)
			{
				float num42 = settlement3.Position2D.DistanceSquared(targetSettlement.Position2D);
				if (num > num42)
				{
					num = num42;
					settlement = settlement3;
				}
				if (num12 > num42)
				{
					num12 = num42;
					settlement2 = settlement3;
				}
				num23++;
				num34++;
			}
			else if (settlement3.IsCastle)
			{
				float num43 = settlement3.Position2D.DistanceSquared(targetSettlement.Position2D);
				if (num12 > num43)
				{
					num12 = num43;
					settlement2 = settlement3;
				}
				num34++;
			}
		}
		if (settlement2 != null)
		{
			num12 = Campaign.Current.Models.MapDistanceModel.GetDistance(targetSettlement, settlement2);
		}
		if (settlement == settlement2)
		{
			num = num12;
		}
		else if (settlement != null)
		{
			num = Campaign.Current.Models.MapDistanceModel.GetDistance(targetSettlement, settlement);
		}
		float num44 = 1f;
		float num45 = MathF.Min(2f, MathF.Sqrt(num34)) / 2f;
		float num46 = MathF.Min(2f, MathF.Sqrt(num23)) / 2f;
		if (num45 > 0f && num46 < 1f)
		{
			num45 += 1f - num46;
		}
		num44 += 0.5f * (2f - (num45 + num46));
		float num2 = missionType switch
		{
			Army.ArmyTypes.Besieger => MathF.Max(0f, distance - Campaign.AverageDistanceBetweenTwoFortifications) * 0.15f + distance2 * 0.15f * num44 + num * 0.5f * num46 + num12 * 0.2f * num45, 
			Army.ArmyTypes.Raider => MathF.Max(0f, distance - Campaign.AverageDistanceBetweenTwoFortifications) * 0.15f + distance2 * 0.5f * num44 + num * 0.2f * num46 + num12 * 0.15f * num45, 
			_ => MathF.Max(0f, distance - Campaign.AverageDistanceBetweenTwoFortifications) * 0.15f + distance2 * 0.5f * num44 + num * 0.25f * num46 + num12 * 0.1f * num45, 
		};
		float num3 = missionType switch
		{
			Army.ArmyTypes.Besieger => targetSettlement.IsTown ? ReasonableDistanceForBesiegingTown : ReasonableDistanceForBesiegingCastle, 
			Army.ArmyTypes.Defender => targetSettlement.IsVillage ? ReasonableDistanceForDefendingVillage : ReasonableDistanceForDefendingTownOrCastle, 
			_ => ReasonableDistanceForRaiding, 
		};
		distanceScore = ((num2 < num3) ? (1f + (1f - num2 / num3) * 0.5f) : (num3 / num2 * (num3 / num2) * ((missionType != Army.ArmyTypes.Defender) ? (num3 / num2) : 1f)));
		if (distanceScore < 0.1f)
		{
			powerScore = 0f;
			distanceScore = 0f;
			settlementImportanceScore = 0f;
			return 0f;
		}
		float num4 = 1f;
		if (mobileParty.Army != null && mobileParty.Army.Cohesion < 40f)
		{
			num4 *= mobileParty.Army.Cohesion / 40f;
		}
		if (num4 < 0.25f)
		{
			powerScore = 0f;
			distanceScore = 0f;
			settlementImportanceScore = 0f;
			return 0f;
		}
		if (missionType == Army.ArmyTypes.Defender)
		{
			float num5 = 0f;
			float num6 = 0f;
			foreach (WarPartyComponent warPartyComponent in mapFaction.WarPartyComponents)
			{
				MobileParty mobileParty2 = warPartyComponent.MobileParty;
				if (mobileParty2 == mobileParty || (mobileParty2.Army != null && mobileParty2.Army == mobileParty.Army) || mobileParty2.AttachedTo != null)
				{
					continue;
				}
				if (mobileParty2.Army != null)
				{
					Army army = mobileParty2.Army;
					if (((army.AIBehavior == Army.AIBehaviorFlags.Gathering || army.AIBehavior == Army.AIBehaviorFlags.WaitingForArmyMembers) && army.AiBehaviorObject == targetSettlement) || (army.AIBehavior != Army.AIBehaviorFlags.Gathering && army.AIBehavior != Army.AIBehaviorFlags.WaitingForArmyMembers && army.AiBehaviorObject == targetSettlement) || (army.LeaderParty.TargetParty != null && (army.LeaderParty.TargetParty == targetSettlement.LastAttackerParty || (army.LeaderParty.TargetParty.MapEvent != null && army.LeaderParty.TargetParty.MapEvent == targetSettlement.LastAttackerParty.MapEvent) || (army.LeaderParty.TargetParty.BesiegedSettlement != null && army.LeaderParty.TargetParty.BesiegedSettlement == targetSettlement.LastAttackerParty.BesiegedSettlement))))
					{
						num6 += army.TotalStrength;
					}
				}
				else if ((mobileParty2.DefaultBehavior == AiBehavior.DefendSettlement && mobileParty2.TargetSettlement == targetSettlement) || (mobileParty2.TargetParty != null && (mobileParty2.TargetParty == targetSettlement.LastAttackerParty || (mobileParty2.TargetParty.MapEvent != null && mobileParty2.TargetParty.MapEvent == targetSettlement.LastAttackerParty.MapEvent) || (mobileParty2.TargetParty.BesiegedSettlement != null && mobileParty2.TargetParty.BesiegedSettlement == targetSettlement.LastAttackerParty.BesiegedSettlement))))
				{
					num6 += mobileParty2.Party.TotalStrength;
				}
			}
			MobileParty lastAttackerParty2 = targetSettlement.LastAttackerParty;
			if ((targetSettlement.LastAttackerParty.MapEvent != null && targetSettlement.LastAttackerParty.MapEvent.MapEventSettlement == targetSettlement) || targetSettlement.LastAttackerParty.BesiegedSettlement == targetSettlement)
			{
				LocatableSearchData<MobileParty> searchData = MobileParty.StartFindingLocatablesAroundPosition(targetSettlement.GatePosition, 6f);
				for (MobileParty mobileParty3 = MobileParty.FindNextLocatable(ref searchData); mobileParty != null; mobileParty = MobileParty.FindNextLocatable(ref searchData))
				{
					if (mobileParty3.Aggressiveness > 0f && mobileParty3.MapFaction == lastAttackerParty2.MapFaction)
					{
						num5 += ((mobileParty3.Aggressiveness > 0.5f) ? 1f : (mobileParty3.Aggressiveness * 2f)) * mobileParty3.Party.TotalStrength;
					}
				}
			}
			else
			{
				num5 = lastAttackerParty2.Army?.TotalStrength ?? lastAttackerParty2.Party.TotalStrength;
			}
			float num7 = ourStrength + num6;
			float num8 = MathF.Max(100f, num5) * 1.1f;
			float num9 = num8 * 2.5f;
			powerScore = ((num7 >= num9) ? (num9 / num7 * (num9 / num7)) : MathF.Min(1f, num7 / num8 * (num7 / num8)));
			if (num7 < num8)
			{
				powerScore *= 0.9f;
			}
			if (ourStrength < num5)
			{
				powerScore *= MathF.Pow(ourStrength / num5, 0.25f);
			}
		}
		else
		{
			float num10 = targetSettlement.Party.TotalStrength;
			float num11 = 0f;
			bool flag = Hero.MainHero.CurrentSettlement == targetSettlement;
			foreach (MobileParty mobileParty4 in targetSettlement.Parties)
			{
				if (mobileParty4.Aggressiveness > 0.01f || mobileParty4.IsGarrison || mobileParty4.IsMilitia)
				{
					float num13 = ((mobileParty4 == MobileParty.MainParty) ? 0.5f : ((mobileParty4.Army != null && mobileParty4.Army.LeaderParty == MobileParty.MainParty) ? 0.8f : 1f));
					float num14 = (flag ? 0.8f : 1f);
					num10 += num13 * num14 * mobileParty4.Party.TotalStrength;
					if (!mobileParty4.IsGarrison && !mobileParty4.IsMilitia && mobileParty4.LeaderHero != null)
					{
						num11 += num13 * num14 * mobileParty4.Party.TotalStrength;
					}
				}
			}
			float num15 = 0f;
			float num16 = 0f;
			num16 = ((missionType != 0 || mobileParty.BesiegedSettlement == targetSettlement) ? 1f : (targetSettlement.IsTown ? 4f : 3f));
			float num17 = MathF.Min(1f, distance2 / DistanceOfMobilePartyDivider);
			num16 *= 1f - 0.6f * (1f - num17) * (1f - num17);
			if (num10 < 100f && missionType == Army.ArmyTypes.Besieger)
			{
				num16 *= 0.5f + 0.5f * (num10 / 100f);
			}
			if ((mobileParty.MapEvent == null || mobileParty.MapEvent.MapEventSettlement != targetSettlement) && targetSettlement.MapFaction.IsKingdomFaction)
			{
				if (numberOfEnemyFactionSettlements < 0)
				{
					numberOfEnemyFactionSettlements = targetSettlement.MapFaction.Settlements.Count;
				}
				if (totalEnemyMobilePartyStrength < 0f)
				{
					totalEnemyMobilePartyStrength = targetSettlement.MapFaction.TotalStrength;
				}
				totalEnemyMobilePartyStrength *= 0.5f;
				float b = (totalEnemyMobilePartyStrength - num11) / ((float)numberOfEnemyFactionSettlements + 10f);
				num15 = MathF.Max(0f, b) * num16;
			}
			float num18 = ((missionType == Army.ArmyTypes.Besieger) ? (1.25f + 0.25f * (float)targetSettlement.Town.GetWallLevel()) : 1f);
			if (missionType == Army.ArmyTypes.Besieger && targetSettlement.Town.FoodStocks < 100f)
			{
				num18 -= 0.5f * (num18 - 1f) * ((100f - targetSettlement.Town.FoodStocks) / 100f);
			}
			float num19 = ((missionType == Army.ArmyTypes.Besieger && mobileParty.LeaderHero != null) ? (mobileParty.LeaderHero.RandomFloat(0.1f) + (MathF.Max(MathF.Min(1.2f, mobileParty.Aggressiveness), 0.8f) - 0.8f) * 0.5f) : 0f);
			float num20 = num10 * (num18 - num19) + num15 + 0.1f;
			if (ourStrength < num20 * ((missionType == Army.ArmyTypes.Besieger) ? 1f : 0.6f))
			{
				powerScore = 0f;
				settlementImportanceScore = 1f;
				return 0f;
			}
			float num21 = 0f;
			if ((missionType == Army.ArmyTypes.Besieger && distance2 < RaidDistanceLimit) || (missionType == Army.ArmyTypes.Raider && targetSettlement.Party.MapEvent != null))
			{
				LocatableSearchData<MobileParty> searchData = MobileParty.StartFindingLocatablesAroundPosition((mobileParty.SiegeEvent != null && mobileParty.SiegeEvent.BesiegedSettlement == targetSettlement) ? mobileParty.Position2D : targetSettlement.GatePosition, 9f);
				for (MobileParty mobileParty5 = MobileParty.FindNextLocatable(ref searchData); mobileParty != null; mobileParty = MobileParty.FindNextLocatable(ref searchData))
				{
					if (mobileParty5.CurrentSettlement != targetSettlement && mobileParty5.Aggressiveness > 0.01f && mobileParty5.MapFaction == targetSettlement.Party.MapFaction)
					{
						float num22 = ((mobileParty5 == MobileParty.MainParty || (mobileParty5.Army != null && mobileParty5.Army.LeaderParty == MobileParty.MainParty)) ? 0.5f : 1f);
						float num24 = 1f;
						if (mobileParty.MapEvent != null && mobileParty.MapEvent.MapEventSettlement == targetSettlement)
						{
							float num25 = mobileParty5.Position2D.Distance(mobileParty.Position2D);
							num24 = 1f - num25 / 16f;
						}
						num21 += num24 * mobileParty5.Party.TotalStrength * num22;
					}
				}
				if (num21 < ourStrength)
				{
					num21 = MathF.Max(0f, num21 - ourStrength * 0.33f);
				}
				num20 += num21;
				num20 -= num15;
				if (targetSettlement.MapFaction.IsKingdomFaction)
				{
					if (numberOfEnemyFactionSettlements < 0)
					{
						numberOfEnemyFactionSettlements = targetSettlement.MapFaction.Settlements.Count;
					}
					if (totalEnemyMobilePartyStrength < 0f)
					{
						totalEnemyMobilePartyStrength = targetSettlement.MapFaction.TotalStrength;
					}
					totalEnemyMobilePartyStrength *= 0.5f;
					float b2 = (totalEnemyMobilePartyStrength - (num11 + num21)) / ((float)numberOfEnemyFactionSettlements + 10f);
					num15 = MathF.Max(0f, b2) * num16;
				}
				num20 += num15;
			}
			float num26 = ((missionType == Army.ArmyTypes.Raider) ? 0.6f : 0.4f);
			float num27 = ((missionType == Army.ArmyTypes.Raider) ? 0.9f : 0.8f);
			float num28 = ((missionType == Army.ArmyTypes.Raider) ? 2.5f : 3f);
			float num29 = ourStrength / num20;
			powerScore = ((ourStrength > num20 * num28) ? 1f : ((num29 > 2f) ? (num27 + (1f - num27) * ((num29 - 2f) / (num28 - 2f))) : ((num29 > 1f) ? (num26 + (num27 - num26) * ((num29 - 1f) / 1f)) : (num26 * 0.9f * num29 * num29))));
		}
		powerScore = ((powerScore > 1f) ? 1f : powerScore);
		float num30 = ((missionType == Army.ArmyTypes.Raider) ? targetSettlement.GetSettlementValueForEnemyHero(mobileParty.LeaderHero) : targetSettlement.GetSettlementValueForFaction(mapFaction));
		float y = (targetSettlement.IsVillage ? 0.5f : 0.33f);
		settlementImportanceScore = MathF.Pow(num30 / 50000f, y);
		float num31 = 1f;
		if (missionType == Army.ArmyTypes.Raider)
		{
			if (targetSettlement.Village.Bound.Town.FoodStocks < 100f)
			{
				settlementImportanceScore *= 1f + 0.3f * ((100f - targetSettlement.Village.Bound.Town.FoodStocks) / 100f);
			}
			settlementImportanceScore *= 1.5f;
			num31 += ((mobileParty.Army != null) ? 0.5f : 1f) * ((mobileParty.LeaderHero != null && mobileParty.LeaderHero.Clan != null && mobileParty.LeaderHero.Clan.Gold < 10000) ? ((10000f - (float)mobileParty.LeaderHero.Clan.Gold) / 20000f) : 0f);
		}
		float num32 = missionType switch
		{
			Army.ArmyTypes.Besieger => 0.8f, 
			Army.ArmyTypes.Defender => targetSettlement.IsVillage ? 1.28f : 1.28f, 
			_ => 0.28f * (1f + (1f - targetSettlement.SettlementHitPoints)), 
		};
		if (missionType == Army.ArmyTypes.Defender && ((targetSettlement.IsFortification && targetSettlement.LastAttackerParty.BesiegedSettlement != targetSettlement) || (!targetSettlement.IsFortification && targetSettlement.LastAttackerParty.MapEvent == null)))
		{
			MobileParty lastAttackerParty3 = targetSettlement.LastAttackerParty;
			float distance3 = Campaign.Current.Models.MapDistanceModel.GetDistance(lastAttackerParty3, targetSettlement);
			float num33 = MathF.Min(GiveUpDistanceLimit, distance3) / GiveUpDistanceLimit;
			num32 = num33 * 0.8f + (1f - num33) * num32;
		}
		float num35 = 1f;
		if ((missionType == Army.ArmyTypes.Raider || missionType == Army.ArmyTypes.Besieger) && targetSettlement.OwnerClan != null && mobileParty.LeaderHero != null)
		{
			int relationWithClan = 0;
			if (mobileParty != null && mobileParty.LeaderHero != null && mobileParty.LeaderHero.Clan != null)
			{
				relationWithClan = mobileParty.LeaderHero.Clan.GetRelationWithClan(targetSettlement.OwnerClan);
			}
			if (relationWithClan > 0)
			{
				num35 = 1f - ((missionType == Army.ArmyTypes.Besieger) ? 0.4f : 0.8f) * (MathF.Sqrt(relationWithClan) / 10f);
			}
			else if (relationWithClan < 0)
			{
				num35 = 1f + ((missionType == Army.ArmyTypes.Besieger) ? 0.1f : 0.05f) * (MathF.Sqrt(0f - (float)relationWithClan) / 10f);
			}
		}
		float num36 = 1f;
		if (mobileParty.MapFaction != null && mobileParty.MapFaction.IsKingdomFaction && mobileParty.MapFaction.Leader == Hero.MainHero && (missionType != Army.ArmyTypes.Defender || (targetSettlement.LastAttackerParty != null && targetSettlement.LastAttackerParty.MapFaction != Hero.MainHero.MapFaction)))
		{
			StanceLink stanceLink = ((missionType != Army.ArmyTypes.Defender) ? Hero.MainHero.MapFaction.GetStanceWith(targetSettlement.MapFaction) : Hero.MainHero.MapFaction.GetStanceWith(targetSettlement.LastAttackerParty.MapFaction));
			if (stanceLink != null)
			{
				if (stanceLink.BehaviorPriority == 1)
				{
					if (missionType == Army.ArmyTypes.Besieger || missionType == Army.ArmyTypes.Raider)
					{
						num36 = 0.65f;
					}
					else if (missionType == Army.ArmyTypes.Defender)
					{
						num36 = 1.1f;
					}
				}
				else if (stanceLink.BehaviorPriority == 2 && (missionType == Army.ArmyTypes.Besieger || missionType == Army.ArmyTypes.Raider))
				{
					num36 = 1.3f;
				}
			}
		}
		float num37 = 1f;
		if (mobileParty.SiegeEvent != null && mobileParty.SiegeEvent.BesiegedSettlement == targetSettlement)
		{
			num37 = 4f;
		}
		float num38 = 1f;
		if (missionType == Army.ArmyTypes.Raider && mobileParty.MapEvent != null && mobileParty.MapEvent.IsRaid)
		{
			num38 = ((mobileParty.MapEvent.MapEventSettlement == targetSettlement) ? 1.3f : 0.3f);
		}
		float num39 = 1f;
		if (targetSettlement.SiegeEvent != null && targetSettlement.SiegeEvent.BesiegerCamp.BesiegerParty.MapFaction == mobileParty.MapFaction)
		{
			float num40 = targetSettlement.SiegeEvent.BesiegerCamp.GetInvolvedPartiesForEventType().Sum((PartyBase x) => x.TotalStrength) / targetSettlement.GetInvolvedPartiesForEventType().Sum((PartyBase x) => x.TotalStrength);
			num39 += Math.Max(0f, 3f - num40);
		}
		float num41 = num35 * distanceScore * powerScore * settlementImportanceScore * num31 * num32 * num36 * num4 * num37 * num38 * num39;
		if (mobileParty.Objective == MobileParty.PartyObjective.Defensive && missionType == Army.ArmyTypes.Defender)
		{
			num41 *= 1.2f;
		}
		else if (mobileParty.Objective == MobileParty.PartyObjective.Aggressive && (missionType == Army.ArmyTypes.Besieger || missionType == Army.ArmyTypes.Raider))
		{
			num41 *= 1.2f;
		}
		return (num41 < 0f) ? 0f : num41;
	}
}
