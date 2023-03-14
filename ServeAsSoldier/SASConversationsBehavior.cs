using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace ServeAsSoldier;

internal class SASConversationsBehavior : CampaignBehaviorBase
{
	private Dictionary<Hero, CampaignTime> LastDrink = new Dictionary<Hero, CampaignTime>();

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
	}

	private void OnSessionLaunched(CampaignGameStarter campaignStarter)
	{
		campaignStarter.AddPlayerLine("sas_talk_wanderer", "hero_main_options", "sas_talk_ask_wanderer", "{=FLT0000274}Can I talk to someone else in your party?", () => Hero.OneToOneConversationHero.PartyBelongedTo != null && Hero.OneToOneConversationHero.PartyBelongedTo.MemberRoster.TotalHeroes > 1 && Hero.OneToOneConversationHero.PartyBelongedTo != MobileParty.MainParty, delegate
		{
			List<Hero> list = new List<Hero>();
			foreach (TroopRosterElement current in Hero.OneToOneConversationHero.PartyBelongedTo.MemberRoster.GetTroopRoster())
			{
				if (current.Character.IsHero && current.Character.HeroObject != Hero.OneToOneConversationHero)
				{
					list.Add(current.Character.HeroObject);
				}
			}
			ConversationSentence.SetObjectsToRepeatOver(list, 10);
		});
		campaignStarter.AddDialogLine("sas_talk_wanderer_2", "sas_talk_ask_wanderer", "sas_talk_ask_wanderer_list", "{=St6NxphR}Be my guest, who would you like to talk to?", null, null);
		campaignStarter.AddRepeatablePlayerLine("sas_talk_wanderer_3", "sas_talk_ask_wanderer_list", "sas_talk_ask_wanderer_selected", "{=!}{NAME}", "I was thinking of a different person", "sas_talk_ask_wanderer", delegate
		{
			Hero hero = ConversationSentence.CurrentProcessedRepeatObject as Hero;
			ConversationSentence.SelectedRepeatLine.SetTextVariable("NAME", hero.Name);
			return true;
		}, delegate
		{
			Campaign.Current.ConversationManager.EndConversation();
			CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, (PartyBase)null, false, false, false, false, false), new ConversationCharacterData((ConversationSentence.SelectedRepeatObject as Hero).CharacterObject, (ConversationSentence.SelectedRepeatObject as Hero).PartyBelongedTo.Party, false, false, false, false, false));
		});
		campaignStarter.AddPlayerLine("sas_hire_wanderer", "hero_main_options", "sas_companion_hire", "{=FLT0000234}I can use someone like you in my retinue.", () => Clan.PlayerClan.Companions.Count < Clan.PlayerClan.CompanionLimit && CharacterObject.OneToOneConversationCharacter.HeroObject.IsWanderer && CharacterObject.OneToOneConversationCharacter.HeroObject.Clan != Clan.PlayerClan && ((Test.followingHero != null && Test.EnlistTier >= 6) || (Clan.PlayerClan.MapFaction == Hero.OneToOneConversationHero.MapFaction && CharacterObject.OneToOneConversationCharacter.HeroObject.CurrentSettlement == null)), delegate
		{
		});
		campaignStarter.AddDialogLine("sas_companion_hire_reponse_a", "sas_companion_hire", "lord_pretalk", "{=FLT0000235}Sorry, but I am happy with serving lord {LORD}.", delegate
		{
			MBTextManager.SetTextVariable("LORD", (CharacterObject.OneToOneConversationCharacter != null && CharacterObject.OneToOneConversationCharacter.HeroObject != null && CharacterObject.OneToOneConversationCharacter.HeroObject.PartyBelongedTo != null && CharacterObject.OneToOneConversationCharacter.HeroObject.PartyBelongedTo.LeaderHero != null) ? CharacterObject.OneToOneConversationCharacter.HeroObject.PartyBelongedTo.LeaderHero.EncyclopediaLinkWithName : new TextObject());
			return CharacterObject.OneToOneConversationCharacter.HeroObject.PartyBelongedTo != null && CharacterObject.OneToOneConversationCharacter.HeroObject.PartyBelongedTo.LeaderHero != null && CharacterObject.OneToOneConversationCharacter.HeroObject.GetRelation(CharacterObject.OneToOneConversationCharacter.HeroObject.PartyBelongedTo.LeaderHero) >= 50;
		}, null);
		campaignStarter.AddDialogLine("sas_companion_hire_reponse_b", "sas_companion_hire", "lord_pretalk", "{=FLT0000236}Sorry, but you and I simply dont get along.", () => CharacterObject.OneToOneConversationCharacter.HeroObject.GetRelation(Hero.MainHero) <= -10, null, 99);
		campaignStarter.AddDialogLine("sas_companion_hire_reponse_c", "sas_companion_hire", "sas_companion_hire_2", "{=FLT0000237}Sure, but I don't come cheap.  I need enough gold to pay off current enlistment contract with {LORD} and I want a bit extra leftover for myself.  In total, I need {GOLD}{COIN}.", delegate
		{
			MBTextManager.SetTextVariable("LORD", (CharacterObject.OneToOneConversationCharacter != null && CharacterObject.OneToOneConversationCharacter.HeroObject != null && CharacterObject.OneToOneConversationCharacter.HeroObject.PartyBelongedTo != null && CharacterObject.OneToOneConversationCharacter.HeroObject.PartyBelongedTo.LeaderHero != null) ? CharacterObject.OneToOneConversationCharacter.HeroObject.PartyBelongedTo.LeaderHero.EncyclopediaLinkWithName : new TextObject());
			MBTextManager.SetTextVariable("GOLD", (CharacterObject.OneToOneConversationCharacter != null && CharacterObject.OneToOneConversationCharacter.HeroObject != null) ? GetHireCost(CharacterObject.OneToOneConversationCharacter.HeroObject).ToString() : "0");
			MBTextManager.SetTextVariable("COIN", "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
			return CharacterObject.OneToOneConversationCharacter.HeroObject.PartyBelongedTo != null && CharacterObject.OneToOneConversationCharacter.HeroObject.PartyBelongedTo.LeaderHero != null;
		}, null, 97);
		campaignStarter.AddDialogLine("sas_companion_hire_reponse_d", "sas_companion_hire", "sas_companion_hire_2", "{=FLT0000238}Sure, but I don't come cheap.  In total, I need {GOLD}{COIN}.", delegate
		{
			MBTextManager.SetTextVariable("GOLD", (CharacterObject.OneToOneConversationCharacter != null && CharacterObject.OneToOneConversationCharacter.HeroObject != null) ? GetHireCost(CharacterObject.OneToOneConversationCharacter.HeroObject).ToString() : "0");
			MBTextManager.SetTextVariable("COIN", "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
			return CharacterObject.OneToOneConversationCharacter.HeroObject.PartyBelongedTo == null;
		}, null, 96);
		campaignStarter.AddPlayerLine("sas_hire_wanderer_2_reponse_a", "sas_companion_hire_2", "lord_pretalk", "{=EiFPu9Np}Right... {GOLD_AMOUNT} Here you are.", delegate
		{
			MBTextManager.SetTextVariable("GOLD_AMOUNT", (CharacterObject.OneToOneConversationCharacter != null && CharacterObject.OneToOneConversationCharacter.HeroObject != null) ? (GetHireCost(CharacterObject.OneToOneConversationCharacter.HeroObject) + "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">") : "0");
			return Hero.MainHero.Gold >= GetHireCost(CharacterObject.OneToOneConversationCharacter.HeroObject);
		}, delegate
		{
			GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, Hero.OneToOneConversationHero, GetHireCost(CharacterObject.OneToOneConversationCharacter.HeroObject));
			Equipment firstBattleEquipment2 = CharacterObject.OneToOneConversationCharacter.FirstBattleEquipment;
			AddCompanionAction.Apply(Clan.PlayerClan, Hero.OneToOneConversationHero);
			AddHeroToPartyAction.Apply(Hero.OneToOneConversationHero, MobileParty.MainParty);
			Test.GiveStateIssueEquipment(Hero.OneToOneConversationHero, firstBattleEquipment2);
		});
		campaignStarter.AddPlayerLine("sas_hire_wanderer_2_reponse_b", "sas_companion_hire_2", "lord_pretalk", "{=65UMAav2}I can't afford that just now.", () => true, delegate
		{
		});
		campaignStarter.AddPlayerLine("sas_wanderer_tavern_drinks", "hero_main_options", "sas_wanderer_tavern_drinks", "{=FLT0000239}Let's go for a round of drinks at the tavern!  I will pay!", () => CharacterObject.OneToOneConversationCharacter.HeroObject.IsWanderer && Test.followingHero != null && Test.followingHero.PartyBelongedTo != null && Test.followingHero.PartyBelongedTo.CurrentSettlement != null && Test.followingHero.PartyBelongedTo.CurrentSettlement.IsTown && Hero.MainHero.Gold >= 100, delegate
		{
		});
		campaignStarter.AddDialogLine("sas_wanderer_tavern_drinks_reponse_a", "sas_wanderer_tavern_drinks", "lord_pretalk", "{=FLT0000240}What exactly are you planning?  Are you trying to slip some poison in to my drink or steal my coin purse when I am passed out.  I can tell you are up to no good.  Now if you don't have anything else to say, get out of my sight.", () => CharacterObject.OneToOneConversationCharacter.HeroObject.GetRelation(Hero.MainHero) <= -10, null);
		campaignStarter.AddDialogLine("sas_wanderer_tavern_drinks_reponse_b", "sas_wanderer_tavern_drinks", "lord_pretalk", "{=FLT0000241}I appreciate the offer, but if I drink anymore the commander will have my hide.", () => isDrunk(CharacterObject.OneToOneConversationCharacter.HeroObject) && Hero.OneToOneConversationHero.Clan != null && Hero.OneToOneConversationHero.Clan != Clan.PlayerClan, null, 99);
		campaignStarter.AddDialogLine("sas_wanderer_tavern_drinks_reponse_d", "sas_wanderer_tavern_drinks", "lord_pretalk", "{=FLT0000243}Thank you captain, but if drink anymore, I won't be able to complete my duties.", () => isDrunk(CharacterObject.OneToOneConversationCharacter.HeroObject) && Hero.OneToOneConversationHero.Clan != null && Hero.OneToOneConversationHero.Clan == Clan.PlayerClan, null, 98);
		campaignStarter.AddDialogLine("sas_wanderer_tavern_drinks_reponse_c", "sas_wanderer_tavern_drinks", "close_window", "{=FLT0000242}It has been ages since I gotten a decent drink!  Let's go!", () => true, delegate
		{
			GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, Hero.OneToOneConversationHero, 100);
			if (LastDrink.ContainsKey(Hero.OneToOneConversationHero))
			{
				LastDrink.Remove(Hero.OneToOneConversationHero);
			}
			LastDrink.Add(Hero.OneToOneConversationHero, CampaignTime.Now);
			Clan clan2 = Hero.OneToOneConversationHero.Clan;
			Hero.OneToOneConversationHero.CompanionOf = null;
			ChangeRelationAction.ApplyPlayerRelation(Hero.OneToOneConversationHero, 1, affectRelatives: false);
			Hero.OneToOneConversationHero.CompanionOf = clan2;
		}, 97);
		campaignStarter.AddPlayerLine("sas_wanderer_surgeon", "hero_main_options", "sas_wanderer_surgeon", "{=FLT0000244}You seem injured.  Let me bandage your wounds.  I am the camp surgeon afterall.", () => CharacterObject.OneToOneConversationCharacter.HeroObject.IsWounded && Test.followingHero != null && Test.followingHero.PartyBelongedTo != null && !Hero.OneToOneConversationHero.IsPrisoner && Test.currentAssignment == Test.Assignment.Surgeon, delegate
		{
			if (Hero.OneToOneConversationHero.GetTraitLevel(DefaultTraits.Mercy) >= 0)
			{
				if (Hero.OneToOneConversationHero.IsWanderer)
				{
					Clan clan = Hero.OneToOneConversationHero.Clan;
					Hero.OneToOneConversationHero.CompanionOf = null;
					ChangeRelationAction.ApplyPlayerRelation(Hero.OneToOneConversationHero, 1, affectRelatives: false);
					Hero.OneToOneConversationHero.CompanionOf = clan;
				}
				else
				{
					ChangeRelationAction.ApplyPlayerRelation(Hero.OneToOneConversationHero, 1, affectRelatives: false);
				}
			}
			Hero.MainHero.AddSkillXp(DefaultSkills.Medicine, 3 * (Hero.OneToOneConversationHero.MaxHitPoints - Hero.OneToOneConversationHero.HitPoints));
			Hero.OneToOneConversationHero.HitPoints = Hero.OneToOneConversationHero.MaxHitPoints;
		});
		campaignStarter.AddDialogLine("sas_wanderer_surgeon_reponse_a", "sas_wanderer_surgeon", "close_window", "{=FLT0000245}You worthless layabout!  Where were you when - Never mind.  Hurry up and get it done already!", () => Hero.OneToOneConversationHero.GetTraitLevel(DefaultTraits.Mercy) < 0 && Hero.OneToOneConversationHero.GetRelationWithPlayer() < 10f, null);
		campaignStarter.AddDialogLine("sas_wanderer_surgeon_reponse_b", "sas_wanderer_surgeon", "close_window", "{=FLT0000246}Can you take look at my {BODY_PART}?.  It is in a lot of pain.", delegate
		{
			MBTextManager.SetTextVariable("BODY_PART", new List<TextObject>
			{
				new TextObject("{=FLT0000247}head"),
				new TextObject("{=FLT0000248}arm"),
				new TextObject("{=FLT0000249}neck"),
				new TextObject("{=FLT0000250}chest"),
				new TextObject("{=FLT0000251}leg"),
				new TextObject("{=FLT0000252}foot"),
				new TextObject("{=FLT0000253}hand"),
				new TextObject("{=FLT0000254}back")
			}.GetRandomElement());
			return true;
		}, null, 99);
		campaignStarter.AddPlayerLine("sas_prisoner_hire", "defeated_lord_answer", "sas_prisoner_hire_response", "{=FLT0000275}How would you like to work for me instead?  Maybe this bag of {GOLD}{COIN} will persuade you?", delegate
		{
			MBTextManager.SetTextVariable("GOLD", GetHireCost(CharacterObject.OneToOneConversationCharacter.HeroObject));
			MBTextManager.SetTextVariable("COIN", "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
			return Hero.OneToOneConversationHero.IsWanderer && Hero.OneToOneConversationHero.CompanionOf != null && (Test.followingHero == null || Test.EnlistTier >= 6) && Hero.OneToOneConversationHero.CompanionOf != Clan.PlayerClan && Hero.MainHero.Gold >= GetHireCost(CharacterObject.OneToOneConversationCharacter.HeroObject);
		}, delegate
		{
		});
		campaignStarter.AddDialogLine("sas_prisoner_hire_response_a", "sas_prisoner_hire_response", "close_window", "{=FLT0000276}How can I saw no to such a generous offer.  You have yourself a deal!", () => Hero.OneToOneConversationHero.GetRelation(Hero.OneToOneConversationHero.CompanionOf.Leader) < 50 || Hero.OneToOneConversationHero.GetRelation(Hero.MainHero) > Hero.OneToOneConversationHero.GetRelation(Hero.OneToOneConversationHero.CompanionOf.Leader), delegate
		{
			GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, Hero.OneToOneConversationHero, GetHireCost(CharacterObject.OneToOneConversationCharacter.HeroObject));
			Equipment firstBattleEquipment = CharacterObject.OneToOneConversationCharacter.FirstBattleEquipment;
			AddCompanionAction.Apply(Clan.PlayerClan, Hero.OneToOneConversationHero);
			AddHeroToPartyAction.Apply(Hero.OneToOneConversationHero, MobileParty.MainParty);
			Test.GiveStateIssueEquipment(Hero.OneToOneConversationHero, firstBattleEquipment);
		});
		campaignStarter.AddDialogLine("sas_prisoner_hire_response_b", "sas_prisoner_hire_response", "defeated_lord_answer", "{=FLT0000277}You can not buy my loyalty with gold!", () => Hero.OneToOneConversationHero.GetRelation(Hero.OneToOneConversationHero.CompanionOf.Leader) >= 50, delegate
		{
		}, 99);
	}

	public bool isDrunk(Hero Wander)
	{
		if (LastDrink.TryGetValue(Wander, out var time))
		{
			if (time.ElapsedDaysUntilNow <= 3f)
			{
				return true;
			}
			return false;
		}
		return false;
	}

	public int GetHireCost(Hero Wander)
	{
		if (Wander.PartyBelongedTo != null && Wander.PartyBelongedTo.LeaderHero != null)
		{
			return Wander.Level * 1000;
		}
		return Wander.Level * 300;
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_last_drink", ref LastDrink);
	}
}
