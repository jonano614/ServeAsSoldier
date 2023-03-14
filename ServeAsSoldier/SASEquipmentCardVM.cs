using System;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace ServeAsSoldier;

public class SASEquipmentCardVM : ViewModel
{
	private MBBindingList<BindingListStringItem> _nameText;

	private MBBindingList<ItemFlagVM> _ItemFlagList;

	private MBBindingList<ItemMenuTooltipPropertyVM> _itemProperties;

	private ImageIdentifierVM _image;

	public ItemObject _item;

	private Func<WeaponComponentData, ItemObject.ItemUsageSetFlags> _getItemUsageSetFlags;

	private SASEquipmentSelectorVM _container;

	private string _equipmentType;

	public MBBindingList<ItemMenuTooltipPropertyVM> ItemProperties
	{
		get
		{
			return _itemProperties;
		}
		set
		{
			if (value != _itemProperties)
			{
				_itemProperties = value;
				base.OnPropertyChangedWithValue((object)value, "ItemProperties");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<BindingListStringItem> ItemName
	{
		get
		{
			return _nameText;
		}
		set
		{
			if (value != _nameText)
			{
				_nameText = value;
				OnPropertyChanged("ItemName");
			}
		}
	}

	[DataSourceProperty]
	public ImageIdentifierVM Image
	{
		get
		{
			return _image;
		}
		set
		{
			if (value != _image)
			{
				_image = value;
				base.OnPropertyChangedWithValue((object)value, "Image");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<ItemFlagVM> ItemFlagList
	{
		get
		{
			return _ItemFlagList;
		}
		set
		{
			if (value != _ItemFlagList)
			{
				_ItemFlagList = value;
				base.OnPropertyChangedWithValue((object)value, "ItemFlagList");
			}
		}
	}

	public SASEquipmentCardVM(ItemObject item, SASEquipmentSelectorVM container, string equipmentType)
	{
		ItemName = new MBBindingList<BindingListStringItem>();
		if (item != null)
		{
			ItemName.Add(new BindingListStringItem(item.Name.ToString()));
			Image = new ImageIdentifierVM(item);
		}
		else
		{
			ItemName.Add(new BindingListStringItem("Empty"));
		}
		ItemFlagList = new MBBindingList<ItemFlagVM>();
		ItemProperties = new MBBindingList<ItemMenuTooltipPropertyVM>();
		_item = item;
		_container = container;
		_equipmentType = equipmentType;
		_getItemUsageSetFlags = GetItemUsageSetFlag;
		if (item != null && item.WeaponComponent != null && item.WeaponComponent.Item != null)
		{
			AddWeaponItemFlags(ItemFlagList, item.WeaponComponent.Item.GetWeaponWithUsageIndex(0));
		}
		if (item != null)
		{
			AddGeneralItemFlags(ItemFlagList, item);
		}
		if (item != null && item.HorseComponent != null)
		{
			AddHorseItemFlags(ItemFlagList, item);
		}
		if (item != null)
		{
			CreateColoredProperty(ItemProperties, "", item.Value + "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\"/>", UIColors.Gold, 1, null, TooltipProperty.TooltipPropertyFlags.Cost);
		}
		if (item != null && item.Culture != null && item.Culture.Name != null)
		{
			CreateColoredProperty(ItemProperties, "Culture: ", item.Culture.Name.ToString(), Color.FromUint(item.Culture.Color));
		}
		else if (item != null)
		{
			CreateColoredProperty(ItemProperties, "Culture: ", "No Culture", UIColors.Gold);
		}
		if (item != null && item.RelevantSkill != null && item.Difficulty > 0)
		{
			AddSkillRequirement(item, ItemProperties, isComparison: false);
		}
		int type;
		if (item != null && item.HorseComponent != null)
		{
			MBBindingList<ItemMenuTooltipPropertyVM> itemProperties = ItemProperties;
			string definition = new TextObject("{=08abd5af7774d311cadc3ed900b47754}Type: ").ToString();
			type = (int)item.Type;
			CreateProperty(itemProperties, definition, GameTexts.FindText("str_inventory_type_" + type).ToString());
			AddIntProperty(new TextObject("{=mountTier}Mount Tier: "), (int)(item.Tier + 1));
			AddIntProperty(new TextObject("{=c7638a0869219ae845de0f660fd57a9d}Charge Damage: "), new EquipmentElement(item).GetModifiedMountCharge(in EquipmentElement.Invalid));
			AddIntProperty(new TextObject("{=c7638a0869219ae845de0f660fd57a9d}Charge Damage: "), new EquipmentElement(item).GetModifiedMountSpeed(in EquipmentElement.Invalid));
			AddIntProperty(new TextObject("{=3025020b83b218707499f0de3135ed0a}Maneuver: "), new EquipmentElement(item).GetModifiedMountManeuver(in EquipmentElement.Invalid));
			AddIntProperty(GameTexts.FindText("str_hit_points"), new EquipmentElement(item).GetModifiedMountHitPoints());
			if (item.HasHorseComponent && item.HorseComponent.IsMount)
			{
				CreateProperty(ItemProperties, new TextObject("{=9sxECG6e}Mount Type: ").ToString(), item.ItemCategory.GetName().ToString());
			}
		}
		if (item != null && item.WeaponComponent != null)
		{
			WeaponComponentData weaponWithUsageIndex = item.WeaponComponent.Item.GetWeaponWithUsageIndex(0);
			CreateProperty(ItemProperties, new TextObject("{=8cad4a279770f269c4bb0dc7a357ee1e}Class: ").ToString(), GameTexts.FindText("str_inventory_weapon", ((int)weaponWithUsageIndex.WeaponClass).ToString()).ToString());
			if (item.BannerComponent == null)
			{
				AddIntProperty(new TextObject("{=weaponTier}Weapon Tier: "), (int)(item.Tier + 1));
			}
			ItemObject.ItemTypeEnum itemTypeFromWeaponClass = WeaponComponentData.GetItemTypeFromWeaponClass(weaponWithUsageIndex.WeaponClass);
			if (itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.OneHandedWeapon || itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.TwoHandedWeapon || itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.Polearm)
			{
				if (weaponWithUsageIndex.SwingDamageType != DamageTypes.Invalid)
				{
					AddIntProperty(new TextObject("{=345a87fcc69f626ae3916939ef2fc135}Swing Speed: "), new EquipmentElement(item).GetModifiedSwingSpeedForUsage(0));
					CreateProperty(ItemProperties, GameTexts.FindText("str_swing_damage").ToString(), ItemHelper.GetSwingDamageText(weaponWithUsageIndex, new EquipmentElement(item).ItemModifier).ToString());
				}
				if (weaponWithUsageIndex.ThrustDamageType != DamageTypes.Invalid)
				{
					AddIntProperty(GameTexts.FindText("str_thrust_speed"), new EquipmentElement(item).GetModifiedThrustSpeedForUsage(0));
					CreateProperty(ItemProperties, GameTexts.FindText("str_thrust_damage").ToString(), ItemHelper.GetThrustDamageText(weaponWithUsageIndex, new EquipmentElement(item).ItemModifier).ToString());
				}
				AddIntProperty(new TextObject("{=c6e4c8588ca9e42f6e1b47b11f0f367b}Length: "), weaponWithUsageIndex.WeaponLength);
				AddIntProperty(new TextObject("{=ca8b1e8956057b831dfc665f54bae4b0}Handling: "), new EquipmentElement(item).GetModifiedHandlingForUsage(0));
			}
			if (itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.Thrown)
			{
				AddIntProperty(new TextObject("{=5fa36d2798479803b4518a64beb4d732}Weapon Length: "), weaponWithUsageIndex.WeaponLength);
				CreateProperty(ItemProperties, new TextObject("{=c9c5dfed2ca6bcb7a73d905004c97b23}Damage: ").ToString(), ItemHelper.GetMissileDamageText(weaponWithUsageIndex, new EquipmentElement(item).ItemModifier).ToString());
				AddIntProperty(GameTexts.FindText("str_missile_speed"), new EquipmentElement(item).GetModifiedMissileSpeedForUsage(0));
				AddIntProperty(new TextObject("{=5dec16fa0be433ade3c4cb0074ef366d}Accuracy: "), weaponWithUsageIndex.Accuracy);
				AddIntProperty(new TextObject("{=05fdfc6e238429753ef282f2ce97c1f8}Stack Amount: "), new EquipmentElement(item).GetModifiedStackCountForUsage(0));
			}
			if (itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.Shield)
			{
				AddIntProperty(new TextObject("{=74dc1908cb0b990e80fb977b5a0ef10d}Speed: "), new EquipmentElement(item).GetModifiedSwingSpeedForUsage(0));
				AddIntProperty(GameTexts.FindText("str_hit_points"), new EquipmentElement(item).GetModifiedMaximumHitPointsForUsage(0));
			}
			if (itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.Bow || itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.Crossbow)
			{
				AddIntProperty(new TextObject("{=74dc1908cb0b990e80fb977b5a0ef10d}Speed: "), new EquipmentElement(item).GetModifiedSwingSpeedForUsage(0));
				CreateProperty(ItemProperties, new TextObject("{=c9c5dfed2ca6bcb7a73d905004c97b23}Damage: ").ToString(), ItemHelper.GetThrustDamageText(weaponWithUsageIndex, new EquipmentElement(item).ItemModifier).ToString());
				AddIntProperty(new TextObject("{=5dec16fa0be433ade3c4cb0074ef366d}Accuracy: "), weaponWithUsageIndex.Accuracy);
				AddIntProperty(GameTexts.FindText("str_missile_speed"), new EquipmentElement(item).GetModifiedMissileSpeedForUsage(0));
				if (itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.Crossbow)
				{
					AddIntProperty(new TextObject("{=6adabc1f82216992571c3e22abc164d7}Ammo Limit: "), weaponWithUsageIndex.MaxDataValue);
				}
			}
			if (weaponWithUsageIndex.IsAmmo)
			{
				if (itemTypeFromWeaponClass != ItemObject.ItemTypeEnum.Arrows && itemTypeFromWeaponClass != ItemObject.ItemTypeEnum.Bolts)
				{
					AddIntProperty(new TextObject("{=5dec16fa0be433ade3c4cb0074ef366d}Accuracy: "), weaponWithUsageIndex.Accuracy);
				}
				CreateProperty(ItemProperties, new TextObject("{=c9c5dfed2ca6bcb7a73d905004c97b23}Damage: ").ToString(), ItemHelper.GetThrustDamageText(weaponWithUsageIndex, new EquipmentElement(item).ItemModifier).ToString());
				AddIntProperty(new TextObject("{=05fdfc6e238429753ef282f2ce97c1f8}Stack Amount: "), new EquipmentElement(item).GetModifiedStackCountForUsage(0));
			}
		}
		if (item == null || item.ArmorComponent == null)
		{
			return;
		}
		AddIntProperty(new TextObject("{=armorTier}Armor Tier: "), (int)(item.Tier + 1));
		MBBindingList<ItemMenuTooltipPropertyVM> itemProperties2 = ItemProperties;
		string definition2 = new TextObject("{=08abd5af7774d311cadc3ed900b47754}Type: ").ToString();
		type = (int)item.Type;
		CreateProperty(itemProperties2, definition2, GameTexts.FindText("str_inventory_type_" + type).ToString());
		if (new EquipmentElement(item).GetModifiedHeadArmor() != 0)
		{
			CreateProperty(ItemProperties, GameTexts.FindText("str_head_armor").ToString(), new EquipmentElement(item).GetModifiedHeadArmor().ToString());
		}
		if (item.ArmorComponent.BodyArmor != 0)
		{
			if (GetItemTypeWithItemObject(item) == EquipmentIndex.HorseHarness)
			{
				CreateProperty(ItemProperties, new TextObject("{=305cf7f98458b22e9af72b60a131714f}Horse Armor: ").ToString(), new EquipmentElement(item).GetModifiedMountBodyArmor().ToString());
			}
			else
			{
				CreateProperty(ItemProperties, GameTexts.FindText("str_body_armor").ToString(), new EquipmentElement(item).GetModifiedBodyArmor().ToString());
			}
		}
		if (new EquipmentElement(item).GetModifiedLegArmor() != 0)
		{
			CreateProperty(ItemProperties, GameTexts.FindText("str_leg_armor").ToString(), new EquipmentElement(item).GetModifiedLegArmor().ToString());
		}
		if (new EquipmentElement(item).GetModifiedArmArmor() != 0)
		{
			CreateProperty(ItemProperties, new TextObject("{=cf61cce254c7dca65be9bebac7fb9bf5}Arm Armor: ").ToString(), new EquipmentElement(item).GetModifiedArmArmor().ToString());
		}
	}

	public void Apply()
	{
		foreach (CharacterObject selectedCharacter in _container.selectedCharacters)
		{
			EquipmentElement modifiedEquipment = default(EquipmentElement);
			if (_item != null)
			{
				if (_item.ArmorComponent != null)
				{
					modifiedEquipment = new EquipmentElement(_item, MBObjectManager.Instance.GetObject<ItemModifier>("sas_armor"));
				}
				else if (_item.HorseComponent != null)
				{
					modifiedEquipment = new EquipmentElement(_item, MBObjectManager.Instance.GetObject<ItemModifier>("sas_horse"));
				}
				else if (_item.WeaponComponent != null)
				{
					modifiedEquipment = new EquipmentElement(_item, MBObjectManager.Instance.GetObject<ItemModifier>("sas_weapon"));
				}
			}
			if (selectedCharacter != CharacterObject.PlayerCharacter && !Test.CompanionOldGear.ContainsKey(selectedCharacter))
			{
				Test.CompanionOldGear.Add(selectedCharacter, new Equipment(selectedCharacter.FirstBattleEquipment));
			}
			selectedCharacter.FirstBattleEquipment[ToEquipmentSlot(_equipmentType)] = modifiedEquipment;
		}
		EquipmentSelectorBehavior.DeleteVMLayer();
	}

	public static EquipmentIndex ToEquipmentSlot(string equipment)
	{
		return equipment switch
		{
			"Wep0" => EquipmentIndex.WeaponItemBeginSlot, 
			"Wep1" => EquipmentIndex.Weapon1, 
			"Wep2" => EquipmentIndex.Weapon2, 
			"Wep3" => EquipmentIndex.Weapon3, 
			"Head" => EquipmentIndex.NumAllWeaponSlots, 
			"Cape" => EquipmentIndex.Cape, 
			"Body" => EquipmentIndex.Body, 
			"Gloves" => EquipmentIndex.Gloves, 
			"Leg" => EquipmentIndex.Leg, 
			"Horse" => EquipmentIndex.ArmorItemEndSlot, 
			"Harness" => EquipmentIndex.HorseHarness, 
			_ => EquipmentIndex.None, 
		};
	}

	public EquipmentIndex GetItemTypeWithItemObject(ItemObject item)
	{
		if (item == null)
		{
			return EquipmentIndex.None;
		}
		switch (item.Type)
		{
		case ItemObject.ItemTypeEnum.Horse:
			return EquipmentIndex.ArmorItemEndSlot;
		case ItemObject.ItemTypeEnum.Arrows:
			return EquipmentIndex.WeaponItemBeginSlot;
		case ItemObject.ItemTypeEnum.Bolts:
			return EquipmentIndex.WeaponItemBeginSlot;
		case ItemObject.ItemTypeEnum.Shield:
			return EquipmentIndex.WeaponItemBeginSlot;
		case ItemObject.ItemTypeEnum.HeadArmor:
			return EquipmentIndex.NumAllWeaponSlots;
		case ItemObject.ItemTypeEnum.BodyArmor:
			return EquipmentIndex.Body;
		case ItemObject.ItemTypeEnum.LegArmor:
			return EquipmentIndex.Leg;
		case ItemObject.ItemTypeEnum.HandArmor:
			return EquipmentIndex.Gloves;
		case ItemObject.ItemTypeEnum.Cape:
			return EquipmentIndex.Cape;
		case ItemObject.ItemTypeEnum.HorseHarness:
			return EquipmentIndex.HorseHarness;
		case ItemObject.ItemTypeEnum.Banner:
			return EquipmentIndex.ExtraWeaponSlot;
		default:
			if (item.WeaponComponent != null)
			{
				return EquipmentIndex.WeaponItemBeginSlot;
			}
			return EquipmentIndex.None;
		}
	}

	public void Click()
	{
		InformationManager.DisplayMessage(new InformationMessage(_item.Name.ToString()));
	}

	private void AddIntProperty(TextObject description, int Value)
	{
		string value = Value.ToString();
		CreateColoredProperty(ItemProperties, description.ToString(), value, Colors.White);
	}

	private void AddSkillRequirement(ItemObject item, MBBindingList<ItemMenuTooltipPropertyVM> itemProperties, bool isComparison)
	{
		string text = "";
		if (item.Difficulty > 0)
		{
			text = item.RelevantSkill.Name.ToString();
			text += " ";
			text += item.Difficulty;
		}
		string definition = new TextObject("{=154a34f8caccfc833238cc89d38861e8}Requires: ").ToString();
		CreateColoredProperty(itemProperties, definition, text, (_container.selectedCharacters.First().GetSkillValue(item.RelevantSkill) >= item.Difficulty) ? UIColors.PositiveIndicator : UIColors.NegativeIndicator);
	}

	private ItemMenuTooltipPropertyVM CreateColoredProperty(MBBindingList<ItemMenuTooltipPropertyVM> targetList, string definition, string value, Color color, int textHeight = 0, HintViewModel hint = null, TooltipProperty.TooltipPropertyFlags propertyFlags = TooltipProperty.TooltipPropertyFlags.None)
	{
		if (color == Colors.Black)
		{
			CreateProperty(targetList, definition, value, textHeight, hint);
			return null;
		}
		ItemMenuTooltipPropertyVM itemMenuTooltipPropertyVM = new ItemMenuTooltipPropertyVM(definition, value, textHeight, color, onlyShowWhenExtended: false, hint, propertyFlags);
		targetList.Add(itemMenuTooltipPropertyVM);
		return itemMenuTooltipPropertyVM;
	}

	private ItemMenuTooltipPropertyVM CreateProperty(MBBindingList<ItemMenuTooltipPropertyVM> targetList, string definition, string value, int textHeight = 0, HintViewModel hint = null)
	{
		ItemMenuTooltipPropertyVM itemMenuTooltipPropertyVM = new ItemMenuTooltipPropertyVM(definition, value, textHeight, onlyShowWhenExtended: false, hint);
		targetList.Add(itemMenuTooltipPropertyVM);
		return itemMenuTooltipPropertyVM;
	}

	private void AddWeaponItemFlags(MBBindingList<ItemFlagVM> list, WeaponComponentData weapon)
	{
		if (weapon == null)
		{
			return;
		}
		ItemObject.ItemUsageSetFlags itemUsageFlags = _getItemUsageSetFlags(weapon);
		foreach (var valueTuple in CampaignUIHelper.GetFlagDetailsForWeapon(weapon, itemUsageFlags))
		{
			list.Add(new ItemFlagVM(valueTuple.Item1, valueTuple.Item2));
		}
	}

	private void AddGeneralItemFlags(MBBindingList<ItemFlagVM> list, ItemObject item)
	{
		if (item.IsUniqueItem)
		{
			list.Add(new ItemFlagVM("GeneralFlagIcons\\unique", GameTexts.FindText("str_inventory_flag_unique")));
		}
		if (item.IsCivilian)
		{
			list.Add(new ItemFlagVM("GeneralFlagIcons\\civillian", GameTexts.FindText("str_inventory_flag_civillian")));
		}
		if (item.ItemFlags.HasAnyFlag(ItemFlags.NotUsableByFemale))
		{
			list.Add(new ItemFlagVM("GeneralFlagIcons\\male_only", GameTexts.FindText("str_inventory_flag_male_only")));
		}
		if (item.ItemFlags.HasAnyFlag(ItemFlags.NotUsableByMale))
		{
			list.Add(new ItemFlagVM("GeneralFlagIcons\\female_only", GameTexts.FindText("str_inventory_flag_female_only")));
		}
	}

	private void AddHorseItemFlags(MBBindingList<ItemFlagVM> list, ItemObject item)
	{
		if (!item.HorseComponent.IsLiveStock)
		{
			if (item.ItemCategory == DefaultItemCategories.PackAnimal)
			{
				list.Add(new ItemFlagVM("MountFlagIcons\\weight_carrying_mount", GameTexts.FindText("str_inventory_flag_carrying_mount")));
			}
			else
			{
				list.Add(new ItemFlagVM("MountFlagIcons\\speed_mount", GameTexts.FindText("str_inventory_flag_speed_mount")));
			}
		}
	}

	private ItemObject.ItemUsageSetFlags GetItemUsageSetFlag(WeaponComponentData item)
	{
		if (!string.IsNullOrEmpty(item.ItemUsage))
		{
			return MBItem.GetItemUsageSetFlags(item.ItemUsage);
		}
		return (ItemObject.ItemUsageSetFlags)0;
	}
}
