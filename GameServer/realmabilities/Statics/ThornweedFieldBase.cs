using DOL.Database;
using DOL.GS.Spells;

namespace DOL.GS.RealmAbilities.Statics
{
    public class ThornweedFieldBase : GenericBase 
    {
		protected override string GetStaticName() {return "Thornwood Field";}
		protected override ushort GetStaticModel() {return 2653;}
		protected override ushort GetStaticEffect() {return 7028;}
		private DbSpell dbs;
		private Spell   s;
		private SpellLine sl;
		public ThornweedFieldBase (int damage) 
        {
			dbs = new DbSpell();
			dbs.Name = GetStaticName();
			dbs.Icon = GetStaticEffect();
			dbs.ClientEffect = GetStaticEffect();
			dbs.Damage = damage;
			dbs.DamageType = (int)eDamageType.Natural;
			dbs.Target = "Enemy";
			dbs.Radius = 0;
			dbs.Type = eSpellType.DamageSpeedDecrease.ToString();
			dbs.Value = 50;
			dbs.Duration = 5;
			dbs.Pulse = 0;
			dbs.PulsePower = 0;
			dbs.Power = 0;
			dbs.CastTime = 0;
			dbs.Range = WorldMgr.VISIBILITY_DISTANCE;
			s = new Spell(dbs,1);
			sl = GlobalSpellsLines.RealmSpellsSpellLine;
		}
		protected override void CastSpell (GameLiving target)
        {
            if (!target.IsAlive) return;
			if (GameServer.ServerRules.IsAllowedToAttack(m_caster, target, true))
            {
				ISpellHandler snare = ScriptMgr.CreateSpellHandler(m_caster, s, sl);
				snare.StartSpell(target);
			}
		}
	}
}
