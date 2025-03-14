using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	/// <summary>
	/// SelfCraftWindow is the object used to cambine item alone
	/// </summary>
	public class SelfCraftWindow : ITradeWindow
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

		public SelfCraftWindow(GamePlayer owner, DbInventoryItem item)
		{
			if (owner == null)
				throw new ArgumentNullException("owner");
			
			m_owner = owner;
			m_tradeItems = new ArrayList(10);

			m_itemToCombine = new ArrayList(1);
			m_itemToCombine.Add(item);
		}

		#region Fields

		/// <summary>
		/// Holds a list of items used to combine with
		/// </summary>
		protected ArrayList m_tradeItems;
		/// <summary>
		/// Holds the owner of this window and items in it
		/// </summary>
		protected GamePlayer m_owner;
		/// <summary>
		/// Holds the item used to combine on
		/// </summary>
		protected ArrayList m_itemToCombine;
		/// <summary>
		/// Holds if we have accepted the trade or not
		/// </summary>
		protected bool	m_tradeAccept;
		/// <summary>
		/// Stores the begin changes count
		/// </summary>
		protected int m_changesCount;

		#endregion

		#region Properties

		/// <summary>
		/// Returns the array of items used to combine with
		/// </summary>
		public ArrayList TradeItems
		{
			get { return m_tradeItems; }
			set { m_tradeItems = value; }
		}

		/// <summary>
		/// Returns the array of items the partner offer for trade
		/// </summary>
		public ArrayList PartnerTradeItems
		{
			get { return m_itemToCombine; }
		}

		/// <summary>
		/// Returns the money we offer for trade
		/// </summary>
		public long TradeMoney
		{
			get { return 0; }
			set {  }
		}

		/// <summary>
		/// Returns the money the partner offer for trade
		/// </summary>
		public long PartnerTradeMoney
		{
			get { return 0; }
		}

		/// <summary>
		/// Gets the owner of this window and items in it
		/// </summary>
		public GamePlayer Owner
		{
			get { return m_owner; }
		}

		/// <summary>
		/// Gets the owner of this window and items in it
		/// </summary>
		public GamePlayer Partner
		{
			get { return null; }
		}

		private readonly Lock _lock = new();
		public Lock Lock => _lock;

		/// <summary>
		/// Gets the item count in trade window
		/// </summary>
		public int ItemsCount
		{
			get { return m_tradeItems.Count; }
		}

		/// <summary>
		/// Gets the item count in partner trade window
		/// </summary>
		public int PartnerItemsCount
		{
			get { return m_itemToCombine.Count; }
		}

		/// <summary>
		/// Gets or sets the repair flag is switched
		/// </summary>
		public bool Repairing
		{
			get { return false; }
			set 
			{
				m_owner.Out.SendMessage("You cannot repair while self-crafting!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
			}
		}

		/// <summary>
		/// Gets or sets the combine flag is switched
		/// </summary>
		public bool Combine
		{
			get { return true; }
			set 
			{
				m_owner.Out.SendMessage("Combine flag is autoset while self-crafting!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
			}
		}

		#endregion

		#region Add/Remove/Update

		/// <summary>
		/// The max amount of items that can be added to tradewindow
		/// </summary>
		public const int MAX_ITEMS = 10;

		/// <summary>
		/// Adds an item to the tradewindow
		/// </summary>
		/// <param name="itemForTrade">InventoryItem to add</param>
		/// <returns>true if added</returns>
		public bool AddItemToTrade(DbInventoryItem itemForTrade)
		{
			lock(Lock)
			{
				if(!itemForTrade.IsDropable || !itemForTrade.IsPickable || itemForTrade.IsNotLosingDur)
					return false;
				if (TradeItems.Contains(itemForTrade))
					return false;
				if (TradeItems.Count >= MAX_ITEMS)
				{
					TradeUpdate();
					return false;
				}

				TradeItems.Add(itemForTrade);
				TradeUpdate();
				return true;
			}
		}

		/// <summary>
		/// Adds money to the tradewindow
		/// </summary>
		/// <param name="money">Array of money values to add</param>
		public void AddMoneyToTrade(long money)
		{
			return;
		}

		/// <summary>
		/// Removes an item from the tradewindow
		/// </summary>
		/// <param name="itemToRemove"></param>
		public void RemoveItemToTrade(DbInventoryItem itemToRemove)
		{
			if (itemToRemove == null)
				return;

			lock(Lock)
			{
				TradeItems.Remove(itemToRemove);
				if(!m_tradeAccept) TradeUpdate();
			}
		}

		/// <summary>
		/// Updates the trade window
		/// </summary>
		public void TradeUpdate()
		{
			lock (Lock)
			{
				if (m_changesCount > 0) return;
				if (m_changesCount < 0)
				{
					m_changesCount = 0;
					if (log.IsErrorEnabled)
						log.Error("Changes count is less than 0, forgot to add m_changesCount++?\n\n" + Environment.StackTrace);
				}

				DbInventoryItem itemToCombine = (DbInventoryItem)PartnerTradeItems[0];
                // --------------------------------------------------------------
                // Luhz Crafting Update:
                // Players may now have any, and all, "primary" crafting skills.
                // AbstractCraftingSkill skill = CraftingMgr.getSkillbyEnum(m_owner.CraftingPrimarySkill);
                AbstractCraftingSkill skill = null;
                lock (m_owner.TradeWindow.Lock)
                {
                    foreach (DbInventoryItem i in (ArrayList)m_owner.TradeWindow.TradeItems.Clone())
                    {
                        if (i.Object_Type == (int)eObjectType.AlchemyTincture)
                        {
                            if (m_owner.GetCraftingSkillValue(eCraftingSkill.Alchemy) > 0)
                            {
                                skill = CraftingMgr.getSkillbyEnum(eCraftingSkill.Alchemy);
                                break;
                            }
                        }
                        else if (i.Object_Type == (int)eObjectType.SpellcraftGem)
                        {
                            if (m_owner.GetCraftingSkillValue(eCraftingSkill.SpellCrafting) > 0)
                            {
                                skill = CraftingMgr.getSkillbyEnum(eCraftingSkill.SpellCrafting);
                                break;
                            }
                        }
                    }
                }
                // --------------------------------------------------------------
				if(skill != null && skill is AdvancedCraftingSkill && itemToCombine != null)
				{
					if(((AdvancedCraftingSkill)skill).IsAllowedToCombine(m_owner, itemToCombine))
					{
						if(skill is SpellCrafting)
							((SpellCrafting)skill).ShowSpellCraftingInfo(m_owner, itemToCombine);
					}
				}

				m_owner.Out.SendTradeWindow();
			}
		}

		#endregion	

		#region AcceptTrade/CloseTrade
		
		/// <summary>
		/// Called each time a player push the accept button to accept the trade
		/// </summary>
		public bool AcceptTrade()
		{
			m_tradeAccept = true;

			lock (Lock)
			{
                // --------------------------------------------------------------
                // Luhz Crafting Update:
                // Players may now have any, and all, "primary" crafting skills.
                // AbstractCraftingSkill skill = CraftingMgr.getSkillbyEnum(m_owner.CraftingPrimarySkill);
                AbstractCraftingSkill skill = null;
                lock (m_owner.TradeWindow.Lock)
                {
                    foreach (DbInventoryItem i in (ArrayList)m_owner.TradeWindow.TradeItems.Clone())
                    {
                        if (i.Object_Type == (int)eObjectType.AlchemyTincture)
                        {
                            if (m_owner.GetCraftingSkillValue(eCraftingSkill.Alchemy) > 0)
                            {
                                skill = CraftingMgr.getSkillbyEnum(eCraftingSkill.Alchemy);
                                break;
                            }
                        }
                        else if (i.Object_Type == (int)eObjectType.SpellcraftGem)
                        {
                            if (m_owner.GetCraftingSkillValue(eCraftingSkill.SpellCrafting) > 0)
                            {
                                skill = CraftingMgr.getSkillbyEnum(eCraftingSkill.SpellCrafting);
                                break;
                            }
                        }
                    }
                }
                // --------------------------------------------------------------
				if(skill != null && skill is AdvancedCraftingSkill)
				{
					((AdvancedCraftingSkill)skill).CombineItems(m_owner);
				}
			}
				
			CloseTrade();
			return true;
		}

		/// <summary>
		/// Closes the tradewindow
		/// </summary>
		public void CloseTrade()
		{	
			lock (Lock)
			{
				m_owner.Out.SendCloseTradeWindow();
			}
			lock (m_owner)
			{
				m_owner.TradeWindow = null;
			}
		}
		#endregion
	}
}
