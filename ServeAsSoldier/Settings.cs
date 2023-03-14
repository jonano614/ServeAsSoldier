using System;

namespace ServeAsSoldier;

[Serializable]
public class Settings
{
	public int Level1XP;

	public int Level2XP;

	public int Level3XP;

	public int Level4XP;

	public int Level5XP;

	public int Level6XP;

	public int Level7XP;

	public bool AIRecruitWanders;

	public bool AllowEventBattleSkip;

	public int XPtoWageRatio;

	public int PromotionToVassalXP;

	public int RetirementXP;

	public int MaxWage;

	public int DailyXP;
}
