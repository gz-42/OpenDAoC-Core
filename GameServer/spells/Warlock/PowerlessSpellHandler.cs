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
using System.Reflection;
using System.Text;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.SkillHandler;

namespace DOL.GS.Spells
{
	/// <summary>
	/// 
	/// </summary>
	[SpellHandler(eSpellType.Powerless)]
	public class PowerlessSpellHandler : PrimerSpellHandler
	{
		public override bool CheckBeginCast(GameLiving selectedTarget)
		{
			if (!base.CheckBeginCast(selectedTarget)) return false;
            GameSpellEffect RangeSpell = SpellHandler.FindEffectOnTarget(Caster, "Range");
  			if(RangeSpell != null) { MessageToCaster("You already preparing a Range spell", eChatType.CT_System); return false; }
            GameSpellEffect UninterruptableSpell = SpellHandler.FindEffectOnTarget(Caster, "Uninterruptable");
  			if(UninterruptableSpell != null) { MessageToCaster("You already preparing a Uninterruptable spell", eChatType.CT_System); return false; }
            GameSpellEffect PowerlessSpell = SpellHandler.FindEffectOnTarget(Caster, "Powerless");
            if (PowerlessSpell != null) { MessageToCaster("You must finish casting Powerless before you can cast it again", eChatType.CT_System); return false; }
            return true;
		}
		
		// constructor
		public PowerlessSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
}
