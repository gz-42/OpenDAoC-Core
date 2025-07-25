using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DOL.Database;

namespace DOL.GS
{
	public class GameNpcInventoryTemplate : GameLivingInventory
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Holds inventory item instances already used in inventory templates
		/// </summary>
		protected static readonly Hashtable m_usedInventoryItems = new Hashtable(1024);
		protected static readonly Lock _usedInventoryItems = new();

		/// <summary>
		/// Holds already used inventory template instances
		/// </summary>
		protected static readonly Hashtable m_usedInventoryTemplates = new Hashtable(256);
		protected static readonly Lock _usedInventoryTemplates = new();

		/// <summary>
		/// Holds an empty inventory template instance
		/// </summary>
		public static readonly GameNpcInventoryTemplate EmptyTemplate;

		/// <summary>
		/// Static constructor
		/// </summary>
		static GameNpcInventoryTemplate()
		{
			GameNpcInventoryTemplate temp = new GameNpcInventoryTemplate().CloseTemplate();
			Thread.MemoryBarrier();
			EmptyTemplate = temp;
		}

		/// <summary>
		/// Create the hash table
		/// </summary>
		public static bool Init()
		{
			try
			{
				m_npcEquipmentCache = new Dictionary<string, List<DbNpcEquipment>>(1000);

				foreach (DbNpcEquipment equip in GameServer.Database.SelectAllObjects<DbNpcEquipment>())
				{
					if (!m_npcEquipmentCache.TryGetValue(equip.TemplateID, out List<DbNpcEquipment> npcEquipment))
					{
						npcEquipment = [];
						m_npcEquipmentCache[equip.TemplateID] = npcEquipment;
					}

					npcEquipment.Add(equip);
				}

				return true;
			}
			catch (Exception e)
			{
				log.Error(e);
			}

			return false;
		}

		/// <summary>
		/// Holds the closed flag, if true template cannot be modified
		/// </summary>
		protected bool m_isClosed;

		/// <summary>
		/// Gets the closed flag
		/// </summary>
		public bool IsClosed
		{
			get { return m_isClosed; }
		}

		/// <summary>
		/// Check if the slot is valid in the inventory
		/// </summary>
		/// <param name="slot">SlotPosition to check</param>
		/// <returns>the slot if it's valid or eInventorySlot.Invalid if not</returns>
		protected override eInventorySlot GetValidInventorySlot(eInventorySlot slot)
		{
			foreach (eInventorySlot visibleSlot in VISIBLE_SLOTS)
				if (visibleSlot == slot)
					return slot;
			return eInventorySlot.Invalid;
		}

		#region AddNPCEquipment/RemoveNPCEquipment/CloseTemplate/CloneTemplate

		/// <summary>
		/// Adds item to template reusing inventory item instances from other templates.
		/// </summary>
		/// <param name="slot">The equipment slot</param>
		/// <param name="model">The equipment model</param>
		/// <returns>true if added</returns>
		public bool AddNPCEquipment(eInventorySlot slot, int model)
		{
			return AddNPCEquipment(slot, model, 0, 0, 0);
		}

		/// <summary>
		/// Adds item to template reusing inventory item instances from other templates.
		/// </summary>
		/// <param name="slot">The equipment slot</param>
		/// <param name="model">The equipment model</param>
		/// <param name="color">The equipment color</param>
		/// <returns>true if added</returns>
		public bool AddNPCEquipment(eInventorySlot slot, int model, int color)
		{
			return AddNPCEquipment(slot, model, color, 0, 0);
		}

		/// <summary>
		/// Adds item to template reusing inventory item instances from other templates.
		/// </summary>
		/// <param name="slot">The equipment slot</param>
		/// <param name="model">The equipment model</param>
		/// <param name="color">The equipment color</param>
		/// <param name="effect">The equipment effect</param>
		/// <returns>true if added</returns>
		public bool AddNPCEquipment(eInventorySlot slot, int model, int color, int effect)
		{
			return AddNPCEquipment(slot, model, color, effect, 0);
		}

		/// <summary>
		/// Adds item to template reusing iventory  item instances from other templates.
		/// </summary>
		/// <param name="slot">The equipment slot</param>
		/// <param name="model">The equipment model</param>
		/// <param name="color">The equipment color</param>
		/// <param name="effect">The equipment effect</param>
		/// <param name="extension">The equipment extension</param>
		/// <returns>true if added</returns>
		public bool AddNPCEquipment(eInventorySlot slot, int model, int color, int effect, int extension, int emblem = 0)
		{
			lock (Lock)
			{
				lock (_usedInventoryItems)
				{
					if (m_isClosed)
						return false;
					slot = GetValidInventorySlot(slot);
					if (slot == eInventorySlot.Invalid)
						return false;
					//Changed to support randomization of slots - if we try to load a weapon in the same spot with a different model,
					//let's make it random 50% chance to either overwrite the item or leave it be
					if (m_items.ContainsKey(slot))
					{
						//50% chance to keep the item we have
						if (Util.Chance(50))
							return false;
						//Let's remove the old item!
						m_items.Remove(slot);
					}
					string itemID = string.Format("{0}:{1},{2},{3},{4}", slot, model, color, effect, extension);
					DbInventoryItem item = null;

					if (!m_usedInventoryItems.ContainsKey(itemID))
					{
						item = new GameInventoryItem();
						item.Template = new DbItemTemplate();
						item.Template.Id_nb = itemID;
						item.Model = model;
						item.Color = color;
						item.Effect = effect;
						item.Extension = (byte)extension;
						item.Emblem = emblem;
						item.SlotPosition = (int)slot;
					}
					else
						return false;

					m_items.Add(slot, item);
				}
			}
			return true;
		}

		/// <summary>
		/// Removes item from slot if template is not closed.
		/// </summary>
		/// <param name="slot">The slot to remove</param>
		/// <returns>true if removed</returns>
		public bool RemoveNPCEquipment(eInventorySlot slot)
		{
			lock (Lock)
			{
				slot = GetValidInventorySlot(slot);

				if (slot == eInventorySlot.Invalid)
					return false;

				if (m_isClosed)
					return false;

				if (!m_items.ContainsKey(slot))
					return false;

				m_items.Remove(slot);

				return true;
			}
		}

		/// <summary>
		/// Closes this template and searches for other identical templates.
		/// Template cannot be modified after it was closed, clone it instead.
		/// </summary>
		/// <returns>Invetory template instance that should be used</returns>
		public GameNpcInventoryTemplate CloseTemplate()
		{
			lock (Lock)
			{
				lock (_usedInventoryTemplates)
				{
					lock (_usedInventoryItems)
					{
						m_isClosed = true;
						StringBuilder templateID = new StringBuilder(m_items.Count * 16);
						foreach (DbInventoryItem item in new SortedList(m_items).Values)
						{
							if (templateID.Length > 0)
								templateID.Append(";");
							templateID.Append(item.Id_nb);
						}

						GameNpcInventoryTemplate finalTemplate = m_usedInventoryTemplates[templateID.ToString()] as GameNpcInventoryTemplate;
						if (finalTemplate == null)
						{
							finalTemplate = this;
							m_usedInventoryTemplates[templateID.ToString()] = this;
							foreach (var de in m_items)
							{
								if (!m_usedInventoryItems.Contains(de.Key))
									m_usedInventoryItems.Add(de.Key, de.Value);
							}
						}

						return finalTemplate;
					}
				}
			}
		}

		/// <summary>
		/// Creates a copy of the GameNpcInventoryTemplate.
		/// </summary>
		/// <returns>Open copy of this template</returns>
		public GameNpcInventoryTemplate CloneTemplate()
		{
			lock (Lock)
			{
				var clone = new GameNpcInventoryTemplate();
				clone.m_changedSlots = new List<eInventorySlot>(m_changedSlots);
				clone.m_changesCounter = m_changesCounter;

				foreach (var de in m_items)
				{
					DbInventoryItem oldItem = de.Value;

					DbInventoryItem item = new GameInventoryItem();
					item.Template = new DbItemTemplate();
					item.Template.Id_nb = oldItem.Id_nb;
					item.Model = oldItem.Model;
					item.Color = oldItem.Color;
					item.Effect = oldItem.Effect;
					item.Extension = oldItem.Extension;
					item.Emblem = oldItem.Emblem;
					item.SlotPosition = oldItem.SlotPosition;
					clone.m_items.Add(de.Key, item);
				}

				clone.m_isClosed = false;

				return clone;
			}
		}

		#endregion

		#region LoadFromDatabase/SaveIntoDatabase

		/// <summary>
		/// Cache for fast loading of npc equipment
		/// </summary>
		protected static Dictionary<string, List<DbNpcEquipment>> m_npcEquipmentCache = null;

		public override bool LoadFromDatabase(string templateId)
		{
			if (string.IsNullOrEmpty(templateId))
				return false;

			if (!m_npcEquipmentCache.TryGetValue(templateId, out List<DbNpcEquipment> items))
				items = DOLDB<DbNpcEquipment>.SelectObjects(DB.Column("templateID").IsEqualTo(templateId)).ToList();

			if (items == null || items.Count == 0)
			{
				if (log.IsWarnEnabled)
					log.Warn($"Failed loading NPC inventory template: {templateId}");

				return false;
			}

			LoadInventory(templateId, items);
			return true;
		}

		public override Task<IList> StartLoadFromDatabaseTask(string templateId)
		{
			throw new NotImplementedException();
		}

		public override bool LoadInventory(string templateId, IList items)
		{
			lock (Lock)
			{
				foreach (DbNpcEquipment npcItem in items)
				{
					if (!AddNPCEquipment((eInventorySlot) npcItem.Slot, npcItem.Model, npcItem.Color, npcItem.Effect, npcItem.Extension, npcItem.Emblem))
					{
						if (log.IsWarnEnabled)
							log.Warn($"Error adding NPC equipment for templateID {templateId}, ModelID={npcItem.Model}, slot={npcItem.Slot}");
					}
				}
			}

			return true;
		}

		public override bool SaveIntoDatabase(string templateId)
		{
			lock (Lock)
			{
				try
				{
					if (templateId == null)
						throw new ArgumentNullException("templateID");

					var npcEquipment = DOLDB<DbNpcEquipment>.SelectObjects(DB.Column("templateID").IsEqualTo(templateId));

					// delete removed item templates
					foreach (DbNpcEquipment npcItem in npcEquipment)
					{
						if (!m_items.ContainsKey((eInventorySlot)npcItem.Slot))
							GameServer.Database.DeleteObject(npcItem);
					}

					// save changed item templates
					foreach (DbInventoryItem item in m_items.Values)
					{
						bool foundInDB = false;
						foreach (DbNpcEquipment npcItem in npcEquipment)
						{
							if (item.SlotPosition != npcItem.Slot)
								continue;

							if (item.Model != npcItem.Model || item.Color != npcItem.Color || item.Effect != npcItem.Effect || item.Emblem != npcItem.Emblem)
							{
								npcItem.Model = item.Model;
								npcItem.Color = item.Color;
								npcItem.Effect = item.Effect;
								npcItem.Extension = item.Extension;
								npcItem.Emblem = item.Emblem;
								GameServer.Database.SaveObject(npcItem);
							}

							foundInDB = true;

							break;
						}

						if (!foundInDB)
						{
							DbNpcEquipment npcItem = new DbNpcEquipment();
							npcItem.Slot = item.SlotPosition;
							npcItem.Model = item.Model;
							npcItem.Color = item.Color;
							npcItem.Effect = item.Effect;
							npcItem.TemplateID = templateId;
							npcItem.Extension = item.Extension;
							npcItem.Emblem = item.Emblem;
							GameServer.Database.AddObject(npcItem);
						}
					}

					return true;
				}
				catch (Exception e)
				{
					if (log.IsErrorEnabled)
						log.Error("Error saving NPC inventory template, templateID=" + templateId, e);

					return false;
				}
			}
		}

		#endregion

		#region methods not allowed in inventory template

		/// <summary>
		/// Overridden. Inventory template cannot be modified.
		/// </summary>
		/// <param name="slot"></param>
		/// <param name="item"></param>
		/// <returns>false</returns>
		public override bool AddItem(eInventorySlot slot, DbInventoryItem item)
		{
			return false;
		}

		/// <summary>
		/// Overridden. Inventory template cannot be modified.
		/// </summary>
		/// <param name="item">the item to remove</param>
		/// <returns>false</returns>
		public override bool RemoveItem(DbInventoryItem item)
		{
			return false;
		}

		/// <summary>
		/// Overridden. Inventory template cannot be modified.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="count"></param>
		/// <returns>false</returns>
		public override bool AddCountToStack(DbInventoryItem item, int count)
		{
			return false;
		}

		/// <summary>
		/// Overridden. Inventory template cannot be modified.
		/// </summary>
		/// <param name="item">the item to remove</param>
		/// <param name="count">the count of items to be removed from the stack</param>
		/// <returns>false</returns>
		public override bool RemoveCountFromStack(DbInventoryItem item, int count)
		{
			return false;
		}

		/// <summary>
		/// Overridden. Inventory template cannot be modified.
		/// </summary>
		/// <param name="fromSlot"></param>
		/// <param name="toSlot"></param>
		/// <param name="itemCount"></param>
		/// <returns></returns>
		public override bool MoveItem(eInventorySlot fromSlot, eInventorySlot toSlot, int itemCount)
		{
			return false;
		}

		/// <summary>
		/// Overridden. Inventory template cannot be modified.
		/// </summary>
		/// <param name="fromItem">First Item</param>
		/// <param name="toItem">Second Item</param>
		/// <returns>false</returns>
		protected override bool CombineItems(DbInventoryItem fromItem, DbInventoryItem toItem)
		{
			return false;
		}

		/// <summary>
		/// Overridden. Inventory template cannot be modified.
		/// </summary>
		/// <param name="fromSlot">First SlotPosition</param>
		/// <param name="toSlot">Second SlotPosition</param>
		/// <param name="itemCount">How many items to move</param>
		/// <returns>false</returns>
		protected override bool StackItems(eInventorySlot fromSlot, eInventorySlot toSlot, int itemCount)
		{
			return false;
		}

		/// <summary>
		/// Overridden. Inventory template cannot be modified.
		/// </summary>
		/// <param name="fromSlot">First SlotPosition</param>
		/// <param name="toSlot">Second SlotPosition</param>
		/// <returns>false</returns>
		protected override bool SwapItems(eInventorySlot fromSlot, eInventorySlot toSlot)
		{
			return false;
		}

		/// <summary>
		/// Overridden. Inventory template cannot be modified.
		/// </summary>
		/// <param name="template">The ItemTemplate</param>
		/// <param name="count">The count of items to add</param>
		/// <param name="minSlot">The first slot</param>
		/// <param name="maxSlot">The last slot</param>
		/// <returns>false</returns>
		public override bool AddTemplate(DbInventoryItem template, int count, eInventorySlot minSlot, eInventorySlot maxSlot)
		{
			return false;
		}

		/// <summary>
		/// Overridden. Inventory template cannot be modified.
		/// </summary>
		/// <param name="templateID">The ItemTemplate ID</param>
		/// <param name="count">The count of items to add</param>
		/// <param name="minSlot">The first slot</param>
		/// <param name="maxSlot">The last slot</param>
		/// <returns>false</returns>
		public override bool RemoveTemplate(string templateID, int count, eInventorySlot minSlot, eInventorySlot maxSlot)
		{
			return false;
		}

		#endregion
	}
}
