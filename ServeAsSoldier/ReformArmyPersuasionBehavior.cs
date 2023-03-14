using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace ServeAsSoldier;

internal class ReformArmyPersuasionBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
	}

	private void OnSessionLaunched(CampaignGameStarter campaignStarter)
	{
		TextObject textObject1 = new TextObject("{}I am here to rejoin your warband!  Wait!  Were you just hiding here this whole time?!  How about you stop being such a scared pussy bitch and lead the men like you are suppose to?!");
		campaignStarter.AddPlayerLine("reform_legion_start", "lord_talk_speak_diplomacy_2", "reform_legion", textObject1.ToString(), () => CharacterObject.OneToOneConversationCharacter.HeroObject != null && CharacterObject.OneToOneConversationCharacter.HeroObject.PartyBelongedTo == null && !CharacterObject.OneToOneConversationCharacter.HeroObject.Clan.IsMinorFaction && Hero.MainHero.Clan.Kingdom == null && CharacterObject.OneToOneConversationCharacter.HeroObject.Clan.Kingdom != null && Test.GetLordRelations(CharacterObject.OneToOneConversationCharacter.HeroObject) > 1000 && CharacterObject.OneToOneConversationCharacter.HeroObject.CurrentSettlement != null && Mission.Current == null, delegate
		{
			MobilePartyHelper.SpawnLordParty(CharacterObject.OneToOneConversationCharacter.HeroObject, CharacterObject.OneToOneConversationCharacter.HeroObject.CurrentSettlement);
			SubModule.test.JoinPartyAction();
		});
		campaignStarter.AddDialogLine("persuasion_reform_legion_answer", "reform_legion", "close_window", "{}Okay I will[rf:very_negative_hi, rb:very_negative]", () => true, null);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
