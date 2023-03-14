using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace ServeAsSoldier;

internal class SoldierPartyHealingModel : DefaultPartyHealingModel
{
	public override float GetSurvivalChance(PartyBase party, CharacterObject character, DamageTypes damageType, PartyBase enemyParty = null)
	{
		if ((character.IsHero && character.HeroObject.Name.ToString() == "Thief of the North") || (character.IsHero && character.HeroObject.Name.ToString() == "King's guard"))
		{
			return 0f;
		}
		return base.GetSurvivalChance(party, character, damageType, enemyParty);
	}

	public override ExplainedNumber GetDailyHealingHpForHeroes(MobileParty party, bool includeDescriptions = false)
	{
		if (Test.followingHero != null && party == MobileParty.MainParty)
		{
			ExplainedNumber result2 = new ExplainedNumber(24f, includeDescriptions, new TextObject("{=FLT0000191}Serving As Soldier Base"));
			float MedicineBonus = (float)Hero.MainHero.GetSkillValue(DefaultSkills.Medicine) / 100f;
			result2.AddFactor(MedicineBonus, new TextObject("{=FLT0000192}Medicine Skill"));
			if (Test.followingHero.PartyBelongedTo != null && Test.followingHero.PartyBelongedTo.CurrentSettlement != null)
			{
				result2.AddFactor((1f + MedicineBonus) * 2f, new TextObject("{=M0Gpl0dH}In Settlement"));
			}
			return result2;
		}
		return GetDailyHealingForRegulars(party, includeDescriptions);
	}
}
