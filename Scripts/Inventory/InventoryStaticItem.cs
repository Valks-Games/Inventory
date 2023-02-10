﻿namespace Inventory;

public class InventoryStaticItem : InventoryItem
{
	public override bool Visible { get => Sprite2D.Visible; set => Sprite2D.Visible = value; }
	private Sprite2D Sprite2D { get; set; }
	private ItemStatic ItemStatic { get; set; }
	private Inventory Inv { get; set; }

	public InventoryStaticItem(Inventory inv, Node parent, ItemStatic itemStatic, Item item)
	{
		Inv = inv;
		ItemStatic = itemStatic;
		Item = item;
		Sprite2D = new Sprite2D
		{
			Texture = itemStatic.Texture,
			Position = Vector2.One * (inv.SlotSize / 2),
			Scale = Vector2.One * (inv.SlotSize / 25)
		};

		parent.AddChild(Sprite2D);
	}

	public override Node2D GenerateGraphic()
	{
		var sprite = new Sprite2D
		{
			Texture = ItemStatic.Texture,
			Position = Vector2.One * (Inv.SlotSize / 2),
			Scale = Vector2.One * (Inv.SlotSize / 25)
		};

		return sprite;
	}

	public override void QueueFreeGraphic() => Sprite2D.QueueFree();
	public override void Hide() => Sprite2D.Hide();
	public override void Show() => Sprite2D.Show();
}
