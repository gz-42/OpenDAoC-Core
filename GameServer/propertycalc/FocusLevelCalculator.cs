using System;

namespace DOL.GS.PropertyCalc
{
    /// <summary>
    /// The Focus Level calculator
    ///
    /// BuffBonusCategory1 is used for buffs, uncapped
    /// BuffBonusCategory2 unused
    /// BuffBonusCategory3 unused
    /// BuffBonusCategory4 unused
    /// BuffBonusMultCategory1 unused
    /// </summary>
    [PropertyCalculator(eProperty.Focus_Darkness, eProperty.Focus_Matter)]
    [PropertyCalculator(eProperty.Focus_Mind, eProperty.Focus_Arboreal)]
    [PropertyCalculator(eProperty.Focus_EtherealShriek, eProperty.Focus_Witchcraft)]
    public class FocusLevelCalculator : PropertyCalculator
    {
        public FocusLevelCalculator() { }

        public override int CalcValue(GameLiving living, eProperty property)
        {
            if (living is GamePlayer player)
            {
                int itemBonus = player.ItemBonus[property];
                int focusLevel = player.BaseBuffBonusCategory[property];

                if (SkillBase.CheckPropertyType(property, ePropertyType.Focus) && player.CharacterClass.IsFocusCaster)
                {
                    focusLevel += player.BaseBuffBonusCategory[eProperty.AllFocusLevels];
                    itemBonus = Math.Max(itemBonus, player.ItemBonus[eProperty.AllFocusLevels]);
                }

                return focusLevel + Math.Min(50, itemBonus);
            }

            return 0;
        }
    }
}
