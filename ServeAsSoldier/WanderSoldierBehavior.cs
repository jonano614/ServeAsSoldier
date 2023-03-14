using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace ServeAsSoldier;

internal class WanderSoldierBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, Tick);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, Tok);
	}

	private void Tok()
	{
		foreach (Hero hero in Campaign.Current.AliveHeroes)
		{
			if (hero.IsWanderer && hero.CompanionOf != null && hero.CompanionOf != Clan.PlayerClan && MBRandom.RandomInt(10) == 1)
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(hero, hero.CompanionOf.Leader, 1);
			}
		}
	}

	private void Tick()
	{
		foreach (Hero hero2 in Campaign.Current.AliveHeroes)
		{
			if (hero2.IsWanderer && hero2.CompanionOf != null && hero2.CompanionOf != Clan.PlayerClan && hero2.PartyBelongedTo == null)
			{
				TextObject text = new TextObject("{=FLT0000230}{HERO} is no longer employeed by the {CLAN}");
				text.SetTextVariable("CLAN", hero2.CompanionOf.Name.ToString());
				text.SetTextVariable("HERO", hero2.Name.ToString());
				InformationManager.DisplayMessage(new InformationMessage(text.ToString()));
				RemoveCompanionAction.ApplyByFire(hero2.CompanionOf, hero2);
			}
		}
		if (getWandererCount() - getEmployeedWandererCount() < 100 && SubModule.settings.AIRecruitWanders)
		{
			Hero hero = HeroCreator.CreateHeroAtOccupation(Occupation.Wanderer);
			hero.ChangeState(Hero.CharacterStates.Active);
		}
	}

	private int getWandererCount()
	{
		int count = 0;
		foreach (Hero hero in Campaign.Current.AliveHeroes)
		{
			if (hero.IsWanderer)
			{
				count++;
			}
		}
		return count;
	}

	private int getEmployeedWandererCount()
	{
		int count = 0;
		foreach (Hero hero in Campaign.Current.AliveHeroes)
		{
			if (hero.IsWanderer && hero.CompanionOf != null && hero.CompanionOf != Clan.PlayerClan)
			{
				count++;
			}
		}
		return count;
	}

	public int getWanderinClanCount(Clan clan)
	{
		int count = 0;
		foreach (Hero hero in clan.Heroes)
		{
			if (hero.IsWanderer)
			{
				count++;
			}
		}
		return count;
	}

	private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
	{
		if (settlement == null || !settlement.IsTown || mobileParty == null || mobileParty.LeaderHero == null || hero == null || mobileParty.LeaderHero != hero || !hero.IsLord || hero.Clan == null || hero.Clan.Kingdom == null || hero.Clan.IsMinorFaction || hero.Clan == Clan.PlayerClan || !SubModule.settings.AIRecruitWanders)
		{
			return;
		}
		Hero recruit = null;
		foreach (Hero settlementHero in settlement.HeroesWithoutParty)
		{
			if (settlementHero.IsWanderer && settlementHero.CompanionOf == null && recruit == null && (settlementHero.Culture == hero.Culture || settlementHero.Culture == hero.MapFaction.Culture) && getWanderinClanCount(hero.Clan) < hero.Clan.CompanionLimit)
			{
				recruit = settlementHero;
			}
		}
		if (recruit != null)
		{
			AddHeroToPartyAction.Apply(recruit, mobileParty);
			TextObject text = new TextObject("{=FLT0000229}{LORD} recruited {HERO}");
			text.SetTextVariable("LORD", hero.Name.ToString());
			text.SetTextVariable("HERO", recruit.Name.ToString());
			InformationManager.DisplayMessage(new InformationMessage(text.ToString()));
			recruit.CompanionOf = hero.Clan;
			giveGear(recruit, recruit.Culture);
		}
	}

	private void giveGear(Hero settlementHero, CultureObject culture)
	{
		List<CharacterObject> templates = new List<CharacterObject>();
		int tier = settlementHero.Level / 5 + 1;
		List<Equipment> list = new List<Equipment>();
		int maxTier = -1;
		foreach (CharacterObject troop2 in Test.GetTroopsList(culture))
		{
			if (troop2.Tier <= tier && troop2.Tier > maxTier)
			{
				maxTier = troop2.Tier;
			}
		}
		if (maxTier == -1)
		{
			return;
		}
		foreach (CharacterObject troop in Test.GetTroopsList(culture))
		{
			if (troop.Tier == maxTier)
			{
				list.Add(troop.Equipment);
			}
		}
		settlementHero.CharacterObject.Equipment.FillFrom(list.GetRandomElement());
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
