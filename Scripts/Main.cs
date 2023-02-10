global using Godot;
global using System;
global using System.Linq;
global using System.Collections.Generic;

namespace Inventory;

public partial class Main : Node
{
	public static void AddToCanvasLayer(Node node) => CanvasLayer.AddChild(node);
	private static Node CanvasLayer { get; set; }

	public override void _Ready()
	{
		CanvasLayer = GetNode<Node>("CanvasLayer");

		// Setup chest inventory
		var chest = GetNode<Chest>("Chest");

		var inv = chest.Inventory;

		for (int i = 0; i < 9; i++)
			inv.SetItem(i, new Item(Items.Coin));

		inv.SetItem(0, 2, new Item(Items.Coin, 3));
		inv.SetItem(1, 2, new Item(Items.CoinSnowy));
	}
}
