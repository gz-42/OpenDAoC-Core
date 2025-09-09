﻿using System.Collections.Generic;
using System.Threading;
using DOL.AI.Brain;
using DOL.GS.Keeps;

namespace DOL.GS
{
    public class NpcCastingComponent : CastingComponent
    {
        private GameNPC _npcOwner;
        private Dictionary<GameObject, List<SpellWaitingForLosCheck>> _spellsWaitingForLosCheck = new();
        private Lock _spellsWaitingForLosCheckLock = new();

        private bool IsCasterGuardOrImmobile => _npcOwner is GuardCaster || _npcOwner.MaxSpeedBase == 0;

        public NpcCastingComponent(GameNPC npcOwner) : base(npcOwner)
        {
            _npcOwner = npcOwner;
        }

        public override bool RequestCastSpell(Spell spell, SpellLine spellLine, ISpellCastingAbilityHandler spellCastingAbilityHandler = null, GameLiving target = null)
        {
            // `spellCastingAbilityHandler` is unused for NPCs.

            Spell spellToCast;

            if (spellLine.KeyName is GlobalSpellsLines.Mob_Spells)
            {
                // NPC spells will get the level equal to their caster
                spellToCast = (Spell) spell.Clone();
                spellToCast.Level = _npcOwner.Level;
            }
            else
                spellToCast = spell;

            if (target == _npcOwner || target == null)
                return RequestCastSpellInternal(spellToCast, spellLine, null, target);

            GamePlayer losChecker = target as GamePlayer;

            if (losChecker == null && _npcOwner.Brain is IControlledBrain controlledBrain)
                losChecker = controlledBrain.GetPlayerOwner();

            if (losChecker == null && _npcOwner.Brain is StandardMobBrain brain)
            {
                List<GamePlayer> playersInRadius = _npcOwner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE);

                if (playersInRadius.Count > 0)
                    losChecker = playersInRadius[Util.Random(playersInRadius.Count - 1)];
            }

            if (losChecker == null)
                return RequestCastSpellInternal(spellToCast, spellLine, null, target);

            SpellWaitingForLosCheck spellWaitingForLosCheck = new(spellToCast, spellLine);

            lock (_spellsWaitingForLosCheckLock)
            {
                if (_spellsWaitingForLosCheck.TryGetValue(target, out var list))
                    list.Add(spellWaitingForLosCheck);
                else
                    _spellsWaitingForLosCheck[target] = [spellWaitingForLosCheck];
            }

            losChecker.Out.SendCheckLos(_npcOwner, target, CastSpellLosCheckReply);
            return true; // Consider the NPC is casting while waiting for the reply to prevent it from moving.
        }

        public override void OnSpellCast(Spell spell)
        {
            if (!spell.IsHarmful || !spell.IsInstantCast)
                return;

            _npcOwner.ApplyInstantHarmfulSpellDelay();
        }

        public override void ClearSpellHandlers()
        {
            // Make sure NPCs don't start casting pending spells after being told to stop.
            lock (_spellsWaitingForLosCheckLock)
            {
                _spellsWaitingForLosCheck.Clear();
            }

            // Don't clear the attack spell queue here.
            if (_npcOwner.Brain is NecromancerPetBrain necromancerPetBrain)
                necromancerPetBrain.ClearSpellQueue();

            base.ClearSpellHandlers();
        }

        public bool IsAllowedToFollow(GameObject target)
        {
            if (!IsCasterGuardOrImmobile)
                return true;

            if (target is not GameLiving livingTarget)
                return false;

            return livingTarget.ActiveWeaponSlot is not eActiveWeaponSlot.Distance && livingTarget.IsWithinRadius(_npcOwner, livingTarget.attackComponent.AttackRange);
        }

        private void CastSpellLosCheckReply(GamePlayer losChecker, LosCheckResponse response, ushort sourceOID, ushort targetOID)
        {
            GameObject target = _npcOwner.CurrentRegion.GetObject(targetOID);

            if (target == null)
                return;

            lock (_spellsWaitingForLosCheckLock)
            {
                if (!_spellsWaitingForLosCheck.TryGetValue(target, out var list))
                    return;

                bool success = response is LosCheckResponse.True;

                foreach (SpellWaitingForLosCheck spellWaitingForLosCheck in list)
                {
                    Spell spell = spellWaitingForLosCheck.Spell;
                    SpellLine spellLine = spellWaitingForLosCheck.SpellLine;

                    if (success && spellLine != null && spell != null)
                        RequestCastSpellInternal(spell, spellLine, null, target as GameLiving, losChecker);
                    else
                        _npcOwner.OnCastSpellLosCheckFail(target);
                }

                list.Clear();
            }
        }

        private readonly struct SpellWaitingForLosCheck
        {
            public readonly Spell Spell;
            public readonly SpellLine SpellLine;

            public SpellWaitingForLosCheck(Spell spell, SpellLine spellLine)
            {
                Spell = spell;
                SpellLine = spellLine;
            }
        }
    }
}
