using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace ServeAsSoldier;

public class SASEquipmentSelectorVM : ViewModel
{
	public MBBindingList<SASEquipmentCardVM> Cards;

	private CharacterViewModel _unitCharacter;

	public List<CharacterObject> selectedCharacters = new List<CharacterObject>();

	private MBBindingList<BindingListStringItem> _nameText;

	private MBBindingList<CharacterEquipmentItemVM> _armorsList;

	private MBBindingList<CharacterEquipmentItemVM> _weaponsList;

	public MBBindingList<SASEquipmentCardRowVM> Rows { get; set; }

	[DataSourceProperty]
	public MBBindingList<BindingListStringItem> Name
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
				OnPropertyChanged("Name");
			}
		}
	}

	[DataSourceProperty]
	public CharacterViewModel UnitCharacter
	{
		get
		{
			return _unitCharacter;
		}
		set
		{
			if (value != _unitCharacter)
			{
				_unitCharacter = value;
				base.OnPropertyChangedWithValue((object)value, "UnitCharacter");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CharacterEquipmentItemVM> ArmorsList
	{
		get
		{
			return _armorsList;
		}
		set
		{
			if (value != _armorsList)
			{
				_armorsList = value;
				base.OnPropertyChangedWithValue((object)value, "ArmorsList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CharacterEquipmentItemVM> WeaponsList
	{
		get
		{
			return _weaponsList;
		}
		set
		{
			if (value != _weaponsList)
			{
				_weaponsList = value;
				base.OnPropertyChangedWithValue((object)value, "WeaponsList");
			}
		}
	}

	public SASEquipmentSelectorVM(List<ItemObject> items, string equipmentType)
	{
		selectedCharacters.Add(CharacterObject.PlayerCharacter);
		Cards = new MBBindingList<SASEquipmentCardVM>();
		Rows = new MBBindingList<SASEquipmentCardRowVM>();
		Cards.Add(new SASEquipmentCardVM(null, this, equipmentType));
		foreach (ItemObject item in items)
		{
			Cards.Add(new SASEquipmentCardVM(item, this, equipmentType));
			if (Cards.Count == 4)
			{
				Rows.Add(new SASEquipmentCardRowVM(Cards));
				Cards = new MBBindingList<SASEquipmentCardVM>();
			}
		}
		if (Cards.Count > 0)
		{
			Rows.Add(new SASEquipmentCardRowVM(Cards));
		}
		UnitCharacter = new CharacterViewModel(CharacterViewModel.StanceTypes.EmphasizeFace);
		UnitCharacter.FillFrom(selectedCharacters.First());
		Name = new MBBindingList<BindingListStringItem>();
		Name.Add(new BindingListStringItem(selectedCharacters.First().Name.ToString()));
		ArmorsList = new MBBindingList<CharacterEquipmentItemVM>();
		WeaponsList = new MBBindingList<CharacterEquipmentItemVM>();
		ArmorsList.Add(new CharacterEquipmentItemVM(selectedCharacters.First().Equipment[EquipmentIndex.NumAllWeaponSlots].Item));
		ArmorsList.Add(new CharacterEquipmentItemVM(selectedCharacters.First().Equipment[EquipmentIndex.Cape].Item));
		ArmorsList.Add(new CharacterEquipmentItemVM(selectedCharacters.First().Equipment[EquipmentIndex.Body].Item));
		ArmorsList.Add(new CharacterEquipmentItemVM(selectedCharacters.First().Equipment[EquipmentIndex.Gloves].Item));
		ArmorsList.Add(new CharacterEquipmentItemVM(selectedCharacters.First().Equipment[EquipmentIndex.Leg].Item));
		WeaponsList.Add(new CharacterEquipmentItemVM(selectedCharacters.First().Equipment[EquipmentIndex.WeaponItemBeginSlot].Item));
		WeaponsList.Add(new CharacterEquipmentItemVM(selectedCharacters.First().Equipment[EquipmentIndex.Weapon1].Item));
		WeaponsList.Add(new CharacterEquipmentItemVM(selectedCharacters.First().Equipment[EquipmentIndex.Weapon2].Item));
		WeaponsList.Add(new CharacterEquipmentItemVM(selectedCharacters.First().Equipment[EquipmentIndex.Weapon3].Item));
	}

	public override void RefreshValues()
	{
	}

	public void Copy()
	{
		List<InquiryElement> inquiryElements = new List<InquiryElement>();
		foreach (TroopRosterElement troop3 in MobileParty.MainParty.MemberRoster.GetTroopRoster())
		{
			if (troop3.Character.HeroObject != null && troop3.Character.HeroObject.Clan == Clan.PlayerClan && troop3.Character != selectedCharacters.First())
			{
				inquiryElements.Add(new InquiryElement(troop3.Character, troop3.Character.Name.ToString() + "'s Gear", new ImageIdentifier(CharacterCode.CreateFrom(troop3.Character)), isEnabled: true, EquipmentHint(troop3.Character.BattleEquipments.First())));
			}
		}
		List<CharacterObject> validTroopSets = new List<CharacterObject>();
		foreach (CharacterObject troop2 in Test.GetTroopsList(Test.followingHero.Culture))
		{
			if (troop2.Tier <= Test.EnlistTier)
			{
				validTroopSets.Add(troop2);
			}
		}
		foreach (CharacterObject troop in validTroopSets)
		{
			inquiryElements.Add(new InquiryElement(troop, troop.Name.ToString() + "'s Gear", new ImageIdentifier(CharacterCode.CreateFrom(troop)), isEnabled: true, EquipmentHint(troop.BattleEquipments.First())));
		}
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData("Give " + selectedCharacters.First().Name.ToString() + " the same gear as ", "", inquiryElements, isExitShown: true, 1, "Continue", null, delegate(List<InquiryElement> args)
		{
			List<InquiryElement> list = args;
			if (list == null || list.Any())
			{
				InformationManager.HideInquiry();
				SubModule.ExecuteActionOnNextTick(delegate
				{
					CopyGear(args.Select((InquiryElement element) => element.Identifier as CharacterObject).First());
				});
			}
		}, null));
	}

	private void CopyGear(CharacterObject character)
	{
		List<CharacterObject> validTroopSets = new List<CharacterObject>();
		foreach (CharacterObject troop in Test.GetTroopsList(Test.followingHero.Culture))
		{
			if (troop.Tier <= Test.EnlistTier)
			{
				validTroopSets.Add(troop);
			}
		}
		HashSet<ItemObject> gear = new HashSet<ItemObject>();
		EquipmentIndex[] allIndex = new EquipmentIndex[11]
		{
			EquipmentIndex.WeaponItemBeginSlot,
			EquipmentIndex.Weapon1,
			EquipmentIndex.Weapon2,
			EquipmentIndex.Weapon3,
			EquipmentIndex.NumAllWeaponSlots,
			EquipmentIndex.Cape,
			EquipmentIndex.Body,
			EquipmentIndex.Gloves,
			EquipmentIndex.Leg,
			EquipmentIndex.ArmorItemEndSlot,
			EquipmentIndex.HorseHarness
		};
		foreach (CharacterObject troop2 in validTroopSets)
		{
			foreach (Equipment equipment in troop2.BattleEquipments)
			{
				EquipmentIndex[] array = allIndex;
				foreach (EquipmentIndex index2 in array)
				{
					if (!gear.Contains(equipment[index2].Item) && equipment[index2].Item != null)
					{
						gear.Add(equipment[index2].Item);
					}
				}
			}
		}
		foreach (CharacterObject selectedCharacter in selectedCharacters)
		{
			EquipmentIndex[] array2 = allIndex;
			foreach (EquipmentIndex index in array2)
			{
				if (gear.Contains(character.FirstBattleEquipment[index].Item) || character.FirstBattleEquipment[index].Item == null)
				{
					EquipmentElement modifiedEquipment = default(EquipmentElement);
					if (character.FirstBattleEquipment[index].Item != null)
					{
						if (character.FirstBattleEquipment[index].Item.ArmorComponent != null)
						{
							modifiedEquipment = new EquipmentElement(character.FirstBattleEquipment[index].Item, MBObjectManager.Instance.GetObject<ItemModifier>("sas_armor"));
						}
						else if (character.FirstBattleEquipment[index].Item.HorseComponent != null)
						{
							modifiedEquipment = new EquipmentElement(character.FirstBattleEquipment[index].Item, MBObjectManager.Instance.GetObject<ItemModifier>("sas_horse"));
						}
						else if (character.FirstBattleEquipment[index].Item.WeaponComponent != null)
						{
							modifiedEquipment = new EquipmentElement(character.FirstBattleEquipment[index].Item, MBObjectManager.Instance.GetObject<ItemModifier>("sas_weapon"));
						}
					}
					if (selectedCharacter != CharacterObject.PlayerCharacter && !Test.CompanionOldGear.ContainsKey(selectedCharacter))
					{
						Test.CompanionOldGear.Add(selectedCharacter, new Equipment(selectedCharacter.FirstBattleEquipment));
					}
					selectedCharacter.FirstBattleEquipment[index] = modifiedEquipment;
				}
				else if (character.FirstBattleEquipment[index].Item != null)
				{
					InformationManager.DisplayMessage(new InformationMessage(character.FirstBattleEquipment[index].Item.Name.ToString() + " is not avalibe in the armoury"));
				}
			}
		}
		EquipmentSelectorBehavior.DeleteVMLayer();
	}

	public static string EquipmentHint(Equipment equipment)
	{
		string s = "";
		EquipmentIndex[] slots = new EquipmentIndex[11]
		{
			EquipmentIndex.WeaponItemBeginSlot,
			EquipmentIndex.Weapon1,
			EquipmentIndex.Weapon2,
			EquipmentIndex.Weapon3,
			EquipmentIndex.NumAllWeaponSlots,
			EquipmentIndex.Cape,
			EquipmentIndex.Body,
			EquipmentIndex.Gloves,
			EquipmentIndex.Leg,
			EquipmentIndex.ArmorItemEndSlot,
			EquipmentIndex.HorseHarness
		};
		EquipmentIndex[] array = slots;
		foreach (EquipmentIndex slot in array)
		{
			if (equipment.GetEquipmentFromSlot(slot).Item != null && equipment.GetEquipmentFromSlot(slot).Item.Name != null)
			{
				s = s + equipment.GetEquipmentFromSlot(slot).Item.Name.ToString() + "\n";
			}
		}
		return s;
	}

	public void Switch()
	{
		List<InquiryElement> inquiryElements = new List<InquiryElement>();
		foreach (TroopRosterElement troop in MobileParty.MainParty.MemberRoster.GetTroopRoster())
		{
			if (troop.Character.HeroObject != null && troop.Character.HeroObject.Culture != null && troop.Character.HeroObject.Culture == Clan.PlayerClan.Culture)
			{
				inquiryElements.Add(new InquiryElement(troop.Character, troop.Character.HeroObject.EncyclopediaLinkWithName.ToString(), new ImageIdentifier(CharacterCode.CreateFrom(troop.Character.HeroObject.CharacterObject)), isEnabled: true, null));
			}
		}
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData("Select character(s) to equip with the gear", "", inquiryElements, isExitShown: true, MobileParty.MainParty.MemberRoster.TotalHeroes, "Continue", null, delegate(List<InquiryElement> args)
		{
			List<InquiryElement> list = args;
			if (list == null || list.Any())
			{
				InformationManager.HideInquiry();
				SubModule.ExecuteActionOnNextTick(delegate
				{
					selectedCharacters = args.Select((InquiryElement element) => element.Identifier as CharacterObject).ToList();
					UnitCharacter = new CharacterViewModel(CharacterViewModel.StanceTypes.EmphasizeFace);
					UnitCharacter.FillFrom(selectedCharacters.First());
					Name = new MBBindingList<BindingListStringItem>();
					Name.Add(new BindingListStringItem(selectedCharacters.First().Name.ToString()));
					ArmorsList.Clear();
					WeaponsList.Clear();
					ArmorsList.Add(new CharacterEquipmentItemVM(selectedCharacters.First().Equipment[EquipmentIndex.NumAllWeaponSlots].Item));
					ArmorsList.Add(new CharacterEquipmentItemVM(selectedCharacters.First().Equipment[EquipmentIndex.Cape].Item));
					ArmorsList.Add(new CharacterEquipmentItemVM(selectedCharacters.First().Equipment[EquipmentIndex.Body].Item));
					ArmorsList.Add(new CharacterEquipmentItemVM(selectedCharacters.First().Equipment[EquipmentIndex.Gloves].Item));
					ArmorsList.Add(new CharacterEquipmentItemVM(selectedCharacters.First().Equipment[EquipmentIndex.Leg].Item));
					WeaponsList.Add(new CharacterEquipmentItemVM(selectedCharacters.First().Equipment[EquipmentIndex.WeaponItemBeginSlot].Item));
					WeaponsList.Add(new CharacterEquipmentItemVM(selectedCharacters.First().Equipment[EquipmentIndex.Weapon1].Item));
					WeaponsList.Add(new CharacterEquipmentItemVM(selectedCharacters.First().Equipment[EquipmentIndex.Weapon2].Item));
					WeaponsList.Add(new CharacterEquipmentItemVM(selectedCharacters.First().Equipment[EquipmentIndex.Weapon3].Item));
				});
			}
		}, null));
	}

	public void Close()
	{
		EquipmentSelectorBehavior.DeleteVMLayer();
	}
}
