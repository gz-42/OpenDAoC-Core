using DOL.GS.Spells;

namespace DOL.GS.Effects
{
    public class AtlasOF_ForestheartAmbusherECSEffect : ECSGameAbilityEffect
    {
        public AtlasOF_ForestheartAmbusherECSEffect(in ECSGameEffectInitParams initParams) : base(initParams)
        {
            EffectType = eEffect.ForestheartAmbusher;
            Start();
        }

        public override ushort Icon => 4268;
        public override string Name => "Forestheart Ambusher";
        public override bool HasPositiveEffect => true;
        public SummonAnimistAmbusher PetSpellHander;

        public override void OnStartEffect()
        {
            SpellLine RAspellLine = GlobalSpellsLines.RealmSpellsSpellLine;
            Spell ForestheartAmbusher = SkillBase.GetSpellByID(90802);

            if (ForestheartAmbusher != null)
                Owner.CastSpell(ForestheartAmbusher, RAspellLine);

            base.OnStartEffect();
        }

        public override void OnStopEffect()
        {
            // The effect can be cancelled before the spell if fired by the casting service, in which case 'PetSpellHander' can be null.
            if (PetSpellHander?.Pet.IsBeingHandledByReaperService == false)
            {
                PetSpellHander.Pet.Health = 0; // To send proper remove packet.
                PetSpellHander.Pet.Delete();
            }

            base.OnStopEffect();
        }

        public void Cancel(bool playerCancel)
        {
            Stop(playerCancel);
            OnStopEffect();
        }
    }
}
