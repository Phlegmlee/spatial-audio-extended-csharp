using Godot;
namespace SpatialAudioCS;

/// <summary>
/// TODO: Documentation
/// </summary>
[Tool, Icon("uid://borvk2otixktj"), GlobalClass]
public partial class SpatialReflectionNavigationAgent3D : Node3D
{
	#region Signals

	/// <summary>
	/// Emitted whenever a path is found or refreshed.
	/// </summary>
	/// <param name="worldPath"></param>
	/// <param name="directPath"></param>
	[Signal] public delegate void PathUpdatedEventHandler(Vector3[] worldPath, bool directPath);

	/// <summary>
	/// Emitted when pathing fails while direct line is blocked.
	/// </summary>
	/// <param name="worldOrigin"></param>
	/// <param name="worldTarget"></param>
	[Signal] public delegate void PathFailedEventHandler(Vector3 worldOrigin, Vector3 worldTarget);

	/// <summary>
	/// Emitted each frame after proxy position is updated.
	/// </summary>
	/// <param name="worldProxy"></param>
	[Signal] public delegate void AudioProxyPosUpdatedEventHandler(Vector3 worldProxy);

	/// <summary>
	/// Emitted after graph rebuild completes.
	/// </summary>
	/// <param name="pointCount"></param>
	[Signal] public delegate void GraphRebuiltEventHandler(int pointCount);

	#endregion

	#region Exports

	// TODO: Exports

	#endregion

	#region Internal

	// TODO: Internal

	#endregion

	#region Editor Configuration

	// TODO: Editor Configuration

	#endregion

	#region Gameplay Logic

	// TODO: Gameplay Logic

	#endregion

	#region Utils

	// TODO: Utils

	#endregion

	#region Debug

	// TODO: Debug

	#endregion
}
