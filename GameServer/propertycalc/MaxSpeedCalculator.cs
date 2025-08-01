using System.Linq;
using DOL.AI.Brain;
using DOL.GS.Effects;
using DOL.GS.RealmAbilities;

namespace DOL.GS.PropertyCalc
{
    /// <summary>
    /// The Max Speed calculator
    /// BuffBonusCategory1 unused
    /// BuffBonusCategory2 unused
    /// BuffBonusCategory3 unused
    /// BuffBonusCategory4 unused
    /// BuffBonusMultCategory1 used for all multiplicative speed bonuses
    /// </summary>
    [PropertyCalculator(eProperty.MaxSpeed)]
    public class MaxSpeedCalculator : PropertyCalculator
    {
        public static readonly double SPEED1 = 1.44;
        public static readonly double SPEED2 = 1.59;
        public static readonly double SPEED3 = 1.74;
        public static readonly double SPEED4 = 1.89;
        public static readonly double SPEED5 = 2.04;

        public override int CalcValue(GameLiving living, eProperty property)
        {
            if ((living.IsMezzed || living.IsStunned) && living.effectListComponent.GetEffects().FirstOrDefault(x => x.GetType() == typeof(SpeedOfSoundECSEffect)) == null)
                return 0;

            double speed = living.BuffBonusMultCategory1.Get((int)property);

            if (living is GamePlayer player)
            {
                // Since Dark Age of Camelot's launch, we have heard continuous feedback from our community about the movement speed in our game. The concerns over how slow
                // our movement is has continued to grow as we have added more and more areas in which to travel. Because we believe these concerns are valid, we have decided
                // to make a long requested change to the game, enhancing the movement speed of all players who are out of combat. This new run state allows the player to move
                // faster than normal run speed, provided that the player is not in any form of combat. Along with this change, we have slightly increased the speed of all
                // secondary speed buffs (see below for details). Both of these changes are noticeable but will not impinge upon the supremacy of the primary speed buffs available
                // to the Bard, Skald and Minstrel.
                // - The new run speed does not work if the player is in any form of combat. All combat timers must also be expired.
                // - The new run speed will not stack with any other run speed spell or ability, except for Sprint.
                // - Pets that are not in combat have also received the new run speed, only when they are following, to allow them to keep up with their owners.

                double horseSpeed = player.IsOnHorse ? player.ActiveHorse.Speed * 0.01 : 1.0;

                if (speed > horseSpeed)
                    horseSpeed = 1.0;

                if (ServerProperties.Properties.ENABLE_PVE_SPEED)
                {
                    // OF zones technically aren't in a RvR region.
                    if (speed == 1 && !player.InCombat && !player.IsStealthed && !player.CurrentRegion.IsRvR && !player.CurrentZone.IsRvR)
                        speed *= 1.25; // New run speed is 125% when no buff.
                }

                if (player.IsEncumbered && player.Client.Account.PrivLevel == 1 && ServerProperties.Properties.ENABLE_ENCUMBERANCE_SPEED_LOSS)
                {
                    speed *= player.MaxSpeedModifierFromEncumbrance;

                    if (speed <= 0)
                        speed = 0;
                }

                if (player.IsStealthed && player.Client.Account.PrivLevel == 1)
                {
                    AtlasOF_MasteryOfStealth mos = player.GetAbility<AtlasOF_MasteryOfStealth>();
                    //GameSpellEffect bloodrage = SpellHandler.FindEffectOnTarget(player, "BloodRage");
                    //VanishEffect vanish = player.EffectList.GetOfType<VanishEffect>();
                    double stealthSpec = player.GetModifiedSpecLevel(Specs.Stealth);

                    if (stealthSpec > player.Level)
                        stealthSpec = player.Level;

                    speed *= 0.3 + (stealthSpec + 10) * 0.3 / (player.Level + 10);

                    //if (vanish != null)
                    //    speed *= vanish.SpeedBonus;

                    if (mos != null)
                        speed *= 1 + mos.GetAmountForLevel(mos.Level) / 100.0;

                    //if (bloodrage != null)
                    //    speed *= 1 + (bloodrage.Spell.Value * 0.01); // 25 * 0.01 = 0.25 (a.k 25%) value should be 25.5

                    if (player.effectListComponent.ContainsEffectForEffectType(eEffect.ShadowRun))
                        speed *= 2;
                }

                if (GameRelic.IsPlayerCarryingRelic(player))
                {
                    if (speed > 1.0)
                        speed = 1.0;

                    horseSpeed = 1.0;
                }

                if (player.IsSprinting)
                    speed *= 1.3;

                speed *= horseSpeed;
            }
            else if (living is GameNPC npc)
            {
                if (!living.InCombat && npc.Brain is IControlledBrain brain)
                {
                    GameLiving owner = brain.Owner;

                    if (owner != null && owner == brain.Body.FollowTarget)
                    {
                        GamePlayer playerOwner = brain.GetPlayerOwner();

                        if (!living.InCombat)
                        {
                            if (playerOwner != null)
                            {
                                owner = playerOwner;
                                GameNPC ownerSteed = playerOwner.Steed;

                                if (ownerSteed != null)
                                    return ownerSteed.MaxSpeed;
                            }

                            int distance = brain.Body.GetDistanceTo(owner);

                            if (playerOwner != null && playerOwner.IsSprinting)
                                speed *= 1.3;

                            if (distance > 20)
                                speed *= 1.25;

                            if (living is NecromancerPet && distance > 700)
                                speed *= 1.25;

                            double ownerSpeedAdjust = (double) owner.MaxSpeed / owner.MaxSpeedBase;

                            if (ownerSpeedAdjust > 1.0)
                                speed *= ownerSpeedAdjust;
                        }
                    }
                }

                double healthPercent = living.Health / (double) living.MaxHealth;

                if (healthPercent < 0.33)
                    speed *= 0.2 + healthPercent * (0.8 / 0.33); // 33% HP = full speed, 0% HP = 20% speed
            }

            speed = living.MaxSpeedBase * speed + 0.5; // 0.5 is to fix the rounding error when converting to int so root results in speed 2 ((191 * 0.01 = 1.91) + 0.5 = 2.41).

            if (speed <= 0.5) // Fix for the rounding fix above. (???)
                return 0;

            return (int)speed;
        }
    }
}
