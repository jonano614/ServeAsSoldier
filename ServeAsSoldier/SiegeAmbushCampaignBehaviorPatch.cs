using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;

namespace ServeAsSoldier;

[HarmonyPatch(typeof(SiegeAmbushCampaignBehavior), "OnPlayerSiegeStarted")]
internal class SiegeAmbushCampaignBehaviorPatch
{
	private static bool Prefix(SiegeAmbushCampaignBehavior __instance)
	{
		if (Test.followingHero != null)
		{
			var siegeEvent = Test.followingHero.PartyBelongedTo.SiegeEvent;
			if (siegeEvent != null && siegeEvent.BesiegerCamp.IsPreparationComplete)
			{
				typeof(SiegeAmbushCampaignBehavior).GetField("_lastAmbushTime", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SetValue(__instance, CampaignTime.Now);
			}
			return false;
		}
		return true;
	}
}
