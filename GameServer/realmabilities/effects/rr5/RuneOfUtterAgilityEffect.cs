using System;
using System.Collections;
using System.Collections.Generic;
using DOL.GS.PacketHandler;
using DOL.GS.RealmAbilities;

namespace DOL.GS.Effects
{
	/// <summary>
	/// Mastery of Concentration
	/// </summary>
	public class RuneOfUtterAgilityEffect : TimedEffect
	{
		private GameLiving owner;

		public RuneOfUtterAgilityEffect()
			: base(15000)
		{
		}

		public override void Start(GameLiving target)
		{
			base.Start(target);
			owner = target;
			GamePlayer player = target as GamePlayer;
			if (player != null)
			{
				foreach (GamePlayer p in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				{
					p.Out.SendSpellEffectAnimation(player, player, Icon, 0, false, 1);
				}
				player.OtherBonus[eProperty.EvadeChance] += 90;
			}
		}

		public override void Stop()
		{
			GamePlayer player = owner as GamePlayer;
			if (player != null)
				player.OtherBonus[eProperty.EvadeChance] -= 90;
			base.Stop();
		}

		public override string Name { get { return "Rune Of Utter Agility"; } }

		public override ushort Icon { get { return 3073; } }

		public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>();
				list.Add("Increases your evade chance up to 90% for 30 seconds.");
				return list;
			}
		}
	}
}