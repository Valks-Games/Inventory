﻿using static System.Net.Mime.MediaTypeNames;

namespace Inventory;

public class InventorySlot
{
	public Label ItemCountLabel { get; set; }
	private Label DebugLabel { get; set; }

	private Panel Panel { get; set; }
	public InventoryItem InventoryItem { get; set; }
	public Inventory Inventory { get; set; }
	private bool JustPickedUpItem { get; set; }

	public InventorySlot(Inventory inv, Node parent)
	{
		Inventory = inv;

		Panel = new Panel
		{
			CustomMinimumSize = Vector2.One * Inventory.SlotSize
		};

		Panel.GuiInput += (inputEvent) =>
		{
			if (inputEvent is not InputEventMouseButton eventMouseButton)
				return;

			if (eventMouseButton.IsLeftClickPressed())
				HandleLeftClick();

			if (eventMouseButton.IsRightClickPressed())
				HandleRightClick();
		};

		Panel.MouseEntered += () =>
		{
			Inventory.ActiveInventorySlot = this;

			var cursorItem = ItemCursor.GetItem();

			ContinuousLeftClickPickup(cursorItem);
			ContinuousRightClickPlace(cursorItem);
			ContinuousShiftClickTransfer();

			if (InventoryItem != null)
				ItemPanelDescription.Display(InventoryItem.Item);
		};

		Panel.MouseExited += () =>
		{
			Inventory.ActiveInventorySlot = null;

			if (InventoryItem == null)
				return;

			ItemPanelDescription.Clear();
		};

		parent.AddChild(Panel);

		ItemCountLabel = UtilsLabel.CreateItemCountLabel();
		Panel.AddChild(ItemCountLabel);

		DebugLabel = new Label
		{
			Text = "",
			ZIndex = 100,
			CustomMinimumSize = Vector2.One * Inventory.SlotSize,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
		};

		DebugLabel.AddThemeColorOverride("font_shadow_color", Colors.Black);

		Panel.AddChild(DebugLabel);
	}

	private void ContinuousLeftClickPickup(Item cursorItem)
	{
		// Pickup items of the same type
		if (!InputGame.HoldingLeftClick || cursorItem == null || InventoryItem == null)
			return;

		if (cursorItem.Type != InventoryItem.Item.Type)
			return;

		var item = InventoryItem.Item;
		item.Count += cursorItem.Count;

		ItemCursor.SetItem(item);

		RemoveItem();
	}

	private void ContinuousShiftClickTransfer()
	{
		if (!InputGame.HoldingLeftClick || !Input.IsKeyPressed(Key.Shift))
			return;

		TransferItem();
	}

	private void ContinuousRightClickPlace(Item cursorItem)
	{
		if (!InputGame.HoldingRightClick || cursorItem == null)
			return;

		if (InventoryItem == null)
		{
			// Take 1 item from the cursor
			ItemCursor.TakeItem();

			// Pass 1 item from cursor to the inventory slot
			var item = cursorItem.Clone();
			item.Count = 1;
			SetItem(item);
		}
		else
		{
			if (cursorItem.Type != InventoryItem.Item.Type)
				return;

			// Take 1 item from the cursor
			ItemCursor.TakeItem();

			// Pass 1 item from cursor to the inventory slot
			var item = cursorItem.Clone();
			item.Count = 1 + InventoryItem.Item.Count;
			SetItem(item);
		}
	}

	public void SetDebugLabel(string text)
	{
		DebugLabel.Text = text;
	}

	public void SetItem(Item item)
	{
		UpdateItemCountLabel(item.Count);
		InventoryItem?.QueueFreeGraphic();
		InventoryItem = item.Type.ToInventoryItem(Inventory, Panel, item);
	}

	public void RemoveItem()
	{
		// Clear the item graphic for this inventory slot
		InventoryItem.QueueFreeGraphic();
		InventoryItem = null;

		// Clear the item count graphic
		UpdateItemCountLabel(0);
	}

	public Inventory GetOtherInventory() => this.Inventory == Player.Inventory ?
		Inventory.OtherInventory : Player.Inventory;

	private void UpdateItemCountLabel(int count)
	{
		if (count > 1)
			ItemCountLabel.Text = count + "";
		else
			ItemCountLabel.Text = "";
	}

	private void CollectAndMergeAllItemsFrom(Item itemToMergeTo)
	{
		var otherItemCounts = 0;

		// Scan the inventory for items of the same type and combine them to the cursor
		foreach (var slot in Inventory.InventorySlots)
		{
			// Skip the slot we double clicked on
			if (slot == this)
				continue;

			var invItem = slot.InventoryItem;

			// A item exists in this inv slot
			if (invItem != null)
			{
				// The inv slot item is the same type as the cursor item type
				if (invItem.Item.Type == itemToMergeTo.Type)
				{
					otherItemCounts += invItem.Item.Count;

					slot.RemoveItem();
				}
			}
		}

		var counts = itemToMergeTo.Count + otherItemCounts;
		itemToMergeTo.Count = counts;
		ItemCursor.SetItem(itemToMergeTo);
	}

	// Transfer the item in the inventory slot we are currently hovering over
	private void TransferItem()
	{
		if (InventoryItem == null)
			return;

		var targetInv = GetOtherInventory();

		if (targetInv == null)
			targetInv = Player.Inventory;

		var emptySlot = targetInv.TryGetEmptyOrSameTypeSlot(InventoryItem.Item.Type);

		if (emptySlot != -1)
		{
			// Store temporary reference to the item in this inventory slot
			var itemRef = InventoryItem.Item;

			this.RemoveItem();

			var otherInvSlot = targetInv.InventorySlots[emptySlot];

			var otherInvSlotItem = otherInvSlot.InventoryItem;

			if (otherInvSlotItem == null)
				targetInv.InventorySlots[emptySlot].SetItem(itemRef);
			else
			{
				itemRef.Count += otherInvSlotItem.Item.Count;
				targetInv.InventorySlots[emptySlot].SetItem(itemRef);
			}
		}
	}
	
	private void HandleLeftClick()
	{
		var cursorItem = ItemCursor.GetItem();

		// If were double clicking and not holding shift and
		// we are holding a item in our cursor [and the inventory
		// slot we are hovering over has no item or there is
		// a item in this inventory slot and it is of the
		// same item type as the item type in our cursor] then
		// collect all items to the cursor and return preventing
		// any other mouse inventory logic from executing.
		if (InputGame.DoubleClick && !Input.IsKeyPressed(Key.Shift))
		{
			if (cursorItem != null)
			{
				if (InventoryItem == null || InventoryItem.Item.Type == cursorItem.Type)
				{
					CollectAndMergeAllItemsFrom(cursorItem);

					// We collected all items to the cursor. Do not let anything else happen.
					return;
				}
			}
			// There is an item in the cursor and this is a double click
			else
			{
				if (InventoryItem != null)
				{
					CollectAndMergeAllItemsFrom(InventoryItem.Item);

					RemoveItem(); // prevent dupe glitch

					// We collected all items to the cursor. Do not let anything else happen.
					return;
				}
			}
		}

		// There is no item in this inventory slot
		if (InventoryItem == null)
		{
			if (!JustPickedUpItem)
			{
				// Is there a item attached to the cursor?
				if (cursorItem != null)
				{
					// Remove the item from the cursor
					ItemCursor.RemoveItem();

					// Set the item in this inventory slot to the item from the cursor
					SetItem(cursorItem);
				}
			}
		}
		// There is a item in this inventory slot
		else
		{
			// Recap
			// 1. Left click
			// 2. There is an item in the inventory slot

			JustPickedUpItem = true;
			Panel.GetTree().CreateTimer(InputGame.DoubleClickTime / 1000.0).Timeout += () => 
				JustPickedUpItem = false;

			// There is an item attached to the cursor
			if (cursorItem != null)
			{
				// The cursor and inv slot items are of the same type
				if (cursorItem.Type == InventoryItem.Item.Type)
				{
					var item = cursorItem.Clone();
					item.Count += InventoryItem.Item.Count;

					ItemCursor.RemoveItem();

					SetItem(item);
				}
				// The cursor and inv slot items are of different types
				else
				{
					// Recap:
					// 1. Left Click
					// 2. There is an item in the inventory slot
					// 3. There is a item in the cursor
					// 4. The cursor item and inv slot item are of different types
					// 
					// So lets swap the cursor item with the inv slot item

					var item = InventoryItem.Item;

					this.RemoveItem();

					ItemCursor.RemoveItem();

					// Set the item in this inventory slot to the item from the cursor
					SetItem(cursorItem);

					// Attach the item from the inventory slot to the cursor (pick up the item)
					ItemCursor.SetItem(item);
				}
			}
			// There is no item attached to the cursor
			else
			{
				// Shift + Click
				if (Input.IsKeyPressed(Key.Shift))
				{
					TransferItem();

					return;
				}

				var item = InventoryItem.Item;

				this.RemoveItem();

				// Set the item in this inventory slot to the item from the cursor
				ItemCursor.SetItem(item);
			}
		}
	}

	private void HandleRightClick()
	{
		var cursorItem = ItemCursor.GetItem();

		// Is there a item attached to the cursor?
		if (cursorItem != null)
		{
			// There is no item in this inventory slot
			if (InventoryItem == null)
			{
				// Take 1 item from the cursor
				ItemCursor.TakeItem();

				// Pass 1 item from cursor to the inventory slot
				var item = cursorItem.Clone();
				item.Count = 1;
				SetItem(item);
			}
			// There is a item in this inventory slot
			else
			{
				// Is the cursor item being held of the same type for the item in the inventory slot?
				if (cursorItem.Type == InventoryItem.Item.Type)
				{
					ItemCursor.TakeItem();

					var item = cursorItem.Clone();

					// Take 1 item from cursor and the item count from the inv slot and combine them
					item.Count = 1 + InventoryItem.Item.Count;

					// Set all these items to the inv slot
					SetItem(item);
				}
			}
		}
		// There is no item being held in the cursor
		else
		{
			// There is a item in this inventory slot
			if (InventoryItem != null)
			{
				// Shift + Right Click = Split Stack
				if (Input.IsKeyPressed(Key.Shift))
				{
					var itemA = InventoryItem.Item;

					// Prevent duping 1 item to 2 items
					if (itemA.Count == 1)
						return;

					var itemB = itemA.Clone();
					
					var half = itemA.Count / 2;
					var remainder = itemA.Count % 2;

					itemA.Count = half + remainder;
					itemB.Count = half;

					SetItem(itemA);
					ItemCursor.SetItem(itemB);

					return;
				}

				var invSlotItemCount = InventoryItem.Item.Count;

				// Is this the last item in the stack?
				if (invSlotItemCount - 1 == 0)
				{
					ItemCursor.SetItem(InventoryItem.Item);

					RemoveItem();
				}
				// There are two or more items in this inv slot
				else
				{
					// Recap:
					// 1. User did a right click
					// 2. There is no item being held in the cursor
					// 3. There are two or more items in this inv slot

					// Lets take 1 item from the inv slot and bring it to the cursor
					InventoryItem.Item.Count -= 1;

					UpdateItemCountLabel(InventoryItem.Item.Count);
					
					var item = InventoryItem.Item.Clone();
					item.Count = 1;
					ItemCursor.SetItem(item);
				}
			}
		}
	}
}