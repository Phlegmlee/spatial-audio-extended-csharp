#if TOOLS
using Godot;
namespace SpatialAudioCS;

[Tool]
public partial class Plugin : EditorPlugin
{
	private Button AddAcousticButton = null;

	EditorInterface editorInterface = null;

	public override void _EnterTree()
	{
		editorInterface = EditorInterface.Singleton;
		editorInterface.GetSelection().SelectionChanged += OnSelectionChanged;
	}

	public override void _ExitTree()
	{
		// TODO: RemoveButton()
		// TODO: Disconnect from selection signal.
	}

	private void OnSelectionChanged()
	{
		Godot.Collections.Array<Node> selections = editorInterface.GetSelection().GetSelectedNodes();
		
		if (selections.Count == 1 && selections[0] is CollisionObject3D || selections[0] is CsgShape3D)
		{
			Node3D selected = selections[0] as Node3D;
			bool alreadyHas = AcousticBody.IsAcousticBodyOnNode(selected);
			if (!alreadyHas)
			{
				AddButton(selected);
				return;
			}
			RemoveButton();
		}
	}

	private void AddButton(Node3D target)
	{
		
	}

	private void RemoveButton()
	{
		
	}

	// TODO: private void OnAddAcousticButtonPressed(Node3D target)
}
#endif
