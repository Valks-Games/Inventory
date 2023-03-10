namespace Inventory;

public partial class Chest : Node2D
{
	public Inventory Inventory { get; set; }
	public bool IsOpen { get; private set; }

	private AnimatedSprite2D AnimatedSprite2D { get; set; }

	public override void _Ready()
	{
		Inventory = new(this);
		AnimatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
	}

	public void Open()
	{
		if (IsOpen)
			return;

		IsOpen = true;
		Inventory.OtherInventory = Inventory;
		Inventory.Open();

		if (Player.Inventory.IsHotbar)
			Player.Inventory.SwitchToFullInventory();

		AnimatedSprite2D.Play("open");
	}

	public void Close()
	{
		IsOpen = false;

		// Clean up item animations
		foreach (var slot in Inventory.OtherInventory.InventorySlots)
			slot.ResetCleanUpAnimations();

		foreach (var slot in Player.Inventory.InventorySlots)
			slot.ResetCleanUpAnimations();

		Inventory.OtherInventory = null;
		Inventory.Close();

		if (!Player.Inventory.IsHotbar)
			Player.Inventory.SwitchToHotbarAnimated();

		AnimatedSprite2D.Play("close");
		ItemPanelDescription.Clear();
	}

	private void _on_area_2d_body_entered(Node2D body)
	{
		if (body is not Player)
			return;

		Inventory.ActiveChest = this;
	}

	private void _on_area_2d_body_exited(Node2D body)
	{
		if (body is not Player)
			return;

		Inventory.ActiveChest = null;
		
		if (IsOpen)
			Close();
	}
}
