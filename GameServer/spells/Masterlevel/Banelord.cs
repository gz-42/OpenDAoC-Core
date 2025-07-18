using DOL.AI.Brain;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    //http://www.camelotherald.com/masterlevels/ma.php?ml=Banelord
    //shared timer 1
    #region Banelord-1
    [SpellHandler(eSpellType.CastingSpeedDebuff)]
    public class CastingSpeedDebuff : MasterlevelDebuffHandling
    {
        public override eProperty Property1 { get { return eProperty.CastingSpeed; } }
		
		public override void ApplyEffectOnTarget(GameLiving target)
		{
			base.ApplyEffectOnTarget(target);
			target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
		}

        // constructor
        public CastingSpeedDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //shared timer 5 for ml2 - shared timer 3 for ml8
    #region Banelord-2/8
    [SpellHandler(eSpellType.PBAEDamage)]
    public class PBAEDamage : MasterlevelHandling
    {
        // constructor
        public PBAEDamage(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            //For Banelord ML 8, it drains Life from the Caster
            if (Spell.Damage > 0)
            {
                int chealth;
                chealth = (m_caster.Health * (int)Spell.Damage) / 100;

                if (m_caster.Health < chealth)
                    chealth = 0;

                m_caster.Health -= chealth;
            }
            base.FinishSpellCast(target);
        }

        public override void OnDirectEffect(GameLiving target)
        {
            if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;

            GamePlayer player = target as GamePlayer;
            if (target is GamePlayer)
            {
                int mana;
                int health;
                int end;

                int value = (int)Spell.Value;
                mana = (player.Mana * value) / 100;
                end = (player.Endurance * value) / 100;
                health = (player.Health * value) / 100;

                //You don't gain RPs from this Spell
                if (player.Health < health)
                    player.Health = 1;
                else
                    player.Health -= health;

                if (player.Mana < mana)
                    player.Mana = 1;
                else
                    player.Mana -= mana;

                if (player.Endurance < end)
                    player.Endurance = 1;
                else
                    player.Endurance -= end;

                GameSpellEffect effect2 = SpellHandler.FindEffectOnTarget(target, "Mesmerize");
                if (effect2 != null)
                {
                    effect2.Cancel(true);
                    return;
                }
                foreach (GamePlayer ply in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    SendEffectAnimation(player, 0, false, 1);
                }
                player.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
            }
        }

        public override double CalculateSpellResistChance(GameLiving target)
        {
            return 25;
        }
    }
    #endregion

    //shared timer 3
    #region Banelord-3
    [SpellHandler(eSpellType.Oppression)]
    public class OppressionSpellHandler : MasterlevelHandling
    {
        public override bool HasConflictingEffectWith(ISpellHandler compare)
        {
            return true;
        }
        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        public override double CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            if (effect.Owner is GamePlayer)
                ((GamePlayer)effect.Owner).UpdateEncumbrance();
			effect.Owner.StartInterruptTimer(effect.Owner.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            GameSpellEffect mezz = SpellHandler.FindEffectOnTarget(target, "Mesmerize");
            if (mezz != null)
                mezz.Cancel(false);
            base.ApplyEffectOnTarget(target);
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            if (effect.Owner is GamePlayer)
                ((GamePlayer)effect.Owner).UpdateEncumbrance();
            return base.OnEffectExpires(effect, noMessages);
        }
        public OppressionSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //shared timer 1
    #region Banelord-4
    [SpellHandler(eSpellType.MLFatDebuff)]
    public class MLFatDebuffHandler : MasterlevelDebuffHandling
    {
        public override eProperty Property1 { get { return eProperty.FatigueConsumption; } }	

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            GameSpellEffect effect2 = SpellHandler.FindEffectOnTarget(target, "Mesmerize");
            if (effect2 != null)
            {
                effect2.Cancel(false);
                return;
            }
            base.ApplyEffectOnTarget(target);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
			effect.Owner.StartInterruptTimer(effect.Owner.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
            base.OnEffectStart(effect);
        }

        public override double CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        // constructor
        public MLFatDebuffHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }
    #endregion

    //shared timer 5
    #region Banelord-5
    [SpellHandler(eSpellType.MissHit)]
    public class MissHit : MasterlevelBuffHandling
    {
        public override eProperty Property1 { get { return eProperty.MissHit; } }

        // constructor
        public MissHit(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //shared timer 1
    #region Banelord-6
    #region ML6Snare
    [SpellHandler(eSpellType.MLUnbreakableSnare)]
    public class MLUnbreakableSnare : BanelordSnare
    {
        protected override int CalculateEffectDuration(GameLiving target)
        {
            int duration = Spell.Duration;
            if (duration < 1)
                duration = 1;
            else if (duration > (Spell.Duration * 4))
                duration = (Spell.Duration * 4);
            return duration;
        }

        public override double CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        public MLUnbreakableSnare(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
        }
    }
    #endregion
    #region ML6Stun
    [SpellHandler(eSpellType.UnrresistableNonImunityStun)]
    public class UnrresistableNonImunityStun : MasterlevelHandling
    {
        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            if (target.HasAbility(Abilities.CCImmunity)||target.HasAbility(Abilities.StunImmunity))
            {
                MessageToCaster("Your target is immune to this effect!", eChatType.CT_SpellResisted);
                return;
            }

            base.ApplyEffectOnTarget(target);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            effect.Owner.IsStunned = true;
            effect.Owner.attackComponent.StopAttack();
            effect.Owner.StopCurrentSpellcast();
            effect.Owner.DisableTurning(true);

            SendEffectAnimation(effect.Owner, 0, false, 1);

            MessageToLiving(effect.Owner, Spell.Message1, eChatType.CT_Spell);
            MessageToCaster(Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, true)), eChatType.CT_Spell);
            Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, true)), eChatType.CT_Spell, effect.Owner, m_caster);

            GamePlayer player = effect.Owner as GamePlayer;
            if (player != null)
            {
                player.Client.Out.SendUpdateMaxSpeed();
                if (player.Group != null)
                    player.Group.UpdateMember(player, false, false);
            }
            else
            {
                effect.Owner.attackComponent.StopAttack();
            }

            base.OnEffectStart(effect);
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            effect.Owner.IsStunned = false;
            effect.Owner.DisableTurning(false);

            if (effect.Owner == null) return 0;

            GamePlayer player = effect.Owner as GamePlayer;

            if (player != null)
            {
                player.Client.Out.SendUpdateMaxSpeed();
                if (player.Group != null)
                    player.Group.UpdateMember(player, false, false);
            }
            else
            {
                GameNPC npc = effect.Owner as GameNPC;
                if (npc != null)
                {
                    IOldAggressiveBrain aggroBrain = npc.Brain as IOldAggressiveBrain;
                    if (aggroBrain != null)
                        aggroBrain.AddToAggroList(Caster, 1);
                }
            }
            return 0;
        }

        protected override int CalculateEffectDuration(GameLiving target)
        {
            return Spell.Duration;
        }

        public override bool HasConflictingEffectWith(ISpellHandler compare)
        {
            if (Spell.EffectGroup != 0 || compare.Spell.EffectGroup != 0)
                return Spell.EffectGroup == compare.Spell.EffectGroup;
            if (compare.Spell.SpellType == eSpellType.UnrresistableNonImunityStun) return true;
            return base.HasConflictingEffectWith(compare);
        }

        public override double CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        public override bool HasPositiveEffect
        {
            get
            {
                return false;
            }
        }

        public UnrresistableNonImunityStun(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
        }
    }
    #endregion
    #endregion

    //shared timer 3
    #region Banelord-7
    [SpellHandler(eSpellType.BLToHit)]
    public class BLToHit : MasterlevelBuffHandling
    {
        public override eProperty Property1 { get { return eProperty.ToHitBonus; } }

        // constructor
        public BLToHit(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //shared timer 5
    #region Banelord-9
    [SpellHandler(eSpellType.EffectivenessDebuff)]
    public class EffectivenessDeBuff : MasterlevelHandling
    {
        /// <summary>
        /// called after normal spell cast is completed and effect has to be started
        /// </summary>
        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }


        /// <summary>
        /// When an applied effect starts
        /// duration spells only
        /// </summary>
        /// <param name="effect"></param>
        public override void OnEffectStart(GameSpellEffect effect)
        {
            GamePlayer player = effect.Owner as GamePlayer;
            if (player != null)
            {
                player.Effectiveness -= Spell.Value * 0.01;
                player.Out.SendUpdateWeaponAndArmorStats();
                player.Out.SendStatusUpdate();
            }
        }

        /// <summary>
        /// When an applied effect expires.
        /// Duration spells only.
        /// </summary>
        /// <param name="effect">The expired effect</param>
        /// <param name="noMessages">true, when no messages should be sent to player and surrounding</param>
        /// <returns>immunity duration in milliseconds</returns>
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            GamePlayer player = effect.Owner as GamePlayer;
            if (player != null)
            {
                player.Effectiveness += Spell.Value * 0.01;
                player.Out.SendUpdateWeaponAndArmorStats();
                player.Out.SendStatusUpdate();
            }
            return 0;
        }

        public EffectivenessDeBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //no shared timer
    #region Banelord-10
    [SpellHandler(eSpellType.Banespike)]
    public class BanespikeHandler : MasterlevelBuffHandling
    {
        public override eProperty Property1 { get { return eProperty.MeleeDamage; } }

        public BanespikeHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion
}

#region MisshitCalc

namespace DOL.GS.PropertyCalc
{
    /// <summary>
    /// The melee damage bonus percent calculator
    ///
    /// BuffBonusCategory1 is used for buffs
    /// BuffBonusCategory2 unused
    /// BuffBonusCategory3 is used for debuff
    /// BuffBonusCategory4 unused
    /// BuffBonusMultCategory1 unused
    /// </summary>
    [PropertyCalculator(eProperty.MissHit)]
    public class MissHitPercentCalculator : PropertyCalculator
    {
        public override int CalcValue(GameLiving living, eProperty property)
        {
            return (int)(
                +living.BaseBuffBonusCategory[property]
                + living.SpecBuffBonusCategory[property]
                - living.DebuffCategory[property]
                + living.OtherBonus[property]);
        }
    }
}

#endregion