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
using System;
using System.Collections;
using System.Collections.Generic;

using DOL.GS;
using DOL.AI.Brain;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.MetalGuard)]
    public class MetalGuardSpellHandler : ArmorAbsorptionBuff
    {
        public override IList<GameLiving> SelectTargets(GameObject castTarget)
        {
            var list = new List<GameLiving>();
            GameLiving target = castTarget as GameLiving;

            if (Caster is GamePlayer)
            {
                GamePlayer casterPlayer = (GamePlayer)Caster;
                Group group = casterPlayer.Group;
                if(group == null) return list; // Should not appen since it is checked in ability handler
                int spellRange = Spell.CalculateEffectiveRange(Caster);
                if (group != null)
                {
                    lock (group)
                    {
                        foreach (GamePlayer groupPlayer in casterPlayer.GetPlayersInRadius((ushort)m_spell.Radius))
                        {
                            if (casterPlayer.Group.IsInTheGroup(groupPlayer))
                            {
                                if (groupPlayer != casterPlayer && groupPlayer.IsAlive)
                                {
                                    list.Add(groupPlayer);
                                    IControlledBrain npc = groupPlayer.ControlledBrain;
                                    if (npc != null)
                                        if (casterPlayer.IsWithinRadius( npc.Body, spellRange ))
                                            list.Add(npc.Body);
                                }
                            }
                        }
                    }
                }
            }
            return list;
        }    	    	
        public MetalGuardSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
