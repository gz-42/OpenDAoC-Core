using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.SkillHandler;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Spell Handler for firing arrows
	/// </summary>
	public class ArrowSpellHandler : SpellHandler
	{
		/// <summary>
		/// Does this spell break stealth on start?
		/// </summary>
		public override bool UnstealthCasterOnStart
		{
			get { return false; }
		}

		protected override double CalculateDamageEffectiveness()
		{
			// Half of the damage is magical.
			// Subtract any spelldamage bonus and re-calculate after half damage is calculated.
			return CasterEffectiveness * (0.5 - Caster.GetModified(eProperty.SpellDamage) * 0.01);
		}

		/// <summary>
		/// Fire arrow
		/// </summary>
		/// <param name="target"></param>
		public override void FinishSpellCast(GameLiving target)
		{
			m_caster.Endurance -= CalculateEnduranceCost();
			//if ((target is Keeps.GameKeepDoor || target is Keeps.GameKeepComponent) && Spell.SpellType != "SiegeArrow")
			//{
			//    MessageToCaster(String.Format("Your spell has no effect on the {0}!", target.Name), eChatType.CT_SpellResisted);
			//    return;
			//}
			base.FinishSpellCast(target);
		}
		

		#region LOS Checks for Keeps
		/// <summary>
		/// called when spell effect has to be started and applied to targets
		/// </summary>
		public override bool StartSpell(GameLiving target)
		{
			int targetCount = 0;

			foreach (GameLiving targ in SelectTargets(target))
			{
				DealDamage(targ);
				targetCount++;

				if (Spell.Target == eSpellTarget.AREA && targetCount >= Spell.Value)
				{
					// Volley is limited to Volley # + 2 targets.  This number is stored in Spell.Value
					break;
				}
			}

			return true;
		}

		private void DealDamageCheckLOS(GamePlayer player, bool response, ushort targetOID)
		{
			if (response)
			{
				GameLiving target = (GameLiving)(Caster.CurrentRegion.GetObject(targetOID));
				if (target != null)
					DealDamage(target);
			}
		}

		private void DealDamage(GameLiving target)
		{
			int ticksToTarget = m_caster.GetDistanceTo(target) * 100 / 85; // 85 units per 1/10s
			int delay = 1 + ticksToTarget / 100;
			foreach (GamePlayer player in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				player.Out.SendSpellEffectAnimation(m_caster, target, m_spell.ClientEffect, (ushort)(delay), false, 1);
			}
			ArrowOnTargetAction arrow = new ArrowOnTargetAction(Caster, target, this);
			arrow.Start(1 + ticksToTarget);
		}
		#endregion

		/// <summary>
		/// Delayed action when arrow reach the target
		/// </summary>
		protected class ArrowOnTargetAction : ECSGameTimerWrapperBase
		{
			/// <summary>
			/// The arrow target
			/// </summary>
			protected readonly GameLiving m_arrowTarget;

			/// <summary>
			/// The spell handler
			/// </summary>
			protected readonly ArrowSpellHandler m_handler;

			/// <summary>
			/// Constructs a new ArrowOnTargetAction
			/// </summary>
			/// <param name="actionSource">The action source</param>
			/// <param name="arrowTarget">The arrow target</param>
			/// <param name="spellHandler"></param>
			public ArrowOnTargetAction(GameLiving actionSource, GameLiving arrowTarget, ArrowSpellHandler spellHandler)
				: base(actionSource)
			{
				if (arrowTarget == null)
					throw new ArgumentNullException("arrowTarget");
				if (spellHandler == null)
					throw new ArgumentNullException("spellHandler");
				m_arrowTarget = arrowTarget;
				m_handler = spellHandler;
			}

			protected override int OnTick(ECSGameTimer timer)
			{
				// A lot of things here seem to be outdated and need to be cleaned up.

				GameLiving target = m_arrowTarget;
				GameLiving caster = (GameLiving) timer.Owner;

				if (target == null || !target.IsAlive || target.ObjectState != GameObject.eObjectState.Active || target.CurrentRegionID != caster.CurrentRegionID)
					return 0;

				double missrate = 100 - m_handler.CalculateToHitChance(target);
				// add defence bonus from last executed style if any
				AttackData targetAD = target.attackComponent.attackAction.LastAttackData;
				if (targetAD != null
				    && targetAD.AttackResult == eAttackResult.HitStyle
				    && targetAD.Style != null)
				{
					missrate += targetAD.Style.BonusToDefense;
				}

				AttackData ad = m_handler.CalculateDamageToTarget(target);

				// check for bladeturn miss
				if (ad.AttackResult == eAttackResult.Missed)
				{
					return 0;
				}

				if (Util.ChanceDouble(missrate))
				{
					ad.AttackResult = eAttackResult.Missed;
					m_handler.MessageToCaster("You miss!", eChatType.CT_YouHit);
					m_handler.MessageToLiving(target, caster.GetName(0, false) + " missed!", eChatType.CT_Missed);
					target.OnAttackedByEnemy(ad);
					target.StartInterruptTimer(target.SpellInterruptDuration, ad.AttackType, caster);
					if (target is GameNPC)
					{
						IOldAggressiveBrain aggroBrain = ((GameNPC)target).Brain as IOldAggressiveBrain;
						if (aggroBrain != null)
							aggroBrain.AddToAggroList(caster, 1);
					}
					return 0;
				}

				ad.Damage = (int)((double)ad.Damage * (1.0 + caster.GetModified(eProperty.SpellDamage) * 0.01));

				bool arrowBlock = false;

				if (target is GamePlayer && !target.IsStunned && !target.IsMezzed && !target.IsSitting && m_handler.Spell.LifeDrainReturn != (int)Archery.eShotType.Critical)
				{
					GamePlayer player = (GamePlayer)target;
					DbInventoryItem lefthand = player.ActiveLeftWeapon;
					if (lefthand != null && (player.ActiveWeapon == null || player.ActiveWeapon.Item_Type == Slot.RIGHTHAND || player.ActiveWeapon.Item_Type == Slot.LEFTHAND))
					{
						if (target.IsObjectInFront(caster, 180) && lefthand.Object_Type == (int)eObjectType.Shield)
						{
							// TODO: shield size vs number of attackers not calculated
							double shield = 0.5 * player.GetModifiedSpecLevel(Specs.Shields);
							double blockchance = ((player.Dexterity * 2) - 100) / 40.0 + shield + (0 * 3) + 5;
							blockchance += 30;
							if (blockchance >= 100) blockchance = 99;
							if (blockchance <= 0) blockchance = 1;

							if (target.IsEngaging)
							{
								EngageECSGameEffect engage = (EngageECSGameEffect)EffectListService.GetEffectOnTarget(target, eEffect.Engage);
								if (engage != null && target.attackComponent.AttackState && engage.EngageTarget == caster)
								{
									// Engage raised block change to 85% if attacker is engageTarget and player is in attackstate
									// You cannot engage a mob that was attacked within the last X seconds...
									if (engage.EngageTarget.LastAttackedByEnemyTick > GameLoop.GameLoopTime - EngageAbilityHandler.ENGAGE_ATTACK_DELAY_TICK)
									{
										if (engage.Owner is GamePlayer)
											(engage.Owner as GamePlayer).Out.SendMessage(engage.EngageTarget.GetName(0, true) + " has been attacked recently and you are unable to engage.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
									}  // Check if player has enough endurance left to engage
									else if (engage.Owner.Endurance < EngageAbilityHandler.ENGAGE_ENDURANCE_COST)
									{
										engage.Cancel(false, true); // if player ran out of endurance cancel engage effect
									}
									else
									{
										engage.Owner.Endurance -= EngageAbilityHandler.ENGAGE_ENDURANCE_COST;
										if (engage.Owner is GamePlayer)
											(engage.Owner as GamePlayer).Out.SendMessage("You concentrate on blocking the blow!", eChatType.CT_Skill, eChatLoc.CL_SystemWindow);

										if (blockchance < 95)
											blockchance = 95;
									}
								}
							}

							if (blockchance >= Util.Random(1, 100))
							{
								arrowBlock = true;
								m_handler.MessageToLiving(player, "You block " + caster.GetName(0, false) + "'s arrow!", eChatType.CT_System);

								if (m_handler.Spell.Target != eSpellTarget.AREA)
								{
									m_handler.MessageToCaster(player.GetName(0, true) + " blocks your arrow!", eChatType.CT_System);
									m_handler.DamageTarget(ad, false, 0x02);
								}
							}
						}
					}
				}

				if (arrowBlock == false)
				{
					// now calculate the magical part of arrow damage (similar to bolt calculation).  Part 1 Physical, Part 2 Magical

					double damage = m_handler.Spell.Damage / 2; // another half is physical damage
					if (target is GamePlayer)
						ad.ArmorHitLocation = ((GamePlayer)target).CalculateArmorHitLocation(ad);

					DbInventoryItem armor = null;
					if (target.Inventory != null)
						armor = target.Inventory.GetItem((eInventorySlot)ad.ArmorHitLocation);

					double ws = (caster.Level * 8 * (1.0 + (caster.GetModified(eProperty.Dexterity) - 50) / 200.0));

					damage *= ((ws + 90.68) / (target.GetArmorAF(ad.ArmorHitLocation) + 20 * 4.67));
					damage *= 1.0 - Math.Min(0.85, ad.Target.GetArmorAbsorb(ad.ArmorHitLocation));
					ad.Modifier = (int)(damage * (ad.Target.GetResist(ad.DamageType) + SkillBase.GetArmorResist(armor, ad.DamageType)) / -100.0);
					damage += ad.Modifier;

					double effectiveness = caster.Effectiveness;
					effectiveness += (caster.GetModified(eProperty.SpellDamage) * 0.01);
					damage = damage * effectiveness;

					damage *= (1.0 + RelicMgr.GetRelicBonusModifier(caster.Realm, eRelicType.Magic));

					if (damage < 0) damage = 0;

					ad.Damage += (int)damage;

					if (caster.ActiveWeapon != null)
					{
						// Quality
						ad.Damage -= (int)(ad.Damage * (100 - caster.ActiveWeapon.Quality) * .01);

						// Condition
						ad.Damage = (int)((double)ad.Damage * Math.Min(1.0, (double)caster.ActiveWeapon.Condition / (double)caster.ActiveWeapon.MaxCondition));

						// Patch Note:  http://support.darkageofcamelot.com/kb/article.php?id=931
						// - The Damage Per Second (DPS) of your bow will have an effect on your damage for archery shots. If the effective DPS
						//   of your equipped bow is less than that of your max DPS for the level of archery shot you are using, the damage of your
						//   shot will be reduced. Max DPS for a particular level can be found by using this equation: (.3 * level) + 1.2

						int spellRequiredDPS = 12 + 3 * m_handler.Spell.Level;

						if (caster.ActiveWeapon.DPS_AF < spellRequiredDPS)
						{
							double percentReduction = (double)caster.ActiveWeapon.DPS_AF / (double)spellRequiredDPS;
							ad.Damage = (int)(ad.Damage * percentReduction);
						}
					}

					if (ad.Damage < 0)
						ad.Damage = 0;

					ad.Damage = (int)Math.Min(ad.Damage, m_handler.DamageCap(effectiveness));

					if (ad.CriticalDamage > 0)
					{
						if (m_handler.Spell.Target == eSpellTarget.AREA)
						{
							ad.CriticalDamage = 0;
						}
						else
						{
							int critMax = (target is GamePlayer) ? ad.Damage / 2 : ad.Damage;
							ad.CriticalDamage = Util.Random(critMax / 10, critMax);
						}
					}

					target.ModifyAttack(ad);

					m_handler.SendDamageMessages(ad);
					m_handler.DamageTarget(ad, false, 0x14);
					target.StartInterruptTimer(target.SpellInterruptDuration, ad.AttackType, caster);
				}


				if (m_handler.Spell.SubSpellID != 0)
				{
					Spell subspell = SkillBase.GetSpellByID(m_handler.Spell.SubSpellID);
					if (subspell != null)
					{
						subspell.Level = m_handler.Spell.Level;
						ISpellHandler spellhandler = ScriptMgr.CreateSpellHandler(m_handler.Caster, subspell, SkillBase.GetSpellLine(GlobalSpellsLines.Combat_Styles_Effect));
						if (spellhandler != null)
						{
							spellhandler.StartSpell(target);
						}
					}
				}

				if (arrowBlock == false && m_handler.Caster.ActiveWeapon != null && GlobalConstants.IsBowWeapon((eObjectType)m_handler.Caster.ActiveWeapon.Object_Type))
				{
					if (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle)
					{
						caster.CheckWeaponMagicalEffect(ad);
					}
				}

				return 0;
			}
		}

		public ArrowSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}
