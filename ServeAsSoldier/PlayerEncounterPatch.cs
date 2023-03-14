using HarmonyLib;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(PlayerEncounter), "EncounterSettlement", MethodType.Getter)]
internal class PlayerEncounterPatch
{
	private static void Postfix(ref Settlement __result)
	{
		if (Test.followingHero != null && Test.followingHero.PartyBelongedTo != null && Test.followingHero.PartyBelongedTo.CurrentSettlement != null)
		{
			__result = Test.followingHero.PartyBelongedTo.CurrentSettlement;
		}
	}
}
