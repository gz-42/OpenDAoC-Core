using System.Collections.Generic;
using System.Linq;
using DOL.GS.API;
using DOL.GS.PacketHandler;

namespace DOL.GS.Effects
{
    public class AtlasOF_BatteryOfLifeECSEffect : ECSGameAbilityEffect
    {
        public AtlasOF_BatteryOfLifeECSEffect(in ECSGameEffectInitParams initParams) : base(initParams)
        {
            EffectType = eEffect.BatteryOfLife;
            Start();
            PulseFreq = 1000;
            NextTick = GameLoop.GameLoopTime;
        }

        private int _healthPool = 0;

        public override ushort Icon => 4274;
        public override string Name => "Battery Of Life";
        public override bool HasPositiveEffect => true;

        public override void OnStartEffect()
        {
            _healthPool = (int) (1000 * (1 + OwnerPlayer.GetModified(eProperty.BuffEffectiveness) * 0.01));
            base.OnStartEffect();
        }

        public override void OnEffectPulse()
        {
            if (_healthPool <= 0)
                Stop();

            if (OwnerPlayer.Group != null)
            {
                Dictionary<GameLiving, int> livingToHeal = new();

                foreach (GameLiving living in OwnerPlayer.Group.GetMembersInTheGroup())
                {
                    if (living.IsWithinRadius(OwnerPlayer, 1500) && living.IsAlive && living != OwnerPlayer)
                        livingToHeal.Add(living, living.Health);
                }

                //Fen found this on stack overflow https://stackoverflow.com/questions/289/how-do-you-sort-a-dictionary-by-value
                //and https://stackoverflow.com/questions/3066182/convert-an-iorderedenumerablekeyvaluepairstring-int-into-a-dictionarystrin
                //Here we sort by health to put lowest at the first. I apologize for this line but it does the job o7
                Dictionary<GameLiving, int> sortedLiving = (from entry in livingToHeal orderby entry.Value ascending select entry).ToDictionary(pair => pair.Key, pair => pair.Value);

                foreach (var healed in sortedLiving)
                {
                    GameLiving currentLiving = healed.Key;
                    int difference = currentLiving.MaxHealth - currentLiving.Health;

                    difference = currentLiving.ChangeHealth(OwnerPlayer, eHealthChangeType.Spell, difference);

                    if (difference > 0)
                    {
                        _healthPool -= difference;

                        if (currentLiving is GamePlayer playerTarget)
                            playerTarget.Out.SendMessage($"{OwnerName}'s Battery of Life heals you for {difference} health points!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);

                        if (Owner is GamePlayer playerCaster)
                            playerCaster.Out.SendMessage($"Your Battery of Life heals {currentLiving.Name} for {difference} health points!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);

                        if (_healthPool <= 0)
                        {
                            Stop();
                            break;
                        }
                    }
                }
            }
        }
    }
}
