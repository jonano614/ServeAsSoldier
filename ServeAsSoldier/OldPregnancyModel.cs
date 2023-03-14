using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;

namespace ServeAsSoldier;

internal class OldPregnancyModel : DefaultPregnancyModel
{
	public override float GetDailyChanceOfPregnancyForHero(Hero hero)
	{
		int num = hero.Children.Count + 1;
		float num2 = 4 + 4 * hero.Clan.Tier;
		float num3 = ((hero != Hero.MainHero && hero.Spouse != Hero.MainHero) ? Math.Min(1f, (2f * num2 - (float)hero.Clan.Lords.Count) / num2) : 1f);
		float num4 = (1.2f - (hero.Age - 18f) * 0.04f) / (float)(num * num) * 0.12f * ((hero.Clan != Clan.PlayerClan) ? num3 : 1f);
		float baseNumber = ((hero.Spouse != null && hero.Age >= 18f && hero.Age <= 45f) ? num4 : 0f);
		ExplainedNumber explainedNumber = new ExplainedNumber(baseNumber);
		if (hero.GetPerkValue(DefaultPerks.Charm.Virile) || hero.Spouse.GetPerkValue(DefaultPerks.Charm.Virile))
		{
			explainedNumber.AddFactor(DefaultPerks.Charm.Virile.PrimaryBonus, DefaultPerks.Charm.Virile.Name);
		}
		return explainedNumber.ResultNumber;
	}
}
