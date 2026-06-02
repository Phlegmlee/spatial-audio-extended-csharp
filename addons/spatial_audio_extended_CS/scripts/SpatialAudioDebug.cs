#if DEBUG
using System.Collections.Generic;
using Godot;
namespace SpatialAudioCS;

[Tool, Icon("uid://c0vs0tbtijiaf")]
public partial class SpatialAudioDebug : Node3D
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
	[Signal] public delegate void SpatialAudioDebugInfoEventHandler(Godot.Collections.Dictionary<string, Variant> info);

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
	private bool _debugRaysExpanded = false;
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
		SharedDebugContainer();

		_debugPanel = new()
		{
			Name = $"DebugPanel_{_parentAudioPlayer.Name}",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};

		StyleBoxFlat panel = new()
		{
			BgColor = new Color(0.0f, 0.0f, 0.0f, 0.65f),
			CornerRadiusTopLeft = 6,
			CornerRadiusTopRight = 6,
			CornerRadiusBottomLeft = 6,
			CornerRadiusBottomRight = 6,
			ContentMarginLeft = 10.0f,
			ContentMarginRight = 10.0f,
			ContentMarginTop = 6.0f,
			ContentMarginBottom = 6.0f,
		};

		_debugPanel.AddThemeStyleboxOverride(nameof(panel), panel);

		VBoxContainer outerVbox = new() { Name = nameof(outerVbox) };

		HBoxContainer header = new() { Name = nameof(header) };

		_debugMinimizeButton = NewButton("▼", nameof(_debugMinimizeButton), new Vector2(20, 20));
		_debugMinimizeButton.Pressed += OnDebugMinimizeToggled;
		header.AddChild(_debugMinimizeButton);

		_debugHeaderLabel = NewRtLabel(nameof(_debugHeaderLabel));
		header.AddChild(_debugHeaderLabel);

		outerVbox.AddChild(header);

		_debugContentVbox = new() { Name = nameof(_debugContentVbox) };

		_debugOverlayLabel = NewRtLabel(nameof(_debugOverlayLabel), new Vector2(420, 0));

		_debugRaysToggle = NewButton("▶ Rays", nameof(_debugRaysToggle), alignment: HorizontalAlignment.Left);
		_debugRaysToggle.Pressed += OnRaysTogglePressed;
		_debugContentVbox.AddChild(_debugRaysToggle);

		_debugRaysScroll = new()
		{
			Name = nameof(_debugRaysScroll),
			CustomMinimumSize = new Vector2(420, 0),
			SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
			Visible = false
		};

		_debugRaysLabel = NewRtLabel(nameof(_debugRaysLabel), new Vector2(420, 0), Control.MouseFilterEnum.Pass);
		_debugRaysLabel.MetaClicked += OnRayMetaClicked;

		_debugRaysScroll.AddChild(_debugRaysLabel);
		_debugContentVbox.AddChild(_debugRaysScroll);

		_debugNavigationToggle = NewButton("▶ Navigation", nameof(_debugNavigationToggle), alignment: HorizontalAlignment.Left, visible: false);
		_debugNavigationToggle.Pressed += OnNavigationTogglePressed;
		_debugContentVbox.AddChild(_debugNavigationToggle);

		_debugNavigationScroll = new()
		{
			Name = nameof(_debugNavigationScroll),
			CustomMinimumSize = new Vector2(420, 0),
			SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
			Visible = false
		};

		_debugNavigationLabel = NewRtLabel(nameof(_debugNavigationLabel), new Vector2(420, 0));

		_debugNavigationScroll.AddChild(_debugNavigationLabel);
		_debugContentVbox.AddChild(_debugNavigationScroll);

		outerVbox.AddChild(_debugContentVbox);
		_debugPanel.AddChild(outerVbox);
		_debugSharedVbox.AddChild(_debugPanel);

		_debugConnectorLine = new()
		{
			Name = $"Connector_{_parentAudioPlayer.Name}",
			Width = 1.5f,
			DefaultColor = Colors.White,
			ZIndex = -1,
		};

		if (Engine.IsEditorHint()) GetViewport().AddChild(_debugConnectorLine);

		_debugSharedLayer.AddChild(_debugConnectorLine);

		RefreshNavigationDebugVisibility();
	}

	private void RefreshNavigationDebugVisibility()
	{
		if (_debugNavigationToggle == null || _debugNavigationScroll == null) return;

		bool visible = _externalNavigationDebugActive;
		_debugNavigationToggle.Visible = visible;
		_debugNavigationScroll.Visible = visible && _debugNavigationExpanded;
		if (_debugNavigationToggle.Visible && _debugNavigationExpanded)
		{
			_debugNavigationToggle.Text = "▼ Navigation";
		}
		else
		{
			_debugNavigationToggle.Text = "▶ Navigation";
		}
	}

	private string FormatNavigationDebugText()
	{
		if (!_externalNavigationDebugActive) return "";

		Dictionary<string, Variant> d = _externalNavigationDebug;

		d.TryGetValue("profile", out Variant profile);
		d.TryGetValue("agent_name", out Variant nodeName);
		d.TryGetValue("path_points", out Variant pathPoints);
		d.TryGetValue("graph_points", out Variant graphPoints);
		d.TryGetValue("graph_edges", out Variant graphEdges);

		return "";
	}

	private void PrintDebug(Node3D listener)
	{
		if (!IsInstanceValid(_debugPanel)) SetupDebugOverlay();

		_debugPanel.Visible = true;

		Godot.Collections.Dictionary<string, Variant> info = new()
		{
			// {"Distance", listener.GlobalPosition.DistanceTo(_parentAudioPlayer.GlobalPosition)},
			// {"VolumeDbTarget", _parentAudioPlayer._targetVolumeDb},
			// {"LowpassCutoff", _parentAudioPlayer._targetLowpassCutoff},
			// {"ReverbRoomSize", _parentAudioPlayer._targetReverbRoomSize},
			// {"ReverbWetness", _parentAudioPlayer._targetReverbWetness},
			// {"ReverbDamping", _parentAudioPlayer._targetReverbDamping},
			// {"WallCount", _parentAudioPlayer._lastWallCount},
			// {"NavigationProxyActive", _externalNavigationDebugActive},
		};

		EmitSignal(SignalName.SpatialAudioDebugInfo, info);

		// TODO: Finish this
	}

	#endregion

	#region Debug Drawing

	// TODO: Debug Drawing

	private void UpdateDebugConnectorLine()
	{
		if (_debugConnectorLine == null || _debugPanel == null) return;

		if (!_debugPanel.Visible)
		{
			_debugConnectorLine.Visible = false;
			return;
		}

		if (Engine.IsEditorHint())
		{
			_debugConnectorLine.Visible = false;
			return;
		}

		Camera3D camera = GetViewport().GetCamera3D();
		if (camera == null || camera.IsPositionBehind(_parentAudioPlayer.GlobalPosition))
		{
			_debugConnectorLine.Visible = false;
			return;
		}

		Vector2 screenPos = camera.UnprojectPosition(_parentAudioPlayer.GlobalPosition);
		Rect2 panelRect = _debugPanel.GetGlobalRect();
		Vector2 panelAnchor = new
		(
			panelRect.Position.X + panelRect.Size.X,
			panelRect.Position.Y + panelRect.Size.Y * 0.5f
		);

		_debugConnectorLine.ClearPoints();
		_debugConnectorLine.AddPoint(panelAnchor);
		_debugConnectorLine.AddPoint(screenPos);
		_debugConnectorLine.Visible = true;
	}

	internal void SetupDebugMesh()
	{
		_debugImmediate = new();

		StandardMaterial3D mat = new()
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			VertexColorUseAsAlbedo = true,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha
		};

		_debugInstance = new()
		{
			Name = "DebugMesh",
			Mesh = _debugImmediate,
			MaterialOverride = mat,
		};

		AddChild(_debugInstance);
	}

	private void DrawDebugShapes(bool editorSelected = false)
	{
		if (_debugImmediate == null) SetupDebugMesh();

		_debugImmediate.ClearSurfaces();
		_debugImmediate.SurfaceBegin(Mesh.PrimitiveType.Lines);

		if (_debugDrawRays || editorSelected) DrawDebugRays();

		bool showRadius = editorSelected || (DebugDrawRadius && EffectsEnabled);
		if (showRadius && _parentAudioPlayer.EnableVolumeAttenuation)
		{
			if (_parentAudioPlayer.InnerRadius > 0.0f)
				DrawWireframeSphere(Vector3.Zero, _parentAudioPlayer.InnerRadius, Colors.LightBlue);

			float outer = _parentAudioPlayer.InnerRadius + _parentAudioPlayer.FalloffDistance;
			DrawWireframeSphere(Vector3.Zero, outer, Colors.Orange);
		}

		if (DebugDrawPlayingState || editorSelected)
		{
			Color stateColor;
			if (_parentAudioPlayer.pendingDelayTimer != null) stateColor = Colors.Yellow;
			else if (_parentAudioPlayer.Playing) stateColor = Colors.Green;
			else stateColor = Colors.Red;

			Node3D listener = _parentAudioPlayer.GetListener();
			float indicatorRadius = 0.15f;
			if (listener != null)
			{
				float dist = _parentAudioPlayer.GlobalPosition.DistanceTo(listener.GlobalPosition);
				indicatorRadius = Mathf.Max(0.15f, dist * 0.02f);
			}
			DrawWireframeSphere(Vector3.Zero, indicatorRadius, stateColor, 16);
		}
		_debugImmediate.SurfaceEnd();
	}

	private void DrawDebugRays()
	{
		List<RayCast3D> raycasts = _parentAudioPlayer.raycasts;
		List<List<Vector3>> reflectionPaths = _parentAudioPlayer.reflectionPaths;
		List<bool> reflectionEscaped = _parentAudioPlayer.reflectionEscaped;
		RayCast3D targetRaycast = _parentAudioPlayer.targetRaycast;
		float MaxRaycastDistance = _parentAudioPlayer.MaxRaycastDistance;

		for (int i = 0; i < raycasts.Count; i++)
		{
			RayCast3D ray = raycasts[i];
			if (ray == null || ray == _parentAudioPlayer.targetRaycast) continue;


			if (i < reflectionPaths.Count && reflectionPaths[i].Count >= 2)
			{
				List<Vector3> path = reflectionPaths[i];
				bool escaped = i < reflectionEscaped.Count && reflectionEscaped[i];
				for (int seg = 0; seg < path.Count; i++)
				{
					Vector3 start = ToLocal(path[seg]);
					Vector3 end = ToLocal(path[seg + 1]);
					Color color;
					if (escaped && seg == path.Count - 2) color = Colors.Red;
					else if (seg == 0) color = Colors.Blue;
					else color = Colors.Purple;

					_debugImmediate.SurfaceSetColor(color);
					_debugImmediate.SurfaceAddVertex(start);
					_debugImmediate.SurfaceSetColor(color);
					_debugImmediate.SurfaceAddVertex(end);
				}
			}
			else
			{
				Vector3 worldDir = (ray.GlobalBasis * ray.TargetPosition).Normalized();
				float drawLength;
				List<float> distances = _parentAudioPlayer.distances;
				if (i < distances.Count && distances[i] > 0.0f) drawLength = Mathf.Min(distances[i], MaxRaycastDistance);
				else drawLength = MaxRaycastDistance;

				Vector3 start = ToLocal(ray.GlobalPosition);
				Vector3 end = ToLocal(ray.GlobalPosition + worldDir * drawLength);
				bool escaped = i < reflectionEscaped.Count && reflectionEscaped[i];
				Color color;
				if (escaped) color = Colors.Red; else color = Colors.Blue;

				_debugImmediate.SurfaceSetColor(color);
				_debugImmediate.SurfaceAddVertex(start);
				_debugImmediate.SurfaceSetColor(color);
				_debugImmediate.SurfaceAddVertex(end);
			}
		}

		Node3D listener = _parentAudioPlayer.GetListener();
		if (targetRaycast != null && listener != null)
		{
			Vector3 toListener = listener.GlobalPosition - _parentAudioPlayer.GlobalPosition;
			float distToListener = toListener.Length();
			float tMax;
			if (_parentAudioPlayer.EnableVolumeAttenuation) 
				tMax = _parentAudioPlayer.InnerRadius + _parentAudioPlayer.FalloffDistance;
			else tMax = MaxRaycastDistance;

			float tLen = Mathf.Min(distToListener, tMax);
			Vector3 tDir;
			if (distToListener > 0.0f) tDir = toListener.Normalized(); else tDir = Vector3.Forward;
			Vector3 tStart = ToLocal(_parentAudioPlayer.GlobalPosition);
			Vector3 tEnd = ToLocal(_parentAudioPlayer.GlobalPosition + tDir * tLen);
			
			Color tc = Colors.Green;
			if (targetRaycast.IsColliding())
			{
				float dToWall = _parentAudioPlayer.GlobalPosition.DistanceTo(targetRaycast.GetCollisionPoint());
				if (dToWall < tLen)
				{
					tc = Colors.Red;
					tEnd = ToLocal(_parentAudioPlayer.GlobalPosition + tDir * dToWall);
				}
			}
			_debugImmediate.SurfaceSetColor(tc);
			_debugImmediate.SurfaceAddVertex(tStart);
			_debugImmediate.SurfaceSetColor(tc);
			_debugImmediate.SurfaceAddVertex(tEnd);
		}
	}

	private void DrawWireframeSphere(Vector3 center, float radius, Color color, int segments = 64)
	{
		for (int plane = 0; plane < 3; plane++)
		{
			for (int i = 0; i < segments; i++)
			{
				float a0 = i / segments * Mathf.Tau;
				float a1 = (i + 1) / segments * Mathf.Tau;
				Vector3 p0;
				Vector3 p1;
				switch (plane)
				{
					// XZ (Horizontal)
					case 0:
						p0 = center + new Vector3(Mathf.Cos(a0) * radius, 0.0f, Mathf.Sin(a0) * radius);
						p1 = center + new Vector3(Mathf.Cos(a1) * radius, 0.0f, Mathf.Sin(a1) * radius);
						break;

					// XY (Front)
					case 1:
						p0 = center + new Vector3(Mathf.Cos(a0) * radius, Mathf.Sin(a0) * radius, 0.0f);
						p1 = center + new Vector3(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius, 0.0f);
						break;

					// YZ (Side)
					case 2:
						p0 = center + new Vector3(0.0f, Mathf.Cos(a0) * radius, Mathf.Sin(a0) * radius);
						p1 = center + new Vector3(0.0f, Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius);
						break;

					default:
						p0 = new();
						p1 = new();
						break;
				}
				_debugImmediate.SurfaceSetColor(color);
				_debugImmediate.SurfaceAddVertex(p0);
				_debugImmediate.SurfaceSetColor(color);
				_debugImmediate.SurfaceAddVertex(p1);
			}
		}
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
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};

		return result;
	}

	private VBoxContainer NewDefaultVBox(string name = "SourceList")
	{
		VBoxContainer result = new()
		{
			Name = name,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};

		return result;
	}

	private RichTextLabel NewRtLabel
	(
		string name = "Label",
		Vector2 customSize = new(),
		Control.MouseFilterEnum mouseFilter = Control.MouseFilterEnum.Ignore
	)
	{
		RichTextLabel result = new()
		{
			Name = name,
			BbcodeEnabled = true,
			FitContent = true,
			CustomMinimumSize = customSize,
			ScrollActive = false,
			MouseFilter = mouseFilter,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		result.AddThemeFontSizeOverride("normal_font_size", 13);
		result.AddThemeFontSizeOverride("bold_font_size", 14);
		result.AddThemeColorOverride("default_color", Colors.White);

		return result;
	}

	private Button NewButton
	(
		string text,
		string name = "Button",
		Vector2 customMinSize = new(),
		HorizontalAlignment alignment = HorizontalAlignment.Center, bool visible = true
	)
	{
		Button result = new()
		{
			Name = name,
			Text = text,
			CustomMinimumSize = customMinSize,
			Alignment = alignment,
			Flat = true,
			Visible = visible
		};
		result.AddThemeColorOverride("front_color", Colors.LightGray);
		result.AddThemeColorOverride("front_hover_color", Colors.White);
		result.AddThemeFontSizeOverride("font_size", 13);

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
		_debugMinimized = !_debugMinimized;
		if (_debugContentVbox != null)
		{
			_debugContentVbox.Visible = !_debugMinimized;
			EmitSignal(SignalName.DebugOverlayToggled, _debugContentVbox.Visible);
		}
		if (_debugMinimizeButton != null && _debugMinimized)
		{
			_debugMinimizeButton.Text = "▶";
		}
		else
		{
			_debugMinimizeButton.Text = "▼";
		}
	}

	private void OnRaysTogglePressed()
	{
		_debugRaysExpanded = !_debugRaysExpanded;
		_debugRaysScroll.Visible = _debugRaysExpanded;
	}

	private void OnNavigationTogglePressed()
	{
		_debugNavigationExpanded = !_debugNavigationExpanded;
		RefreshNavigationDebugVisibility();
	}

	private void OnRayMetaClicked(Variant meta)
	{
		string key = meta.ToString();
		if (key.StartsWith("ray_"))
		{
			int index = key.Substr(0, 4).ToInt();
			_debugRayReflectiosExpanded ??= [];
			_debugRayReflectiosExpanded[index] = !_debugRayReflectiosExpanded[index];
		}
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