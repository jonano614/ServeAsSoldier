using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(EncounterGameMenuBehavior), "game_menu_encounter_attack_on_consequence")]
internal class NoHorseSiegePatch
{
	private static bool Prefix(MenuCallbackArgs args)
	{
		if (Test.followingHero != null && Test.followingHero.PartyBelongedTo.SiegeEvent != null && Hero.MainHero.CharacterObject.Equipment[EquipmentIndex.ArmorItemEndSlot].Item != null && ContainsParty(Test.followingHero.PartyBelongedTo.MapEvent.PartiesOnSide(BattleSideEnum.Attacker), Test.followingHero.PartyBelongedTo))
		{
			InformationManager.DisplayMessage(new InformationMessage("{=FLT0000308}Dismount from horse first before joining siege"));
			return false;
		}
		return true;
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
}
