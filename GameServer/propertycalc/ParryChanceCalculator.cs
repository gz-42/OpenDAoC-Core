/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

using DOL.AI.Brain;

namespace DOL.GS.PropertyCalc
{
    /// <summary>
    /// The parry chance calculator. Returns 0 .. 1000 chance.
    /// 
    /// BuffBonusCategory1 unused
    /// BuffBonusCategory2 unused
    /// BuffBonusCategory3 unused
    /// BuffBonusCategory4 unused
    /// BuffBonusMultCategory1 unused
    /// </summary>
    [PropertyCalculator(eProperty.ParryChance)]
    public class ParryChanceCalculator : PropertyCalculator
    {
        public override int CalcValue(GameLiving living, eProperty property)
        {
            int chance = 0;

            if (living is GamePlayer player)
            {
                if (player.HasSpecialization(Specs.Parry))
                    chance += (player.Dexterity * 2 - 100) / 4 + (player.GetModifiedSpecLevel(Specs.Parry) - 1) * (10 / 2) + 50;

                chance += player.BaseBuffBonusCategory[property] * 10;
                chance += player.SpecBuffBonusCategory[property] * 10;
                chance -= player.DebuffCategory[property] * 10;
                chance += player.OtherBonus[property] * 10;
                chance += player.AbilityBonus[property] * 10;
            }
            else if (living is GameNPC npc)
            {
                chance += npc.ParryChance * 10;

                if (living is NecromancerPet pet && pet.Brain is IControlledBrain)
                {
                    chance += pet.BaseBuffBonusCategory[property] * 10;
                    chance += pet.SpecBuffBonusCategory[property] * 10;
                    chance -= pet.DebuffCategory[property] * 10;
                    chance += pet.OtherBonus[property] * 10;
                    chance += pet.AbilityBonus[property] * 10;
                    chance += (pet.GetModified(eProperty.Dexterity) * 2 - 100) / 4;
                }
            }

            return chance;
        }
    }
}
