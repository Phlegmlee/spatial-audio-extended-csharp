using Godot;
namespace SpatialAudioCS;

/// <summary>
/// TODO: Documentation
/// </summary>
[Tool, Icon("addons/spatial_audio_extended_CS/assets/acoustic_body.svg")]
public partial class AcousticBody : Node
{
	#region Exports

	private AcousticMaterial _acousticMaterial;
	/// <summary>
	/// The acoustic material that describes how this 
	/// surface absorbs, scatters, and transmits sound energy.
	/// </summary>
	[Export]
	public AcousticMaterial AcousticMaterial
	{
		get => _acousticMaterial;
		set
		{
			_acousticMaterial = value;
#if TOOLS
			UpdateConfigurationWarnings();
#endif
		}
	}

	#endregion

	#region Gameplay Logic

	internal static AcousticBody FindAcousticBodyOnNode(Node node)
	{
		if (node == null) return null;

		foreach (Node child in node.GetChildren())
		{
			if (child is AcousticBody) return child as AcousticBody;
		}

		return null;
	}

	internal static AcousticBody FindAcousticBodyForRaycastCollider(Node collider)
	{
		if (collider == null) return null;

		// TODO: Direct lookup CollisionObject3D

		// TODO: Handle CSG shape internal static body 3D

		return null;
	}

	#endregion

#if TOOLS
	#region Editor Configuration

	// public override void _Ready()
	// {
	// 	if (Engine.IsEditorHint())
	// 	{
	// 		// TODO
	// 	}

	// 	base._Ready();
	// }

	// TODO: config warnigns
	// public override string[] _GetConfigurationWarnings()
	// {
	// 	return base._GetConfigurationWarnings();
	// }

	#endregion

	#region Event Handlers

	private void OnSiblingsChanged()
	{
		UpdateConfigurationWarnings();
	}

	#endregion
#endif
}
