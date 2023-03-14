using System.Reflection;
using HarmonyLib;
using SandBox.ViewModelCollection.Nameplate;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(SettlementNameplateVM), "RefreshBindValues")]
internal class PartyNamePlateTrackPatch
{
	private static void Postfix(SettlementNameplateVM __instance)
	{
		if (__instance.Settlement == Test.Tracked)
		{
			typeof(SettlementNameplateVM).GetMethod("Track", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[0]);
		}
		if (__instance.Settlement == Test.Untracked)
		{
			typeof(SettlementNameplateVM).GetMethod("Untrack", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[0]);
		}
	}
}
