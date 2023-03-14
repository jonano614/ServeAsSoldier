using System.Reflection;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace ServeAsSoldier;

internal class AbonadonedOrphanEvent : CampaignBehaviorBase
{
	private static bool isFemale;

	public override void RegisterEvents()
	{
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, Tick);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, MenuItems);
	}

	private void MenuItems(CampaignGameStarter campaignStarter)
	{
		TextObject textObject = new TextObject("{=FLT0000175}As you walk through the ravaged village, you notice one house in particular that is covered in blood.  As you walk closer the stench of death hits your nostrils.  It become clear to you what happened here.  The stubborn yet foolish family of this house decided to stay behind and defend what little they owned.  The looting soldiers of your army had little patience for those that resisted and cut down the family where they stood.");
		TextObject textObject2 = new TextObject("{=FLT0000176}Suddenly the sound of cries and screams catches your attention.  Someone is still inside.  As you go in to investigate, you quickly find source of the noise.  It is a child, barely older than an infant.  The soldiers that came by earlier either didn't notice or didn't have the heart to kill the child.  You realize that with all the villagers driven away, it might be days or even weeks before anyone return and you are not sure what will happen to the child by then.");
		TextObject textObject3 = new TextObject("{=FLT0000177}Take the child with you.");
		TextObject textObject4 = new TextObject("{=FLT0000178}Leave.");
		campaignStarter.AddGameMenu("abonadoned_orphan", textObject.ToString(), delegate
		{
		});
		campaignStarter.AddGameMenuOption("abonadoned_orphan", "abonadoned_orphan_continue", "Continue", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			GameMenu.SwitchToMenu("abonadoned_orphan_2");
		}, true, -1, false);
		campaignStarter.AddGameMenu("abonadoned_orphan_2", textObject2.ToString(), delegate
		{
		});
		campaignStarter.AddGameMenuOption("abonadoned_orphan_2", "abonadoned_orphan_a", textObject3.ToString(), (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.ShowMercy;
			return true;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			generateChild();
			GameMenu.ActivateGameMenu("party_wait");
		}, true, -1, false);
		campaignStarter.AddGameMenuOption("abonadoned_orphan_2", "abonadoned_orphan_b", textObject4.ToString(), (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			GameMenu.ActivateGameMenu("party_wait");
		}, true, -1, false);
	}

	public static void generateChild()
	{
		Settlement homeSettlement = SettlementHelper.FindNearestVillage();
		CharacterObject template = (isFemale ? homeSettlement.Culture.VillagerFemaleChild : homeSettlement.Culture.VillagerMaleChild);
		Hero child = HeroCreator.CreateSpecialHero(template, homeSettlement, null, Hero.MainHero.Clan, MBRandom.RandomInt(0, 3));
		int age1 = MBRandom.RandomInt(20, 30);
		int age2 = age1 + MBRandom.RandomInt(0, 3);
		Hero mother = HeroCreator.CreateSpecialHero(homeSettlement.Culture.VillageWoman, null, null, null, age1);
		Hero father = (mother.Spouse = HeroCreator.CreateSpecialHero(homeSettlement.Culture.Villager, null, null, null, age2));
		mother.ChangeState(Hero.CharacterStates.Dead);
		father.ChangeState(Hero.CharacterStates.Dead);
		child.ChangeState(Hero.CharacterStates.Active);
		mother.DeathDay = CampaignTime.Now;
		father.DeathDay = CampaignTime.Now;
		child.Mother = mother;
		child.Father = father;
		child.HeroDeveloper.DeriveSkillsFromTraits(isByNaturalGrowth: false, template);
		child.SetNewOccupation(Occupation.Lord);
		child.GetType().GetProperty("Occupation", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SetValue(child, Occupation.Lord);
		AdoptHeroAction.Apply(child);
		TextObject text = new TextObject("{=FLT0000179}{HERO} adopted a child named {CHILD}.");
		text.SetTextVariable("HERO", Hero.MainHero.Name.ToString());
		text.SetTextVariable("CHILD", child.Name.ToString());
		MBInformationManager.AddQuickInformation(text, 0, CharacterObject.PlayerCharacter);
	}

	private void Tick()
	{
		if (Test.followingHero != null && Test.followingHero.PartyBelongedTo != null && Test.followingHero.PartyBelongedTo.DefaultBehavior == AiBehavior.RaidSettlement && Test.followingHero.PartyBelongedTo.TargetSettlement != null && Test.followingHero.PartyBelongedTo.TargetSettlement.Position2D.Distance(Test.followingHero.PartyBelongedTo.Position2D) < 0.5f && Test.print(MBRandom.RandomInt(100)) == 1)
		{
			isFemale = MBRandom.RandomInt(100) < 50;
			GameMenu.SwitchToMenu("abonadoned_orphan");
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
