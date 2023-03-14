using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace ServeAsSoldier;

public class SubModule : MBSubModuleBase
{
	public static Settings settings;

	public static readonly List<Action> ActionsToExecuteNextTick = new List<Action>();

	public static List<Recruit> AdditonalTroops;

	public static Test test;

	protected override void OnBeforeInitialModuleScreenSetAsRoot()
	{
		base.OnBeforeInitialModuleScreenSetAsRoot();
	}

	public override void OnMissionBehaviorInitialize(Mission mission)
	{
		base.OnMissionBehaviorInitialize(mission);
		mission.AddMissionBehavior(new SoldierMission());
	}

	protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
	{
		if (game.GameType is Campaign)
		{
			test = new Test();
			((CampaignGameStarter)gameStarterObject).AddBehavior(test);
			((CampaignGameStarter)gameStarterObject).AddBehavior(new WanderSoldierBehavior());
			((CampaignGameStarter)gameStarterObject).AddBehavior(new SASConversationsBehavior());
			((CampaignGameStarter)gameStarterObject).AddBehavior(new ServeAsSoldierPerks());
			((CampaignGameStarter)gameStarterObject).AddBehavior(new ReformArmyPersuasionBehavior());
			((CampaignGameStarter)gameStarterObject).AddBehavior(new TownRobberEvent());
			((CampaignGameStarter)gameStarterObject).AddBehavior(new AbonadonedOrphanEvent());
			((CampaignGameStarter)gameStarterObject).AddBehavior(new ExtortionByDesertersEvent());
			((CampaignGameStarter)gameStarterObject).AddBehavior(new IllegalPoachersEvents());
			((CampaignGameStarter)gameStarterObject).AddBehavior(new BanditAmbushEvent());
			((CampaignGameStarter)gameStarterObject).AddBehavior(new RivalGangEvent());
			((CampaignGameStarter)gameStarterObject).AddBehavior(new TrainTroopsEvent());
			gameStarterObject.AddModel(new SoldierPartyHealingModel());
			gameStarterObject.AddModel(new OldPregnancyModel());
			gameStarterObject.AddModel(new model());
			gameStarterObject.AddModel(new model2());
		}
	}

	protected override void OnSubModuleLoad()
	{
		base.OnSubModuleLoad();
		loadSettings();
		loadRecruit();
		new Harmony("ServeAsSoldier").PatchAll();
	}

	protected override void OnApplicationTick(float dt)
	{
		base.OnApplicationTick(dt);
		foreach (Action action in ActionsToExecuteNextTick)
		{
			action();
		}
		ActionsToExecuteNextTick.Clear();
	}

	public static void ExecuteActionOnNextTick(Action action)
	{
		if (action != null)
		{
			ActionsToExecuteNextTick.Add(action);
		}
	}

	private void loadSettings()
	{
		string path = Path.Combine(BasePath.Name, "Modules/ServeAsSoldier/settings.xml");
		XmlSerializer ser = new XmlSerializer(typeof(Settings));
		settings = ser.Deserialize(File.OpenRead(path)) as Settings;
	}

	private void loadRecruit()
	{
		string path = Path.Combine(BasePath.Name, "Modules/ServeAsSoldier/ModuleData/Additional_Troops.xml");
		XmlSerializer ser = new XmlSerializer(typeof(List<Recruit>));
		AdditonalTroops = ser.Deserialize(File.OpenRead(path)) as List<Recruit>;
	}
}
