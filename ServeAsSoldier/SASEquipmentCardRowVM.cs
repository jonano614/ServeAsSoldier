using TaleWorlds.Library;

namespace ServeAsSoldier;

public class SASEquipmentCardRowVM : ViewModel
{
	public MBBindingList<SASEquipmentCardVM> Cards { get; set; }

	public SASEquipmentCardRowVM(MBBindingList<SASEquipmentCardVM> cards)
	{
		Cards = cards;
	}
}
