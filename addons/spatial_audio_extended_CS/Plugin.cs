#if TOOLS
using Godot;
namespace SpatialAudioCS;

[Tool]
public partial class Plugin : EditorPlugin
{
	private Button AcousticButton = null;

	EditorInterface editorInterface = null;

	public override void _EnterTree()
	{
		editorInterface = EditorInterface.Singleton;
		editorInterface.GetSelection().SelectionChanged += OnSelectionChanged;
	}

	public override void _ExitTree()
	{
		RemoveButton();
		editorInterface.GetSelection().SelectionChanged -= OnSelectionChanged;
	}

	private void OnSelectionChanged()
	{
		Godot.Collections.Array<Node> selections = editorInterface.GetSelection().GetSelectedNodes();

		if (selections.Count < 1) return;

		if (selections.Count == 1 && selections[0] is CollisionObject3D || selections[0] is CsgShape3D)
		{
			Node3D selected = selections[0] as Node3D;
			bool alreadyHas = AcousticBody.IsAcousticBodyOnNode(selected);
			if (!alreadyHas)
			{
				AddButton(selected);
				return;
			}
		}
		RemoveButton();
	}

	private void AddButton(Node3D target)
	{
		if (AcousticButton != null) return;

		Texture2D srcT = GD.Load<Texture2D>("uid://ces4kkaimtua8");
		Image img = srcT.GetImage();
		img.Resize(16, 16, Image.Interpolation.Lanczos);

		AcousticButton = new()
		{
			Text = nameof(AcousticBody),
			Flat = true,
			TooltipText = "Add an AcousticBody child to this node.",
			Icon = ImageTexture.CreateFromImage(img),
		};
		AcousticButton.AddThemeFontSizeOverride("font_size", 15);

		StyleBoxEmpty emptyStyle = new();
		AcousticButton.AddThemeStyleboxOverride("normal", emptyStyle);
		AcousticButton.AddThemeStyleboxOverride("hover", emptyStyle);
		AcousticButton.AddThemeStyleboxOverride("pressed", emptyStyle);
		AcousticButton.AddThemeStyleboxOverride("focus", emptyStyle);
		AcousticButton.AddThemeConstantOverride("h_separation", 4);

		AcousticButton.Pressed += () => OnAcousticButtonPressed(target);

		AddControlToContainer(CustomControlContainer.SpatialEditorMenu, AcousticButton);
	}

	private void RemoveButton()
	{
		if (AcousticButton == null) return;
		RemoveControlFromContainer(CustomControlContainer.SpatialEditorMenu, AcousticButton);
		AcousticButton.QueueFree();
		AcousticButton = null;
	}

	private void OnAcousticButtonPressed(Node3D target)
	{
		if (target == null || !IsInstanceValid(target))
		{
			RemoveButton();
			return;
		}

		if (AcousticBody.IsAcousticBodyOnNode(target))
		{
			RemoveButton();
			return;
		}

		EditorUndoRedoManager undoRedo = GetUndoRedo();
		AcousticBody aB = new() { Name = nameof(AcousticBody) };

		undoRedo.CreateAction($"Add {nameof(AcousticBody)}");
		undoRedo.AddDoMethod(target, Node.MethodName.AddChild, aB, true);
		undoRedo.AddDoMethod(aB, Node.MethodName.SetOwner, editorInterface.GetEditedSceneRoot());
		undoRedo.AddDoReference(aB);
		undoRedo.AddUndoMethod(target, Node.MethodName.RemoveChild, aB);
		undoRedo.CommitAction();

		editorInterface.GetSelection().Clear();
		editorInterface.GetSelection().AddNode(aB);

		RemoveButton();
	}
}
#endif
