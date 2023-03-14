using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.ScreenSystem;

namespace ServeAsSoldier;

public class EquipmentSelectorBehavior : CampaignBehaviorBase
{
	public static GauntletLayer layer;

	public static GauntletMovie gauntletMovie;

	public static SASEquipmentSelectorVM equipmentSelectorVM;

	public static void CreateVMLayer(List<ItemObject> list, string equipmentType)
	{
		if (layer == null)
		{
			layer = new GauntletLayer(1001);
			if (equipmentSelectorVM == null)
			{
				equipmentSelectorVM = new SASEquipmentSelectorVM(list, equipmentType);
			}
			equipmentSelectorVM.RefreshValues();
			gauntletMovie = (GauntletMovie)layer.LoadMovie("SASEquipmentSelection", equipmentSelectorVM);
			layer.InputRestrictions.SetInputRestrictions();
			ScreenManager.TopScreen.AddLayer(layer);
			layer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(layer);
		}
	}

	public static void DeleteVMLayer()
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (layer != null)
		{
			layer.InputRestrictions.ResetInputRestrictions();
			layer.IsFocusLayer = false;
			if (gauntletMovie != null)
			{
				layer.ReleaseMovie(gauntletMovie);
			}
			topScreen.RemoveLayer(layer);
		}
		layer = null;
		gauntletMovie = null;
		equipmentSelectorVM = null;
	}

	public override void RegisterEvents()
	{
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
