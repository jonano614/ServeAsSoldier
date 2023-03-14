using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(BehaviorComponent), "InformSergeantPlayer")]
internal class BattleCommandsPatch
{
	private static void Postfix(BehaviorComponent __instance)
	{
		if (Test.followingHero == null || Test.EnlistTier >= 6 || (!__instance.Formation.Team.IsPlayerTeam && !__instance.Formation.Team.IsPlayerAlly) || (!Test.AllBattleCommands && !CommandForPlayerFormation(__instance.Formation.PrimaryClass)))
		{
			return;
		}
		TextObject behaviorString = __instance.GetBehaviorString();
		if (behaviorString != null && __instance.GetType() != typeof(BehaviorGeneral) && __instance.GetType() != typeof(BehaviorProtectGeneral))
		{
			MBInformationManager.AddQuickInformation(new TextObject(((__instance.GetType() != typeof(BehaviorHorseArcherSkirmish)) ? (formatclass(__instance.Formation.PrimaryClass) + " ") : "") + behaviorString.ToString().ToLower()), 4000, Test.followingHero.CharacterObject);
			if (__instance.GetType() == typeof(BehaviorHorseArcherSkirmish) || __instance.GetType() == typeof(BehaviorAssaultWalls) || __instance.GetType() == typeof(BehaviorCharge) || __instance.GetType() == typeof(BehaviorSkirmish) || __instance.GetType() == typeof(BehaviorTacticalCharge))
			{
				SoundEvent.PlaySound2D(SoundEvent.GetEventIdFromString("event:/ui/mission/horns/attack"));
			}
			else
			{
				SoundEvent.PlaySound2D(SoundEvent.GetEventIdFromString("event:/ui/mission/horns/move"));
			}
		}
	}

	private static bool CommandForPlayerFormation(FormationClass formation)
	{
		if (formation == FormationClass.Infantry && !Hero.MainHero.CharacterObject.IsRanged && !Hero.MainHero.CharacterObject.IsMounted)
		{
			return true;
		}
		if (formation == FormationClass.Ranged && Hero.MainHero.CharacterObject.IsRanged && !Hero.MainHero.CharacterObject.IsMounted)
		{
			return true;
		}
		if (formation == FormationClass.Cavalry && !Hero.MainHero.CharacterObject.IsRanged && Hero.MainHero.CharacterObject.IsMounted)
		{
			return true;
		}
		if (formation == FormationClass.HorseArcher && Hero.MainHero.CharacterObject.IsRanged && Hero.MainHero.CharacterObject.IsMounted)
		{
			return true;
		}
		return false;
	}

	private static string formatclass(FormationClass primaryClass)
	{
		return primaryClass switch
		{
			FormationClass.Infantry => new TextObject("{=FLT0000140}Infantry").ToString(), 
			FormationClass.Ranged => new TextObject("{=FLT0000141}Archers").ToString(), 
			FormationClass.Cavalry => new TextObject("{=FLT0000142}Cavalry").ToString(), 
			FormationClass.HorseArcher => new TextObject("{=FLT0000143}Horse archers").ToString(), 
			FormationClass.NumberOfDefaultFormations => new TextObject("{=FLT0000144}Skirmishers").ToString(), 
			FormationClass.HeavyInfantry => new TextObject("{=FLT0000145}Heavy infantry").ToString(), 
			FormationClass.LightCavalry => new TextObject("{=FLT0000146}Light cavalry").ToString(), 
			FormationClass.HeavyCavalry => new TextObject("{=FLT0000147}Heavy cavalry").ToString(), 
			_ => "", 
		};
	}
}
