using System.Collections.Generic;
using Godot;
namespace SpatialAudioCS;

// TODO: Examples of usage in docs?

/// <summary>
/// Attach as a child to any <c>CollisionObject3D</c> to give the surface 
/// acoustic properties using an <see cref="SpatialAudioCS.AcousticMaterial"/>.
/// <para>Supported Parent Nodes:</para>
/// <list type="bullet">
/// <item><c>StaticBody3D</c> - static level geometry, walls, floors, props, etc.</item>
/// <item><c>RigidBody3D</c> - dynamic movable objects.</item>
/// <item><c>CharacterBody3D</c> - player or npc bodies</item>
/// <item><c>Area3D</c> - trigger zones / volumes that affect sound, water, fog etc.</item>
/// <item><c>CSGShape3D</c> - must have collision enabled.</item>
/// </list>
/// The AcousticBody must be a direct child of parent node's collison object.
/// <para> <b>TIP:</b> When a compatable node is selected an editor button 
/// is created in same location as the button when a mesh is selected.</para>
/// </summary>
[Tool, Icon("uid://ces4kkaimtua8"), GlobalClass]
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

	// FIXME: The lookup methods may fail or not function properly due to type saftey...

	/// <summary>
	/// Finds the <c>AcousticBody</c> child of the given node if any exist.
	/// </summary>
	/// <param name="node">The node supposedly holding the <c>AcousticBody</c>.</param>
	/// <returns>The FIRST <c>AcousticBody</c> found, or <c>null</c>.</returns>
	internal static AcousticBody FindOnNode(Node node)
	{
		if (node == null) return null;

		foreach (Node child in node.GetChildren())
		{
			if (child is AcousticBody) return child as AcousticBody;
		}

		return null;
	}

	/// <summary>
	/// Finds the <c>AcousticBody</c> for a <c>RayCast Collider</c>.
	/// <para>
	/// Checks the collider first, then walks up the tree to handle
	/// <c>CsgShape3D</c>, where the CSG has an internal <c>StaticBody3D</c>
	/// that must be "bypassed" to find the <c>AcousticBody</c>.
	/// </para>
	/// </summary>
	/// <param name="collider">The <c>RayCast</c> collision.</param>
	/// <returns>The FIRST <c>AcousticBody</c> found, or <c>null</c>.</returns>
	internal static AcousticBody FindForRayCollider(Node collider)
	{
		if (collider == null) return null;

		// Direct lookup CollisionObject3D
		AcousticBody lookupResult = FindOnNode(collider);
		if (lookupResult != null) return lookupResult;

		// Handle CSG shape internal static body 3D
		Node parent = collider.GetParent();
		if (parent != null && parent is CsgShape3D) return FindOnNode(parent);

		return null;
	}

	internal static bool IsOnNode(Node node)
	{
		return FindOnNode(node) != null;
	}

	#endregion

#if TOOLS
	#region Editor Configuration

	/// <inheritdoc />
	public override void _Ready()
	{
		if (Engine.IsEditorHint())
		{
			Node parent = GetParent();

			if (parent == null) return;

			// Unsub then resub, unsub will NOT cause 
			// failure if not already subbed, this is
			// safer and faster than using IsConnected()
			parent.ChildOrderChanged -= OnSiblingsChanged;
			parent.ChildOrderChanged += OnSiblingsChanged;
		}

		base._Ready();
	}

	/// <inheritdoc />
	public override string[] _GetConfigurationWarnings()
	{
		List<string> warnings = [];

		if (AcousticMaterial == null)
		{
			warnings.Add
			(
				"No AcousticMaterial assigned. " +
				"The SpatialAudioPlayer3D will use its " +
				"fallback transmission value for this surface."
			);
		}

		Node parent = GetParent();

		if (parent == null) return [.. warnings];

		int count = 0;
		foreach (Node child in parent.GetChildren())
		{
			if (child is AcousticBody)
			{
				count += 1;
			}

			if (count > 1) warnings.Add
			(
				$"This node has {count} AcousticBody children. " +
				"Only the first one found will be used, remove the extras."
			);
		}

		if (parent is not CollisionObject3D && parent is not CsgShape3D)
		{
			warnings.Add
			(
				"AcousticBody should be a direct" +
				"child of a CollisionObject3D" +
				"(e.g. StaticBody3D, RigidBody3D) or a CSGShape3D."
			);
		}

		if (parent is CsgShape3D p && !p.UseCollision)
		{
			warnings.Add
			(
				"The parent CSGShape3D does not have " +
				"'use_collision' enabled. Occlusion raycasts won't hit it."
			);

			Node grandParent = p.GetParent();
			if (grandParent != null && grandParent.GetClass() == "CsgCombiner3D")
			{
				warnings.Add
				(
					"AcousticBody is on a CSG shape inside " +
					"a CSGCombiner3D. This setup may not work properly " +
					"for raycasts or occlusion."
				);
			}
		}

		return [.. warnings];
	}

	#endregion

	#region Event Handlers

	private void OnSiblingsChanged()
	{
		UpdateConfigurationWarnings();
	}

	#endregion
#endif
}
