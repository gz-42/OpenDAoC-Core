using System;
using DOL.AI.Brain;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.HereticDamageSpeedDecrease)]
	public class HereticDamageSpeedDecrease : HereticSpeedDecreaseSpellHandler
	{
        protected int m_lastdamage = 0;
        protected int m_pulsedamage = 0;
        //    protected int m_pulsecount = -1;

        public override void FinishSpellCast(GameLiving target)
        {
            BeginEffect();
            base.FinishSpellCast(target);
        }

        public override double CalculateDamageVarianceOffsetFromLevelDifference(GameLiving caster, GameLiving target)
        {
            return 0;
        }

        public override bool HasConflictingEffectWith(ISpellHandler compare)
        {
            if (base.HasConflictingEffectWith(compare) == false) return false;
            if (compare.Spell.Duration != Spell.Duration) return false;
            return true;
        }

        public override AttackData CalculateDamageToTarget(GameLiving target)
        {
            AttackData ad = base.CalculateDamageToTarget(target);
            ad.CriticalDamage = 0;
            ad.CriticalChance = 0;
            ad.AttackType = AttackData.eAttackType.Unknown;
            return ad;
        }

        public override void CalculateDamageVariance(GameLiving target, out double min, out double max)
        {
            int speclevel = 1;
            if (m_caster is GamePlayer)
            {
                speclevel = ((GamePlayer)m_caster).GetModifiedSpecLevel(m_spellLine.Spec);
            }
            min = 1;
            max = 1;

            if (target.Level > 0)
            {
                min = 0.5 + (speclevel - 1) / (double)target.Level * 0.5;
            }

            if (speclevel - 1 > target.Level)
            {
                double overspecBonus = (speclevel - 1 - target.Level) * 0.005;
                min += overspecBonus;
                max += overspecBonus;
            }

            if (min > max) min = max;
            if (min < 0) min = 0;
        }


        protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
        {
            base.CreateSpellEffect(target, effectiveness);
            // damage is not reduced with distance
            return new GameSpellEffect(this, m_spell.Duration, m_spellLine.IsBaseLine ? 3000 : 2000, 1);
        }

        /*    public override void OnSpellPulse(PulsingSpellEffect effect)
            {
                if (m_pulsecount == -1)
                    m_pulsecount = m_spell.Pulse;

                if (m_pulsecount > 0)
                {
                    if (m_pulsecount == m_spell.Pulse)
                        m_pulsecount -= 1;

                    m_pulsecount -= 1;
                    base.OnSpellPulse(effect);
                }
                else
                {
                    RemoveEffect();
                }
            }*/

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            SendEffectAnimation(effect.Owner, 0, false, 1);
        }


        public override void OnEffectPulse(GameSpellEffect effect)
        {
            GameLiving t = effect.Owner;

            if (m_caster.Mana < Spell.PulsePower)
            {
                RemoveEffect();
            }
            if (!m_caster.TargetInView)
            {
                RemoveEffect();
                return;
            }
            if ( !m_caster.IsAlive ||
                !effect.Owner.IsAlive ||
                m_caster.Mana < Spell.PulsePower ||
                !m_caster.IsWithinRadius(effect.Owner, Spell.CalculateEffectiveRange(m_caster)) ||
                m_caster.IsMezzed ||
                m_caster.IsStunned ||
                m_caster.TargetObject is not GameLiving ||
                effect.Owner != (m_caster.TargetObject as GameLiving))
            {
                RemoveEffect();
            }

            base.OnEffectPulse(effect);

            SendEffectAnimation(effect.Owner, 0, false, 1);

            MessageToLiving(effect.Owner, Spell.Message1, eChatType.CT_Spell);

            Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, false)), eChatType.CT_YouHit, effect.Owner);

            OnDirectEffect(effect.Owner);

			// A really lame way to charge the correct amount of power per pulse since this spell is cast and maintained without pulsing. - Tolakram
			if (m_focusTargets.Count > 1)
			{
				double powerPerTarget = (double)(effect.Spell.PulsePower / m_focusTargets.Count);

				int powerUsed = (int)powerPerTarget;
				if (Util.ChanceDouble(((double)powerPerTarget - (double)powerUsed)))
					powerUsed += 1;

				if (powerUsed > 0)
					m_caster.Mana -= powerUsed;
			}
			else
			{
				m_caster.Mana -= effect.Spell.PulsePower;
			}
		}


        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            base.OnEffectExpires(effect, noMessages);
            if (!noMessages)
            {
                MessageToLiving(effect.Owner, Spell.Message3, eChatType.CT_SpellExpires);

                Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, false)), eChatType.CT_SpellExpires, effect.Owner);
            }

            return 0;
        }

        public override void OnDirectEffect(GameLiving target)
        {
            if (target == null) return;
            if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;
            if (Util.ChanceDouble(CalculateSpellResistChance(target)))
            {
                OnSpellResist(target);
                return;
            }
            AttackData ad = CalculateDamageToTarget(target);

            if (m_lastdamage <= 0)
            {
                m_lastdamage = ad.Damage;
            }
            else
            {
                m_pulsedamage = Convert.ToInt32(m_lastdamage * 0.25);
                if (target == focustarget)
                    m_lastdamage += m_pulsedamage;
            }

            ad.Damage = m_lastdamage;

            SendEffectAnimation(target, 0, false, 1);
            SendDamageMessages(ad);
            DamageTarget(ad);
        }

        protected virtual void OnSpellResist(GameLiving target)
        {
            m_lastdamage -= Convert.ToInt32(m_lastdamage * 0.25);
            SendEffectAnimation(target, 0, false, 0);
            if (target is GameNPC)
            {
                IControlledBrain brain = ((GameNPC)target).Brain as IControlledBrain;
                if (brain != null)
                {
                    GamePlayer owner = brain.GetPlayerOwner();
                    if (owner != null)
                    {
                        MessageToLiving(owner, "Your " + target.Name + " resists the effect!", eChatType.CT_SpellResisted);
                    }
                }
            }
            else
            {
                MessageToLiving(target, "You resist the effect!", eChatType.CT_SpellResisted);
            }
            MessageToCaster(target.GetName(0, true) + " resists the effect!", eChatType.CT_SpellResisted);

            if (Spell.Damage != 0)
            {
                // notify target about missed attack for spells with damage
                AttackData ad = new AttackData();
                ad.Attacker = Caster;
                ad.Target = target;
                ad.AttackType = AttackData.eAttackType.Spell;
                ad.AttackResult = eAttackResult.Missed;
				ad.SpellHandler = this;
                target.OnAttackedByEnemy(ad);
                target.StartInterruptTimer(target.SpellInterruptDuration, ad.AttackType, Caster);
            }
            else if (Spell.CastTime > 0)
            {
                target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
            }

            if (target is GameNPC)
            {
                IOldAggressiveBrain aggroBrain = ((GameNPC)target).Brain as IOldAggressiveBrain;
                if (aggroBrain != null)
                    aggroBrain.AddToAggroList(Caster, 1);
			}
        }

        public virtual void DamageTarget(AttackData ad)
        {
            ad.AttackResult = eAttackResult.HitUnstyled;
            ad.Target.OnAttackedByEnemy(ad);
            ad.Attacker.DealDamage(ad);
            foreach (GamePlayer player in ad.Attacker.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendCombatAnimation(null, ad.Target, 0, 0, 0, 0, 0x0A, ad.Target.HealthPercent);
            }
        }
	
		public HereticDamageSpeedDecrease(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
}
