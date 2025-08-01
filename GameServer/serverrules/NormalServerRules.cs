using System.Collections;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Keeps;

namespace DOL.GS.ServerRules
{
	/// <summary>
	/// Set of rules for "normal" server type.
	/// </summary>
	[ServerRules(EGameServerType.GST_Normal)]
	public class NormalServerRules : AbstractServerRules
	{
		public override string RulesDescription()
		{
			return "standard Normal server rules";
		}

		public override bool IsAllowedToAttack(GameLiving attacker, GameLiving defender, bool quiet)
		{
			if (!base.IsAllowedToAttack(attacker, defender, quiet))
				return false;

			GameNPC npcAttacker = attacker as GameNPC;

			if (npcAttacker != null && npcAttacker.Brain is IControlledBrain attackerBrain)
			{
				attacker = attackerBrain.GetLivingOwner();
				quiet = true; // Silence all attacks by controlled npcs.
			}

			if (defender is GameNPC npcDefender)
			{
				if (npcDefender.Brain is IControlledBrain defenderBrain)
					defender = defenderBrain.GetLivingOwner();
			}

			// "You can't attack yourself!"
			if (attacker == defender)
			{
				if (!quiet)
					MessageToLiving(attacker, "You can't attack yourself!");

				return false;
			}

			// Don't allow attacks on same realm members.
			if (attacker.Realm == defender.Realm)
			{
				// Allow players to attack their duel partner.
				if (attacker is GamePlayer player && player.IsDuelPartner(defender))
					return true;

				if (npcAttacker != null)
				{
					// Allow confused NPCs to attack realm mates.
					// Pets can't attack their owner however, since attacker == defender.
					if (npcAttacker.IsConfused)
						return true;

					// If the NPC is neutral, delegate to `FactionMgr`.
					if (attacker.Realm == 0)
						return FactionMgr.CanLivingAttack(attacker, defender);
				}

				if (!quiet)
					MessageToLiving(attacker, "You can't attack a member of your realm!");

				return false;
			}

			return true;
		}

		public override bool IsSameRealm(GameLiving source, GameLiving target, bool quiet)
		{
			if(source == null || target == null) 
				return false;

			// if controlled NPC - do checks for owner instead
			if (source is GameNPC)
			{
				IControlledBrain controlled = ((GameNPC)source).Brain as IControlledBrain;
				if (controlled != null)
				{
                    source = controlled.GetLivingOwner();
					quiet = true; // silence all attacks by controlled npc
				}
			}
			if (target is GameNPC)
			{
				IControlledBrain controlled = ((GameNPC)target).Brain as IControlledBrain;
				if (controlled != null)
                    target = controlled.GetLivingOwner();
			}

			if (source == target)
				return true;

			// clients with priv level > 1 are considered friendly by anyone
			if(target is GamePlayer && ((GamePlayer)target).Client.Account.PrivLevel > 1) return true;
			// checking as a gm, targets are considered friendly
			if (source is GamePlayer && ((GamePlayer)source).Client.Account.PrivLevel > 1) return true;

			//Peace flag NPCs are same realm
			if (target is GameNPC)
				if ((((GameNPC)target).Flags & GameNPC.eFlags.PEACE) != 0)
					return true;

			if (source is GameNPC)
				if ((((GameNPC)source).Flags & GameNPC.eFlags.PEACE) != 0)
					return true;

			if(source.Realm != target.Realm)
			{
				if(quiet == false) MessageToLiving(source, target.GetName(0, true) + " is not a member of your realm!");
				return false;
			}
			return true;
		}

		public override bool IsAllowedCharsInAllRealms(GameClient client)
		{
			if (client.Account.PrivLevel > 1)
				return true;
			if (ServerProperties.Properties.ALLOW_ALL_REALMS)
				return true;
			return false;
		}

		public override bool IsAllowedToGroup(GamePlayer source, GamePlayer target, bool quiet)
		{
			if(source == null || target == null) return false;
			
			if (source.Realm != target.Realm)
			{
				if(quiet == false) MessageToLiving(source, "You can't group with a player from another realm!");
				return false;
			}

			return true;
		}


		public override bool IsAllowedToJoinGuild(GamePlayer source, Guild guild)
		{
			if (source == null) 
				return false;

			if (ServerProperties.Properties.ALLOW_CROSS_REALM_GUILDS == false && guild.Realm != eRealm.None && source.Realm != guild.Realm)
			{
				return false;
			}

			return true;
		}

		public override bool IsAllowedToTrade(GameLiving source, GameLiving target, bool quiet)
		{

			if(source == null || target == null) return false;
			
			// clients with priv level > 1 are allowed to trade with anyone
			if(source is GamePlayer && target is GamePlayer)
			{
				if ((source as GamePlayer).Client.Account.PrivLevel > 1 || (target as GamePlayer).Client.Account.PrivLevel > 1)
					return true;
			}
			
			if((source as GamePlayer).NoHelp)
			{
				if(quiet == false) MessageToLiving(source, "You have renounced to any kind of help!");
				if(quiet == false) MessageToLiving(target, "This player has chosen to receive no help!");
				return false;
			}
			
			if((target as GamePlayer).NoHelp)
			{
				if(quiet == false) MessageToLiving(target, "You have renounced to any kind of help!");
				if(quiet == false) MessageToLiving(source, "This player has chosen to receive no help!");
				return false;
			}

			//Peace flag NPCs can trade with everyone
			if (target is GameNPC)
				if ((((GameNPC)target).Flags & GameNPC.eFlags.PEACE) != 0)
					return true;

			if (source is GameNPC)
				if ((((GameNPC)source).Flags & GameNPC.eFlags.PEACE) != 0)
					return true;

			if(source.Realm != target.Realm)
			{
				if(quiet == false) MessageToLiving(source, "You can't trade with enemy realm!");
				return false;
			}
			return true;
		}

		public override bool IsAllowedToUnderstand(GameLiving source, GamePlayer target)
		{
			if(source == null || target == null) return false;

			if (source.CurrentRegionID == 27) return true;

			// clients with priv level > 1 are allowed to talk and hear anyone
			if(source is GamePlayer && ((GamePlayer)source).Client.Account.PrivLevel > 1) return true;
			if(target.Client.Account.PrivLevel > 1) return true;

			//Peace flag NPCs can be understood by everyone

			if (source is GameNPC)
				if ((((GameNPC)source).Flags & GameNPC.eFlags.PEACE) != 0)
					return true;

			if(source.Realm > 0 && source.Realm != target.Realm) return false;
			return true;
		}

		/// <summary>
		/// Is player allowed to bind
		/// </summary>
		/// <param name="player"></param>
		/// <param name="point"></param>
		/// <returns></returns>
		public override bool IsAllowedToBind(GamePlayer player, DbBindPoint point)
		{
			if (point.Realm == 0) return true;
			return player.Realm == (eRealm)point.Realm;
		}

		/// <summary>
		/// Is player allowed to make the item
		/// </summary>
		/// <param name="player"></param>
		/// <param name="item"></param>
		/// <returns></returns>
		public override bool IsAllowedToCraft(GamePlayer player, DbItemTemplate item)
		{
			return player.Realm == (eRealm)item.Realm || (item.Realm == 0 && ServerProperties.Properties.ALLOW_CRAFT_NOREALM_ITEMS);
		}

		/// <summary>
		/// Translates object type to compatible object types based on server type
		/// </summary>
		/// <param name="objectType">The object type</param>
		/// <returns>An array of compatible object types</returns>
		protected override eObjectType[] GetCompatibleObjectTypes(eObjectType objectType)
		{
			if(m_compatibleObjectTypes == null)
			{
				m_compatibleObjectTypes = new Hashtable();
				m_compatibleObjectTypes[(int)eObjectType.Staff] = new eObjectType[] { eObjectType.Staff };
				m_compatibleObjectTypes[(int)eObjectType.Fired] = new eObjectType[] { eObjectType.Fired };
                m_compatibleObjectTypes[(int)eObjectType.MaulerStaff] = new eObjectType[] { eObjectType.MaulerStaff };
				m_compatibleObjectTypes[(int)eObjectType.FistWraps] = new eObjectType[] { eObjectType.FistWraps };

				//alb
				m_compatibleObjectTypes[(int)eObjectType.CrushingWeapon]  = new eObjectType[] { eObjectType.CrushingWeapon };
				m_compatibleObjectTypes[(int)eObjectType.SlashingWeapon]  = new eObjectType[] { eObjectType.SlashingWeapon };
				m_compatibleObjectTypes[(int)eObjectType.ThrustWeapon]    = new eObjectType[] { eObjectType.ThrustWeapon };
				m_compatibleObjectTypes[(int)eObjectType.TwoHandedWeapon] = new eObjectType[] { eObjectType.TwoHandedWeapon };
				m_compatibleObjectTypes[(int)eObjectType.PolearmWeapon]   = new eObjectType[] { eObjectType.PolearmWeapon };
				m_compatibleObjectTypes[(int)eObjectType.Flexible]        = new eObjectType[] { eObjectType.Flexible };
				m_compatibleObjectTypes[(int)eObjectType.Longbow]         = new eObjectType[] { eObjectType.Longbow };
				m_compatibleObjectTypes[(int)eObjectType.Crossbow]        = new eObjectType[] { eObjectType.Crossbow };
				//TODO: case 5: abilityCheck = Abilities.Weapon_Thrown; break;                                         

				//mid
				m_compatibleObjectTypes[(int)eObjectType.Hammer]       = new eObjectType[] { eObjectType.Hammer };
				m_compatibleObjectTypes[(int)eObjectType.Sword]        = new eObjectType[] { eObjectType.Sword };
				m_compatibleObjectTypes[(int)eObjectType.LeftAxe]      = new eObjectType[] { eObjectType.LeftAxe };
				m_compatibleObjectTypes[(int)eObjectType.Axe]          = new eObjectType[] { eObjectType.Axe };
				m_compatibleObjectTypes[(int)eObjectType.HandToHand]   = new eObjectType[] { eObjectType.HandToHand };
				m_compatibleObjectTypes[(int)eObjectType.Spear]        = new eObjectType[] { eObjectType.Spear };
				m_compatibleObjectTypes[(int)eObjectType.CompositeBow] = new eObjectType[] { eObjectType.CompositeBow };
				m_compatibleObjectTypes[(int)eObjectType.Thrown]       = new eObjectType[] { eObjectType.Thrown };

				//hib
				m_compatibleObjectTypes[(int)eObjectType.Blunt]        = new eObjectType[] { eObjectType.Blunt };
				m_compatibleObjectTypes[(int)eObjectType.Blades]       = new eObjectType[] { eObjectType.Blades };
				m_compatibleObjectTypes[(int)eObjectType.Piercing]     = new eObjectType[] { eObjectType.Piercing };
				m_compatibleObjectTypes[(int)eObjectType.LargeWeapons] = new eObjectType[] { eObjectType.LargeWeapons };
				m_compatibleObjectTypes[(int)eObjectType.CelticSpear]  = new eObjectType[] { eObjectType.CelticSpear };
				m_compatibleObjectTypes[(int)eObjectType.Scythe]       = new eObjectType[] { eObjectType.Scythe };
				m_compatibleObjectTypes[(int)eObjectType.RecurvedBow]  = new eObjectType[] { eObjectType.RecurvedBow };

				m_compatibleObjectTypes[(int)eObjectType.Shield]       = new eObjectType[] { eObjectType.Shield };
				m_compatibleObjectTypes[(int)eObjectType.Poison]       = new eObjectType[] { eObjectType.Poison };
				//TODO: case 45: abilityCheck = Abilities.instruments; break;
			}

			eObjectType[] res = (eObjectType[])m_compatibleObjectTypes[(int)objectType];
			if(res == null)
				return new eObjectType[0];
			return res;
		}

		/// <summary>
		/// Gets the player name based on server type
		/// </summary>
		/// <param name="source">The "looking" player</param>
		/// <param name="target">The considered player</param>
		/// <returns>The name of the target</returns>
		public override string GetPlayerName(GamePlayer source, GamePlayer target)
		{
			if (IsSameRealm(source, target, true))
				return target.Name;

			return source.RaceToTranslatedName(target.Race, target.Gender);
		}

		/// <summary>
		/// Gets the player last name based on server type
		/// </summary>
		/// <param name="source">The "looking" player</param>
		/// <param name="target">The considered player</param>
		/// <returns>The last name of the target</returns>
		public override string GetPlayerLastName(GamePlayer source, GamePlayer target)
		{
			if (IsSameRealm(source, target, true))
				return target.LastName;

			return target.RealmRankTitle(source.Client.Account.Language);
		}

		/// <summary>
		/// Gets the player guild name based on server type
		/// </summary>
		/// <param name="source">The "looking" player</param>
		/// <param name="target">The considered player</param>
		/// <returns>The guild name of the target</returns>
		public override string GetPlayerGuildName(GamePlayer source, GamePlayer target)
		{
			if (IsSameRealm(source, target, true))
				return target.GuildName;

			return string.Empty;
		}
	
		/// <summary>
		/// Gets the player's custom title based on server type
		/// </summary>
		/// <param name="source">The "looking" player</param>
		/// <param name="target">The considered player</param>
		/// <returns>The custom title of the target</returns>
		public override string GetPlayerTitle(GamePlayer source, GamePlayer target)
		{
			if (IsSameRealm(source, target, true))
				return target.CurrentTitle.GetValue(source, target);
			
			return string.Empty;
		}

		/// <summary>
		/// Reset the keep with special server rules handling
		/// </summary>
		/// <param name="lord">The lord that was killed</param>
		/// <param name="killer">The lord's killer</param>
		public override void ResetKeep(GuardLord lord, GameObject killer)
		{
			base.ResetKeep(lord, killer);
			lord.Component.Keep.Reset((eRealm)killer.Realm);
		}
	}
}
