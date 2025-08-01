using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
	[SpellHandler(eSpellType.HereticDamageOverTime)]
	public class HereticDoTSpellHandler : HereticPiercingMagic
	{

		public override void FinishSpellCast(GameLiving target)
		{
			m_caster.Mana -= PowerCost(target);
			base.FinishSpellCast(target);
		}

		public override double CalculateDamageVarianceOffsetFromLevelDifference(GameLiving caster, GameLiving target)
		{
			return 0;
		}

		public override bool HasConflictingEffectWith(ISpellHandler compare)
		{
			if (Spell.EffectGroup != 0 || compare.Spell.EffectGroup != 0)
				return Spell.EffectGroup == compare.Spell.EffectGroup;
			if (base.HasConflictingEffectWith(compare) == false) return false;
			if (compare.Spell.Duration != Spell.Duration) return false;
			return true;
		}

		public override AttackData CalculateDamageToTarget(GameLiving target)
		{
			AttackData ad = base.CalculateDamageToTarget(target);
			ad.CriticalDamage = 0;
			ad.CriticalChance = 0;
			return ad;
		}

		public override void ApplyEffectOnTarget(GameLiving target)
		{
			base.ApplyEffectOnTarget(target);
			target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
		}

		protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
		{
			// damage is not reduced with distance
			//return new GameSpellEffect(this, m_spell.Duration*10-1, m_spellLine.IsBaseLine ? 3000 : 2000, 1);
			return new GameSpellEffect(this, m_spell.Duration, m_spellLine.IsBaseLine ? 3000 : 2000, 1);
		}

		public override void OnEffectStart(GameSpellEffect effect)
		{
			SendEffectAnimation(effect.Owner, 0, false, 1);
		}

		public override void OnEffectPulse(GameSpellEffect effect)
		{
			if ( !m_caster.IsAlive ||
				!effect.Owner.IsAlive ||
				m_caster.Mana < Spell.PulsePower ||
				!m_caster.IsWithinRadius(effect.Owner, Spell.CalculateEffectiveRange(m_caster)) ||
				m_caster.IsMezzed ||
				m_caster.IsStunned ||
				m_caster.TargetObject is not GameLiving ||
				effect.Owner != (m_caster.TargetObject as GameLiving))
			{
				effect.Cancel(false);
				return;
			}

			base.OnEffectPulse(effect);
			SendEffectAnimation(effect.Owner, 0, false, 1);
			// An acidic cloud surrounds you!
			MessageToLiving(effect.Owner, Spell.Message1, eChatType.CT_Spell);
			// {0} is surrounded by an acidic cloud!
			Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, false)), eChatType.CT_YouHit, effect.Owner);
			OnDirectEffect(effect.Owner);
		}

		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			base.OnEffectExpires(effect, noMessages);
			if (!noMessages) {
				// The acidic mist around you dissipates.
				MessageToLiving(effect.Owner, Spell.Message3, eChatType.CT_SpellExpires);
				// The acidic mist around {0} dissipates.
				Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, false)), eChatType.CT_SpellExpires, effect.Owner);
			}
			return 0;
		}

		public override void OnDirectEffect(GameLiving target)
		{
			if (target == null) return;
			if (!target.IsAlive || target.ObjectState!=GameLiving.eObjectState.Active) return;

			// no interrupts on DoT direct effect
			// calc damage
			AttackData ad = CalculateDamageToTarget(target);
			SendDamageMessages(ad);
			DamageTarget(ad);
		}

		public virtual void DamageTarget(AttackData ad)
		{
			ad.AttackResult = eAttackResult.HitUnstyled;
			ad.Target.OnAttackedByEnemy(ad);
			ad.Attacker.DealDamage(ad);
			foreach(GamePlayer player in ad.Attacker.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE)) {
				player.Out.SendCombatAnimation(null, ad.Target, 0, 0, 0, 0, 0x0A, ad.Target.HealthPercent);
			}
		}

		public HereticDoTSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
}
