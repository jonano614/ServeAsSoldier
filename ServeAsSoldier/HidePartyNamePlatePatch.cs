using HarmonyLib;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem.Party;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(PartyNameplateVM), "RefreshBinding")]
internal class HidePartyNamePlatePatch
{
	private static void Postfix(PartyNameplateVM __instance)
	{
		if (__instance.Party == MobileParty.MainParty)
		{
			if (Test.followingHero != null)
			{
				__instance.IsMainParty = false;
				((NameplateVM)__instance).IsVisibleOnMap = false;
			}
			else
			{
				__instance.IsMainParty = true;
				((NameplateVM)__instance).IsVisibleOnMap = true;
			}
		}
	}
}
