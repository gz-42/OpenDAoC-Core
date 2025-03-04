using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.ServerProperties;

namespace DOL.GS
{
    public class BdSubPet : BdPet
    {
        private static new readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Holds the different subpet ids
        /// </summary>
        public enum SubPetType : byte
        {
            Melee = 0,
            Healer = 1,
            Caster = 2,
            Debuffer = 3,
            Buffer = 4,
            Archer = 5
        }

        public bool MinionsAssisting {
            get { return Owner is CommanderPet commander && commander.MinionsAssisting; }
        }

        protected string m_PetSpecLine = null;
        /// <summary>
        /// Returns the spell line specialization this pet was summoned from
        /// </summary>
        public string PetSpecLine {
            get {
                // This is really inefficient, so only do it once, and only if we actually need it
                if (m_PetSpecLine == null && Brain is IControlledBrain brain && brain.GetPlayerOwner() is GamePlayer player)
                {
                    // Get the spell that summoned this pet
                    DbSpell dbSummoningSpell = DOLDB<DbSpell>.SelectObject(DB.Column("LifeDrainReturn").IsEqualTo(NPCTemplate.TemplateId));
                    if (dbSummoningSpell != null)
                    {
                        // Figure out which spell line the summoning spell is from
                        DbLineXSpell dbLineSpell = DOLDB<DbLineXSpell>.SelectObject(DB.Column("SpellID").IsEqualTo(dbSummoningSpell.SpellID));
                        if (dbLineSpell != null)
                        {
                            // Now figure out what the spec name is
                            SpellLine line = player.GetSpellLine(dbLineSpell.LineName);
                            if (line != null)
                                m_PetSpecLine = line.Spec;
                        }
                    }
                }

                return m_PetSpecLine;
            }
        }

        /// <summary>
        /// Create a minion.
        /// </summary>
        /// <param name="npcTemplate"></param>
        /// <param name="owner"></param>
        public BdSubPet(INpcTemplate npcTemplate) : base(npcTemplate) { }

        /// <summary>
        /// Changes the commander's weapon to the specified weapon template
        /// </summary>
        public void MinionGetWeapon(CommanderPet.eWeaponType weaponType)
        {
            DbItemTemplate itemTemp = CommanderPet.GetWeaponTemplate(weaponType);

            if (itemTemp == null)
                return;

            DbInventoryItem weapon;

            weapon = GameInventoryItem.Create(itemTemp);
            if (weapon != null)
            {
                if (Inventory == null)
                    Inventory = new GameNPCInventory(new GameNpcInventoryTemplate());
                else
                    Inventory.RemoveItem(Inventory.GetItem((eInventorySlot)weapon.Item_Type));

                Inventory.AddItem((eInventorySlot)weapon.Item_Type, weapon);
                SwitchWeapon((eActiveWeaponSlot)weapon.Hand);
            }
        }

        /// <summary>
        /// Sort spells into specific lists, scaling pet spell as appropriate
        /// </summary>
        public override void SortSpells()
        {
            if (Spells.Count < 1 || Level < 1)
                return;

            base.SortSpells();

            if (Properties.PET_SCALE_SPELL_MAX_LEVEL > 0)
            {
                int scaleLevel = Level;

                // Some minions have multiple spells, so only grab their owner's spec once per pet.
                if (Properties.PET_CAP_BD_MINION_SPELL_SCALING_BY_SPEC
                    && Brain is IControlledBrain brain && brain.GetPlayerOwner() is GamePlayer BD)
                {
                    int spec = BD.GetModifiedSpecLevel(PetSpecLine);

                    if (spec > 0 && spec < scaleLevel)
                        scaleLevel = spec;
                }

                ScaleSpells(scaleLevel);
            }
        }

        public override void ScaleSpell(Spell spell, int casterLevel, double baseLineLevel)
        {
            if (Properties.PET_SCALE_SPELL_MAX_LEVEL < 1 || spell == null || Level < 1)
                return;

            if (casterLevel < 1)
            {
                casterLevel = Level;

                // Style procs and subspells can't be scaled in advance, so we need to check BD spec here as well.
                if (Properties.PET_CAP_BD_MINION_SPELL_SCALING_BY_SPEC
                    && Brain is IControlledBrain brain && brain.GetPlayerOwner() is GamePlayer BD)
                {
                    int spec = BD.GetModifiedSpecLevel(PetSpecLine);

                    if (spec > 0 && spec < casterLevel)
                        casterLevel = spec;
                }
            }
        }

        public override void Die(GameObject killer)
        {
            CommanderPet commander = (this.Brain as IControlledBrain).Owner as CommanderPet;
            commander.RemoveControlledBrain(this.Brain as IControlledBrain);
            base.Die(killer);
        }
    }
}
