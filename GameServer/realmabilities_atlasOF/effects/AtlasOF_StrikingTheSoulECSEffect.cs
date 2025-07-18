namespace DOL.GS.Effects
{
    // It looks like the real name should be "Strike the Soul", but renaming it requires updating the database.
    public class StrikingTheSoulECSEffect : ECSGameAbilityEffect
    {
        public StrikingTheSoulECSEffect(in ECSGameEffectInitParams initParams) : base(initParams)
        {
            EffectType = eEffect.StrikingTheSoul;
            Start();
        }

        public override ushort Icon => 4271;
        public override string Name => "Striking the Soul";
        public override bool HasPositiveEffect => true;
        private NecromancerPet _necromancerPet;

        public override void OnStartEffect()
        {
            if (OwnerPlayer == null)
                return;

            _necromancerPet = OwnerPlayer.ControlledBrain?.Body as NecromancerPet;

            // This implies that using the RA before summoning a pet will not make it receive the to-hit bonus.
            if (_necromancerPet == null)
                return;

            _necromancerPet.OtherBonus[eProperty.ToHitBonus] += (int)Effectiveness;
            base.OnStartEffect();
        }

        public override void OnStopEffect()
        {
            if (OwnerPlayer == null || _necromancerPet == null)
                return;

            _necromancerPet.OtherBonus[eProperty.ToHitBonus] -= (int)Effectiveness;
            base.OnStopEffect();
        }
    }
}
