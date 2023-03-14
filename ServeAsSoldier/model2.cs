using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace ServeAsSoldier;

public class model2 : DefaultPartyWageModel
{
	private static readonly TextObject _cultureText = GameTexts.FindText("str_culture");

	private static readonly TextObject _buildingEffects = GameTexts.FindText("str_building_effects");

	private const float MercenaryWageFactor = 1.5f;

	public override ExplainedNumber GetTotalWage(MobileParty mobileParty, bool includeDescriptions = false)
	{
		int num = 0;
		int num12 = 0;
		int num14 = 0;
		int num15 = 0;
		int num16 = 0;
		int num17 = 0;
		int num18 = 0;
		int num19 = 0;
		int num20 = 0;
		int num2 = 0;
		int num3 = 0;
		bool flag = !mobileParty.HasPerk(DefaultPerks.Steward.AidCorps);
		int num4 = 0;
		int num5 = 0;
		for (int i = 0; i < mobileParty.MemberRoster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = mobileParty.MemberRoster.GetElementCopyAtIndex(i);
			CharacterObject character = elementCopyAtIndex.Character;
			int num6 = (flag ? elementCopyAtIndex.Number : (elementCopyAtIndex.Number - elementCopyAtIndex.WoundedNumber));
			if (character.IsHero)
			{
				Hero heroObject = elementCopyAtIndex.Character.HeroObject;
				if (heroObject != character.HeroObject.Clan?.Leader)
				{
					num14 = ((mobileParty.LeaderHero == null || !mobileParty.LeaderHero.GetPerkValue(DefaultPerks.Steward.PaidInPromise)) ? (num14 + elementCopyAtIndex.Character.TroopWage) : (num14 + MathF.Round((float)elementCopyAtIndex.Character.TroopWage * (1f + DefaultPerks.Steward.PaidInPromise.PrimaryBonus * 0.01f))));
				}
				continue;
			}
			if (character.Tier < 4)
			{
				if (character.Culture.IsBandit)
				{
					num20 += elementCopyAtIndex.Character.TroopWage * elementCopyAtIndex.Number;
				}
				num += elementCopyAtIndex.Character.TroopWage * num6;
			}
			else if (character.Tier == 4)
			{
				if (character.Culture.IsBandit)
				{
					num2 += elementCopyAtIndex.Character.TroopWage * elementCopyAtIndex.Number;
				}
				num12 += elementCopyAtIndex.Character.TroopWage * num6;
			}
			else if (character.Tier > 4)
			{
				if (character.Culture.IsBandit)
				{
					num3 += elementCopyAtIndex.Character.TroopWage * elementCopyAtIndex.Number;
				}
				num14 += elementCopyAtIndex.Character.TroopWage * num6;
			}
			if (character.IsInfantry)
			{
				num15 += num6;
			}
			if (character.IsMounted)
			{
				num16 += num6;
			}
			if (character.Occupation == Occupation.CaravanGuard)
			{
				num4 += elementCopyAtIndex.Number;
			}
			if (character.Occupation == Occupation.Mercenary)
			{
				num5 += elementCopyAtIndex.Number;
			}
			if (character.IsRanged)
			{
				num17 += num6;
				if (character.Tier >= 4)
				{
					num18 += num6;
					num19 += elementCopyAtIndex.Character.TroopWage * elementCopyAtIndex.Number;
				}
			}
		}
		ExplainedNumber explainedNumber = new ExplainedNumber(0f, includeDescriptions: false, null);
		if (mobileParty.LeaderHero != null && mobileParty.LeaderHero.GetPerkValue(DefaultPerks.Roguery.DeepPockets))
		{
			num -= num20;
			num12 -= num2;
			num14 -= num3;
			int num7 = num20 + num2 + num3;
			explainedNumber.Add(num7);
			PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Roguery.DeepPockets, mobileParty.LeaderHero.CharacterObject, isPrimaryBonus: false, ref explainedNumber);
		}
		int num8 = num + num12 + num14;
		if (mobileParty.HasPerk(DefaultPerks.Crossbow.PickedShots) && num18 > 0)
		{
			float num9 = (float)num19 * DefaultPerks.Crossbow.PickedShots.PrimaryBonus;
			num8 += (int)num9;
		}
		ExplainedNumber result = new ExplainedNumber(num8, includeDescriptions);
		ExplainedNumber explainedNumber2 = new ExplainedNumber(1f);
		if (mobileParty.IsGarrison && mobileParty.CurrentSettlement?.Town != null)
		{
			if (mobileParty.CurrentSettlement.IsTown)
			{
				PerkHelper.AddPerkBonusForTown(DefaultPerks.OneHanded.MilitaryTradition, mobileParty.CurrentSettlement.Town, ref result);
				PerkHelper.AddPerkBonusForTown(DefaultPerks.TwoHanded.Berserker, mobileParty.CurrentSettlement.Town, ref result);
				PerkHelper.AddPerkBonusForTown(DefaultPerks.Bow.HunterClan, mobileParty.CurrentSettlement.Town, ref result);
				float troopRatio = (float)num15 / (float)mobileParty.MemberRoster.TotalRegulars;
				CalculatePartialGarrisonWageReduction(troopRatio, mobileParty, DefaultPerks.Polearm.StandardBearer, ref result, isSecondaryEffect: true);
				float troopRatio2 = (float)num16 / (float)mobileParty.MemberRoster.TotalRegulars;
				CalculatePartialGarrisonWageReduction(troopRatio2, mobileParty, DefaultPerks.Riding.CavalryTactics, ref result, isSecondaryEffect: true);
				float troopRatio3 = (float)num17 / (float)mobileParty.MemberRoster.TotalRegulars;
				CalculatePartialGarrisonWageReduction(troopRatio3, mobileParty, DefaultPerks.Crossbow.PeasantLeader, ref result, isSecondaryEffect: true);
			}
			else if (mobileParty.CurrentSettlement.IsCastle)
			{
				PerkHelper.AddPerkBonusForTown(DefaultPerks.Steward.StiffUpperLip, mobileParty.CurrentSettlement.Town, ref result);
			}
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Steward.DrillSergant, mobileParty.CurrentSettlement.Town, ref result);
			if (mobileParty.CurrentSettlement.Culture.HasFeat(DefaultCulturalFeats.EmpireGarrisonWageFeat))
			{
				result.AddFactor(DefaultCulturalFeats.EmpireGarrisonWageFeat.EffectBonus, GameTexts.FindText("str_culture"));
			}
			foreach (Building building in mobileParty.CurrentSettlement.Town.Buildings)
			{
				float buildingEffectAmount = building.GetBuildingEffectAmount(BuildingEffectEnum.GarrisonWageReduce);
				if (buildingEffectAmount > 0f)
				{
					explainedNumber2.AddFactor(0f - buildingEffectAmount / 100f, building.Name);
				}
			}
		}
		result.Add(explainedNumber.ResultNumber);
		float value = 0f;
		if (mobileParty != null && mobileParty.LeaderHero != null && mobileParty.LeaderHero.Clan != null && mobileParty.LeaderHero.Clan.Kingdom != null)
		{
			value = ((mobileParty.LeaderHero != null && mobileParty.LeaderHero.Clan.Kingdom != null && !mobileParty.LeaderHero.Clan.IsUnderMercenaryService && mobileParty.LeaderHero.Clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.MilitaryCoronae)) ? 0.1f : 0f);
		}
		if (mobileParty.HasPerk(DefaultPerks.Trade.SwordForBarter, checkSecondaryRole: true))
		{
			float num10 = (float)num4 / (float)mobileParty.MemberRoster.TotalRegulars;
			if (num10 > 0f)
			{
				float value2 = DefaultPerks.Trade.SwordForBarter.SecondaryBonus * num10;
				result.AddFactor(value2, DefaultPerks.Trade.SwordForBarter.Name);
			}
		}
		if (mobileParty.HasPerk(DefaultPerks.Steward.Contractors))
		{
			float num11 = (float)num5 / (float)mobileParty.MemberRoster.TotalRegulars;
			if (num11 > 0f)
			{
				float value3 = DefaultPerks.Steward.Contractors.PrimaryBonus * num11;
				result.AddFactor(value3, DefaultPerks.Steward.Contractors.Name);
			}
		}
		if (mobileParty.HasPerk(DefaultPerks.Trade.MercenaryConnections, checkSecondaryRole: true))
		{
			float num13 = (float)num5 / (float)mobileParty.MemberRoster.TotalRegulars;
			if (num13 > 0f)
			{
				float value4 = DefaultPerks.Trade.MercenaryConnections.SecondaryBonus * num13;
				result.AddFactor(value4, DefaultPerks.Trade.MercenaryConnections.Name);
			}
		}
		result.AddFactor(value, DefaultPolicies.MilitaryCoronae.Name);
		result.AddFactor(explainedNumber2.ResultNumber - 1f, _buildingEffects);
		if (PartyBaseHelper.HasFeat(mobileParty.Party, DefaultCulturalFeats.AseraiIncreasedWageFeat))
		{
			result.AddFactor(DefaultCulturalFeats.AseraiIncreasedWageFeat.EffectBonus, _cultureText);
		}
		if (mobileParty.HasPerk(DefaultPerks.Steward.Frugal))
		{
			result.AddFactor(DefaultPerks.Steward.Frugal.PrimaryBonus * 0.01f, DefaultPerks.Steward.Frugal.Name);
		}
		if (mobileParty.Army != null && mobileParty.HasPerk(DefaultPerks.Steward.EfficientCampaigner, checkSecondaryRole: true))
		{
			result.AddFactor(DefaultPerks.Steward.EfficientCampaigner.SecondaryBonus * 0.01f, DefaultPerks.Steward.EfficientCampaigner.Name);
		}
		if (mobileParty.SiegeEvent != null && mobileParty.SiegeEvent.BesiegerCamp.GetInvolvedPartiesForEventType().Contains(mobileParty.Party) && mobileParty.HasPerk(DefaultPerks.Steward.MasterOfWarcraft))
		{
			result.AddFactor(DefaultPerks.Steward.MasterOfWarcraft.PrimaryBonus * 0.01f, DefaultPerks.Steward.MasterOfWarcraft.Name);
		}
		if (mobileParty.EffectiveQuartermaster != null)
		{
			PerkHelper.AddEpicPerkBonusForCharacter(DefaultPerks.Steward.PriceOfLoyalty, mobileParty.EffectiveQuartermaster.CharacterObject, DefaultSkills.Steward, applyPrimaryBonus: true, ref result);
		}
		return result;
	}

	private void CalculatePartialGarrisonWageReduction(float troopRatio, MobileParty mobileParty, PerkObject perk, ref ExplainedNumber garrisonWageReductionMultiplier, bool isSecondaryEffect)
	{
		if (troopRatio > 0f && mobileParty.CurrentSettlement.Town.Governor != null && PerkHelper.GetPerkValueForTown(perk, mobileParty.CurrentSettlement.Town))
		{
			garrisonWageReductionMultiplier.AddFactor(isSecondaryEffect ? (perk.SecondaryBonus * troopRatio * 0.01f) : (perk.PrimaryBonus * troopRatio * 0.01f), perk.Name);
		}
	}
}
