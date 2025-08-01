using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.BainsheePulseDmg)]
	public class BainsheePulseDmgSpellHandler : SpellHandler
	{
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public const string FOCUS_WEAK = "FocusSpellHandler.Online";
		/// <summary>
		/// Execute direct damage spell
		/// </summary>
		/// <param name="target"></param>
		public override void FinishSpellCast(GameLiving target)
		{
            if (Spell.Pulse != 0)
            {
                GameEventMgr.AddHandler(Caster, GamePlayerEvent.Moving, new DOLEventHandler(EventAction));
                GameEventMgr.AddHandler(Caster, GamePlayerEvent.Dying, new DOLEventHandler(EventAction));
            }
			m_caster.Mana -= PowerCost(target);
			base.FinishSpellCast(target);
		}
        public override bool CancelPulsingSpell(GameLiving living, eSpellType spellType)
        {
            List<ECSGameSpellEffect> concentrationEffects = living.effectListComponent.GetConcentrationEffects();

            for (int i = 0; i < concentrationEffects.Count; i++)
            {
                PulsingSpellEffect effect = null; // concentrationEffects[i] as PulsingSpellEffect;

                if (effect == null)
                    continue;

                if (effect.SpellHandler.Spell.SpellType == spellType)
                {
                    effect.Cancel(false);
                    GameEventMgr.RemoveHandler(Caster, GamePlayerEvent.Moving, new DOLEventHandler(EventAction));
                    GameEventMgr.RemoveHandler(Caster, GamePlayerEvent.Dying, new DOLEventHandler(EventAction));
                    return true;
                }
            }

            return false;
        }
        public void EventAction(DOLEvent e, object sender, EventArgs args)
        {
            GameLiving player = sender as GameLiving;

            if (player == null) return;
            if (Spell.Pulse != 0 && CancelPulsingSpell(Caster, Spell.SpellType))
            {
                MessageToCaster("You cancel your effect.", eChatType.CT_Spell);
                return;
            }
        }

		#region LOS on Keeps

		public override void OnDirectEffect(GameLiving target)
		{
			if (target == null)
				return;

			if (Spell.Target == eSpellTarget.CONE || (Spell.Target == eSpellTarget.ENEMY && Spell.IsPBAoE))
			{
				GamePlayer player = null;
				if (target is GamePlayer)
					player = target as GamePlayer;
				else
				{
					if (Caster is GamePlayer)
						player = Caster as GamePlayer;
					else if (Caster is GameNPC && (Caster as GameNPC).Brain is IControlledBrain)
					{
						IControlledBrain brain = (Caster as GameNPC).Brain as IControlledBrain;
						player = brain.GetPlayerOwner();
					}
				}
				if (player != null)
					player.Out.SendCheckLos(Caster, target, new CheckLosResponse(DealDamageCheckLos));
				else
					DealDamage(target);
			}
			else DealDamage(target);
		}

		private void DealDamageCheckLos(GamePlayer player, eLosCheckResponse response, ushort sourceOID, ushort targetOID)
		{
			if (response is eLosCheckResponse.TRUE)
			{
				try
				{
					GameLiving target = Caster.CurrentRegion.GetObject(targetOID) as GameLiving;

					if (target != null)
						DealDamage(target);
				}
				catch (Exception e)
				{
					if (log.IsErrorEnabled)
						log.Error(string.Format("targetOID:{0} caster:{1} exception:{2}", targetOID, Caster, e));
				}
			}
		}

		private void DealDamage(GameLiving target)
		{
			if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;

			// calc damage
			AttackData ad = CalculateDamageToTarget(target);
			DamageTarget(ad, true);
			SendDamageMessages(ad);
			target.StartInterruptTimer(target.SpellInterruptDuration, ad.AttackType, Caster);
		}

		#endregion

		// constructor
        public BainsheePulseDmgSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}
