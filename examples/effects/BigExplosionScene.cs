using Godot;
using SpatialAudioCS;

public partial class BigExplosionScene : Node3D
{
	[Export] public Area3D bombButton;
	[Export] public Node3D gui;

	[Export] public GpuParticles3D explosion;
	[Export] public SpatialAudioPlayer3D esfx;

	private bool interactable = false;

	public override void _Ready()
	{
		bombButton.BodyEntered += OnButtonAreaInteraction;
		bombButton.BodyExited += OnButtonAreaInteraction;
	}

	public override void _Process(double delta)
	{
		if (!interactable) return;

		if (Input.IsActionJustPressed("interact"))
		{
			explosion.Emitting = true;
			esfx.PlayWithDelay();
		}
	}

	private void OnButtonAreaInteraction(Node body)
	{
		gui.Visible = !gui.Visible;
		interactable = !interactable;
	}
}
