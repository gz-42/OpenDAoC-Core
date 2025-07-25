using DOL.GS.PacketHandler;
using DOL.GS.Spells;

namespace DOL.GS.Effects
{
    public class AtlasOF_IchorECSEffect : ECSGameAbilityEffect
    {
        public new SpellHandler SpellHandler;
        public AtlasOF_IchorECSEffect(in ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.Ichor;
            Start();
        }

        public override ushort Icon { get { return 7029; } }
        public override string Name { get { return "Ichor of the Deep"; } }
        public override bool HasPositiveEffect { get { return false; } }

        public override void OnStartEffect()
        {
            base.OnStartEffect();
            // Send spell message to player if applicable
            if (Owner is GamePlayer gpMessage)
                gpMessage.Out.SendMessage("Constricting bonds surround your body!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);

            // Apply the snare
            Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, this, 1.0 - 99 * 0.01);
            //m_rootExpire = new ECSGameTimer(target, new ECSGameTimer.ECSTimerCallback(RootExpires), duration);
            //GameEventMgr.AddHandler(target, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(OnAttacked));
            Owner.OnMaxSpeedChange();

            // Send root animation and spell message
            foreach (GamePlayer player in Owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellEffectAnimation(Owner, Owner, 7029, 0, false, 1);

                if (player.IsWithinRadius(Owner, WorldMgr.INFO_DISTANCE) && player != Owner)
                    player.Out.SendMessage(Owner.GetName(0, false) + " is surrounded by constricting bonds!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
            }
        }

        public override void OnStopEffect()
        {
            Owner.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, this);
            Owner.OnMaxSpeedChange();
            base.OnStopEffect();
        }
    }
}
