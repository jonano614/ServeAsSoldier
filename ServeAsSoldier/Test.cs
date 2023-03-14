using System;
using System.Collections.Generic;
using System.Reflection;
using Helpers;
using SandBox;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Arena;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using TaleWorlds.ObjectSystem;

namespace ServeAsSoldier;

public class Test : CampaignBehaviorBase
{
	public enum Assignment
	{
		None,
		Grunt_Work,
		Guard_Duty,
		Cook,
		Foraging,
		Surgeon,
		Engineer,
		Quartermaster,
		Scout,
		Sergeant,
		Strategist
	}

	public static Hero followingHero;

	public static CampaignTime enlistTime;

	public static bool disbandArmy;

	public static int EnlistTier;

	public static int xp;

	private static Dictionary<IFaction, int> FactionReputation = new Dictionary<IFaction, int>();

	public static Dictionary<CharacterObject, Equipment> CompanionOldGear = new Dictionary<CharacterObject, Equipment>();

	private static Dictionary<IFaction, int> retirementXP = new Dictionary<IFaction, int>();

	private static Dictionary<Hero, int> LordReputation = new Dictionary<Hero, int>();

	private static List<IFaction> kingVassalOffered = new List<IFaction>();

	public static ItemRoster oldItems = new ItemRoster();

	public static ItemRoster oldGear = new ItemRoster();

	public static ItemRoster tournamentPrizes = new ItemRoster();

	public static bool AllBattleCommands = false;

	public static bool disable_XP = false;

	public static string conversation_type = "";

	public static Assignment currentAssignment;

	private Settlement selected_selement;

	public static Settlement Tracked;

	public static Settlement Untracked;

	public static bool OngoinEvent = false;

	public static bool NoRetreat = false;

	public static bool waitingInReserve = false;

	private MobileParty CavalryDetachment;

	private int[] NextlevelXP = new int[8] { 0, 600, 1700, 3400, 6000, 9400, 14000, 20000 };

	public override void RegisterEvents()
	{
		if (SubModule.settings != null)
		{
			NextlevelXP[1] = SubModule.settings.Level1XP;
			NextlevelXP[2] = SubModule.settings.Level2XP;
			NextlevelXP[3] = SubModule.settings.Level3XP;
			NextlevelXP[4] = SubModule.settings.Level4XP;
			NextlevelXP[5] = SubModule.settings.Level5XP;
			NextlevelXP[6] = SubModule.settings.Level6XP;
			NextlevelXP[7] = SubModule.settings.Level7XP;
		}
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, Tick);
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, Tick2);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, TickDaily);
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
		CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeftEvent);
	}

	private void OnGameMenuOpened(MenuCallbackArgs args)
	{
		if (Campaign.Current.GameMenuManager.NextLocation == null && GameStateManager.Current.ActiveState is MapState)
		{
		}
	}

	private void OnSettlementLeftEvent(MobileParty party, Settlement settlement)
	{
		if (party.LeaderHero != null && party.LeaderHero == followingHero)
		{
			GameMenu.ActivateGameMenu("party_wait");
		}
	}

	private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
		if (followingHero != null && hero == followingHero)
		{
			GameMenu.ActivateGameMenu("party_wait");
			if (settlement.IsTown)
			{
				Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			}
		}
	}

	private void TickDaily()
	{
		if (followingHero != null && followingHero.IsAlive)
		{
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, wage());
			GiveGoldAction.ApplyBetweenCharacters(null, followingHero, wage());
			ChangeFactionRelation(followingHero.MapFaction, 10);
			int XPAmount = ((SubModule.settings == null) ? 10 : SubModule.settings.DailyXP);
			ChangeLordRelation(followingHero, XPAmount);
			xp += XPAmount;
			GetXPForRole();
		}
	}

	private void GetXPForRole()
	{
		SkillObject relaventSkill = null;
		switch (currentAssignment)
		{
		case Assignment.Grunt_Work:
			Hero.MainHero.AddSkillXp(DefaultSkills.Athletics, 100f);
			relaventSkill = DefaultSkills.Athletics;
			break;
		case Assignment.Guard_Duty:
			Hero.MainHero.AddSkillXp(DefaultSkills.Scouting, 100f);
			relaventSkill = DefaultSkills.Scouting;
			break;
		case Assignment.Foraging:
			Hero.MainHero.AddSkillXp(DefaultSkills.Riding, 100f);
			if (followingHero != null && followingHero.PartyBelongedTo != null)
			{
				followingHero.PartyBelongedTo.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("grain"), MBRandom.RandomInt(5));
			}
			relaventSkill = DefaultSkills.Riding;
			break;
		case Assignment.Cook:
			Hero.MainHero.AddSkillXp(DefaultSkills.Steward, 100f);
			relaventSkill = DefaultSkills.Steward;
			break;
		case Assignment.Sergeant:
			Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 100f);
			if (followingHero != null && followingHero.PartyBelongedTo != null)
			{
				AddXPToRandomTroop();
			}
			relaventSkill = DefaultSkills.Leadership;
			break;
		case Assignment.Engineer:
			relaventSkill = DefaultSkills.Engineering;
			break;
		case Assignment.Quartermaster:
			relaventSkill = DefaultSkills.Steward;
			break;
		case Assignment.Scout:
			relaventSkill = DefaultSkills.Scouting;
			break;
		case Assignment.Strategist:
			relaventSkill = DefaultSkills.Tactics;
			break;
		case Assignment.Surgeon:
			relaventSkill = DefaultSkills.Medicine;
			break;
		}
		if (relaventSkill != null && MBRandom.RandomInt(100) == 1)
		{
			if (Hero.MainHero.HeroDeveloper.GetFocus(relaventSkill) < 5)
			{
				Hero.MainHero.HeroDeveloper.AddFocus(relaventSkill, 1, checkUnspentFocusPoints: false);
				TextObject text2 = new TextObject("{=FLT0000223}One focus point gained in {SKILL} from daily assignment");
				text2.SetTextVariable("SKILL", relaventSkill.Name.ToString());
				MBInformationManager.AddQuickInformation(text2, 0, Hero.MainHero.CharacterObject);
				SoundEvent.PlaySound2D(SoundEvent.GetEventIdFromString("event:/ui/notification/levelup"));
			}
			else
			{
				Hero.MainHero.HeroDeveloper.AddAttribute(relaventSkill.CharacterAttribute, 1, checkUnspentPoints: false);
				TextObject text = new TextObject("{=FLT0000224}One attribute point gained in {ATTRIBUTE} from daily assignment");
				text.SetTextVariable("ATTRIBUTE", relaventSkill.CharacterAttribute.Name.ToString());
				MBInformationManager.AddQuickInformation(text, 0, Hero.MainHero.CharacterObject);
				SoundEvent.PlaySound2D(SoundEvent.GetEventIdFromString("event:/ui/notification/levelup"));
			}
		}
	}

	private void AddXPToRandomTroop()
	{
		List<CharacterObject> list = new List<CharacterObject>();
		foreach (TroopRosterElement troop in followingHero.PartyBelongedTo.MemberRoster.GetTroopRoster())
		{
			if (!troop.Character.IsHero && (troop.Character.UpgradeTargets == null || troop.Character.UpgradeTargets.Length == 0))
			{
				list.Add(troop.Character);
			}
		}
		if (list.Count > 0)
		{
			followingHero.PartyBelongedTo.MemberRoster.AddXpToTroop(500, list.GetRandomElement());
		}
	}

	private int wage()
	{
		return (int)(Math.Max(0f, (Hero.MainHero.GetPerkValue(DefaultPerks.Polearm.StandardBearer) ? 1.2f : 1f) * (float)Math.Min(Hero.MainHero.Level * 2 + xp / SubModule.settings.XPtoWageRatio, SubModule.settings.MaxWage)) + (float)MobileParty.MainParty.TotalWage);
	}

	private void Tick2()
	{
		if (followingHero != null && followingHero.PartyBelongedTo != null && (followingHero.PartyBelongedTo.MapEvent == null || (ContainsParty(followingHero.PartyBelongedTo.MapEvent.PartiesOnSide(BattleSideEnum.Attacker), followingHero.PartyBelongedTo) ? followingHero.PartyBelongedTo.MapEvent.AttackerSide.TroopCount : followingHero.PartyBelongedTo.MapEvent.DefenderSide.TroopCount) < 100))
		{
			GameMenu.ActivateGameMenu("party_wait");
		}
		if (followingHero != null && followingHero.IsAlive)
		{
			heal();
			bool leveledUp = false;
			while (EnlistTier < 7 && xp > NextlevelXP[EnlistTier])
			{
				EnlistTier++;
				leveledUp = true;
			}
			if (kingVassalOffered == null)
			{
				kingVassalOffered = new List<IFaction>();
			}
			if (retirementXP == null)
			{
				retirementXP = new Dictionary<IFaction, int>();
			}
			if (!retirementXP.ContainsKey(followingHero.MapFaction))
			{
				retirementXP.Add(followingHero.MapFaction, SubModule.settings.RetirementXP);
			}
			retirementXP.TryGetValue(followingHero.MapFaction, out var retirementXPNeeded);
			if (leveledUp)
			{
				conversation_type = "promotion";
				TextObject text = new TextObject("{=FLT0000000}{HERO} has been promoted to tier {TIER}");
				text.SetTextVariable("HERO", Hero.MainHero.Name.ToString());
				text.SetTextVariable("TIER", EnlistTier.ToString());
				MBInformationManager.AddQuickInformation(text, 0, Hero.MainHero.CharacterObject);
				Campaign.Current.ConversationManager.AddDialogFlow(CreatePromotionDialog());
				CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false, false), new ConversationCharacterData(followingHero.CharacterObject, (PartyBase)null, false, false, false, false, false));
			}
			else if (!kingVassalOffered.Contains(followingHero.MapFaction) && GetFactionRelations(followingHero.MapFaction) >= SubModule.settings.PromotionToVassalXP && !leveledUp)
			{
				if (followingHero.IsFactionLeader)
				{
					conversation_type = "vassalage2";
					Campaign.Current.ConversationManager.AddDialogFlow(KingdomJoinCreateDialog2());
					CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false, false), new ConversationCharacterData(followingHero.CharacterObject, (PartyBase)null, false, false, false, false, false));
				}
				else
				{
					conversation_type = "vassalage";
					Campaign.Current.ConversationManager.AddDialogFlow(KingdomJoinCreateDialog());
					CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false, false), new ConversationCharacterData(followingHero.CharacterObject, (PartyBase)null, false, false, false, false, false));
				}
				kingVassalOffered.Add(followingHero.MapFaction);
			}
			else if (xp > retirementXPNeeded)
			{
				conversation_type = "retirement";
				Campaign.Current.ConversationManager.AddDialogFlow(RetirementCreateDialog());
				CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false, false), new ConversationCharacterData(followingHero.CharacterObject, (PartyBase)null, false, false, false, false, false));
			}
		}
		else
		{
			Tracked = null;
			Untracked = null;
		}
	}

	public static bool ContainsParty(IReadOnlyList<MapEventParty> parties, MobileParty party)
	{
		foreach (MapEventParty p in parties)
		{
			if (p.Party.MobileParty == party)
			{
				return true;
			}
		}
		return false;
	}

	private void heal()
	{
		if (followingHero.CurrentSettlement != null)
		{
			foreach (TroopRosterElement troop in MobileParty.MainParty.MemberRoster.GetTroopRoster())
			{
				if (troop.Character.IsHero)
				{
					troop.Character.HeroObject.Heal(3 * healAmount(), addXp: true);
				}
			}
		}
		else
		{
			foreach (TroopRosterElement troop2 in MobileParty.MainParty.MemberRoster.GetTroopRoster())
			{
				if (troop2.Character.IsHero)
				{
					troop2.Character.HeroObject.Heal(healAmount(), addXp: true);
				}
			}
		}
		if (MobileParty.MainParty.MemberRoster.TotalWoundedRegulars <= 0 || MBRandom.RandomInt(100) < 90)
		{
			return;
		}
		List<TroopRosterElement> list = new List<TroopRosterElement>();
		foreach (TroopRosterElement troop3 in MobileParty.MainParty.MemberRoster.GetTroopRoster())
		{
			if (!troop3.Character.IsHero && troop3.WoundedNumber > 0)
			{
				list.Add(troop3);
			}
		}
		if (list.Count > 0)
		{
			TroopRosterElement randomTroop = list.GetRandomElement();
			InformationManager.DisplayMessage(new InformationMessage(randomTroop.Character.Name.ToString()));
			MobileParty.MainParty.MemberRoster.WoundTroop(randomTroop.Character, -1);
			SkillLevelingManager.OnRegularTroopHealedWhileWaiting(MobileParty.MainParty, 1, list.GetRandomElement().Character.Tier);
		}
	}

	private DialogFlow RetirementCreateDialog()
	{
		TextObject textObject = new TextObject("{=FLT0000001}{HERO}, you have served long enough to fulfill your enlistment.  You can honorably retire and keep your gear, but I have need for talented soldiers.  I will offer you a bonus of 25000 {COIN} if you are willing to re-enlist.");
		TextObject textObject2 = new TextObject("{=FLT0000002}I will re-enlist.");
		TextObject textObject3 = new TextObject("{=FLT0000003}I will retire.");
		textObject.SetTextVariable("COIN", "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
		return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject).Condition(delegate
		{
			if (followingHero != null)
			{
				textObject.SetTextVariable("HERO", Hero.MainHero.EncyclopediaLinkWithName);
			}
			return Hero.OneToOneConversationHero == followingHero && conversation_type == "retirement";
		})
			.Consequence(delegate
			{
				conversation_type = null;
			})
			.BeginPlayerOptions()
			.PlayerOption(textObject2)
			.Consequence(delegate
			{
				ChangeRelationAction.ApplyPlayerRelation(followingHero, 20);
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 25000);
				retirementXP.Remove(followingHero.MapFaction);
				retirementXP.Add(followingHero.MapFaction, xp + 5000);
			})
			.CloseDialog()
			.PlayerOption(textObject3)
			.Consequence(delegate
			{
				ChangeFactionRelation(followingHero.MapFaction, -100000);
				foreach (Clan current in followingHero.Clan.Kingdom.Clans)
				{
					if (!current.IsUnderMercenaryService)
					{
						foreach (Hero current2 in current.Heroes)
						{
							if (current2.IsLord)
							{
								ChangeLordRelation(current2, -100000);
							}
						}
					}
				}
				ChangeRelationAction.ApplyPlayerRelation(followingHero, 20);
				while (Campaign.Current.CurrentMenuContext != null)
				{
					GameMenu.ExitToLast();
				}
				if (retirementXP.ContainsKey(followingHero.MapFaction))
				{
					retirementXP.Remove(followingHero.MapFaction);
				}
				LeaveLordPartyAction(keepgear: true);
			})
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog();
	}

	private int healAmount()
	{
		return 1 + Hero.MainHero.GetSkillValue(DefaultSkills.Medicine) / 100 + ((MBRandom.RandomInt(100) < Hero.MainHero.GetSkillValue(DefaultSkills.Medicine) % 100) ? 1 : 0);
	}

	private DialogFlow KingdomJoinCreateDialog2()
	{
		TextObject textObject = new TextObject("{=FLT0000004}{HERO}, you have shown yourself to be a warrior with no equal.  Hundreds of my enemies have died by your hands.  I need people like you in my kingdom.  I am willing to make you a vassal of my realm.  I will give you a generous bonus if you agree.");
		TextObject textObject2 = new TextObject("{=FLT0000005}I will grant you the settlement of {FIEF} as your personal fief as a reward for your service.");
		TextObject textObject3 = new TextObject("{=FLT0000006}My place is as a soldier on the battlefield.");
		TextObject textObject4 = new TextObject("{=FLT0000007}Talk to me again if you change your mind.");
		TextObject textObject5 = new TextObject("{=FLT0000008}It would be an honor to serve you my liege.");
		TextObject textObject6 = new TextObject("{=FLT0000009}I will grant you a sum of 500000 {COIN} as a reward for your service.");
		textObject6.SetTextVariable("COIN", "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
		return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject).Condition(delegate
		{
			textObject.SetTextVariable("HERO", Hero.MainHero.EncyclopediaLinkWithName);
			return conversation_type == "vassalage2";
		})
			.Consequence(delegate
			{
				conversation_type = null;
			})
			.BeginPlayerOptions()
			.PlayerOption(textObject3)
			.NpcLine(textObject4)
			.CloseDialog()
			.PlayerOption(textObject5)
			.Consequence(delegate
			{
				while (Campaign.Current.CurrentMenuContext != null)
				{
					GameMenu.ExitToLast();
				}
				LeaveLordPartyAction(keepgear: true);
			})
			.BeginNpcOptions()
			.NpcOption(textObject2, delegate
			{
				selected_selement = null;
				List<Settlement> list = new List<Settlement>();
				foreach (Settlement current in Hero.OneToOneConversationHero.Clan.Kingdom.Settlements)
				{
					if (current.IsTown)
					{
						list.Add(current);
					}
				}
				if (list.Count < 1)
				{
					foreach (Settlement current2 in Hero.OneToOneConversationHero.Clan.Kingdom.Settlements)
					{
						if (current2.IsCastle)
						{
							list.Add(current2);
						}
					}
				}
				if (list.Count > 0)
				{
					selected_selement = list.GetRandomElement();
					textObject2.SetTextVariable("FIEF", selected_selement.EncyclopediaLinkWithName);
				}
				return selected_selement != null;
			})
			.Consequence(delegate
			{
				ChangeKingdomAction.ApplyByJoinToKingdom(Hero.MainHero.Clan, Hero.OneToOneConversationHero.Clan.Kingdom);
				ChangeOwnerOfSettlementAction.ApplyByGift(selected_selement, Hero.MainHero);
				kingVassalOffered.Remove(Hero.OneToOneConversationHero.MapFaction);
			})
			.CloseDialog()
			.NpcOption(textObject6, () => true)
			.Consequence(delegate
			{
				ChangeKingdomAction.ApplyByJoinToKingdom(Hero.MainHero.Clan, Hero.OneToOneConversationHero.Clan.Kingdom);
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 500000);
				kingVassalOffered.Remove(Hero.OneToOneConversationHero.MapFaction);
			})
			.CloseDialog()
			.EndNpcOptions()
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog();
	}

	private DialogFlow KingdomJoinCreateDialog()
	{
		TextObject textObject = new TextObject("{=FLT0000010}{HERO}, I received a message from {KING}, the leader of our kingdom.  {KING_GENDER_PRONOUN} would like to speak with you personally about offering you a lordship in our kingdom.  You have the permission to leave my warband.");
		TextObject textObject2 = new TextObject("{=FLT0000011}I would rather stay here.");
		TextObject textObject3 = new TextObject("{=FLT0000012}I will take my leave then.");
		return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject).Condition(delegate
		{
			if (followingHero != null)
			{
				textObject.SetTextVariable("HERO", Hero.MainHero.EncyclopediaLinkWithName);
				textObject.SetTextVariable("KING", followingHero.Clan.Kingdom.Leader.EncyclopediaLinkWithName);
				textObject.SetTextVariable("KING_GENDER_PRONOUN", followingHero.Clan.Kingdom.Leader.IsFemale ? "She" : "He");
			}
			return conversation_type == "vassalage";
		})
			.Consequence(delegate
			{
				conversation_type = null;
			})
			.BeginPlayerOptions()
			.PlayerOption(textObject2)
			.CloseDialog()
			.PlayerOption(textObject3)
			.Consequence(delegate
			{
				while (Campaign.Current.CurrentMenuContext != null)
				{
					GameMenu.ExitToLast();
				}
				LeaveLordPartyAction(keepgear: true);
			})
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog();
	}

	private DialogFlow CreatePromotionDialog()
	{
		TextObject textObject = new TextObject("{=FLT0000013}{HERO}, you have proven yourself to be a fine warrior.  For your bravery and loyalty, I have decided to give you a promotion.  Visit my bladesmith and armourer in the camp and they will provide you with the gear befitting your new rank.");
		TextObject textObject3 = new TextObject("{=FLT0000193}{HERO}, you have proven yourself to be a fine warrior.  For your bravery and loyalty, I have decided to give you a promotion.  You can serve as one of my commanders and you have the privilege of assembling your own retinue of companions and kinsmen while serving under me.  I will pay for their wages as well.");
		TextObject textObject2 = new TextObject("{=FLT0000014}It is an honor my lord.");
		return DialogFlow.CreateDialogFlow("start", 125).NpcLine((EnlistTier == 6) ? textObject3 : textObject).Condition(delegate
		{
			textObject.SetTextVariable("HERO", Hero.MainHero.EncyclopediaLinkWithName);
			textObject3.SetTextVariable("HERO", Hero.MainHero.EncyclopediaLinkWithName);
			return conversation_type == "promotion";
		})
			.Consequence(delegate
			{
				conversation_type = null;
				if (followingHero != null)
				{
					ChangeRelationAction.ApplyPlayerRelation(followingHero, 2 * EnlistTier);
				}
				Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			})
			.BeginPlayerOptions()
			.PlayerOption(textObject2)
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog();
	}

	private void Tick(float f)
	{
		if (followingHero != null && followingHero.IsAlive)
		{
			if (followingHero.PartyBelongedTo != null && !followingHero.IsPrisoner && !Hero.MainHero.IsPrisoner && followingHero.IsAlive)
			{
				if (CavalryDetachment != null && CavalryDetachment.MapEvent == null)
				{
					foreach (TroopRosterElement troop in CavalryDetachment.MemberRoster.GetTroopRoster())
					{
						followingHero.PartyBelongedTo.MemberRoster.AddToCounts(troop.Character, troop.Number);
					}
					DestroyPartyAction.Apply(followingHero.PartyBelongedTo.Party, CavalryDetachment);
					CavalryDetachment = null;
				}
				if (disbandArmy && followingHero.PartyBelongedTo.Army != null && followingHero.PartyBelongedTo.MapEvent == null)
				{
					followingHero.PartyBelongedTo.Army.DisperseArmy(Army.ArmyDispersionReason.Unknown);
					disbandArmy = false;
				}
				if (MobileParty.MainParty.Army != null && followingHero.PartyBelongedTo.MapEvent == null)
				{
					MobileParty.MainParty.Army = null;
				}
				if (Campaign.Current.CurrentMenuContext == null)
				{
					GameMenu.ActivateGameMenu("party_wait");
				}
				if (MobileParty.MainParty.CurrentSettlement != null)
				{
					LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
				}
				UpdateDiplomacy();
				MobileParty.MainParty.Position2D = followingHero.PartyBelongedTo.Position2D;
				PartyBase.MainParty.SetAsCameraFollowParty();
				hidePlayerParty();
				MobileParty.MainParty.IsActive = false;
				disable_XP = false;
				NoRetreat = false;
				if (followingHero.PartyBelongedTo.DefaultBehavior == AiBehavior.BesiegeSettlement && followingHero.PartyBelongedTo.Army == null)
				{
					Settlement target = followingHero.PartyBelongedTo.TargetSettlement;
					followingHero.PartyBelongedTo.ActualClan.Kingdom.CreateArmy(followingHero.PartyBelongedTo.LeaderHero, Hero.MainHero.HomeSettlement, Army.ArmyTypes.Patrolling);
					if (target != null)
					{
						followingHero.PartyBelongedTo.SetMoveBesiegeSettlement(target);
					}
					disbandArmy = false;
				}
				if (followingHero.PartyBelongedTo.MapEvent != null && MobileParty.MainParty.MapEvent == null && !waitingInReserve)
				{
					if (followingHero.PartyBelongedTo.ActualClan.Kingdom == null)
					{
						LeaveLordPartyAction(keepgear: false);
						TextObject textObject = new TextObject("{=FLT0000231}Enlistment cancled due to lord's clan abandoning kingdom");
						if (CavalryDetachment != null)
						{
							DestroyPartyAction.Apply(PartyBase.MainParty, CavalryDetachment);
							return;
						}
					}
					while (Campaign.Current.CurrentMenuContext != null)
					{
						GameMenu.ExitToLast();
					}
					if (followingHero.PartyBelongedTo.Army == null)
					{
						followingHero.PartyBelongedTo.ActualClan.Kingdom.CreateArmy(followingHero.PartyBelongedTo.LeaderHero, Hero.MainHero.HomeSettlement, Army.ArmyTypes.Patrolling);
						disbandArmy = true;
					}
					else if (followingHero.PartyBelongedTo.AttachedTo == null && followingHero.PartyBelongedTo.Army != null && followingHero.PartyBelongedTo != followingHero.PartyBelongedTo.Army.LeaderParty)
					{
						followingHero.PartyBelongedTo.Army = null;
						while (Campaign.Current.CurrentMenuContext != null)
						{
							GameMenu.ExitToLast();
						}
						followingHero.PartyBelongedTo.ActualClan.Kingdom.CreateArmy(followingHero.PartyBelongedTo.LeaderHero, Hero.MainHero.HomeSettlement, Army.ArmyTypes.Patrolling);
						disbandArmy = true;
					}
					if (followingHero.PartyBelongedTo.Army == null)
					{
						Army army = new Army(followingHero.PartyBelongedTo.ActualClan.Kingdom, followingHero.PartyBelongedTo, Army.ArmyTypes.Patrolling)
						{
							AIBehavior = Army.AIBehaviorFlags.Gathering
						};
						army.Gather(Hero.MainHero.HomeSettlement);
						CampaignEventDispatcher.Instance.OnArmyCreated(army);
					}
					followingHero.PartyBelongedTo.Army.AddPartyToMergedParties(MobileParty.MainParty);
					MobileParty.MainParty.Army = followingHero.PartyBelongedTo.Army;
					MobileParty.MainParty.IsActive = true;
					MobileParty.MainParty.SetMoveEngageParty(followingHero.PartyBelongedTo);
					if (followingHero != null && followingHero.PartyBelongedTo != null && followingHero.PartyBelongedTo.MapEvent != null && !followingHero.PartyBelongedTo.MapEvent.IsSiegeAssault)
					{
						AddNearbyParties();
					}
				}
				else if (waitingInReserve)
				{
					while (Campaign.Current.CurrentMenuContext != null)
					{
						GameMenu.ExitToLast();
					}
					GameMenu.ActivateGameMenu("battle_wait");
				}
			}
			else
			{
				LeaveLordPartyAction(keepgear: false);
				if (CavalryDetachment != null)
				{
					DestroyPartyAction.Apply(PartyBase.MainParty, CavalryDetachment);
				}
			}
		}
		else if (!MobileParty.MainParty.IsActive && !Hero.MainHero.IsPrisoner)
		{
			MobileParty.MainParty.IsActive = true;
			showPlayerParty();
			while (Campaign.Current.CurrentMenuContext != null)
			{
				GameMenu.ExitToLast();
			}
		}
		if (Hero.MainHero.IsPrisoner && MobileParty.MainParty != null)
		{
			MobileParty.MainParty.IgnoreByOtherPartiesTill(CampaignTime.DaysFromNow(1f));
		}
	}

	private void AddNearbyParties()
	{
		MobileParty[] nearbyParties = MapEvent.GetNearbyFreeParties(followingHero.PartyBelongedTo.Position2D);
		MobileParty[] array = nearbyParties;
		foreach (MobileParty nearbyParty in array)
		{
			if (!nearbyParty.ShouldBeIgnored && (!nearbyParty.MapFaction.IsAtWarWith(followingHero.MapFaction) || !nearbyParty.MapFaction.IsAtWarWith(followingHero.PartyBelongedTo.MapEventSide.OtherSide.LeaderParty.MapFaction)))
			{
				if (nearbyParty.MapFaction.IsAtWarWith(followingHero.MapFaction))
				{
					List<MobileParty> joinParties2 = (List<MobileParty>)typeof(MapEventSide).GetField("_nearbyPartiesAddedToPlayerMapEvent", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(followingHero.PartyBelongedTo.MapEventSide.OtherSide);
					joinParties2.Add(nearbyParty);
				}
				else
				{
					List<MobileParty> joinParties = (List<MobileParty>)typeof(MapEventSide).GetField("_nearbyPartiesAddedToPlayerMapEvent", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(followingHero.PartyBelongedTo.MapEventSide);
					joinParties.Add(nearbyParty);
				}
			}
		}
	}

	public static void LeaveLordPartyAction(bool keepgear)
	{
		MobileParty.MainParty.IsActive = true;
		UndoDiplomacy();
		showPlayerParty();
		followingHero = null;
		if (!keepgear)
		{
			resetCompanionGear();
			GetOldGear();
			equipGear();
			GetTournamentPrizes();
		}
		if (PlayerEncounter.Current != null)
		{
			PlayerEncounter.Finish();
		}
		TransferAllItems(oldItems, MobileParty.MainParty.ItemRoster);
	}

	private static void resetCompanionGear()
	{
		foreach (KeyValuePair<CharacterObject, Equipment> pair in CompanionOldGear)
		{
			CharacterObject character = pair.Key;
			Equipment equipment = pair.Value;
			character.FirstBattleEquipment.FillFrom(equipment);
		}
	}

	private static void GetTournamentPrizes()
	{
		if (tournamentPrizes == null)
		{
			tournamentPrizes = new ItemRoster();
		}
		foreach (ItemRosterElement item in tournamentPrizes)
		{
			MobileParty.MainParty.ItemRoster.AddToCounts(item.EquipmentElement, item.Amount);
		}
	}

	private void OnSessionLaunched(CampaignGameStarter campaignStarter)
	{
		if (kingVassalOffered == null)
		{
			kingVassalOffered = new List<IFaction>();
		}
		TextObject textObject1 = new TextObject("{=FLT0000015}I want to serve in your warband as a soldier.");
		campaignStarter.AddPlayerLine("join_legions_start", "lord_talk_speak_diplomacy_2", "join_legions_response", textObject1.ToString(), () => CharacterObject.OneToOneConversationCharacter.HeroObject != null && CharacterObject.OneToOneConversationCharacter.HeroObject.PartyBelongedTo != null && CharacterObject.OneToOneConversationCharacter.HeroObject.PartyBelongedTo.LeaderHero == CharacterObject.OneToOneConversationCharacter.HeroObject && followingHero == null && !CharacterObject.OneToOneConversationCharacter.HeroObject.Clan.IsMinorFaction && Hero.MainHero.Clan.Kingdom == null && CharacterObject.OneToOneConversationCharacter.HeroObject.Clan.Kingdom != null, null);
		TextObject textObject12 = new TextObject("{=FLT0000016}There is no way I would let a wanted criminal like you join my ranks.");
		campaignStarter.AddDialogLine("join_legions_response_a", "join_legions_response", "lord_pretalk", textObject12.ToString(), () => CharacterObject.OneToOneConversationCharacter.HeroObject.MapFaction.MainHeroCrimeRating > 30f, null);
		TextObject textObject20 = new TextObject("{=FLT0000017}I don't want you in my warband. You and I don't get along.");
		campaignStarter.AddDialogLine("join_legions_response_b", "join_legions_response", "lord_pretalk", textObject20.ToString(), () => CharacterObject.OneToOneConversationCharacter.HeroObject.GetRelationWithPlayer() <= -10f, null);
		TextObject textObject21 = new TextObject("{=FLT0000018}About that lordship you offered me...");
		campaignStarter.AddPlayerLine("join_faction_start", "lord_talk_speak_diplomacy_2", "join_faction_response", textObject21.ToString(), () => CharacterObject.OneToOneConversationCharacter.HeroObject != null && !CharacterObject.OneToOneConversationCharacter.HeroObject.Clan.IsMinorFaction && Hero.MainHero.Clan.Kingdom == null && CharacterObject.OneToOneConversationCharacter.HeroObject.IsFactionLeader && kingVassalOffered.Contains(CharacterObject.OneToOneConversationCharacter.HeroObject.MapFaction), null);
		TextObject textObject22 = new TextObject("{=FLT0000019}{HERO}, you have shown yourself to be a warrior with no equal.  Hundreds of my enemies have died by your hands.  I need people like you in my kingdom.");
		textObject22.SetTextVariable("HERO", Hero.MainHero.EncyclopediaLinkWithName);
		campaignStarter.AddDialogLine("join_faction_lord_response", "join_faction_response", "join_faction_response_2", textObject22.ToString(), () => true, null);
		TextObject textObject23 = new TextObject("{=FLT0000020}It would be an honor to serve you my liege.");
		campaignStarter.AddPlayerLine("join_faction_player_response_a", "join_faction_response_2", "join_faction_response_3", textObject23.ToString(), () => true, null);
		TextObject textObject24 = new TextObject("{=FLT0000021}I changed my mind.");
		campaignStarter.AddPlayerLine("join_faction_player_response_b", "join_faction_response_2", "lord_pretalk", textObject24.ToString(), () => true, null);
		TextObject textObject25 = new TextObject("{=FLT0000022}I will grant you the settlement of ");
		TextObject textObject26 = new TextObject("{=FLT0000023} as your personal fief as a reward for your service.");
		campaignStarter.AddDialogLine("join_faction_lord_response_2a", "join_faction_response_3", "lord_pretalk", textObject25.ToString() + "{FIEF}" + textObject26.ToString(), delegate
		{
			selected_selement = null;
			List<Settlement> list = new List<Settlement>();
			foreach (Settlement current4 in Hero.OneToOneConversationHero.Clan.Kingdom.Settlements)
			{
				if (current4.IsTown)
				{
					list.Add(current4);
				}
			}
			if (list.Count < 1)
			{
				foreach (Settlement current5 in Hero.OneToOneConversationHero.Clan.Kingdom.Settlements)
				{
					if (current5.IsCastle)
					{
						list.Add(current5);
					}
				}
			}
			if (list.Count > 0)
			{
				selected_selement = list.GetRandomElement();
				MBTextManager.SetTextVariable("FIEF", selected_selement.EncyclopediaLinkWithName);
			}
			return selected_selement != null;
		}, delegate
		{
			ChangeKingdomAction.ApplyByJoinToKingdom(Hero.MainHero.Clan, Hero.OneToOneConversationHero.Clan.Kingdom);
			ChangeOwnerOfSettlementAction.ApplyByGift(selected_selement, Hero.MainHero);
			kingVassalOffered.Remove(Hero.OneToOneConversationHero.MapFaction);
		});
		TextObject textObject2 = new TextObject("{=FLT0000024}I will grant you a sum of 500000 {COIN} as a reward for your service.");
		textObject2.SetTextVariable("COIN", "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
		campaignStarter.AddDialogLine("join_faction_lord_response_2b", "join_faction_response_3", "lord_pretalk", textObject2.ToString(), () => true, delegate
		{
			ChangeKingdomAction.ApplyByJoinToKingdom(Hero.MainHero.Clan, Hero.OneToOneConversationHero.Clan.Kingdom);
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 500000);
			kingVassalOffered.Remove(Hero.OneToOneConversationHero.MapFaction);
		});
		TextObject textObject3 = new TextObject("{=FLT0000025}Sure, you may join.");
		campaignStarter.AddDialogLine("join_legions_response_c", "join_legions_response", "lord_pretalk", textObject3.ToString(), () => true, delegate
		{
			JoinPartyAction();
		});
		campaignStarter.AddWaitGameMenu("party_wait", "Party Leader: {PARTY_LEADER}\n{PARTY_TEXT}", wait_on_init, wait_on_condition, null, wait_on_tick, GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);
		TextObject textObject4 = new TextObject("{=FLT0000027}Visit Weaponsmith");
		campaignStarter.AddGameMenuOption("party_wait", "party_wait_change_equipment", textObject4.ToString(), (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.DefendAction;
			return true;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			SwitchGear();
		}, true, -1, false);
		TextObject textObject5 = new TextObject("{=FLT0000028}Train with the troops");
		campaignStarter.AddGameMenuOption("party_wait", "party_wait_train", textObject5.ToString(), (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
			if (followingHero != null && followingHero.PartyBelongedTo.MemberRoster.TotalHealthyCount < 11)
			{
				args.Tooltip = new TextObject("{=FLT0000029}Not enough men or uninjured men in lord's party");
				args.IsEnabled = false;
			}
			return followingHero != null && followingHero.CurrentSettlement != null && followingHero.CurrentSettlement.IsTown && !followingHero.CurrentSettlement.Town.HasTournament;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			EnterSettlementAction.ApplyForParty(MobileParty.MainParty, followingHero.CurrentSettlement);
			MobileParty.MainParty.IsActive = true;
			disable_XP = true;
			string sceneName = followingHero.CurrentSettlement.LocationComplex.GetLocationWithId("arena").GetSceneName(followingHero.CurrentSettlement.Town.GetWallLevel());
			Location location = followingHero.CurrentSettlement.LocationComplex.GetLocationWithId("arena");
			MissionState.OpenNew("ArenaDuelMission", SandBoxMissions.CreateSandBoxMissionInitializerRecord(sceneName, "", doNotUseLoadingScreen: false, DecalAtlasGroup.Town), (Mission mission) => new MissionBehavior[7]
			{
				new MissionOptionsComponent(),
				new CustomArenaBattleMissionController(followingHero.PartyBelongedTo.MemberRoster),
				new MissionFacialAnimationHandler(),
				new AgentHumanAILogic(),
				new ArenaAgentStateDeciderLogic(),
				new CampaignMissionComponent(),
				new MissionAgentHandler(location)
			}, addDefaultMissionBehaviors: true, needsMemoryCleanup: false);
		}, true, -1, false);
		TextObject textObject6 = new TextObject("{=FLT0000030}Battle Commands : All");
		campaignStarter.AddGameMenuOption("party_wait", "party_wait_battle_commands_on", textObject6.ToString(), (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.Tooltip = new TextObject("{=FLT0000031}Commands for all formations will be shouted during battle\nClick to toggle");
			args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
			return AllBattleCommands;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			AllBattleCommands = false;
			GameMenu.ActivateGameMenu("party_wait");
		}, true, -1, false);
		TextObject textObject7 = new TextObject("{=FLT0000032}Battle Commands : Player Formation Only");
		campaignStarter.AddGameMenuOption("party_wait", "party_wait_battle_commands_on", textObject7.ToString(), (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.Tooltip = new TextObject("{=FLT0000033}Commands for only the player's formation will be shouted during battle\nClick to toggle");
			args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
			return !AllBattleCommands;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			AllBattleCommands = true;
			GameMenu.ActivateGameMenu("party_wait");
		}, true, -1, false);
		TextObject textObject8 = new TextObject("{=FLT0000034}Participate in tournament");
		campaignStarter.AddGameMenuOption("party_wait", "party_wait_tournament", textObject8.ToString(), (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Mission;
			return followingHero != null && followingHero.CurrentSettlement != null && followingHero.CurrentSettlement.IsTown && followingHero.CurrentSettlement.Town.HasTournament;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			disable_XP = true;
			EnterSettlementAction.ApplyForParty(MobileParty.MainParty, followingHero.CurrentSettlement);
			MobileParty.MainParty.IsActive = true;
			TournamentGame tournamentGame = Campaign.Current.TournamentManager.GetTournamentGame(followingHero.CurrentSettlement.Town);
			int upgradeLevel = ((!followingHero.CurrentSettlement.IsTown) ? 1 : followingHero.CurrentSettlement.Town.GetWallLevel());
			string scene = followingHero.CurrentSettlement.LocationComplex.GetScene("arena", upgradeLevel);
			SandBoxMission.OpenTournamentFightMission(scene, tournamentGame, followingHero.CurrentSettlement, followingHero.CurrentSettlement.Culture, isPlayerParticipating: true);
			Campaign.Current.TournamentManager.OnPlayerJoinTournament(tournamentGame.GetType(), followingHero.CurrentSettlement);
			GameMenu.ActivateGameMenu("party_wait");
		}, true, -1, false);
		campaignStarter.AddGameMenuOption("party_wait", "party_wait_talk_to", "{=yYTotiqW}Talk to...", (GameMenuOption.OnConditionDelegate)((GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
			return true;
		}).Invoke, (GameMenuOption.OnConsequenceDelegate)((GameMenuOption.OnConsequenceDelegate)delegate
		{
			GameMenu.SwitchToMenu("party_wait_talk_to_other_members");
		}).Invoke, false, -1, false);
		campaignStarter.AddGameMenu("party_wait_talk_to_other_members", "{=yYTotiqW}Talk to...", party_wait_talk_to_other_members_on_init);
		campaignStarter.AddGameMenuOption("party_wait_talk_to_other_members", "party_wait_talk_to_other_members_item", "{=!}{CHAR_NAME}", (GameMenuOption.OnConditionDelegate)party_wait_talk_to_other_members_item_on_condition, (GameMenuOption.OnConsequenceDelegate)party_wait_talk_to_other_members_item_on_consequence, false, -1, true);
		campaignStarter.AddGameMenuOption("party_wait_talk_to_other_members", "party_wait_talk_to_other_members_back", GameTexts.FindText("str_back").ToString(), (GameMenuOption.OnConditionDelegate)((GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}).Invoke, (GameMenuOption.OnConsequenceDelegate)((GameMenuOption.OnConsequenceDelegate)delegate
		{
			GameMenu.ActivateGameMenu("party_wait");
		}).Invoke, true, -1, false);
		TextObject textObject9 = new TextObject("{=FLT0000035}Show reputation with factions");
		campaignStarter.AddGameMenuOption("party_wait", "party_wait_reputation", textObject9.ToString(), (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
			return true;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			GameMenu.SwitchToMenu("faction_reputation");
		}, true, -1, false);
		TextObject textObject10 = new TextObject("{=FLT0000036}Ask commander for leave");
		campaignStarter.AddGameMenuOption("party_wait", "party_wait_ask_leave", textObject10.ToString(), (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.LeaveTroopsAndFlee;
			return true;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			LeaveLordPartyAction(keepgear: false);
			GameMenu.ExitToLast();
		}, true, -1, false);
		campaignStarter.AddGameMenuOption("party_wait", "party_wait_visit_tavern", "Go to the tavern", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
			return followingHero != null && followingHero.PartyBelongedTo != null && followingHero.PartyBelongedTo.CurrentSettlement != null && followingHero.PartyBelongedTo.CurrentSettlement.IsTown;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			EnterSettlementAction.ApplyForParty(MobileParty.MainParty, followingHero.PartyBelongedTo.CurrentSettlement);
			GameMenu.SwitchToMenu("town_backstreet");
		}, true, -1, false);
		campaignStarter.AddGameMenuOption("town_backstreet", "party_wait_return_tavern", "return to army camp", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return followingHero != null;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			GameMenu.ActivateGameMenu("party_wait");
		}, true, -1, false);
		campaignStarter.AddGameMenuOption("village_wait_menus", "party_wait_return_village_wait", "return to army camp", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return followingHero != null;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			GameMenu.ActivateGameMenu("party_wait");
		}, true, -1, false);
		campaignStarter.AddGameMenuOption("village_looted", "party_wait_return_village_looted", "return to army camp", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return followingHero != null;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			GameMenu.ActivateGameMenu("party_wait");
		}, true, -1, false);
		campaignStarter.AddGameMenuOption("castle_dungeon", "party_wait_return_castle_dungeon", "return to army camp", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return followingHero != null;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			GameMenu.ActivateGameMenu("party_wait");
		}, true, -1, false);
		campaignStarter.AddGameMenuOption("town_wait_menus", "party_wait_return_town_wait", "return to army camp", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return followingHero != null;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			GameMenu.ActivateGameMenu("party_wait");
		}, true, -1, false);
		campaignStarter.AddGameMenuOption("town_arena", "party_wait_return_town_arena", "return to army camp", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return followingHero != null;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			GameMenu.ActivateGameMenu("party_wait");
		}, true, -1, false);
		campaignStarter.AddGameMenuOption("town_enemy_town_keep", "party_wait_return_town_enemy_town_keep", "return to army camp", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return followingHero != null;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			GameMenu.ActivateGameMenu("party_wait");
		}, true, -1, false);
		campaignStarter.AddGameMenuOption("town_keep_bribe", "party_wait_return_town_keep_bribe", "return to army camp", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return followingHero != null;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			GameMenu.ActivateGameMenu("party_wait");
		}, true, -1, false);
		campaignStarter.AddGameMenuOption("town_keep_dungeon", "party_wait_return_town_keep_dungeon", "return to army camp", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return followingHero != null;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			GameMenu.ActivateGameMenu("party_wait");
		}, true, -1, false);
		campaignStarter.AddGameMenuOption("town_keep", "party_wait_return_town_keep", "return to army camp", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return followingHero != null;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			GameMenu.ActivateGameMenu("party_wait");
		}, true, -1, false);
		TextObject textObject11 = new TextObject("{=FLT0000037}Ask for a different assignment");
		campaignStarter.AddGameMenuOption("party_wait", "party_wait_ask_assignment", textObject11.ToString(), (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			conversation_type = "ask_assignment";
			Campaign.Current.ConversationManager.AddDialogFlow(CreateAskAssignmentDialog());
			CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false, false), new ConversationCharacterData(followingHero.CharacterObject, (PartyBase)null, false, false, false, false, false));
		}, true, -1, false);
		TextObject textObject13 = new TextObject("{=FLT0000038}Lure bandits into ambush");
		campaignStarter.AddGameMenuOption("party_wait", "party_wait_attack", textObject13.ToString(), (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
			args.Tooltip = new TextObject("{=FLT0000039}A small group of bandits is way too nimble to catch normally.  The only way to catch them is to trick them into attacking, although there is a chance things could go wrong.");
			if (!Hero.MainHero.CharacterObject.IsMounted)
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("{=FLT0000040}You need to be mounted to do this");
			}
			if (Hero.MainHero.IsWounded)
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("{=FLT0000041}You are wounded");
			}
			return nearbyBandit().Count > 0;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			if (nearbyBandit().Count == 0)
			{
				TextObject textObject34 = new TextObject("{=FLT0000042}The bandits did not fall for your trap.");
				InformationManager.DisplayMessage(new InformationMessage(textObject34.ToString()));
				GameMenu.ActivateGameMenu("party_wait");
			}
			else
			{
				MobileParty randomElement3 = nearbyBandit().GetRandomElement();
				randomElement3.SetMoveEngageParty(followingHero.PartyBelongedTo);
				randomElement3.Ai.SetDoNotMakeNewDecisions(true);
				showPlayerParty();
				MobileParty.MainParty.IsActive = true;
				while (Campaign.Current.CurrentMenuContext != null)
				{
					GameMenu.ExitToLast();
				}
				if ((float)Hero.MainHero.GetSkillValue(DefaultSkills.Tactics) - 5f * MobileParty.MainParty.Position2D.Distance(randomElement3.Position2D) < (float)MBRandom.RandomInt(100))
				{
					Hero.MainHero.HitPoints = Math.Max(0, Hero.MainHero.HitPoints - (5 + MBRandom.RandomInt(15)));
					MBInformationManager.AddQuickInformation(new TextObject("{=FLT0000043}You took some minor injuries while being chased by bandits."), 0, Hero.MainHero.CharacterObject);
				}
				Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 200f);
				GameMenu.ActivateGameMenu("party_wait");
			}
		}, true, -1, false);
		TextObject textObject18 = new TextObject("{=FLT0000184}Attack enemy villagers");
		campaignStarter.AddGameMenuOption("party_wait", "party_wait_villager_attack", textObject18.ToString(), (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Raid;
			args.Tooltip = new TextObject("{=FLT0000185}Ride out with the cavalry to attack enemy villagers");
			if (!Hero.MainHero.CharacterObject.IsMounted)
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("{=FLT0000040}You need to be mounted to do this");
			}
			if (Hero.MainHero.IsWounded)
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("{=FLT0000041}You are wounded");
			}
			return nearbyVillagers().Count > 0;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			if (nearbyVillagers().Count == 0)
			{
				TextObject textObject33 = new TextObject("{=FLT0000186}The villagers managed to escape");
				InformationManager.DisplayMessage(new InformationMessage(textObject33.ToString()));
				GameMenu.ActivateGameMenu("party_wait");
			}
			else
			{
				MobileParty randomElement2 = nearbyVillagers().GetRandomElement();
				showPlayerParty();
				MobileParty.MainParty.IsActive = true;
				while (Campaign.Current.CurrentMenuContext != null)
				{
					GameMenu.ExitToLast();
				}
				MobileParty.MainParty.Position2D = randomElement2.Position2D;
				CavOnly(followingHero.PartyBelongedTo).Position2D = randomElement2.Position2D;
				MobileParty.MainParty.SetMoveEngageParty(randomElement2);
				GameMenu.ActivateGameMenu("party_wait");
			}
		}, true, -1, false);
		TextObject textObject19 = new TextObject("{=FLT0000188}Attack the enemy caravan");
		campaignStarter.AddGameMenuOption("party_wait", "party_wait_villager_attack", textObject19.ToString(), (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.ForceToGiveGoods;
			args.Tooltip = new TextObject("{=FLT0000189}Ride out with the cavalry to attack enemy caravan");
			if (!Hero.MainHero.CharacterObject.IsMounted)
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("{=FLT0000040}You need to be mounted to do this");
			}
			if (Hero.MainHero.IsWounded)
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("{=FLT0000041}You are wounded");
			}
			return nearbyCaravan().Count > 0;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			if (nearbyCaravan().Count == 0)
			{
				TextObject textObject32 = new TextObject("{=FLT0000190}The caravan managed to escape");
				InformationManager.DisplayMessage(new InformationMessage(textObject32.ToString()));
				GameMenu.ActivateGameMenu("party_wait");
			}
			else
			{
				MobileParty randomElement = nearbyCaravan().GetRandomElement();
				showPlayerParty();
				MobileParty.MainParty.IsActive = true;
				while (Campaign.Current.CurrentMenuContext != null)
				{
					GameMenu.ExitToLast();
				}
				MobileParty.MainParty.Position2D = randomElement.Position2D;
				CavOnly(followingHero.PartyBelongedTo).Position2D = randomElement.Position2D;
				MobileParty.MainParty.SetMoveEngageParty(randomElement);
				GameMenu.ActivateGameMenu("party_wait");
			}
		}, true, -1, false);
		TextObject textObject16 = new TextObject("{=FLT0000220}You are waiting in reserve");
		campaignStarter.AddWaitGameMenu("battle_wait", textObject16.ToString(), delegate(MenuCallbackArgs args)
		{
			if (followingHero.MapFaction.Culture.EncounterBackgroundMesh != null)
			{
				args.MenuContext.SetBackgroundMeshName(followingHero.MapFaction.Culture.EncounterBackgroundMesh);
			}
		}, wait_on_condition, null, wait_on_tick2, GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);
		TextObject textObject17 = new TextObject("{=FLT0000222}Rejoin the battle");
		campaignStarter.AddGameMenuOption("battle_wait", "battle_wait_back", textObject17.ToString(), (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
			return true;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			waitingInReserve = false;
			while (Campaign.Current.CurrentMenuContext != null)
			{
				GameMenu.ExitToLast();
			}
			GameMenu.ActivateGameMenu("party_wait");
		}, true, -1, false);
		TextObject textObject14 = new TextObject("{=FLT0000044}Abandon Party");
		campaignStarter.AddGameMenuOption("party_wait", "party_wait_leave", textObject14.ToString(), (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			TextObject textObject31 = new TextObject("{=FLT0000045}This will damage your reputation with the {FACTION}");
			string variable = ((followingHero != null) ? followingHero.MapFaction.Name.ToString() : "DATA CORRUPTION ERROR");
			textObject31.SetTextVariable("FACTION", variable);
			args.Tooltip = textObject31;
			args.optionLeaveType = GameMenuOption.LeaveType.Escape;
			return xp < SubModule.settings.RetirementXP;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			TextObject textObject27 = new TextObject("{=FLT0000044}Abandon Party");
			TextObject textObject28 = new TextObject("{=FLT0000046}Are you sure you want to abandon the party>  This will harm your relations with the entire faction.");
			TextObject textObject29 = new TextObject("{=FLT0000047}Yes");
			TextObject textObject30 = new TextObject("{=FLT0000048}No");
			InformationManager.ShowInquiry(new InquiryData(textObject27.ToString(), textObject28.ToString(), true, true, textObject29.ToString(), textObject30.ToString(), (Action)delegate
			{
				ChangeFactionRelation(followingHero.MapFaction, -100000);
				ChangeCrimeRatingAction.Apply(followingHero.MapFaction, 55f);
				foreach (Clan current2 in followingHero.Clan.Kingdom.Clans)
				{
					if (!current2.IsUnderMercenaryService)
					{
						ChangeRelationAction.ApplyPlayerRelation(current2.Leader, -20);
						foreach (Hero current3 in current2.Heroes)
						{
							if (current3.IsLord)
							{
								ChangeLordRelation(current3, -100000);
							}
						}
					}
				}
				LeaveLordPartyAction(keepgear: true);
				GameMenu.ExitToLast();
			}, (Action)delegate
			{
				GameMenu.ActivateGameMenu("party_wait");
			}, "", 0f, (Action)null));
		}, true, -1, false);
		campaignStarter.AddGameMenu("faction_reputation", "{REPUTATION}", delegate(MenuCallbackArgs args)
		{
			TextObject text = args.MenuContext.GameMenu.GetText();
			string text2 = "";
			foreach (Kingdom current in Campaign.Current.Kingdoms)
			{
				text2 = text2 + current.Name.ToString() + " : " + GetFactionRelations(current) + "\n";
			}
			text.SetTextVariable("REPUTATION", text2);
		});
		TextObject textObject15 = new TextObject("{=FLT0000049}Back");
		campaignStarter.AddGameMenuOption("faction_reputation", "faction_reputation_back", textObject15.ToString(), (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			GameMenu.ActivateGameMenu("party_wait");
		}, true, -1, false);
	}

	public void JoinPartyAction()
	{
		if (Mission.Current != null)
		{
			Mission.Current.EndMission();
		}
		followingHero = Hero.OneToOneConversationHero;
		enlistTime = CampaignTime.Now;
		EnlistTier = 1;
		xp = GetFactionRelations(followingHero.MapFaction) / 2 + GetLordRelations(followingHero) / 2;
		bool leveledUp = false;
		UpdateDiplomacy();
		hidePlayerParty();
		if (oldItems == null)
		{
			oldItems = new ItemRoster();
		}
		else
		{
			oldItems.Clear();
		}
		if (oldGear == null)
		{
			oldGear = new ItemRoster();
		}
		else
		{
			oldGear.Clear();
		}
		if (tournamentPrizes == null)
		{
			tournamentPrizes = new ItemRoster();
		}
		else
		{
			tournamentPrizes.Clear();
		}
		currentAssignment = Assignment.Grunt_Work;
		DisbandParty();
		disbandArmy = false;
		TransferAllItems(MobileParty.MainParty.ItemRoster, oldItems);
		CompanionOldGear.Clear();
		SetOldGear();
		GiveStateIssueEquipment(Hero.MainHero, followingHero.Culture.BasicTroop.Equipment);
		GetOldGear();
		while (Campaign.Current.CurrentMenuContext != null)
		{
			GameMenu.ExitToLast();
		}
		while (EnlistTier < 7 && xp > NextlevelXP[EnlistTier])
		{
			EnlistTier++;
			leveledUp = true;
		}
		if (leveledUp)
		{
			TextObject infotext = new TextObject("{=FLT0000026}{HERO} has enlisted at tier {TIER} due to high reputation with the {FACTION}");
			infotext.SetTextVariable("HERO", Hero.MainHero.Name.ToString());
			infotext.SetTextVariable("TIER", EnlistTier.ToString());
			infotext.SetTextVariable("FACTION", followingHero.MapFaction.Name.ToString());
			MBInformationManager.AddQuickInformation(infotext, 0, Hero.MainHero.CharacterObject);
		}
	}

	private bool conversation_companion_hire_low_rank_on_condition()
	{
		return followingHero != null && EnlistTier < 6;
	}

	private void party_wait_talk_to_other_members_item_on_consequence(MenuCallbackArgs args)
	{
		Hero hero = args.MenuContext.GetSelectedObject() as Hero;
		Campaign.Current.CurrentConversationContext = ConversationContext.PartyEncounter;
		CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false, false), new ConversationCharacterData(hero.CharacterObject, (PartyBase)null, false, false, false, false, false));
	}

	private bool party_wait_talk_to_other_members_item_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
		Hero hero = args.MenuContext.GetCurrentRepeatableObject() as Hero;
		MBTextManager.SetTextVariable("CHAR_NAME", (hero != null) ? new TextObject(hero.Name.ToString() + ((hero.Clan != null) ? (" (" + hero.Clan.Name.ToString() + ")") : "")) : null);
		if (hero != null)
		{
			MenuHelper.SetIssueAndQuestDataForHero(args, hero);
		}
		return true;
	}

	private void party_wait_talk_to_other_members_on_init(MenuCallbackArgs args)
	{
		List<Hero> ConversationHeroes = new List<Hero>();
		if (followingHero.PartyBelongedTo.Army != null)
		{
			foreach (MobileParty party in followingHero.PartyBelongedTo.Army.Parties)
			{
				foreach (TroopRosterElement troop2 in party.MemberRoster.GetTroopRoster())
				{
					if (troop2.Character.IsHero)
					{
						ConversationHeroes.Add(troop2.Character.HeroObject);
					}
				}
			}
		}
		else
		{
			foreach (TroopRosterElement troop in followingHero.PartyBelongedTo.MemberRoster.GetTroopRoster())
			{
				if (troop.Character.IsHero)
				{
					ConversationHeroes.Add(troop.Character.HeroObject);
				}
			}
		}
		if (followingHero.CurrentSettlement != null && followingHero.CurrentSettlement.IsTown)
		{
			foreach (Hero hero in followingHero.CurrentSettlement.HeroesWithoutParty)
			{
				if (hero.Clan == null && hero.IsWanderer)
				{
					ConversationHeroes.Add(hero);
				}
			}
		}
		args.MenuContext.SetRepeatObjectList(ConversationHeroes);
	}

	private void wait_on_tick2(MenuCallbackArgs args, CampaignTime dt)
	{
		if (followingHero != null && followingHero.PartyBelongedTo != null && followingHero.PartyBelongedTo.MapEvent != null && (EncounterMenuPatch.ContainsParty(followingHero.PartyBelongedTo.MapEvent.PartiesOnSide(BattleSideEnum.Attacker), followingHero.PartyBelongedTo) ? followingHero.PartyBelongedTo.MapEvent.AttackerSide.TroopCount : followingHero.PartyBelongedTo.MapEvent.DefenderSide.TroopCount) < 100)
		{
			waitingInReserve = false;
			while (Campaign.Current.CurrentMenuContext != null)
			{
				GameMenu.ExitToLast();
			}
			GameMenu.ActivateGameMenu("party_wait");
		}
	}

	private MobileParty CavOnly(MobileParty partyBelongedTo)
	{
		CavalryDetachment = MobileParty.CreateParty("calavlry detachment", null);
		TextObject customName = new TextObject("{=FLT0000187}Cavalry Detachment");
		CavalryDetachment.InitializeMobilePartyAroundPosition(new TroopRoster(CavalryDetachment.Party), new TroopRoster(CavalryDetachment.Party), partyBelongedTo.GetPosition2D, 1f, 0.5f);
		CavalryDetachment.SetCustomName(customName);
		CavalryDetachment.ActualClan = partyBelongedTo.ActualClan;
		CavalryDetachment.ShouldJoinPlayerBattles = true;
		foreach (TroopRosterElement troop in partyBelongedTo.MemberRoster.GetTroopRoster())
		{
			if (troop.Character.IsMounted && !troop.Character.IsHero)
			{
				int num = troop.Number;
				CharacterObject character = troop.Character;
				partyBelongedTo.MemberRoster.AddToCounts(character, -1 * num);
				CavalryDetachment.MemberRoster.AddToCounts(character, num);
			}
		}
		return CavalryDetachment;
	}

	private List<MobileParty> nearbyCaravan()
	{
		float radius = MobileParty.MainParty.SeeingRange;
		List<MobileParty> list = new List<MobileParty>();
		foreach (MobileParty party in Campaign.Current.MobileParties)
		{
			if (party.IsCaravan && party.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction) && party.Position2D.Distance(MobileParty.MainParty.Position2D) < radius && party.CurrentSettlement == null)
			{
				list.Add(party);
			}
		}
		return list;
	}

	private List<MobileParty> nearbyVillagers()
	{
		float radius = MobileParty.MainParty.SeeingRange;
		List<MobileParty> list = new List<MobileParty>();
		foreach (MobileParty party in Campaign.Current.MobileParties)
		{
			if (party.IsVillager && party.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction) && party.Position2D.Distance(MobileParty.MainParty.Position2D) < radius && party.CurrentSettlement == null)
			{
				list.Add(party);
			}
		}
		return list;
	}

	private List<MobileParty> nearbyBandit()
	{
		float radius = MobileParty.MainParty.SeeingRange;
		List<MobileParty> list = new List<MobileParty>();
		foreach (MobileParty party in Campaign.Current.MobileParties)
		{
			if (followingHero != null && (party.IsBandit || party.IsBanditBossParty) && party.Position2D.Distance(MobileParty.MainParty.Position2D) < radius && party.TargetParty != followingHero.PartyBelongedTo)
			{
				list.Add(party);
			}
		}
		return list;
	}

	private DialogFlow CreateAskAssignmentDialog()
	{
		TextObject textObject = new TextObject("{=FLT0000050}{HERO}, I heard you are not happy with your current assignment.  What to would you rather do instead?");
		TextObject textObject16 = new TextObject("{=FLT0000052}Okay, you can do that.");
		TextObject textObject17 = new TextObject("{=FLT0000053}I want to do manual labor.");
		TextObject textObject18 = new TextObject("{=FLT0000054}I want to do guard duty.");
		TextObject textObject19 = new TextObject("{=FLT0000055}I want to prepare the meals.");
		TextObject textObject20 = new TextObject("{=FLT0000056}I want to forage for supplies.");
		TextObject textObject21 = new TextObject("{=FLT0000057}I want to drill the troops.");
		TextObject textObject22 = new TextObject("{=FLT0000058}Sure thing, friend.");
		TextObject textObject2 = new TextObject("{=FLT0000059}Of course, you are the perfect person for the job.");
		TextObject textObject3 = new TextObject("{=FLT0000060}Sorry, but I don't think you have the skills for the job.");
		TextObject textObject4 = new TextObject("{=FLT0000061}Alright, thank you. Farewell.");
		TextObject textObject5 = new TextObject("{=FLT0000062}You made a very persuasive argument. Fine, you can be one of my sergeants.");
		TextObject textObject6 = new TextObject("{=FLT0000063}I want to lead the scouting expeditions.");
		TextObject textObject7 = new TextObject("{=FLT0000064}You made a very persuasive argument. Fine, you can lead my scouts.");
		TextObject textObject8 = new TextObject("{=FLT0000065}I want manage logistics.");
		TextObject textObject9 = new TextObject("{=FLT0000066}You made a very persuasive argument. Fine, you can be my quartmaster.");
		TextObject textObject10 = new TextObject("{=FLT0000067}I want to build war machines.");
		TextObject textObject11 = new TextObject("{=FLT0000068}You made a very persuasive argument. Fine, you can be an engineer.");
		TextObject textObject12 = new TextObject("{=FLT0000069}I want to take care of the wounded.");
		TextObject textObject13 = new TextObject("{=FLT0000070}You made a very persuasive argument. Fine, you can be a surgeon.");
		TextObject textObject14 = new TextObject("{=FLT0000071}I want to discuss war strategies.");
		TextObject textObject15 = new TextObject("{=FLT0000072}You made a very persuasive argument. Fine, you can be my strategist.");
		return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject).Condition(delegate
		{
			textObject.SetTextVariable("HERO", Hero.MainHero.EncyclopediaLinkWithName);
			return Hero.OneToOneConversationHero == followingHero && conversation_type == "ask_assignment";
		})
			.Consequence(delegate
			{
				conversation_type = "";
			})
			.BeginPlayerOptions()
			.PlayerOption(textObject17)
			.Condition(() => currentAssignment != Assignment.Grunt_Work)
			.Consequence(delegate
			{
				currentAssignment = Assignment.Grunt_Work;
			})
			.NpcLine(textObject16)
			.CloseDialog()
			.PlayerOption(textObject18)
			.Condition(() => currentAssignment != Assignment.Guard_Duty)
			.Consequence(delegate
			{
				currentAssignment = Assignment.Guard_Duty;
			})
			.NpcLine(textObject16)
			.CloseDialog()
			.PlayerOption(textObject19)
			.Condition(() => currentAssignment != Assignment.Cook)
			.Consequence(delegate
			{
				currentAssignment = Assignment.Cook;
			})
			.NpcLine(textObject16)
			.CloseDialog()
			.PlayerOption(textObject20)
			.Condition(() => currentAssignment != Assignment.Foraging)
			.Consequence(delegate
			{
				currentAssignment = Assignment.Foraging;
			})
			.NpcLine(textObject16)
			.CloseDialog()
			.PlayerOption(textObject21)
			.Condition(() => currentAssignment != Assignment.Sergeant)
			.BeginNpcOptions()
			.NpcOption(textObject22, () => followingHero.GetRelationWithPlayer() >= 50f)
			.Consequence(delegate
			{
				currentAssignment = Assignment.Sergeant;
			})
			.CloseDialog()
			.NpcOption(textObject2, () => Hero.MainHero.GetSkillValue(DefaultSkills.Leadership) >= 100)
			.Consequence(delegate
			{
				currentAssignment = Assignment.Sergeant;
			})
			.CloseDialog()
			.NpcOption(textObject3, null)
			.BeginPlayerOptions()
			.PlayerOption(textObject4)
			.CloseDialog()
			.PlayerOption("{=FLT0000051}Would this bag of {GOLD}{COIN} gold change your mind?")
			.Condition(delegate
			{
				MBTextManager.SetTextVariable("COIN", "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
				MBTextManager.SetTextVariable("GOLD", 5 * wage());
				return true;
			})
			.NpcLine(textObject5)
			.Consequence(delegate
			{
				GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, followingHero, 5 * wage());
				currentAssignment = Assignment.Sergeant;
			})
			.CloseDialog()
			.EndPlayerOptions()
			.EndNpcOptions()
			.PlayerOption(textObject6)
			.Condition(() => currentAssignment != Assignment.Scout)
			.BeginNpcOptions()
			.NpcOption(textObject22, () => followingHero.GetRelationWithPlayer() >= 50f)
			.Consequence(delegate
			{
				currentAssignment = Assignment.Scout;
			})
			.CloseDialog()
			.NpcOption(textObject2, () => Hero.MainHero.GetSkillValue(DefaultSkills.Scouting) >= 100)
			.Consequence(delegate
			{
				currentAssignment = Assignment.Scout;
			})
			.CloseDialog()
			.NpcOption(textObject3, null)
			.BeginPlayerOptions()
			.PlayerOption(textObject4)
			.CloseDialog()
			.PlayerOption("{=FLT0000051}Would this bag of {GOLD}{COIN} gold change your mind?")
			.Condition(delegate
			{
				MBTextManager.SetTextVariable("COIN", "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
				MBTextManager.SetTextVariable("GOLD", 5 * wage());
				return true;
			})
			.NpcLine(textObject7)
			.Consequence(delegate
			{
				GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, followingHero, 5 * wage());
				currentAssignment = Assignment.Scout;
			})
			.CloseDialog()
			.EndPlayerOptions()
			.EndNpcOptions()
			.PlayerOption(textObject8)
			.Condition(() => currentAssignment != Assignment.Quartermaster)
			.BeginNpcOptions()
			.NpcOption(textObject22, () => followingHero.GetRelationWithPlayer() >= 50f)
			.Consequence(delegate
			{
				currentAssignment = Assignment.Quartermaster;
			})
			.CloseDialog()
			.NpcOption(textObject2, () => Hero.MainHero.GetSkillValue(DefaultSkills.Steward) >= 100)
			.Consequence(delegate
			{
				currentAssignment = Assignment.Quartermaster;
			})
			.CloseDialog()
			.NpcOption(textObject3, null)
			.BeginPlayerOptions()
			.PlayerOption(textObject4)
			.CloseDialog()
			.PlayerOption("{=FLT0000051}Would this bag of {GOLD}{COIN} gold change your mind?")
			.Condition(delegate
			{
				MBTextManager.SetTextVariable("COIN", "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
				MBTextManager.SetTextVariable("GOLD", 5 * wage());
				return true;
			})
			.NpcLine(textObject9)
			.Consequence(delegate
			{
				GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, followingHero, 5 * wage());
				currentAssignment = Assignment.Quartermaster;
			})
			.CloseDialog()
			.EndPlayerOptions()
			.EndNpcOptions()
			.PlayerOption(textObject10)
			.Condition(() => currentAssignment != Assignment.Engineer)
			.BeginNpcOptions()
			.NpcOption(textObject22, () => followingHero.GetRelationWithPlayer() >= 50f)
			.Consequence(delegate
			{
				currentAssignment = Assignment.Engineer;
			})
			.CloseDialog()
			.NpcOption(textObject2, () => Hero.MainHero.GetSkillValue(DefaultSkills.Engineering) >= 100)
			.Consequence(delegate
			{
				currentAssignment = Assignment.Engineer;
			})
			.CloseDialog()
			.NpcOption(textObject3, null)
			.BeginPlayerOptions()
			.PlayerOption(textObject4)
			.CloseDialog()
			.PlayerOption("{=FLT0000051}Would this bag of {GOLD}{COIN} gold change your mind?")
			.Condition(delegate
			{
				MBTextManager.SetTextVariable("COIN", "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
				MBTextManager.SetTextVariable("GOLD", 5 * wage());
				return true;
			})
			.NpcLine(textObject11)
			.Consequence(delegate
			{
				GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, followingHero, 5 * wage());
				currentAssignment = Assignment.Engineer;
			})
			.CloseDialog()
			.EndPlayerOptions()
			.EndNpcOptions()
			.PlayerOption(textObject12)
			.Condition(() => currentAssignment != Assignment.Surgeon)
			.BeginNpcOptions()
			.NpcOption(textObject22, () => followingHero.GetRelationWithPlayer() >= 50f)
			.Consequence(delegate
			{
				currentAssignment = Assignment.Surgeon;
			})
			.CloseDialog()
			.NpcOption(textObject2, () => Hero.MainHero.GetSkillValue(DefaultSkills.Medicine) >= 100)
			.Consequence(delegate
			{
				currentAssignment = Assignment.Surgeon;
			})
			.CloseDialog()
			.NpcOption(textObject3, null)
			.BeginPlayerOptions()
			.PlayerOption(textObject4)
			.CloseDialog()
			.PlayerOption("{=FLT0000051}Would this bag of {GOLD}{COIN} gold change your mind?")
			.Condition(delegate
			{
				MBTextManager.SetTextVariable("COIN", "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
				MBTextManager.SetTextVariable("GOLD", 5 * wage());
				return true;
			})
			.NpcLine(textObject13)
			.Consequence(delegate
			{
				GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, followingHero, 5 * wage());
				currentAssignment = Assignment.Surgeon;
			})
			.CloseDialog()
			.EndPlayerOptions()
			.EndNpcOptions()
			.PlayerOption(textObject14)
			.Condition(() => currentAssignment != Assignment.Strategist)
			.BeginNpcOptions()
			.NpcOption(textObject22, () => followingHero.GetRelationWithPlayer() >= 50f)
			.Consequence(delegate
			{
				currentAssignment = Assignment.Strategist;
			})
			.CloseDialog()
			.NpcOption(textObject2, () => Hero.MainHero.GetSkillValue(DefaultSkills.Tactics) >= 100)
			.Consequence(delegate
			{
				currentAssignment = Assignment.Strategist;
			})
			.CloseDialog()
			.NpcOption(textObject3, null)
			.BeginPlayerOptions()
			.PlayerOption(textObject4)
			.CloseDialog()
			.PlayerOption("{=FLT0000051}Would this bag of {GOLD}{COIN} gold change your mind?")
			.Condition(delegate
			{
				MBTextManager.SetTextVariable("COIN", "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
				MBTextManager.SetTextVariable("GOLD", 5 * wage());
				return true;
			})
			.NpcLine(textObject15)
			.Consequence(delegate
			{
				GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, followingHero, 5 * wage());
				currentAssignment = Assignment.Strategist;
			})
			.CloseDialog()
			.EndPlayerOptions()
			.EndNpcOptions()
			.EndPlayerOptions()
			.CloseDialog();
	}

	private void DisbandParty()
	{
		if (MobileParty.MainParty.MemberRoster.TotalManCount <= 1)
		{
			return;
		}
		List<TroopRosterElement> list = new List<TroopRosterElement>();
		foreach (TroopRosterElement troop2 in MobileParty.MainParty.MemberRoster.GetTroopRoster())
		{
			if (troop2.Character != Hero.MainHero.CharacterObject && troop2.Character.HeroObject == null)
			{
				list.Add(troop2);
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		foreach (TroopRosterElement troop in list)
		{
			followingHero.PartyBelongedTo.MemberRoster.AddToCounts(troop.Character, troop.Number);
			MobileParty.MainParty.MemberRoster.AddToCounts(troop.Character, -1 * troop.Number);
		}
	}

	private static void equipGear()
	{
		int weaponslot = 0;
		Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.WeaponItemBeginSlot, new EquipmentElement(null));
		Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon1, new EquipmentElement(null));
		Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon2, new EquipmentElement(null));
		Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon3, new EquipmentElement(null));
		Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.NumAllWeaponSlots, new EquipmentElement(null));
		Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Cape, new EquipmentElement(null));
		Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Body, new EquipmentElement(null));
		Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Gloves, new EquipmentElement(null));
		Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Leg, new EquipmentElement(null));
		Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.ArmorItemEndSlot, new EquipmentElement(null));
		Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.HorseHarness, new EquipmentElement(null));
		foreach (ItemRosterElement item in MobileParty.MainParty.ItemRoster)
		{
			if (item.EquipmentElement.Item.Type == ItemObject.ItemTypeEnum.BodyArmor)
			{
				Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Body, item.EquipmentElement);
			}
			else if (item.EquipmentElement.Item.Type == ItemObject.ItemTypeEnum.HeadArmor)
			{
				Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.NumAllWeaponSlots, item.EquipmentElement);
			}
			else if (item.EquipmentElement.Item.Type == ItemObject.ItemTypeEnum.Cape)
			{
				Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Cape, item.EquipmentElement);
			}
			else if (item.EquipmentElement.Item.Type == ItemObject.ItemTypeEnum.HandArmor)
			{
				Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Gloves, item.EquipmentElement);
			}
			else if (item.EquipmentElement.Item.Type == ItemObject.ItemTypeEnum.LegArmor)
			{
				Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Leg, item.EquipmentElement);
			}
			else if (item.EquipmentElement.Item.Type == ItemObject.ItemTypeEnum.Horse)
			{
				Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.ArmorItemEndSlot, item.EquipmentElement);
			}
			else if (item.EquipmentElement.Item.Type == ItemObject.ItemTypeEnum.HorseHarness)
			{
				Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.HorseHarness, item.EquipmentElement);
			}
			else
			{
				if (!isWeapon(item.EquipmentElement.Item))
				{
					continue;
				}
				for (int i = 0; i < item.Amount; i++)
				{
					switch (weaponslot)
					{
					case 0:
						Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.WeaponItemBeginSlot, item.EquipmentElement);
						break;
					case 1:
						Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon1, item.EquipmentElement);
						break;
					case 2:
						Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon2, item.EquipmentElement);
						break;
					case 3:
						Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon3, item.EquipmentElement);
						break;
					}
					weaponslot++;
				}
			}
		}
		MobileParty.MainParty.ItemRoster.Clear();
	}

	public static bool isWeapon(ItemObject item)
	{
		return item.ItemType == ItemObject.ItemTypeEnum.Arrows || item.ItemType == ItemObject.ItemTypeEnum.Bolts || item.ItemType == ItemObject.ItemTypeEnum.Bow || item.ItemType == ItemObject.ItemTypeEnum.Bullets || item.ItemType == ItemObject.ItemTypeEnum.Crossbow || item.ItemType == ItemObject.ItemTypeEnum.Musket || item.ItemType == ItemObject.ItemTypeEnum.OneHandedWeapon || item.ItemType == ItemObject.ItemTypeEnum.Pistol || item.ItemType == ItemObject.ItemTypeEnum.Polearm || item.ItemType == ItemObject.ItemTypeEnum.Shield || item.ItemType == ItemObject.ItemTypeEnum.Thrown || item.ItemType == ItemObject.ItemTypeEnum.TwoHandedWeapon;
	}

	private void SetOldGear()
	{
		EquipmentIndex[] slots = new EquipmentIndex[11]
		{
			EquipmentIndex.WeaponItemBeginSlot,
			EquipmentIndex.Weapon1,
			EquipmentIndex.Weapon2,
			EquipmentIndex.Weapon3,
			EquipmentIndex.NumAllWeaponSlots,
			EquipmentIndex.Cape,
			EquipmentIndex.Body,
			EquipmentIndex.Gloves,
			EquipmentIndex.Leg,
			EquipmentIndex.ArmorItemEndSlot,
			EquipmentIndex.HorseHarness
		};
		EquipmentIndex[] array = slots;
		foreach (EquipmentIndex slot in array)
		{
			if (Hero.MainHero.CharacterObject.Equipment.GetEquipmentFromSlot(slot).Item != null && Hero.MainHero.CharacterObject.Equipment.GetEquipmentFromSlot(slot).Item.Name != null)
			{
				oldGear.AddToCounts(Hero.MainHero.CharacterObject.Equipment.GetEquipmentFromSlot(slot).Item, 1);
			}
		}
	}

	private static void GetOldGear()
	{
		MobileParty.MainParty.ItemRoster.Clear();
		List<ItemRosterElement> list = new List<ItemRosterElement>();
		foreach (ItemRosterElement item in oldGear)
		{
			MobileParty.MainParty.ItemRoster.AddToCounts(item.EquipmentElement, item.Amount);
		}
	}

	public static void TransferAllItems(ItemRoster items1, ItemRoster items2)
	{
		List<ItemRosterElement> move = new List<ItemRosterElement>();
		foreach (ItemRosterElement item2 in items1)
		{
			move.Add(item2);
		}
		foreach (ItemRosterElement item in move)
		{
			items1.Remove(item);
			items2.AddToCounts(item.EquipmentElement.Item, item.Amount);
		}
	}

	public static void SwitchGear()
	{
		conversation_type = "weaponsmith";
		DialogFlow dialog = CreateWeaponsmithDialog();
		Campaign.Current.ConversationManager.AddDialogFlow(dialog);
		CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false, false), new ConversationCharacterData(followingHero.Culture.Armorer, (PartyBase)null, false, false, false, false, false));
	}

	private static DialogFlow CreateWeaponsmithDialog()
	{
		TextObject textObject = new TextObject("Here you go!");
		return DialogFlow.CreateDialogFlow("start", 125).NpcLine("What do you need?").Condition(() => conversation_type == "weaponsmith")
			.Consequence(delegate
			{
				conversation_type = null;
			})
			.BeginPlayerOptions()
			.PlayerOption("A new weapon (slot 1)")
			.NpcLine(textObject)
			.Consequence(delegate
			{
				List<CharacterObject> list21 = new List<CharacterObject>();
				foreach (CharacterObject current31 in GetTroopsList(followingHero.Culture))
				{
					if (current31.Tier <= EnlistTier)
					{
						list21.Add(current31);
					}
				}
				List<ItemObject> list22 = new List<ItemObject>();
				foreach (CharacterObject current32 in list21)
				{
					foreach (Equipment current33 in current32.BattleEquipments)
					{
						if (!list22.Contains(current33[EquipmentIndex.WeaponItemBeginSlot].Item) && current33[EquipmentIndex.WeaponItemBeginSlot].Item != null)
						{
							list22.Add(current33[EquipmentIndex.WeaponItemBeginSlot].Item);
						}
						if (!list22.Contains(current33[EquipmentIndex.Weapon1].Item) && current33[EquipmentIndex.Weapon1].Item != null)
						{
							list22.Add(current33[EquipmentIndex.Weapon1].Item);
						}
						if (!list22.Contains(current33[EquipmentIndex.Weapon2].Item) && current33[EquipmentIndex.Weapon2].Item != null)
						{
							list22.Add(current33[EquipmentIndex.Weapon2].Item);
						}
						if (!list22.Contains(current33[EquipmentIndex.Weapon3].Item) && current33[EquipmentIndex.Weapon3].Item != null)
						{
							list22.Add(current33[EquipmentIndex.Weapon3].Item);
						}
					}
				}
				EquipmentSelectorBehavior.CreateVMLayer(list22, "Wep0");
				Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			})
			.PlayerOption("A new weapon (slot 2)")
			.NpcLine(textObject)
			.Consequence(delegate
			{
				List<CharacterObject> list19 = new List<CharacterObject>();
				foreach (CharacterObject current28 in GetTroopsList(followingHero.Culture))
				{
					if (current28.Tier <= EnlistTier)
					{
						list19.Add(current28);
					}
				}
				List<ItemObject> list20 = new List<ItemObject>();
				foreach (CharacterObject current29 in list19)
				{
					foreach (Equipment current30 in current29.BattleEquipments)
					{
						if (!list20.Contains(current30[EquipmentIndex.WeaponItemBeginSlot].Item) && current30[EquipmentIndex.WeaponItemBeginSlot].Item != null)
						{
							list20.Add(current30[EquipmentIndex.WeaponItemBeginSlot].Item);
						}
						if (!list20.Contains(current30[EquipmentIndex.Weapon1].Item) && current30[EquipmentIndex.Weapon1].Item != null)
						{
							list20.Add(current30[EquipmentIndex.Weapon1].Item);
						}
						if (!list20.Contains(current30[EquipmentIndex.Weapon2].Item) && current30[EquipmentIndex.Weapon2].Item != null)
						{
							list20.Add(current30[EquipmentIndex.Weapon2].Item);
						}
						if (!list20.Contains(current30[EquipmentIndex.Weapon3].Item) && current30[EquipmentIndex.Weapon3].Item != null)
						{
							list20.Add(current30[EquipmentIndex.Weapon3].Item);
						}
					}
				}
				EquipmentSelectorBehavior.CreateVMLayer(list20, "Wep1");
				Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			})
			.PlayerOption("A new weapon (slot 3)")
			.NpcLine(textObject)
			.Consequence(delegate
			{
				List<CharacterObject> list17 = new List<CharacterObject>();
				foreach (CharacterObject current25 in GetTroopsList(followingHero.Culture))
				{
					if (current25.Tier <= EnlistTier)
					{
						list17.Add(current25);
					}
				}
				List<ItemObject> list18 = new List<ItemObject>();
				foreach (CharacterObject current26 in list17)
				{
					foreach (Equipment current27 in current26.BattleEquipments)
					{
						if (!list18.Contains(current27[EquipmentIndex.WeaponItemBeginSlot].Item) && current27[EquipmentIndex.WeaponItemBeginSlot].Item != null)
						{
							list18.Add(current27[EquipmentIndex.WeaponItemBeginSlot].Item);
						}
						if (!list18.Contains(current27[EquipmentIndex.Weapon1].Item) && current27[EquipmentIndex.Weapon1].Item != null)
						{
							list18.Add(current27[EquipmentIndex.Weapon1].Item);
						}
						if (!list18.Contains(current27[EquipmentIndex.Weapon2].Item) && current27[EquipmentIndex.Weapon2].Item != null)
						{
							list18.Add(current27[EquipmentIndex.Weapon2].Item);
						}
						if (!list18.Contains(current27[EquipmentIndex.Weapon3].Item) && current27[EquipmentIndex.Weapon3].Item != null)
						{
							list18.Add(current27[EquipmentIndex.Weapon3].Item);
						}
					}
				}
				EquipmentSelectorBehavior.CreateVMLayer(list18, "Wep2");
				Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			})
			.PlayerOption("A new weapon (slot 4)")
			.NpcLine(textObject)
			.Consequence(delegate
			{
				List<CharacterObject> list15 = new List<CharacterObject>();
				foreach (CharacterObject current22 in GetTroopsList(followingHero.Culture))
				{
					if (current22.Tier <= EnlistTier)
					{
						list15.Add(current22);
					}
				}
				List<ItemObject> list16 = new List<ItemObject>();
				foreach (CharacterObject current23 in list15)
				{
					foreach (Equipment current24 in current23.BattleEquipments)
					{
						if (!list16.Contains(current24[EquipmentIndex.WeaponItemBeginSlot].Item) && current24[EquipmentIndex.WeaponItemBeginSlot].Item != null)
						{
							list16.Add(current24[EquipmentIndex.WeaponItemBeginSlot].Item);
						}
						if (!list16.Contains(current24[EquipmentIndex.Weapon1].Item) && current24[EquipmentIndex.Weapon1].Item != null)
						{
							list16.Add(current24[EquipmentIndex.Weapon1].Item);
						}
						if (!list16.Contains(current24[EquipmentIndex.Weapon2].Item) && current24[EquipmentIndex.Weapon2].Item != null)
						{
							list16.Add(current24[EquipmentIndex.Weapon2].Item);
						}
						if (!list16.Contains(current24[EquipmentIndex.Weapon3].Item) && current24[EquipmentIndex.Weapon3].Item != null)
						{
							list16.Add(current24[EquipmentIndex.Weapon3].Item);
						}
					}
				}
				EquipmentSelectorBehavior.CreateVMLayer(list16, "Wep3");
				Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			})
			.PlayerOption("A new hemlet")
			.NpcLine(textObject)
			.Consequence(delegate
			{
				List<CharacterObject> list13 = new List<CharacterObject>();
				foreach (CharacterObject current19 in GetTroopsList(followingHero.Culture))
				{
					if (current19.Tier <= EnlistTier)
					{
						list13.Add(current19);
					}
				}
				List<ItemObject> list14 = new List<ItemObject>();
				foreach (CharacterObject current20 in list13)
				{
					foreach (Equipment current21 in current20.BattleEquipments)
					{
						if (!list14.Contains(current21[EquipmentIndex.NumAllWeaponSlots].Item) && current21[EquipmentIndex.NumAllWeaponSlots].Item != null)
						{
							list14.Add(current21[EquipmentIndex.NumAllWeaponSlots].Item);
						}
					}
				}
				EquipmentSelectorBehavior.CreateVMLayer(list14, "Head");
				Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			})
			.PlayerOption("A new cape")
			.NpcLine(textObject)
			.Consequence(delegate
			{
				List<CharacterObject> list11 = new List<CharacterObject>();
				foreach (CharacterObject current16 in GetTroopsList(followingHero.Culture))
				{
					if (current16.Tier <= EnlistTier)
					{
						list11.Add(current16);
					}
				}
				List<ItemObject> list12 = new List<ItemObject>();
				foreach (CharacterObject current17 in list11)
				{
					foreach (Equipment current18 in current17.BattleEquipments)
					{
						if (!list12.Contains(current18[EquipmentIndex.Cape].Item) && current18[EquipmentIndex.Cape].Item != null)
						{
							list12.Add(current18[EquipmentIndex.Cape].Item);
						}
					}
				}
				EquipmentSelectorBehavior.CreateVMLayer(list12, "Cape");
				Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			})
			.PlayerOption("A new body armour")
			.NpcLine(textObject)
			.Consequence(delegate
			{
				List<CharacterObject> list9 = new List<CharacterObject>();
				foreach (CharacterObject current13 in GetTroopsList(followingHero.Culture))
				{
					if (current13.Tier <= EnlistTier)
					{
						list9.Add(current13);
					}
				}
				List<ItemObject> list10 = new List<ItemObject>();
				foreach (CharacterObject current14 in list9)
				{
					foreach (Equipment current15 in current14.BattleEquipments)
					{
						if (!list10.Contains(current15[EquipmentIndex.Body].Item) && current15[EquipmentIndex.Body].Item != null)
						{
							list10.Add(current15[EquipmentIndex.Body].Item);
						}
					}
				}
				EquipmentSelectorBehavior.CreateVMLayer(list10, "Body");
				Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			})
			.PlayerOption("New gloves")
			.NpcLine(textObject)
			.Consequence(delegate
			{
				List<CharacterObject> list7 = new List<CharacterObject>();
				foreach (CharacterObject current10 in GetTroopsList(followingHero.Culture))
				{
					if (current10.Tier <= EnlistTier)
					{
						list7.Add(current10);
					}
				}
				List<ItemObject> list8 = new List<ItemObject>();
				foreach (CharacterObject current11 in list7)
				{
					foreach (Equipment current12 in current11.BattleEquipments)
					{
						if (!list8.Contains(current12[EquipmentIndex.Gloves].Item) && current12[EquipmentIndex.Gloves].Item != null)
						{
							list8.Add(current12[EquipmentIndex.Gloves].Item);
						}
					}
				}
				EquipmentSelectorBehavior.CreateVMLayer(list8, "Gloves");
				Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			})
			.PlayerOption("New boots")
			.NpcLine(textObject)
			.Consequence(delegate
			{
				List<CharacterObject> list5 = new List<CharacterObject>();
				foreach (CharacterObject current7 in GetTroopsList(followingHero.Culture))
				{
					if (current7.Tier <= EnlistTier)
					{
						list5.Add(current7);
					}
				}
				List<ItemObject> list6 = new List<ItemObject>();
				foreach (CharacterObject current8 in list5)
				{
					foreach (Equipment current9 in current8.BattleEquipments)
					{
						if (!list6.Contains(current9[EquipmentIndex.Leg].Item) && current9[EquipmentIndex.Leg].Item != null)
						{
							list6.Add(current9[EquipmentIndex.Leg].Item);
						}
					}
				}
				EquipmentSelectorBehavior.CreateVMLayer(list6, "Leg");
				Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			})
			.PlayerOption("A new horse")
			.NpcLine(textObject)
			.Consequence(delegate
			{
				List<CharacterObject> list3 = new List<CharacterObject>();
				foreach (CharacterObject current4 in GetTroopsList(followingHero.Culture))
				{
					if (current4.Tier <= EnlistTier)
					{
						list3.Add(current4);
					}
				}
				List<ItemObject> list4 = new List<ItemObject>();
				foreach (CharacterObject current5 in list3)
				{
					foreach (Equipment current6 in current5.BattleEquipments)
					{
						if (!list4.Contains(current6[EquipmentIndex.ArmorItemEndSlot].Item) && current6[EquipmentIndex.ArmorItemEndSlot].Item != null)
						{
							list4.Add(current6[EquipmentIndex.ArmorItemEndSlot].Item);
						}
					}
				}
				EquipmentSelectorBehavior.CreateVMLayer(list4, "Horse");
				Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			})
			.PlayerOption("A new horse harness")
			.NpcLine(textObject)
			.Consequence(delegate
			{
				List<CharacterObject> list = new List<CharacterObject>();
				foreach (CharacterObject current in GetTroopsList(followingHero.Culture))
				{
					if (current.Tier <= EnlistTier)
					{
						list.Add(current);
					}
				}
				List<ItemObject> list2 = new List<ItemObject>();
				foreach (CharacterObject current2 in list)
				{
					foreach (Equipment current3 in current2.BattleEquipments)
					{
						if (!list2.Contains(current3[EquipmentIndex.HorseHarness].Item) && current3[EquipmentIndex.HorseHarness].Item != null)
						{
							list2.Add(current3[EquipmentIndex.HorseHarness].Item);
						}
					}
				}
				EquipmentSelectorBehavior.CreateVMLayer(list2, "Harness");
				Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			})
			.PlayerOption("Never mind")
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog();
	}

	public static void GiveStateIssueEquipment(Hero hero, Equipment equipment)
	{
		for (EquipmentIndex index = EquipmentIndex.WeaponItemBeginSlot; index < EquipmentIndex.NumEquipmentSetSlots; index++)
		{
			if (equipment[index].Item != null)
			{
				if (equipment[index].Item.ArmorComponent != null)
				{
					hero.CharacterObject.Equipment[index] = new EquipmentElement(equipment[index].Item, MBObjectManager.Instance.GetObject<ItemModifier>("sas_armor"));
				}
				else if (equipment[index].Item.HorseComponent != null)
				{
					hero.CharacterObject.Equipment[index] = new EquipmentElement(equipment[index].Item, MBObjectManager.Instance.GetObject<ItemModifier>("sas_horse"));
				}
				else if (equipment[index].Item.WeaponComponent != null)
				{
					hero.CharacterObject.Equipment[index] = new EquipmentElement(equipment[index].Item, MBObjectManager.Instance.GetObject<ItemModifier>("sas_weapon"));
				}
			}
			else
			{
				hero.CharacterObject.Equipment[index] = equipment[index];
			}
		}
	}

	private string heroDetails(Hero hero)
	{
		string s = "";
		s = s + new TextObject("{=FLT0000195}One-Handed : ").ToString() + hero.GetSkillValue(DefaultSkills.OneHanded) + "\n";
		s = s + new TextObject("{=FLT0000196}Two-Handed : ").ToString() + hero.GetSkillValue(DefaultSkills.TwoHanded) + "\n";
		s = s + new TextObject("{=FLT0000197}Polearm : ").ToString() + hero.GetSkillValue(DefaultSkills.Polearm) + "\n";
		s = s + new TextObject("{=FLT0000198}Bow : ").ToString() + hero.GetSkillValue(DefaultSkills.Bow) + "\n";
		s = s + new TextObject("{=FLT0000199}Crossbow : ").ToString() + hero.GetSkillValue(DefaultSkills.Crossbow) + "\n";
		s = s + new TextObject("{=FLT0000200}Throwing : ").ToString() + hero.GetSkillValue(DefaultSkills.Throwing) + "\n";
		s = s + new TextObject("{=FLT0000201}Riding : ").ToString() + hero.GetSkillValue(DefaultSkills.Riding) + "\n";
		return s + new TextObject("{=FLT0000202}Athletics : ").ToString() + hero.GetSkillValue(DefaultSkills.Athletics) + "\n";
	}

	private bool isAvalible(Hero hero)
	{
		return !hero.IsPrisoner && hero.PartyBelongedTo == null && hero.CurrentSettlement != null;
	}

	public static List<CharacterObject> GetTroopsList(CultureObject culture)
	{
		List<CharacterObject> MainLineUnits = new List<CharacterObject>();
		Stack<CharacterObject> stack = new Stack<CharacterObject>();
		stack.Push(culture.BasicTroop);
		MainLineUnits.Add(culture.BasicTroop);
		stack.Push(culture.EliteBasicTroop);
		MainLineUnits.Add(culture.EliteBasicTroop);
		foreach (Recruit recruit in SubModule.AdditonalTroops)
		{
			CharacterObject character = recruit.getCharacter();
			if (character != null && character.Culture == culture && !MainLineUnits.Contains(character))
			{
				stack.Push(character);
				MainLineUnits.Add(character);
			}
		}
		while (!stack.IsEmpty())
		{
			CharacterObject popped = stack.Pop();
			if (popped.UpgradeTargets == null || popped.UpgradeTargets.Length == 0)
			{
				continue;
			}
			for (int i = 0; i < popped.UpgradeTargets.Length; i++)
			{
				if (i >= 2)
				{
					return MainLineUnits;
				}
				if (!MainLineUnits.Contains(popped.UpgradeTargets[i]))
				{
					MainLineUnits.Add(popped.UpgradeTargets[i]);
					stack.Push(popped.UpgradeTargets[i]);
				}
			}
		}
		return MainLineUnits;
	}

	public static void ChangeFactionRelation(IFaction faction, int amount)
	{
		if (FactionReputation == null)
		{
			FactionReputation = new Dictionary<IFaction, int>();
		}
		if (FactionReputation.TryGetValue(faction, out var value))
		{
			value += amount;
			FactionReputation.Remove(faction);
			FactionReputation.Add(faction, Math.Max(0, value));
		}
		else
		{
			FactionReputation.Add(faction, amount);
		}
		if (followingHero != null && amount > 0 && MBRandom.RandomInt(1000) <= amount)
		{
			ChangeRelationAction.ApplyPlayerRelation(followingHero, 1);
		}
	}

	public static void ChangeLordRelation(Hero hero, int amount)
	{
		if (LordReputation == null)
		{
			LordReputation = new Dictionary<Hero, int>();
		}
		if (LordReputation.TryGetValue(hero, out var value))
		{
			value += amount;
			LordReputation.Remove(hero);
			LordReputation.Add(hero, Math.Max(0, value));
		}
		else
		{
			LordReputation.Add(hero, Math.Max(0, amount));
		}
	}

	public static int GetFactionRelations(IFaction faction)
	{
		if (FactionReputation == null)
		{
			FactionReputation = new Dictionary<IFaction, int>();
			return 0;
		}
		if (FactionReputation.TryGetValue(faction, out var value))
		{
			return value;
		}
		return 0;
	}

	public static int GetLordRelations(Hero hero)
	{
		if (LordReputation == null)
		{
			LordReputation = new Dictionary<Hero, int>();
			return 0;
		}
		if (LordReputation.TryGetValue(hero, out var value))
		{
			return value;
		}
		return 0;
	}

	private void wait_on_tick(MenuCallbackArgs args, CampaignTime time)
	{
		waitingInReserve = false;
		updatePartyMenu(args);
	}

	public static TextObject GetMobilePartyBehaviorText(MobileParty party)
	{
		if (Tracked != party.TargetSettlement)
		{
			Untracked = Tracked;
		}
		Tracked = party.TargetSettlement;
		TextObject textObject;
		if (party.DefaultBehavior == AiBehavior.Hold)
		{
			textObject = new TextObject("{=FLT0000079}Holding");
		}
		else if (party.ShortTermBehavior == AiBehavior.EngageParty && party.ShortTermTargetParty != null)
		{
			textObject = new TextObject("{=FLT0000080}Engaging {TARGET_PARTY}");
			textObject.SetTextVariable("TARGET_PARTY", party.ShortTermTargetParty.Name);
			if (party.ShortTermTargetParty.LeaderHero != null)
			{
				textObject = HyperlinkTexts.GetHeroHyperlinkText(party.ShortTermTargetParty.LeaderHero.EncyclopediaLink, textObject);
			}
		}
		else if (party.DefaultBehavior == AiBehavior.GoAroundParty && party.ShortTermBehavior == AiBehavior.GoToPoint)
		{
			textObject = new TextObject("{=FLT0000081}Chasing {TARGET_PARTY}");
			textObject.SetTextVariable("TARGET_PARTY", party.TargetParty.Name);
			if (party.ShortTermTargetParty != null && party.ShortTermTargetParty.LeaderHero != null && party.ShortTermTargetParty != null && party.ShortTermTargetParty.LeaderHero != null)
			{
				textObject = HyperlinkTexts.GetHeroHyperlinkText(party.ShortTermTargetParty.LeaderHero.EncyclopediaLink, textObject);
			}
		}
		else if (party.ShortTermBehavior == AiBehavior.FleeToPoint && party.ShortTermTargetParty != null)
		{
			textObject = new TextObject("{=FLT0000082}Running from {TARGET_PARTY}");
			textObject.SetTextVariable("TARGET_PARTY", party.ShortTermTargetParty.Name);
			if (party.ShortTermTargetParty.LeaderHero != null)
			{
				textObject = HyperlinkTexts.GetHeroHyperlinkText(party.ShortTermTargetParty.LeaderHero.EncyclopediaLink, textObject);
			}
		}
		else if (party.ShortTermBehavior == AiBehavior.FleeToGate && party.ShortTermTargetParty != null)
		{
			textObject = new TextObject("{=FLT0000083}Running from {TARGET_PARTY} to settlement");
			textObject.SetTextVariable("TARGET_PARTY", party.ShortTermTargetParty.Name);
			if (party.ShortTermTargetParty.LeaderHero != null)
			{
				textObject = HyperlinkTexts.GetHeroHyperlinkText(party.ShortTermTargetParty.LeaderHero.EncyclopediaLink, textObject);
			}
		}
		else if (party.DefaultBehavior == AiBehavior.DefendSettlement)
		{
			textObject = new TextObject("{=FLT0000084}Defending {TARGET_SETTLEMENT}");
			textObject.SetTextVariable("TARGET_SETTLEMENT", party.TargetSettlement.EncyclopediaLinkWithName);
		}
		else if (party.DefaultBehavior == AiBehavior.RaidSettlement)
		{
			textObject = new TextObject("{=FLT0000085}Raiding {TARGET_SETTLEMENT}");
			textObject.SetTextVariable("TARGET_SETTLEMENT", party.TargetSettlement.EncyclopediaLinkWithName);
		}
		else if (party.DefaultBehavior == AiBehavior.BesiegeSettlement)
		{
			textObject = new TextObject("{=FLT0000086}Besieging {TARGET_SETTLEMENT}");
			textObject.SetTextVariable("TARGET_SETTLEMENT", party.TargetSettlement.EncyclopediaLinkWithName);
		}
		else if (party.ShortTermBehavior == AiBehavior.GoToPoint)
		{
			if (party.ShortTermTargetParty != null)
			{
				textObject = new TextObject("{=FLT0000082}Running from {TARGET_PARTY}");
				textObject.SetTextVariable("TARGET_PARTY", party.ShortTermTargetParty.Name);
				if (party.ShortTermTargetParty.LeaderHero != null)
				{
					textObject = HyperlinkTexts.GetHeroHyperlinkText(party.ShortTermTargetParty.LeaderHero.EncyclopediaLink, textObject);
				}
			}
			else if (party.TargetSettlement == null)
			{
				textObject = ((party.DefaultBehavior != AiBehavior.PatrolAroundPoint) ? new TextObject("{=FLT0000090}Gathering Army") : new TextObject("{=FLT0000088}Patrolling"));
			}
			else
			{
				textObject = ((party.DefaultBehavior == AiBehavior.PatrolAroundPoint) ? new TextObject("{=FLT0000087}Patrolling around {TARGET_SETTLEMENT}") : new TextObject("{=FLT0000089}Travelling."));
				textObject.SetTextVariable("TARGET_SETTLEMENT", (party.TargetSettlement != null) ? party.TargetSettlement.EncyclopediaLinkWithName : party.HomeSettlement.EncyclopediaLinkWithName);
			}
		}
		else if (party.ShortTermBehavior == AiBehavior.GoToSettlement)
		{
			if (party.ShortTermBehavior == AiBehavior.GoToSettlement && party.ShortTermTargetSettlement != null && party.ShortTermTargetSettlement != party.TargetSettlement)
			{
				textObject = new TextObject("{=FLT0000091}Running to {TARGET_PARTY}");
				textObject.SetTextVariable("TARGET_PARTY", party.ShortTermTargetSettlement.EncyclopediaLinkWithName);
			}
			else if (party.DefaultBehavior == AiBehavior.GoToSettlement && party.TargetSettlement != null)
			{
				textObject = new TextObject("{=FLT0000092}Travelling to {TARGET_PARTY}");
				textObject.SetTextVariable("TARGET_PARTY", party.TargetSettlement.EncyclopediaLinkWithName);
			}
			else if (party.ShortTermTargetParty != null)
			{
				textObject = new TextObject("{=FLT0000082}Running from {TARGET_PARTY}");
				textObject.SetTextVariable("TARGET_PARTY", party.ShortTermTargetParty.Name);
				if (party.ShortTermTargetParty.LeaderHero != null)
				{
					textObject = HyperlinkTexts.GetHeroHyperlinkText(party.ShortTermTargetParty.LeaderHero.EncyclopediaLink, textObject);
				}
			}
			else
			{
				textObject = new TextObject("{=FLT0000093}Travelling  to a settlement");
			}
		}
		else if (party.ShortTermBehavior == AiBehavior.AssaultSettlement)
		{
			textObject = new TextObject("{=FLT0000094}Attacking {TARGET_SETTLEMENT}");
			textObject.SetTextVariable("TARGET_SETTLEMENT", party.ShortTermTargetSettlement.EncyclopediaLinkWithName);
		}
		else if (party.DefaultBehavior == AiBehavior.EscortParty)
		{
			textObject = new TextObject("{=FLT0000095}Following {TARGET_PARTY}");
			textObject.SetTextVariable("TARGET_PARTY", (party.ShortTermTargetParty != null) ? party.ShortTermTargetParty.Name : party.TargetParty.Name);
			if (party.ShortTermTargetParty != null && party.ShortTermTargetParty.LeaderHero != null)
			{
				textObject = HyperlinkTexts.GetHeroHyperlinkText(party.ShortTermTargetParty.LeaderHero.EncyclopediaLink, textObject);
			}
			else if (party.TargetParty != null && party.TargetParty.LeaderHero != null)
			{
				textObject = HyperlinkTexts.GetHeroHyperlinkText(party.TargetParty.LeaderHero.EncyclopediaLink, textObject);
			}
		}
		else
		{
			textObject = new TextObject("{=FLT0000096}Unknown Behavior");
		}
		return textObject;
	}

	private string getFormation()
	{
		if (Hero.MainHero.CharacterObject.IsRanged && Hero.MainHero.CharacterObject.IsMounted)
		{
			return new TextObject("{=FLT0000097}Horse Archer").ToString();
		}
		if (Hero.MainHero.CharacterObject.IsMounted)
		{
			return new TextObject("{=FLT0000098}Cavalry").ToString();
		}
		if (Hero.MainHero.CharacterObject.IsRanged)
		{
			return new TextObject("{=FLT0000099}Ranged").ToString();
		}
		return new TextObject("{=FLT0000100}Infantry").ToString();
	}

	private bool wait_on_condition(MenuCallbackArgs args)
	{
		return true;
	}

	private void wait_on_init(MenuCallbackArgs args)
	{
		updatePartyMenu(args);
	}

	private void updatePartyMenu(MenuCallbackArgs args)
	{
		if (followingHero == null || followingHero.PartyBelongedTo == null || followingHero.IsDead)
		{
			while (Campaign.Current.CurrentMenuContext != null)
			{
				GameMenu.ExitToLast();
			}
			followingHero = null;
			return;
		}
		if (followingHero != null && followingHero.MapFaction != null && followingHero.MapFaction.Culture != null && followingHero.MapFaction.Culture.EncounterBackgroundMesh != null)
		{
			args.MenuContext.SetBackgroundMeshName(followingHero.MapFaction.Culture.EncounterBackgroundMesh);
		}
		if (followingHero != null && followingHero.PartyBelongedTo != null)
		{
			TextObject text = args.MenuContext.GameMenu.GetText();
			string s = "";
			if (followingHero.PartyBelongedTo.Army == null || followingHero.PartyBelongedTo.AttachedTo == null)
			{
				TextObject text2 = new TextObject("{=FLT0000101}Party Objective");
				s = s + text2.ToString() + " : " + GetMobilePartyBehaviorText(followingHero.PartyBelongedTo)?.ToString() + "\n";
			}
			else
			{
				TextObject text3 = new TextObject("{=FLT0000102}Army Objective");
				s = s + text3.ToString() + " : " + GetMobilePartyBehaviorText(followingHero.PartyBelongedTo.Army.LeaderParty)?.ToString() + "\n";
			}
			TextObject text4 = new TextObject("{=FLT0000103}Enlistment Time");
			TextObject text5 = new TextObject("{=FLT0000104}Enlistment Tier");
			TextObject text6 = new TextObject("{=FLT0000105}Formation");
			TextObject text7 = new TextObject("{=FLT0000106}Wage");
			TextObject text8 = new TextObject("{=FLT0000107}Current Experience");
			TextObject text9 = new TextObject("{=FLT0000108}Next Level Experience");
			TextObject text10 = new TextObject("{=FLT0000109}When not fighting");
			s = s + text4.ToString() + " : " + enlistTime.ToString() + "\n";
			s = s + text5.ToString() + " : " + EnlistTier + "\n";
			s = s + text6.ToString() + " : " + getFormation() + "\n";
			s = s + text7.ToString() + " : " + ((MobileParty.MainParty.TotalWage > 0) ? (wage() - MobileParty.MainParty.TotalWage + "(+" + MobileParty.MainParty.TotalWage + ")") : wage().ToString()) + "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">\n";
			s = s + text8.ToString() + " : " + xp + "\n";
			if (EnlistTier < 7)
			{
				s = s + text9.ToString() + " : " + NextlevelXP[EnlistTier] + "\n";
			}
			s = s + text10.ToString() + " " + getAssignmentDescription(currentAssignment) + "\n";
			text.SetTextVariable("PARTY_LEADER", followingHero.EncyclopediaLinkWithName);
			text.SetTextVariable("PARTY_TEXT", s);
		}
	}

	private static void hidePlayerParty()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		if (((PartyVisual)PartyBase.MainParty.Visuals).HumanAgentVisuals != null)
		{
			((PartyVisual)PartyBase.MainParty.Visuals).HumanAgentVisuals.GetEntity().SetVisibilityExcludeParents(visible: false);
		}
		if (((PartyVisual)PartyBase.MainParty.Visuals).MountAgentVisuals != null)
		{
			((PartyVisual)PartyBase.MainParty.Visuals).MountAgentVisuals.GetEntity().SetVisibilityExcludeParents(visible: false);
		}
	}

	private static void showPlayerParty()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		if (((PartyVisual)PartyBase.MainParty.Visuals).HumanAgentVisuals != null)
		{
			((PartyVisual)PartyBase.MainParty.Visuals).HumanAgentVisuals.GetEntity().SetVisibilityExcludeParents(visible: true);
		}
		if (((PartyVisual)PartyBase.MainParty.Visuals).MountAgentVisuals != null)
		{
			((PartyVisual)PartyBase.MainParty.Visuals).MountAgentVisuals.GetEntity().SetVisibilityExcludeParents(visible: true);
		}
	}

	private static void UpdateDiplomacy()
	{
		foreach (IFaction faction in Campaign.Current.Factions)
		{
			if (faction != null && faction.IsAtWarWith(followingHero.MapFaction) && !faction.IsAtWarWith(Clan.PlayerClan.MapFaction))
			{
				DeclareWarAction.Apply(faction, Clan.PlayerClan.MapFaction);
			}
			else if (faction != null && !faction.IsAtWarWith(followingHero.MapFaction) && faction.IsAtWarWith(Clan.PlayerClan.MapFaction))
			{
				MakePeaceAction.Apply(faction, Clan.PlayerClan.MapFaction);
			}
		}
	}

	private static void UndoDiplomacy()
	{
		if (MobileParty.MainParty.CurrentSettlement != null)
		{
			LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
		}
		foreach (IFaction faction in Campaign.Current.Factions)
		{
			if (faction != null && faction.IsAtWarWith(followingHero.MapFaction) && !faction.IsBanditFaction)
			{
				MakePeaceAction.Apply(Hero.MainHero.MapFaction, faction);
			}
		}
	}

	public string getAssignmentDescription(Assignment assignment)
	{
		return assignment switch
		{
			Assignment.Grunt_Work => new TextObject("{=FLT0000110}You are currently assigned to perform grunt work.  Most tasks are unpleasant, tiring or involve menial labor. (Passive Daily Athletics XP)").ToString(), 
			Assignment.Guard_Duty => new TextObject("{=FLT0000111}You are currently assigned to guard duty.  You spend many sleepless nights keeping watch for signs of intruders. (Passive Daily Scouting XP)").ToString(), 
			Assignment.Cook => new TextObject("{=FLT0000112}You are currently assigned as one of the cooks.  You prepare the camp meals with whatever limited ingredients avalible. (Passive Daily Steward XP)").ToString(), 
			Assignment.Foraging => new TextObject("{=FLT0000113}You are currently assigned to forage.  You ride through the nearby countryside looking for food. (Passive Daily Riding XP and Daily Food To Party)").ToString(), 
			Assignment.Surgeon => new TextObject("{=FLT0000114}You are currently assigned as the surgeon. You spend your time taking care of the wounded men. (Medicine XP from party)").ToString(), 
			Assignment.Engineer => new TextObject("{=FLT0000115}You are currently assigned as the engineer.  The party relies on your knowledge of siegecraft to build war machines. (Engineering XP from party)").ToString(), 
			Assignment.Quartermaster => new TextObject("{=FLT0000116}You are currently assigned to quartermaster. You make sure that the party is well supplied and the troops get paid on time. (Steward XP from party)").ToString(), 
			Assignment.Scout => new TextObject("{=FLT0000117}You are currently assigned to lead the scouting parties.  You and your men spend their time looking for signs of enemy parties and easy passages through difficult terrain.  (Scouting XP from party)").ToString(), 
			Assignment.Sergeant => new TextObject("{=FLT0000118}You are currently assigned as one of the sergeants.  You drill the men for war and discipline anyone who steps out of line. (Passive Daily Leadership XP and Daily XP To Troops In Party)").ToString(), 
			Assignment.Strategist => new TextObject("{=FLT0000119}You are currently assigned as the strategist.  You spend your time in the commander's tent discussing war plans. (Tactics XP from party)").ToString(), 
			_ => new TextObject("{=FLT0000120}You have no current assigned duties.  You spend your idle time drinking, gambling, and chatting with the idle soilders.").ToString(), 
		};
	}

	public static int print(int value)
	{
		return value;
	}

	public override void SyncData(IDataStore dataStore)
	{
		MobileParty.MainParty.IsActive = true;
		dataStore.SyncData("_following_hero", ref followingHero);
		dataStore.SyncData("_assigned_role", ref currentAssignment);
		dataStore.SyncData("_vassal_offers", ref kingVassalOffered);
		dataStore.SyncData("_enlist_tier", ref EnlistTier);
		dataStore.SyncData("_enlist_xp", ref xp);
		dataStore.SyncData("_faction_reputation", ref FactionReputation);
		dataStore.SyncData("_retirement_xp", ref retirementXP);
		dataStore.SyncData("_lord_reputation", ref LordReputation);
		dataStore.SyncData("_enlist_date", ref enlistTime);
		dataStore.SyncData("_old_inventory", ref oldItems);
		dataStore.SyncData("_old_gear", ref oldGear);
		dataStore.SyncData("_ongoing_event", ref OngoinEvent);
		dataStore.SyncData("_wainting_in_reserve", ref waitingInReserve);
		dataStore.SyncData("_tournament_prizes", ref tournamentPrizes);
		dataStore.SyncData("_companion_old_gear", ref CompanionOldGear);
	}
}
