using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;

namespace ServeAsSoldier;

public class SaveDefiner : SaveableTypeDefiner
{
	public SaveDefiner()
		: base(1436500012)
	{
	}

	protected override void DefineEnumTypes()
	{
		AddEnumDefinition(typeof(Test.Assignment), 1);
	}

	protected override void DefineClassTypes()
	{
	}

	protected override void DefineContainerDefinitions()
	{
		ConstructContainerDefinition(typeof(Dictionary<CharacterObject, Equipment>));
	}
}
