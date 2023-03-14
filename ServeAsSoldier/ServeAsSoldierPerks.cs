using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace ServeAsSoldier;

public class ServeAsSoldierPerks : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		setAdditionalPerkDescriptions();
	}

	private void setAdditionalPerkDescriptions()
	{
		TextObject text1 = new TextObject("{=FLT0000205}Serve As Soldier : Winning tournaments while enlisted grants 50% more xp for promotions.");
		AddPerkDescription(DefaultPerks.OneHanded.Duelist, text1.ToString());
		TextObject text5 = new TextObject("{=FLT0000206}Serve As Soldier : Training with troops grants 50% more xp towards combat skill.");
		AddPerkDescription(DefaultPerks.OneHanded.Trainer, text5.ToString());
		TextObject text6 = new TextObject("{=FLT0000207}Serve As Soldier : Knock downs while training with troops grant 0.1 renown.");
		AddPerkDescription(DefaultPerks.TwoHanded.ShowOfStrength, text6.ToString());
		TextObject text7 = new TextObject("{=FLT0000208}Serve As Soldier : Knock downs while in battles grant 0.3 renown.");
		AddPerkDescription(DefaultPerks.TwoHanded.BaptisedInBlood, text7.ToString());
		TextObject text8 = new TextObject("{=FLT0000209}Serve As Soldier : Knock downs while fighting melee on foot in battles grant 0.3 renown.");
		AddPerkDescription(DefaultPerks.Polearm.Phalanx, text8.ToString());
		TextObject text9 = new TextObject("{=FLT0000210}Serve As Soldier : Wages earned increased by 20%.");
		AddPerkDescription(DefaultPerks.Polearm.StandardBearer, text9.ToString());
		TextObject text10 = new TextObject("{=FLT0000211}Serve As Soldier : Knock downs caused by ranged damage in battles grant 0.3 renown.");
		AddPerkDescription(DefaultPerks.Bow.RenownedArcher, text10.ToString());
		TextObject text11 = new TextObject("{=FLT0000212}Serve As Soldier : Headshots in battle generate extra skill xp.");
		AddPerkDescription(DefaultPerks.Bow.BullsEye, text11.ToString());
		TextObject text12 = new TextObject("{=FLT0000213}Serve As Soldier : Crossbow shots in battle that hit from over 50 meters away generate extra skill xp.");
		AddPerkDescription(DefaultPerks.Crossbow.LongShots, text12.ToString());
		TextObject text2 = new TextObject("{=FLT0000214}Serve As Soldier : Crossbow shots in battle while mounted generate extra skill xp.");
		AddPerkDescription(DefaultPerks.Crossbow.Steady, text2.ToString());
		TextObject text3 = new TextObject("{=FLT0000215}Serve As Soldier : Receive a 1000 gold bonus for every enemy lord knocked out in battle.");
		AddPerkDescription(DefaultPerks.Throwing.HeadHunter, text3.ToString());
		TextObject text4 = new TextObject("{=FLT0000216}Serve As Soldier : Knock outs with throwing weapons while training with troops grants 0.2 renown.");
		AddPerkDescription(DefaultPerks.Throwing.ThrowingCompetitions, text4.ToString());
	}

	private void AddPerkDescription(PerkObject perk, string description)
	{
		TextObject newDescription = new TextObject(perk.Description.ToString() + "\n \n" + description);
		BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
		typeof(PropertyObject).GetField("_description", bindFlags).SetValue(perk, newDescription);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
