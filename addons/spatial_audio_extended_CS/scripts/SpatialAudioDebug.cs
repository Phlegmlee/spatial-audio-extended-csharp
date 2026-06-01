#if DEBUG
using System.Collections.Generic;
using Godot;
namespace SpatialAudioCS;

[Tool, Icon("uid://c0vs0tbtijiaf")]
public partial class SpatialAudioDebug : Control
{
	#region Signals

	/// <summary>
	/// Emitted when the debug overlay visibility is toggled.
	/// </summary>
	/// <param name="visible"></param>
	[Signal] public delegate void DebugOverlayToggledEventHandler(bool visible);

	/// <summary>
	/// Emits a compact diagnostics dictionary when the debug overlay is shown.
	/// <para>Keys: <c>Distance</c>, <c>VolumeDbTarget</c>, <c>LowpassCutoff</c>, <c>ReverbRoomSize</c>, <c>WallCount</c></para>
	/// </summary>
	/// <param name="info">Keys: <c>Distance</c>, <c>VolumeDbTarget</c>, <c>LowpassCutoff</c>, <c>ReverbRoomSize</c>, <c>WallCount</c></param>
	[Signal] public delegate void SpatialAudioDebugInfoEventHandler(Godot.Collections.Dictionary info);

	#endregion

	#region Exports

	private bool _debugDrawRays = true;
	/// <summary>
	/// Draws coloured lines for every raycast at runtime (in-game).
	/// <para>Blue = omni room-sensing rays</para>
	/// Green = target ray (clear line of sight)
	/// <para>Red = target ray (occluded).</para>
	/// <b>Note:</b> Rays are always shown in the editor when the node is selected.
	/// </summary>
	[Export]
	public bool DebugDrawRays
	{
		get => _debugDrawRays;
		set => _debugDrawRays = value;
	}

	private bool _debugDrawRadius = true;
	/// <summary>
	/// Draws wireframe spheres for the inner radius (cyan) and outer boundary
	/// (orange) at runtime (in-game) when volume attenuation is enabled.
	/// <para><b>Note:</b> Radius shapes are always shown in the editor when the node is selected.</para>
	/// </summary>
	[Export]
	public bool DebugDrawRadius
	{
		get => _debugDrawRadius;
		set => _debugDrawRadius = value;
	}

	private bool _debugDrawPlayingState = true;
	/// <summary>
	/// Draws a small wireframe sphere at the emitter origin that indicates
	/// playback state:
	/// <para>green = playing</para>
	/// red = stopped
	/// <para><b>Note:</b> Always shown in the editor when the node is selected.</para>
	/// </summary>
	[Export]
	public bool DebugDrawPlayingState
	{
		get => _debugDrawPlayingState;
		set => _debugDrawPlayingState = value;
	}

	private bool _displayDebugInfo = true;
	/// <summary>
	/// Displays key spatial-audio diagnostics as an on-screen overlay every
	/// update cycle while within the radius of the source.
	/// <para>Keys: <c>Distance</c>, <c>VolumeDbTarget</c>, <c>LowpassCutoff</c>, <c>ReverbRoomSize</c>, <c>WallCount</c></para>
	/// </summary>
	[Export]
	public bool DisplayDebugInfo
	{
		get => _displayDebugInfo;
		set
		{
			_displayDebugInfo = value;
#if TOOLS
			NotifyPropertyListChanged();
#endif
		}
	}

	private Key _debugToggleEffectsKey = Key.F1;
	/// <summary>
	/// While the debug overlay is visible, press this key to toggle all
	/// spatial-audio effects on/off so you can A/B compare the difference.
	/// </summary>
	[Export]
	public Key DebugToggleEffectsKey
	{
		get => _debugToggleEffectsKey;
		set => _debugToggleEffectsKey = value;
	}

	private Key _debugToggleShapesKey = Key.F2;
	/// <summary>
	/// While the debug overlay is visible, press this key to toggle debug
	/// shape drawing (rays, spheres) on/off at runtime.
	/// </summary>
	[Export]
	public Key DebugToggleShapesKey
	{
		get => _debugToggleShapesKey;
		set => _debugToggleShapesKey = value;
	}

	private bool _effectsEnabled = true;
	[Export]
	public bool EffectsEnabled
	{
		get => _effectsEnabled;
		set
		{
			_effectsEnabled = value;
		}
	}

	#endregion

	#region Internal

	private SpatialAudioPlayer3D _parentAudioPlayer = null;

	private ImmediateMesh _debugImmediate = null;
	private MeshInstance3D _debugInstance = null;

	private PanelContainer _debugPanel = null;
	private bool _debugMinimized = false;
	private Button _debugMinimizeButton = null;
	private RichTextLabel _debugHeaderLabel = null;
	private VBoxContainer _debugContentVbox = null;
	private RichTextLabel _debugOverlayLabel = null;
	private RichTextLabel _debugRaysLabel = null;
	private ScrollContainer _debugRaysScroll = null;
	private Button _debugRaysToggle = null;
	private bool _raysExpanded = false;
	private RichTextLabel _debugNavigationLabel = null;
	private ScrollContainer _debugNavigationScroll = null;
	private Button _debugNavigationToggle = null;
	private bool _debugNavigationExpanded = false;
	private Line2D _debugConnectorLine = null;
	private float _debugOcclAbsWeight = 0.0f;

	private Dictionary<int, bool> _debugRayReflectiosExpanded = [];
	private bool _externalNavigationDebugActive = false;
	private Dictionary<string, Variant> _externalNavigationDebug = [];

	private CanvasLayer _debugSharedLayer = null;
	private ScrollContainer _debugSharedScroll = null;
	private VBoxContainer _debugSharedVbox = null;

	internal void SetExternalNavigationDebugData(bool active, Dictionary<string, Variant> info)
	{
		_externalNavigationDebugActive = active;
		if (active)
		{
			_externalNavigationDebug = new Dictionary<string, Variant>(info);
		}
		else
		{
			_externalNavigationDebug = [];
		}
		RefreshNavigationDebugVisibility();
	}

	internal void ClearExternalNavigationDebugData()
	{
		_externalNavigationDebugActive = false;
		_externalNavigationDebug.Clear();
		RefreshNavigationDebugVisibility();
	}

	#endregion

	#region Runtime

	/// <summary>
	/// See: <see cref="EditorReady"/> for editor tool.
	/// <para>Anything below the deferred call is in game logic.</para>
	/// </summary>
	public override void _Ready()
	{
		if (Engine.IsEditorHint())
		{
			CallDeferred(MethodName.EditorReady);
			return;
		}
	}

	/// <summary>
	/// Look for unhandled input for the debug pannel interactions.
	/// </summary>
	/// <param name="event"></param>
	public override void _UnhandledInput(InputEvent @event)
	{
		if (!DisplayDebugInfo || Engine.IsEditorHint()) return;

		if (_debugPanel == null || !_debugPanel.Visible) return;

		if (@event is InputEventKey eventKey && @event.IsPressed() && !@event.IsEcho())
		{
			if (eventKey.Keycode == DebugToggleEffectsKey)
			{
				EffectsEnabled = !EffectsEnabled;
				GetViewport().SetInputAsHandled();
			}
			else if (eventKey.Keycode == DebugToggleShapesKey)
			{
				DebugDrawRays = !DebugDrawRays;
				DebugDrawRadius = !DebugDrawRadius;
				GetViewport().SetInputAsHandled();
			}
		}
	}

	/// <summary>
	/// Disconnect from signal and remove per-instance debug nodes from the shared container.
	/// </summary>
	public override void _ExitTree()
	{
		_parentAudioPlayer.EnableDebugToggled -= OnEnableDebugToggled;

		if (IsInstanceValid(_debugPanel))
		{
			_debugPanel.GetParent()?.RemoveChild(_debugPanel);

			_debugPanel.QueueFree();
		}

		_debugPanel = null;

		if (IsInstanceValid(_debugConnectorLine))
		{
			_debugConnectorLine.GetParent()?.RemoveChild(_debugConnectorLine);

			_debugConnectorLine.QueueFree();
		}

		_debugConnectorLine = null;
	}

	#region Debug Overlay

	// TODO: Debug Overlay

	private void SharedDebugContainer()
	{
		if (Engine.IsEditorHint())
		{
			if (_debugSharedScroll != null && IsInstanceValid(_debugSharedScroll)) return;

			_debugSharedScroll = NewDefaultScroll();
			_debugSharedVbox = NewDefaultVBox();
			_debugSharedVbox.AddThemeConstantOverride("separation", 6);

			_debugSharedScroll.AddChild(_debugSharedVbox);
			GetViewport().AddChild(_debugSharedScroll);
		}
		else
		{
			if (_debugSharedLayer != null && IsInstanceValid(_debugSharedLayer)) return;

			_debugSharedLayer = NewDefaultCanvas();
			_debugSharedScroll = NewDefaultScroll();
			_debugSharedVbox = NewDefaultVBox();

			_debugSharedScroll.AddChild(_debugSharedVbox);
			_debugSharedLayer.AddChild(_debugSharedScroll);
			GetTree().Root.CallDeferred(Node.MethodName.AddChild, _debugSharedLayer);
		}
	}

	private void SetupDebugOverlay()
	{

	}

	private void RefreshNavigationDebugVisibility()
	{

	}

	private string FormatNavigationDebugText()
	{
		return "";
	}

	private void PrintDebug(Node3D listener)
	{

	}

	#endregion

	#region Debug Drawing

	// TODO: Debug Drawing

	private void UpdateDebugConnectorLine()
	{

	}

	internal void SetupDebugMesh()
	{

	}

	private void DrawDebugShapes(bool editorSelected = false)
	{

	}

	private void DrawDebugRays()
	{

	}

	private void DrawWireframeSphere(Vector3 center, float radius, Color color, int segments = 64)
	{

	}

	#endregion

	#endregion

	#region Utils

	private ScrollContainer NewDefaultScroll(string name = "DebugScroll")
	{
		ScrollContainer result = new()
		{
			Name = name,
			AnchorLeft = 0.0f,
			AnchorTop = 0.0f,
			AnchorRight = 0.0f,
			AnchorBottom = 1.0f,
			OffsetLeft = 8.0f,
			OffsetTop = 8.0f,
			OffsetRight = 460.0f,
			OffsetBottom = -8.0f,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			MouseFilter = MouseFilterEnum.Ignore,
		};

		return result;
	}

	private VBoxContainer NewDefaultVBox(string name = "SourceList")
	{
		VBoxContainer result = new()
		{
			Name = name,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		return result;
	}

	private CanvasLayer NewDefaultCanvas(string name = "SpatialAudioDebugOverlay")
	{
		CanvasLayer result = new()
		{
			Name = name,
			Layer = 100,
		};

		return result;
	}

	#endregion

	#region Event Handlers

	private void OnEnableDebugToggled(bool value)
	{
		if (value) return;

		if (!IsInsideTree()) return;

		QueueFree();
	}

	private void OnDebugMinimizeToggled()
	{

	}

	private void OnRaysTogglePressed()
	{

	}

	private void OnNavigationTogglePressed()
	{

	}

	private void OnRayMetaClicked(Variant meta)
	{

	}

	#endregion

	#region Editor Config

	/// <summary>
	/// This is <see cref="_Ready"/> but for the editor only.
	/// </summary>
	private void EditorReady()
	{
		_parentAudioPlayer = GetParent() as SpatialAudioPlayer3D;
		_parentAudioPlayer.EnableDebugToggled += OnEnableDebugToggled;

		if (DebugDrawRays || DebugDrawRadius || DebugDrawPlayingState) SetupDebugMesh();
	}

	public override void _ValidateProperty(Godot.Collections.Dictionary property)
	{
		List<StringName> debugProperties =
		[
			PropertyName.DebugToggleEffectsKey, PropertyName.DebugToggleShapesKey,
		];
		if (!DisplayDebugInfo)
		{
			foreach (StringName propName in debugProperties)
			{
				if ((StringName)property["name"] == propName)
				{
					property["usage"] = (int)~PropertyUsageFlags.Editor;
				}
			}
		}
	}

	#endregion
}
#endif