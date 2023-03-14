using HarmonyLib;
using SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(DefaultNotificationsCampaignBehavior), "OnCompanionRemoved")]
internal class DisableFireNorificationPatch
{
	private static bool Prefix(Hero hero, RemoveCompanionAction.RemoveCompanionDetail detail)
	{
		if (detail == RemoveCompanionAction.RemoveCompanionDetail.ByTurningToLord)
		{
			TextObject textObject = new TextObject("{=2Lj0WkSF}{COMPANION.NAME} is now a {?COMPANION.GENDER}noblewoman{?}lord{\\?} of the {KINGDOM}.");
			textObject.SetCharacterProperties("COMPANION", hero.CharacterObject);
			textObject.SetTextVariable("KINGDOM", Clan.PlayerClan.Kingdom.Name);
			MBInformationManager.AddQuickInformation(textObject, 0, null, "event:/ui/notification/relation");
		}
		return false;
	}
}
