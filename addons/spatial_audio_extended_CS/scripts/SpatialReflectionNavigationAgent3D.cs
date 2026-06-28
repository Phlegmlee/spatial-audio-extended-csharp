using System;
using System.Collections.Generic;
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

	#region Exports - Navigation

	/// <summary>
	/// Options for High-level tuning preset for graph generation.
	/// </summary>
	public enum NavigationProfileEnum
	{
		/// <summary>
		/// Keep your manual values for all properties.
		/// </summary>
		Custom,
		/// <summary>
		/// Optimized for open spaces with few obstacles.
		/// </summary>
		OpenAreas,
		/// <summary>
		/// Optimized for narrow corridors and tight spaces.
		/// </summary>
		Hallways,
	}
	private NavigationProfileEnum _navigationProfile = NavigationProfileEnum.Custom;
	/// <summary>
	/// High-level tuning preset for graph generation.
	/// <para><c>Custom</c> keeps your manual values.</para>
	/// <c>OpenAreas</c> optimized for open spaces with few obstacles.
	/// <para><c>Hallways</c> optimized for narrow corridors and tight spaces.</para>
	/// </summary>
	[ExportGroup("Navigation")]
	[Export]
	public NavigationProfileEnum NavigationProfile
	{
		get => _navigationProfile;
		set
		{
			_navigationProfile = value;
#if TOOLS
			ApplyNavigationProfilePreset();
			NotifyPropertyListChanged();
#endif
		}
	}

	private float _updateInterval = 0.15f;
	/// <summary>
	/// Recompute interval (in seconds) for the path query.
	/// </summary>
	[Export(PropertyHint.Range, "0.01f, 2.0f, 0.01f, suffix:s")]
	public float UpdateInterval
	{
		get => _updateInterval;
		set => _updateInterval = value;
	}

	private bool _skipRecompute = true;
	/// <summary>
	/// Skip full path solve when orgin and listener moved less than the configured thresholds.
	/// </summary>
	[Export]
	public bool SkipRecomputeWhenStatic
	{
		get => _skipRecompute;
		set
		{
			_skipRecompute = value;
#if TOOLS
			NotifyPropertyListChanged();
#endif
		}
	}

	private float _recomputeOriginThreshold = 0.05f;
	/// <summary>
	/// Movement threshold for origin required to trigger a full recompute.
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 5.0f, 0.01f, suffix:m")]
	public float RecomputeOriginThreshold
	{
		get => _recomputeOriginThreshold;
		set => _recomputeOriginThreshold = value;
	}

	private float _recomputeTargetThreshold = 0.08f;
	/// <summary>
	/// Movement threshold for listener required to trigger a full recompute.
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 5.0f, 0.01f, suffix:m")]
	public float RecomputeTargetThreshold
	{
		get => _recomputeTargetThreshold;
		set => _recomputeTargetThreshold = value;
	}

	private float _staticRecomputeInterval = 0.75f;
	/// <summary>
	/// Forced full solve interval while static skipping is active (0 disables periodic full solves).
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 5.0f, 0.01f, suffix:s")]
	public float StaticRecomputeInterval
	{
		get => _staticRecomputeInterval;
		set => _staticRecomputeInterval = value;
	}

	private float _navigationRadius = 0.18f;
	/// <summary>
	/// Spherical bounds radius used for airborne sample points around the graph anchor.
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 5.0f, 0.01f, suffix:m")]
	public float NavigationRadius
	{
		get => _navigationRadius;
		set
		{
			_navigationRadius = value;
#if TOOLS
			MarkGraphDirty();
#endif
		}
	}

	private int _samplePointCount = 96;
	/// <summary>
	/// Number of random points generated inside the sphere.
	/// </summary>
	[Export(PropertyHint.Range, "8, 512, 1")]
	public int SamplePointCount
	{
		get => _samplePointCount;
		set
		{
			_samplePointCount = value;
			_samplesDirty = true;
#if TOOLS
			MarkGraphDirty();
#endif
		}
	}

	private ulong _sampleSeed = 1337;
	/// <summary>
	/// Deterministic random seed for stable sample layout.
	/// </summary>
	[Export]
	public ulong SampleSeed
	{
		get => _sampleSeed;
		set
		{
			_sampleSeed = value;
			_samplesDirty = true;
#if TOOLS
			MarkGraphDirty();
#endif
		}
	}

	private float _maxConnDist = 8.0f;
	/// <summary>
	/// Maximum edge length allowed between graph waypoints.
	/// </summary>
	[Export(PropertyHint.Range, "0.5f, 128.0f, 0.1f, suffix:m")]
	public float MaxConnectionDistance
	{
		get => _maxConnDist;
		set
		{
			_maxConnDist = value;
#if TOOLS
			MarkGraphDirty();
#endif
		}
	}

	private int _graphNeighborLimit = 10;
	/// <summary>
	/// Max connected neighbors per waypoint in the cached graph.
	/// </summary>
	[Export(PropertyHint.Range, "2, 32, 1")]
	public int GraphNeighborLimit
	{
		get => _graphNeighborLimit;
		set
		{
			_graphNeighborLimit = value;
#if TOOLS
			MarkGraphDirty();
#endif
		}
	}

	private int _dynamicConnLimit = 6;
	/// <summary>
	/// Max visible graph links from dynamic start/goal to graph.
	/// </summary>
	[Export(PropertyHint.Range, "1, 32, 1")]
	public int DynamicConnectionLimit
	{
		get => _dynamicConnLimit;
		set => _dynamicConnLimit = value;
	}

	private int _dynamicCanMulti = 4;
	/// <summary>
	/// umber of best dynamic-link candidates kept before visibility tests.
	/// Higher values are more robust but increase physics query cost.
	/// </summary>
	[Export(PropertyHint.Range, "1, 12, 1")]
	public int DynamicCanidateMultiplier
	{
		get => _dynamicCanMulti;
		set => _dynamicCanMulti = value;
	}

	private float _clearanceRadius = 0.35f;
	/// <summary>
	/// Collision clearance radius around sampled points and edge checkpoints.
	/// </summary>
	[Export(PropertyHint.Range, "0.01f, 5.0f, 0.01f, suffix:m")]
	public float ClearanceRadius
	{
		get => _clearanceRadius;
		set
		{
			_clearanceRadius = value;
			_clearanceShape.Radius = value;
#if TOOLS
			MarkGraphDirty();
#endif
		}
	}

	private int _edgeClearChecks = 1;
	/// <summary>
	/// Number of interior checkpoints used to validate each graph edge.
	/// </summary>
	[Export(PropertyHint.Range, "0, 8, 1")]
	public int EdgeClearanceChecks
	{
		get => _edgeClearChecks;
		set
		{
			_edgeClearChecks = value;
#if TOOLS
			MarkGraphDirty();
#endif
		}
	}

	private float _graphRecenterDist = 2.0f;
	/// <summary>
	/// Rebuild the cached graph when anchor moves more than this distance.
	/// </summary>
	[Export(PropertyHint.Range, "0.1f, 64.0f, 0.1f, suffix:m")]
	public float GraphRecenterDistance
	{
		get => _graphRecenterDist;
		set => _graphRecenterDist = value;
	}

	private bool _useReachableScan = true;
	/// <summary>
	/// Build graph using collision-reachable flood scan from origin (better for tight spaces).
	/// </summary>
	[Export]
	public bool UseReachableScan
	{
		get => _useReachableScan;
		set
		{
			_useReachableScan = value;
#if TOOLS
			MarkGraphDirty();
			NotifyPropertyListChanged();
#endif
		}
	}

	#endregion

	#region Export - Scanning

	/// <summary>
	/// Options for neighbor connectivity for reachable scan.
	/// </summary>
	public enum ScanNeighborModeEnum
	{
		/// <summary>
		/// 6-connected neighbors along cardinal axes (fastest, best for open areas).
		/// </summary>
		Axis_6,
		/// <summary>
		/// 18-connected neighbors including diagonals (moderate cost, better for mixed spaces).
		/// </summary>
		Diagonal_18,
		/// <summary>
		/// 26-connected neighbors including diagonals and vertical (highest cost, best for tight 3D spaces).
		/// </summary>
		Full_26,
	}
	private ScanNeighborModeEnum _scanNeighborMode = ScanNeighborModeEnum.Axis_6;
	/// <summary>
	/// Neighbor connectivity for reachable scan.
	/// </summary>
	[ExportSubgroup("Scanning")]
	[Export] 
	public ScanNeighborModeEnum ScanNeighborMode
	{
		get => _scanNeighborMode;
		set
		{
			_scanNeighborMode = value;
#if TOOLS
			MarkGraphDirty();
#endif
		}
	}

	private float _scanCellSize = 1.0f;
	/// <summary>
	/// Grid cell size for reachable scan.
	/// </summary>
	[Export(PropertyHint.Range, "0.2f, 8.0f, 0.1f, suffix:m")]
	public float ScanCellSize
	{
		get => _scanCellSize;
		set
		{
			_scanCellSize = value;
#if TOOLS
			MarkGraphDirty();
#endif
		}
	}

	private int _scanedCellMax = 4096;
	/// <summary>
	/// Hard cap on scanned cells for reachable graph build.
	/// </summary>
	[Export(PropertyHint.Range, "64, 32768, 1")]
	public int ScanedCellMax
	{
		get => _scanedCellMax;
		set
		{
			_scanedCellMax = value;
#if TOOLS
			MarkGraphDirty();
#endif
		}
	}

	private int _scannedMaxExtent = 0;
	/// <summary>
	/// Maximum absolute grid extent from the start cell (0 = unlimited).
	/// Useful to cap scan cost in dense or complex scenes.
	/// </summary>
	[Export(PropertyHint.Range, "0, 512, 1")]
	public int ScannedMaxExtent
	{
		get => _scannedMaxExtent;
		set
		{
			_scannedMaxExtent = value;
#if TOOLS
			MarkGraphDirty();
#endif
		}
	}

	private float _scanCellInset = 0.08f;
	/// <summary>
	/// Extra margin from voxel center toward free space to reduce wall hugging.
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 0.45f, 0.01f")]
	public float ScanCellInset
	{
		get => _scanCellInset;
		set
		{
			_scanCellInset = value;
#if TOOLS
			MarkGraphDirty();
#endif
		}
	}

	private float _heuristicWeight = 1.0f;
	[Export(PropertyHint.Range, "0.1f, 5.0f, 0.05f")]
	public float HeuristicWeight
	{
		get => _heuristicWeight;
		set => _heuristicWeight = value;
	}
	
	#endregion

	#region Exports - Distance Mode

	/// <summary>
	/// Options for distance function used for movement and heuristic.
	/// </summary>
	public enum DistanceModeEnum
	{
		/// <summary>
		/// Straight-line distance (fastest, best for general use).
		/// </summary>
		Euclidean,
		/// <summary>
		/// Axis-aligned distance (fast for grid-like layouts, less accurate in open 3D).
		/// </summary>
		Manhattan,
	}
	private DistanceModeEnum _distanceMode = DistanceModeEnum.Euclidean;
	/// <summary>
	/// Distance function for movement and heuristic.
	/// </summary>
	[ExportSubgroup("Distance Mode")]
	[Export]
	public DistanceModeEnum DistanceMode
	{
		get => _distanceMode;
		set => _distanceMode = value;
	}

	private bool _useUnitCost = false;
	/// <summary>
	/// If true, every edge has the same movement cost.
	/// </summary>
	[Export]
	public bool UseUnitCost
	{
		get => _useUnitCost;
		set
		{
			_useUnitCost = value;
#if TOOLS
			NotifyPropertyListChanged();
#endif
		}
	}

	private float _unitCost = 1.0f;
	/// <summary>
	/// Unit cost used when use_unit_cost is true.
	/// </summary>
	[Export(PropertyHint.Range, "0.01f, 50.0f, 0.01f")]
	public float UnitCost
	{
		get => _unitCost;
		set => _unitCost = value;
	}

	private bool _reuseLastPath = true;
	/// <summary>
	/// Reuse previous solved around-corner path until it becomes invalid.
	/// </summary>
	[Export]
	public bool ReuseLastPath
	{
		get => _reuseLastPath;
		set
		{
			_reuseLastPath = value;
#if TOOLS
			NotifyPropertyListChanged();
#endif
		}
	}

	private float _reuseOriginTol = 0.60f;
	/// <summary>
	/// Maximum movement of the origin before cached path reuse is rejected.
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 16.0f, 0.05f, suffix:m")]
	public float ReuseOriginTolerance
	{
		get => _reuseOriginTol;
		set => _reuseOriginTol = value;
	}

	private float _reuseTarTol = 0.90f;
	/// <summary>
	///  Maximum movement of the target before cached path reuse is rejected.
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 16.0f, 0.05f, suffix:m")]
	public float ReuseTargetTolerance
	{
		get => _reuseTarTol;
		set => _reuseTarTol = value;
	}

	private float _reuseMaxDetRatio = 2.0f;
	/// <summary>
	/// Reject cached path if too long compared to direct distance.
	/// </summary>
	[Export(PropertyHint.Range, "1.0f, 6.0f, 0.05f")]
	public float ReuseMaxDetourRatio
	{
		get => _reuseMaxDetRatio;
		set => _reuseMaxDetRatio = value;
	}

	private bool _smoothPathWithVis = true;
	/// <summary>
	/// If true, run visibility-based line-of-sight smoothing on solved paths.
	/// </summary>
	[Export]
	public bool SmoothPathWithVisiblity
	{
		get => _smoothPathWithVis;
		set => _smoothPathWithVis = value;
	}

	private uint _collisionMask = 1 << 0;
	[Export(PropertyHint.Layers3DPhysics)]
	public uint CollisionMask
	{
		get => _collisionMask;
		set => _collisionMask = value;
	}

	private bool _collideWithAreas = false;
	/// <summary>
	/// If true, include Area3D nodes as blockers.
	/// </summary>
	[Export]
	public bool CollideWithAreas
	{
		get => _collideWithAreas;
		set => _collideWithAreas = value;
	}

	private bool _collideWithBodies = true;
	/// <summary>
	/// If true, include PhysicsBody3D nodes as blockers.
	/// </summary>
	[Export]
	public bool CollideWithBodies
	{
		get => _collideWithBodies;
		set => _collideWithBodies = value;
	}

	private bool _ignoreListenerBody = true;
	/// <summary>
	/// Ignore the active camera's collision parent/body in path queries.
	/// </summary>
	[Export]
	public bool IgnoreListenerBody
	{
		get => _ignoreListenerBody;
		set => _ignoreListenerBody = value;
	}

	private Godot.Collections.Array<Node3D> _excludedCollisionNodes;
	[Export]
	public Godot.Collections.Array<Node3D> ExcludedCollisionNodes
	{
		get => _excludedCollisionNodes;
		set
		{
			_excludedCollisionNodes = value;
			_exclusionsDirty = true;
		}
	}

	#endregion

	#region Exports - Origin

	/// <summary>
	/// Options for where the path starts from.
	/// </summary>
	public enum OriginModeEnum
	{
		/// <summary>
		/// Use the world position of the resolved node as the origin.
		/// </summary>
		NodePosition,
		/// <summary>
		/// Apply a local-space offset to the resolved node position.
		/// </summary>
		NodeWithLocalOffset,
		/// <summary>
		/// Always use the same world position, unaffected by parent movement.
		/// </summary>
		FixedWorldPosition,
	}
	private OriginModeEnum _originMode = OriginModeEnum.NodeWithLocalOffset;
	/// <summary>
	/// Where the path starts from.
	/// </summary>
	[ExportGroup("Origin")]
	[Export]
	public OriginModeEnum OriginMode
	{
		get => _originMode;
		set => _originMode = value;
	}

	// FIXME: These don't have backers, do they need backers?

	/// <summary>
	/// Origin node override when using NODE_* origin modes.
	/// </summary>
	[Export]
	public Node3D OriginOverride { get; set; } = null;

	/// <summary>
	/// Local-space offset applied after resolving origin node.
	/// </summary>
	[Export]
	public Vector3 OriginLocalOffset { get; set; } = Vector3.Zero;

	/// <summary>
	/// Fixed world start position (unaffected by parent movement).
	/// </summary>
	[Export]
	public Vector3 FixedWorldOrigin { get; set; } = Vector3.Zero;

	/// <summary>
	/// Capture the current resolved node origin into fixed_world_origin on ready.
	/// </summary>
	[Export]
	public bool CaptureFixedOriginOnReady { get; set; } = false;

	#endregion

	#region Exports - Target

	/// <summary>
	/// Manual target override. If null, active camera is used.
	/// </summary>
	[ExportGroup("Target")]
	[Export]
	public Node3D TargetOverride { get; set; } = null;

	#endregion

	#region Exports - Proxy

	private bool _moveAudioPlayer = false;
	/// <summary>
	/// If enabled, this agent moves an audio player proxy along the computed path.
	/// </summary>
	[ExportGroup("Audio Proxy")]
	[Export]
	public bool MoveAudioPlayer
	{
		get => _moveAudioPlayer;
		set
		{
			_moveAudioPlayer = value;
#if TOOLS
			NotifyPropertyListChanged();
#endif
		}
	}

	private SpatialAudioPlayer3D _audioPlayerNode = null;
	/// <summary>
	/// Explicit audio player node to move.
	/// </summary>
	[Export]
	public SpatialAudioPlayer3D AudioPlayerNode
	{
		get => _audioPlayerNode;
		set
		{
			_audioPlayerNode = value;
#if TOOLS
			RefreshProxyRadius();
			UpdateConfigurationWarnings();
#endif
		}
	}

	private bool _autoFindAudioPlayerChild = true;
	/// <summary>
	/// If true and audio_player_node is empty, auto-find first AudioStreamPlayer3D child.
	/// </summary>
	[ExportGroup("Audio Proxy")]
	[Export]
	public bool AutoFindAudioPlayerChild
	{
		get => _autoFindAudioPlayerChild;
		set
		{
			_autoFindAudioPlayerChild = value;
#if TOOLS
			RefreshProxyRadius();
			UpdateConfigurationWarnings();
			NotifyPropertyListChanged();
#endif
		}
	}

	/// <summary>
	/// If true, only move to a waypoint when the direct line is blocked.
	/// </summary>
	[Export]
	public bool ProxyOnlyWhenBlocked { get; set; } = true;

	/// <summary>
	/// Path index used for the proxy (1 = first waypoint after origin).
	/// </summary>
	[Export(PropertyHint.Range, "1, 64, 1")]
	public int ProxyWaypointIndex { get; set; } = 2;

	/// <summary>
	/// Smooths proxy movement (0 = snap instantly).
	/// </summary>
	[Export(PropertyHint.Range, "1.0f, 60.0f, 0.1f")]
	public float AudioProxyLerpSpeed { get; set; } = 30.0f;

	/// <summary>
	/// Lerp speed while backing away from a too-close listener.
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 40.0f, 0.1f")]
	public float AudioProxyBackoffLerpSpeed { get; set; } = 4.0f;

	/// <summary>
	/// Vertical offset added to the proxy target position.
	/// </summary>
	[Export(PropertyHint.Range, "-5.0f, 5.0f, 0.01f, suffix:m")]
	public float AudioProxyHeightOffset { get; set; } = 0.0f;

	/// <summary>
	/// Return the moved audio node to origin when proxy movement is disabled.
	/// </summary>
	[Export]
	public bool RestoreAudioToOriginWhenDisabled { get; set; } = true;

	/// <summary>
	/// While reflected, temporarily force inner_radius to 0 when proxy is outside source inner zone.
	/// </summary>
	[Export]
	public bool ProxyForceInnerRadiusOutsideSourceZone { get; set; } = true;

	private bool _enableProxyListenerBackoff = true;
	/// <summary>
	/// Keep reflected proxy from getting too close to listener by backing up on the path.
	/// </summary>
	[Export]
	public bool EnableProxyListenerBackoff
	{
		get => _enableProxyListenerBackoff;
		set
		{
			_enableProxyListenerBackoff = value;
#if TOOLS
			NotifyPropertyListChanged();
#endif
		}
	}

	/// <summary>
	/// Enter backoff when listener is nearer than this to the reflected proxy.
	/// </summary>
	[Export(PropertyHint.Range, "0.1f, 20.0f, 0.05f, suffix:m")]
	public float ProxyMinListenerDistance { get; set; } = 1.6f;

	/// <summary>
	/// Exit backoff only when listener distance exceeds this threshold.
	/// </summary>
	[Export(PropertyHint.Range, "0.1f, 20.0f, 0.05f, suffix:m")]
	public float ProxyBackoffReleaseDistance { get; set; } = 2.2f;

	/// <summary>
	/// Distance from listener along the solved path where proxy retreats to.
	/// </summary>
	[Export(PropertyHint.Range, "0.1f, 50.0f, 0.05f, suffix:m")]
	public float ProxyBackoffPathDistance { get; set; } = 2.8f;

	private bool _proxySpringArmEnabled = true;
	/// <summary>
	/// Keep reflected proxy from getting too close to listener by backing up on the path.
	/// </summary>
	[Export]
	public bool ProxySpringArmEnabled
	{
		get => _proxySpringArmEnabled;
		set
		{
			_proxySpringArmEnabled = value;
#if TOOLS
			NotifyPropertyListChanged();
#endif
		}
	}

	/// <summary>
	/// Minimum allowed listener distance for the reflected proxy.
	/// </summary>
	[Export(PropertyHint.Range, "0.1f, 20.0f, 0.05f, suffix:m")]
	public float ProxySpringMinDistance { get; set; } = 1.5f;

	/// <summary>
	/// Extra retreat amount when listener breaches the minimum distance.
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 4.0f, 0.05f")]
	public float ProxySpringPushStrength { get; set; } = 1.0f;

	/// <summary>
	/// Spring response speed while pushing away from listener.
	/// </summary>
	[Export(PropertyHint.Range, "0.1f, 40.0f, 0.1f")]
	public float ProxySpringPushSpeed { get; set; } = 10.0f;

	/// <summary>
	/// Spring response speed while relaxing back toward the base proxy target.
	/// </summary>
	[Export(PropertyHint.Range, "0.1f, 40.0f, 0.1f")]
	public float ProxySpringReturnSpeed { get; set; } = 6.0f;

	#endregion

	#region Exports - Reflection

	private bool _applyRefectionVolumeLoss = true;
	/// <summary>
	/// Apply additional reflection loudness loss based on proxy distance from source origin.
	/// </summary>
	[ExportGroup("Reflection Audio")]
	[Export]
	public bool ApplyRefectionVolumeLoss
	{
		get => _applyRefectionVolumeLoss;
		set
		{
			_applyRefectionVolumeLoss = true;
#if TOOLS
			NotifyPropertyListChanged();
#endif
		}
	}

	/// <summary>
	/// Added loss per meter from origin to proxy.
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 6.0f, 0.01f, suffix:dB/m")]
	public float ReflectionLossDbPerMeter { get; set; } = 0.45f;

	/// <summary>
	/// Non-linear exponent for reflection loss distance.
	/// </summary>
	[Export(PropertyHint.Range, "0.2f, 3.0f, 0.05f")]
	public float ReflectionLossPower { get; set; } = 1.0f;

	/// <summary>
	/// Max additional loss from reflection routing.
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 80.0f, 0.1f, suffix:dB")]
	public float ReflectionMaxLossDb { get; set; } = 20.0f;

	private bool _proxyOcclusionTransitionSmoothing = true;
	/// <summary>
	/// Hold occlusion open briefly while proxy transitions to reflected position.
	/// </summary>
	[Export]
	public bool ProxyOcclusionTransitionSmoothing
	{
		get => _proxyOcclusionTransitionSmoothing;
		set
		{
			_proxyOcclusionTransitionSmoothing = true;
#if TOOLS
			NotifyPropertyListChanged();
#endif
		}
	}

	/// <summary>
	/// Hold duration used when returning from reflected proxy back to direct mode.
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 1.0f, 0.01f, suffix:s")]
	public float ProxyOcclusionHoldSeconds { get; set; } = 0.18f;

	/// <summary>
	/// Extend hold while returning movement is still larger than this threshold.
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 10.0f, 0.01f, suffix:m")]
	public float ProxyOcclusionHoldMoveThreshold { get; set; } = 0.25f;

	#endregion

	#region Exports - Debug

	private bool _previewPathingInEditor = false;
	/// <summary>
	/// When false, full runtime behavior is disabled in editor.
	/// Selected-node preview for bounds/path still runs for inspection.
	/// </summary>
	[ExportGroup("Debug")]
	[Export]
	public bool PreviewPathingInEditor
	{
		get => _previewPathingInEditor;
		set
		{
#if TOOLS
			bool wasEnabled = PreviewPathingInEditor;
#endif
			_previewPathingInEditor = value;
#if TOOLS
			if (wasEnabled && !value) ResetAudioProxyToOrigin();
			NotifyPropertyListChanged();
#endif
		}
	}

	/// <summary>
	/// Draws the navigation sphere centered at the resolved origin.
	/// </summary>
	[Export]
	public bool DebugDrawBounds { get; set; } = false;

	private bool _debugDrawPath = false;
	/// <summary>
	/// Draws the currently solved path polyline.
	/// </summary>
	[Export]
	public bool DebugDrawPath
	{
		get => _debugDrawPath;
		set
		{
			_debugDrawPath = value;
#if TOOLS
			NotifyPropertyListChanged();
#endif
		}
	}

	[Export]
	public bool DebugDrawDirectLineWhenBlocked { get; set; } = false;

	[Export]
	public bool DebugDrawGraphPoints { get; set; } = false;

	[Export]
	public bool DebugDrawGraphEdges { get; set; } = false;

	private bool _debugDrawAudioProxy = false;
	/// <summary>
	/// 
	/// </summary>
	[Export]
	public bool DebugDrawAudioProxy
	{
		get => _debugDrawAudioProxy;
		set
		{
			_debugDrawAudioProxy = value;
#if TOOLS
			NotifyPropertyListChanged();
#endif
		}
	}

	[Export(PropertyHint.Range, "0.02f, 2.0f, 0.01f, suffix:m")]
	public float DebugAudioProxyRadius { get; set; } = 0.20f;

	[Export]
	public Color DebugBoundsColor { get; set; } = new(0.12f, 0.65f, 1.0f, 0.65f);

	[Export]
	public Color DebugPathColor { get; set; } = new(0.2f, 1.0f, 0.25f, 0.95f);

	[Export]
	public Color DebugBlockedColor { get; set; } = new(1.0f, 0.25f, 0.2f, 0.95f);

	[Export]
	public Color DebugGraphColor { get; set; } = new(1.0f, 0.9f, 0.2f, 0.50f);

	[Export]
	public Color DebugAudioProxyColor { get; set; } = new(1.0f, 0.25f, 0.9f, 0.95f);

	#endregion

	#endregion

	#region Internal

	private double _timeAccum = 0.0;
	private double _timeSinceRecompute = 0.0;
	private bool _samplesDirty = true;
	private bool _graphDirty = true;
	private List<Vector3> _sampleOffsets = [];
	private SphereShape3D _clearanceShape = new();

	private Vector3 _graphAnchor = Vector3.Zero;
	private Vector3 _gridOrigin = Vector3.Zero;
	private List<Vector3> _graphPoints = [];
	private List<List<int>> _graphEdges = [];
	private int _graphEdgeCount = 0;
	private List<Vector3> _cachedWaypoints = [];
	private Vector3 _cachedOrigin = Vector3.Zero;
	private Vector3 _cachedTarget = Vector3.Zero;
	private float _cachedPathLen = 0.0f;
	private float _currentPathLen = 0.0f;

	private Vector3[] _currentPath = [];
	private bool _hasValidPath = false;
	private bool _isDirectPath = false;
	private Vector3 _lastWorldOrigin = Vector3.Zero;
	private Vector3 _lastWorldTarget = Vector3.Zero;
	private Vector3 _lastSolveOrigin = Vector3.Zero;
	private Vector3 _lastSolveTarget = Vector3.Zero;
	private bool _hasLastSolve = false;

	//SpatialAudioPlayer3D _audioProxyRef = null;
	Vector3 _currentProxy = Vector3.Zero;
	Vector3 _targetProxy = Vector3.Zero;
	bool _proxyReady = false;
	bool _proxyInBackoff = false;
	float _lastRefVolOffsetDb = float.PositiveInfinity;
	float _springArmDistFromEnd = 0.0f;
	bool _springArmReady = false;
	bool _wasRefProxyActive = false;
	bool _proxyHasInner = false;
	float _proxyInnerRad = -1.0f;
	bool _forceProxyInnerRad = false;

	private PhysicsRayQueryParameters3D _rayQuery;
	private PhysicsShapeQueryParameters3D _shapeQuery;

	private Dictionary<string, bool> _segVisCache = [];
	private Dictionary<Vector3I, bool> _pFreeCache = [];
	private bool _exclusionsDirty = true;
	Godot.Collections.Array<Rid> _cachedBaseExclusions = [];

	private readonly List<StringName> _NAV_PROFILE_CUSTOM_ONLY_PROPS = [
		PropertyName.SkipRecomputeWhenStatic,
		PropertyName.RecomputeOriginThreshold,
		PropertyName.RecomputeTargetThreshold,
		PropertyName.StaticRecomputeInterval,
		PropertyName.NavigationRadius,
		PropertyName.SamplePointCount,
		PropertyName.SampleSeed,
		PropertyName.MaxConnectionDistance,
		PropertyName.GraphNeighborLimit,
		PropertyName.DynamicConnectionLimit,
		PropertyName.DynamicCanidateMultiplier,
		PropertyName.ClearanceRadius,
		PropertyName.EdgeClearanceChecks,
		PropertyName.GraphRecenterDistance,
		PropertyName.UseReachableScan,
		PropertyName.ScanCellSize,
		PropertyName.ScanNeighborMode,
		PropertyName.ScanedCellMax,
		PropertyName.ScannedMaxExtent,
		PropertyName.ScanCellInset,
		PropertyName.HeuristicWeight,
		PropertyName.DistanceMode,
		PropertyName.UseUnitCost,
		PropertyName.UnitCost,
		PropertyName.ReuseLastPath,
		PropertyName.ReuseOriginTolerance,
		PropertyName.ReuseTargetTolerance,
		PropertyName.ReuseMaxDetourRatio,
		PropertyName.SmoothPathWithVisiblity,
		PropertyName.CollisionMask,
		PropertyName.CollideWithAreas,
		PropertyName.CollideWithBodies,
		PropertyName.IgnoreListenerBody,
		PropertyName.ExcludedCollisionNodes,
	];

	#endregion

	#region Gameplay Logic

	/// <inheritdoc />
	public override void _Ready()
	{
		if (Engine.IsEditorHint()) EditorReady();

		_clearanceShape.Radius = ClearanceRadius;
		_shapeQuery = new()
		{
			CollisionMask = CollisionMask,
			CollideWithAreas = CollideWithAreas,
			CollideWithBodies = CollideWithBodies,
		};
		_rayQuery = new()
		{
			CollisionMask = CollisionMask,
			CollideWithAreas = CollideWithAreas,
			CollideWithBodies = CollideWithBodies,
			HitFromInside = true,
		};

		if (CaptureFixedOriginOnReady) FixedWorldOrigin = ResolveNodeOrigin();

		ApplyNavigationProfilePreset();
		CheckRebuildSamples();
		RefreshProxyRadius();
		RecomputePath();
	}

	/// <inheritdoc />
	public override void _Process(double delta)
	{
		bool editorPreview = Engine.IsEditorHint() && !PreviewPathingInEditor;
		bool editorSelected = false;
		if (editorPreview) editorSelected = IsEditorSelected();

		if (editorPreview)
		{
			if (editorSelected && _timeAccum < UpdateInterval)
			{ _timeSinceRecompute += delta; _timeAccum += delta; }
			else { _timeAccum = 0.0; RecomputePath(); }

			DrawDebug(editorSelected);

			return;
		}

		_timeSinceRecompute += delta;
		_timeAccum += delta;
		if (_timeAccum >= UpdateInterval)
		{ _timeAccum = 0.0; RecomputePath(); }
		UpdateAudio(delta);
		DrawDebug(false);
	}

	#endregion

	#region Utils - Path

	private void RecomputePath()
	{
		World3D world = GetWorld3D();
		if (world == null) return;

		Node3D target = GetTargetNode();
		if (target == null)
		{ SetFailedPath(Vector3.Zero, Vector3.Zero, false); return; }

		Vector3 worldOrigin = ResolveWorldOrigin();
		Vector3 worldTarget = target.GlobalPosition;
		_lastWorldOrigin = worldOrigin;
		_lastWorldTarget = worldTarget;

		if (SkipRecomputeWhenStatic && _hasLastSolve && !_graphDirty)
		{
			float originDelta = worldOrigin.DistanceTo(_lastSolveOrigin);
			float targetDelta = worldTarget.DistanceTo(_lastSolveTarget);

			if
			(
				originDelta < RecomputeOriginThreshold && targetDelta < RecomputeTargetThreshold
				&&
				(StaticRecomputeInterval <= 0.0f || _timeSinceRecompute < StaticRecomputeInterval)
			) return;
		}

		PhysicsDirectSpaceState3D space = world.DirectSpaceState;
		Godot.Collections.Array<Rid> exclusions = BuildExclusions(target);
		_segVisCache.Clear();
		_pFreeCache.Clear();

		if (GetFirstHit(space, worldOrigin, worldTarget, exclusions).Count == 0)
		{
			SetPath([worldOrigin, worldTarget], true);
			CommitSolveState(worldOrigin, worldTarget);
			return;
		}

		if (ReuseLastPath)
		{
			Vector3[] reused = TryReuseCachedPath(space, worldOrigin, worldTarget, exclusions);
			if (reused.Length >= 2)
			{
				SetPath(reused, false);
				CommitSolveState(worldOrigin, worldTarget);
				return;
			}
		}

		CheckRebuildGraph(space, worldOrigin, exclusions);
		if (_graphPoints.Count == 0)
		{
			SetFailedPath(worldOrigin, worldTarget, true);
			CommitSolveState(worldOrigin, worldTarget);
			return;
		}

		float[] startLinks = FindDynamicLinks(space, worldOrigin, worldTarget, exclusions);
		float[] goalLinks = FindDynamicLinks(space, worldTarget, worldOrigin, exclusions);
		if (startLinks.Length == 0 || goalLinks.Length == 0)
		{
			SetFailedPath(worldOrigin, worldTarget, true);
			CommitSolveState(worldOrigin, worldTarget);
			return;
		}

		Vector3[] graphPath = FindPathGreedyAStar(worldOrigin, worldTarget, startLinks, goalLinks);
		if (SmoothPathWithVisiblity && graphPath.Length > 2) graphPath = SmoothPath(space, graphPath, exclusions);

		if (graphPath.Length >= 2) SetPath([.. graphPath], false);
		else SetFailedPath(worldOrigin, worldTarget, true);

		CommitSolveState(worldOrigin, worldTarget);
	}

	private Godot.Collections.Dictionary GetFirstHit
	(
		PhysicsDirectSpaceState3D space,
		Vector3 from,
		Vector3 to,
		Godot.Collections.Array<Rid> exclusions
	)
	{
		_rayQuery.From = from;
		_rayQuery.To = to;
		_rayQuery.Exclude = exclusions;
		_rayQuery.CollisionMask = CollisionMask;
		_rayQuery.CollideWithAreas = CollideWithAreas;
		_rayQuery.CollideWithBodies = CollideWithBodies;
		_rayQuery.HitFromInside = true;
		return space.IntersectRay(_rayQuery);
	}

	private Vector3[] FindPathGreedyAStar(Vector3 worldOrigin, Vector3 worldTarget, float[] startLinks, float[] goalLinks)
	{
		Dictionary<int, bool> goalSet = [];
		foreach (int idx in goalLinks) goalSet[idx] = true;

		List<List<float>> frontier = [];
		HeapPush(frontier, [0.0f, 0]); // [fScore, nodeId]

		Dictionary<float, float> travelCost = new() { { 0, 0.0f }, };
		Dictionary<float, float> breadcrumb = new() { { 0, -1 }, };

		while (!(frontier.Count == 0))
		{
			List<float> best = HeapPop(frontier);
			float bestF = best[0];
			float bestId = best[1];

			if (!travelCost.TryGetValue(bestId, out float currentG))
			{
				currentG = float.PositiveInfinity;
				travelCost.Add(bestId, currentG);
				continue;
			}

			float expectedF = currentG + EstimateCost(IdToPoint((int)bestId, worldOrigin, worldTarget), worldTarget);
			if (bestF > expectedF + 0.0001f) continue;

			if (bestId == 1) break;

			int[] neighbors = NeighborsOf((int)bestId, startLinks, goalSet);

			foreach (int n in neighbors)
			{
				float moveCost = ComputeMoveCost(
					IdToPoint((int)bestId, worldOrigin, worldTarget),
					IdToPoint(n, worldOrigin, worldTarget)
				);
				float nextG = currentG + moveCost;
				if (travelCost.TryGetValue(n, out float nTCost))
				{
					nTCost = float.PositiveInfinity;
					travelCost.Add(n, nTCost);
				}
				if (nextG < nTCost)
				{
					travelCost[n] = nextG;
					breadcrumb[n] = bestId;
					float f = nextG + EstimateCost(IdToPoint(n, worldOrigin, worldTarget), worldTarget);
					HeapPush(frontier, [f, n]);
				}
			}
		}

		if (!travelCost.ContainsKey(1)) return [];

		List<int> idPath = [1];
		while (idPath[^1] != 0)
		{
			idPath.Add((int)breadcrumb[idPath[^1]]);
		}
		idPath.Reverse();

		List<Vector3> worldPath = [];
		foreach (int id in idPath)
		{
			worldPath.Add(IdToPoint(id, worldOrigin, worldTarget));
		}

		return [.. worldPath];
	}

	private void SetPath(Vector3[] path, bool direct)
	{
		_currentPath = path;
		_hasValidPath = path.Length >= 2;
		_isDirectPath = direct && _hasValidPath;
		_currentPathLen = GetPathLength(path);
		if (_hasValidPath)
		{
			_cachedOrigin = path[0];
			_cachedTarget = path[^1];
			_cachedPathLen = _currentPathLen;
			_cachedWaypoints = [];
			if (!_isDirectPath && path.Length > 2)
			{
				for (int i = 0; i < path.Length - 1; i++) _cachedWaypoints.Add(path[i]);
			}
		}
		EmitSignal(SignalName.PathUpdated, _currentPath, _isDirectPath);
	}

	private void SetFailedPath(Vector3 origin, Vector3 target, bool blocked)
	{
		_currentPath = [];
		_hasValidPath = false;
		_isDirectPath = false;
		_currentPathLen = 0.0f;
		if (blocked) EmitSignal(SignalName.PathFailed, origin, target);
	}

	private void HeapPush(List<List<float>> heap, List<float> item)
	{
		heap.Add(item);
		int i = heap.Count - 1;
		do
		{
			int p = (i - 1) >> 1;
			if (heap[p][0] <= heap[i][0]) break;
			(heap[i], heap[p]) = (heap[p], heap[i]);
			i = p;
		}
		while (i > 0);
	}

	private List<float> HeapPop(List<List<float>> heap)
	{
		List<float> top = heap[0];
		List<float> last = heap[^1];
		heap.RemoveAt(heap.Count - 1);

		if (!(heap.Count == 0))
		{
			heap[0] = last;
			byte exit = 0;
			int i = 0;
			do
			{
				int l = i * 2 + 1;
				int r = l + 1;
				if (l >= heap.Count) exit++;
				int m = l;
				if (r < heap.Count && heap[r][0] < heap[l][0]) m = r;
				if (heap[i][0] <= heap[m][0]) exit++;
				(heap[m], heap[i]) = (heap[i], heap[m]);
				i = m;
			}
			while (exit == 0);
		}
		return top;
	}

	private float EstimateCost(Vector3 from, Vector3 to)
	{
		return HeuristicWeight * Distance(from, to);
	}
	
	private float Distance(Vector3 from, Vector3 to)
	{
		switch (DistanceMode)
		{
			case DistanceModeEnum.Manhattan:
				Vector3 distVect = (from - to).Abs();
				return distVect.X + distVect.Y + distVect.Z;

			default:
				return from.DistanceTo(to);
		}
	}
	
	private int[] NeighborsOf(int id, float[] startLinks, Dictionary<int, bool> goalSet)
	{
		List<int> result = [];

		if (id == 1) return [];
		if (id == 0)
		{
			foreach (int link in startLinks)
			{
				result.Add(link + 2);
			}
			return [.. result];
		}

		int graphIdx = id - 2;
		foreach (int edge in _graphEdges[graphIdx])
		{
			result.Add(edge + 2);
		}

		if (goalSet.ContainsKey(graphIdx)) result.Add(1);

		return [.. result];
	}

	private float ComputeMoveCost(Vector3 from, Vector3 to)
	{
		if (UseUnitCost) return UnitCost;
		return Distance(from, to);
	}

	private Vector3 IdToPoint(int id, Vector3 worldOrigin, Vector3 worldTarget)
	{
		if (id == 0) return worldOrigin;
		if (id == 1) return worldTarget;
		return _graphPoints[id - 2];
	}

	private Vector3[] TryReuseCachedPath
	(
		PhysicsDirectSpaceState3D space,
		Vector3 worldOrigin,
		Vector3 worldTarget,
		Godot.Collections.Array<Rid> exclusions
	)
	{
		List<Vector3> result = [];
		if (_cachedWaypoints.Count == 0) return [.. result];
		if (worldOrigin.DistanceTo(_cachedOrigin) > ReuseOriginTolerance) return [.. result];
		if (worldTarget.DistanceTo(_cachedTarget) > ReuseTargetTolerance) return [.. result];

		result.Add(worldOrigin);

		foreach (Vector3 p in _cachedWaypoints) result.Add(p);

		result.Add(worldTarget);

		Vector3[] packedResult = [.. result];

		if (IsPathValid(space, result, exclusions))
		{
			if (SmoothPathWithVisiblity && packedResult.Length > 2) packedResult = SmoothPath(space, packedResult, exclusions);
			float resultLen = GetPathLength(packedResult);
			float directDist = worldOrigin.DistanceTo(worldTarget);
			if (_cachedPathLen > 0.0f && resultLen > _cachedPathLen * 1.20f) return [];
			if (resultLen > directDist * ReuseMaxDetourRatio) return [];
			return packedResult;
		}

		return [];
	}

	private bool IsPathValid
	(
		PhysicsDirectSpaceState3D space,
		List<Vector3> worldPath,
		Godot.Collections.Array<Rid> exclusions
	)
	{
		if (worldPath.Count < 2) return false;
		for (int i = 0; i < worldPath.Count - 1; i++)
		{
			if (!IsSegmentClear(space, worldPath[i], worldPath[i + 1], exclusions)) return false;
		}
		return true;
	}

	private bool IsSegmentClear
	(
		PhysicsDirectSpaceState3D space,
		Vector3 from,
		Vector3 to,
		Godot.Collections.Array<Rid> exclusions
	)
	{
		if (from.IsEqualApprox(to)) return true;

		string key = SegmentKey(from, to);

		if (_segVisCache.ContainsKey(key)) return _segVisCache[key];

		bool visible = true;
		Godot.Collections.Dictionary hit = GetFirstHit(space, from, to, exclusions);
		if (!(hit.Count == 0)) visible = false;
		else if (EdgeClearanceChecks > 0)
		{
			for (int i = 0; i < EdgeClearanceChecks; i++)
			{
				float t = i + 1 / EdgeClearanceChecks + 1;
				Vector3 p = new
				(
					float.Lerp(from.X, to.X, t),
					float.Lerp(from.Y, to.Y, t),
					float.Lerp(from.Z, to.Z, t)
				);
				if (!IsPointFree(space, p, exclusions)) { visible = false; break; }
			}
		}

		_segVisCache[key] = visible;
		return visible;
	}

	private string SegmentKey(Vector3 a, Vector3 b)
	{
		Vector3I ka = PointKey(a);
		Vector3I kb = PointKey(b);

		if (ka.X > kb.X || (ka.X == kb.X && (ka.Y > kb.Y || (ka.Y == kb.Y && ka.Z > kb.Z))))
		{
			(kb, ka) = (ka, kb);
		}
		return $"{ka.X}|{ka.Y}|{ka.Z}>{kb.X}|{kb.Y}|{kb.Z}";
	}

	private Vector3I PointKey(Vector3 point)
	{
		Vector3I result = new
		(
			(int)Math.Round(point.X * 100.0),
			(int)Math.Round(point.Y * 100.0),
			(int)Math.Round(point.Z * 100.0)
		);

		return result;
	}

	private bool IsPointFree
	(
		PhysicsDirectSpaceState3D space,
		Vector3 p,
		Godot.Collections.Array<Rid> exclusions)
	{
		Vector3I key = PointKey(p);
		if (_pFreeCache.TryGetValue(key, out bool result)) return result;

		_shapeQuery.Transform = new(Basis.Identity, p);
		_shapeQuery.Exclude = exclusions;
		_shapeQuery.CollisionMask = CollisionMask;
		_shapeQuery.CollideWithAreas = CollideWithAreas;
		_shapeQuery.CollideWithBodies = CollideWithBodies;
		Godot.Collections.Array<Godot.Collections.Dictionary> hits = space.IntersectShape(_shapeQuery, 1);
		bool isFree = hits.Count == 0;
		_pFreeCache[key] = isFree;
		return isFree;
	}

	private Vector3[] SmoothPath
	(
		PhysicsDirectSpaceState3D space,
		Vector3[] worldPath,
		Godot.Collections.Array<Rid> exclusions)
	{
		if (worldPath.Length <= 2) return worldPath;

		List<Vector3> smoothed = [worldPath[0]];
		int anchor = 0;
		while (anchor < worldPath.Length - 1)
		{
			int furthest = worldPath.Length - 1;
			while (furthest > anchor + 1)
			{
				if (IsSegmentClear(space, worldPath[anchor], worldPath[furthest], exclusions)) break;
				furthest -= 1;
			}
			smoothed.Add(worldPath[furthest]);
			anchor = furthest;
		}

		return [.. smoothed];
	}
	
	private float GetPathLength(Vector3[] worldPath)
	{
		if (worldPath.Length < 2) return 0.0f;

		float result = 0.0f;
		for (int i = 0; i < worldPath.Length - 1; i++)
		{
			result += worldPath[i].DistanceTo(worldPath[i + 1]);
		}
		return result;
	}

	#endregion

	#region Utils - Graph

	// Rebuild graph if needed.
	private void CheckRebuildGraph
	(
		PhysicsDirectSpaceState3D space,
		Vector3 worldOrigin,
		Godot.Collections.Array<Rid> exclusions
	)
	{
		bool mustRecenter = _graphPoints.Count == 0 ||
		_graphAnchor.DistanceTo(worldOrigin) > GraphRecenterDistance;

		if (!_graphDirty && !mustRecenter) return;

		CheckRebuildSamples();
		_graphAnchor = worldOrigin;
		_gridOrigin = worldOrigin;
		_graphPoints.Clear();
		_graphEdges.Clear();
		_graphEdgeCount = 0;

		if (UseReachableScan) BuildGraphReachableScan(space, exclusions);
		else BuildGraphRandomSamples(space, exclusions);

		_graphDirty = false;
		EmitSignal(SignalName.GraphRebuilt, _graphPoints.Count);
	}

	private void BuildGraphRandomSamples
	(PhysicsDirectSpaceState3D space, Godot.Collections.Array<Rid> exclusions)
	{
		foreach (Vector3 offset in _sampleOffsets)
		{
			Vector3 point = _graphAnchor + offset;
			if (IsPointFree(space, point, exclusions)) _graphPoints.Add(point);
		}

		int count = _graphPoints.Count;
		_graphEdges.Clear();
		_graphEdges.Resize(count);
		for (int i = 0; i < count; i++)
		{
			List<float> candis = [];
			Vector3 p = _graphPoints[i];
			int keepCount = Math.Max(GraphNeighborLimit * 2, GraphNeighborLimit + 2);
			float maxConnSq = MaxConnectionDistance * MaxConnectionDistance;
			for (int j = 0; j < count; j++)
			{
				if (i == j) continue;
				float dSq = p.DistanceSquaredTo(_graphPoints[j]);
				if (dSq <= maxConnSq) InsertSortedPair(candis, dSq, j, keepCount);
			}

			int added = 0;
			foreach (float cand in candis)
			{
				if (added >= GraphNeighborLimit) break;
				if (EdgeExists(i, (int)cand)) continue;
				if (IsSegmentClear(space, _graphPoints[i], _graphPoints[(int)cand], exclusions))
				{
					AddEdge(i, (int)cand);
					added += 1;
				}
			}
		}
	}

	private void BuildGraphReachableScan
	(PhysicsDirectSpaceState3D space, Godot.Collections.Array<Rid> exclusions)
	{
		List<Vector3I> neighborDirs = GetScanNeighborDirs(ScanNeighborMode);
		List<Vector3I> queue = [];
		int head = 0;
		Dictionary<Vector3I, int> indexOf = [];
		float radiusSq = NavigationRadius * NavigationRadius;
		int maxEx = ScannedMaxExtent;

		Vector3I startCell = Vector3I.Zero;
		Vector3 startPos = CellToWorld(startCell, _gridOrigin, ScanCellSize);
		if (!IsPointFree(space, startPos, exclusions)) return;

		indexOf[startCell] = 0;
		queue.Add(startCell);
		_graphPoints.Add(startPos);
		_graphEdges.Add([]);

		while (head < queue.Count && _graphPoints.Count < ScanedCellMax)
		{
			Vector3I cell = queue[head];
			head += 1;
			int cellIdx = indexOf[cell];
			Vector3 cellPos = _graphPoints[cellIdx];

			foreach (Vector3I dir in neighborDirs)
			{
				Vector3I nextCell = cell + dir;
				if (maxEx > 0 &&
				(
					Math.Abs(nextCell.X) > maxEx || Math.Abs(nextCell.Y) > maxEx || Math.Abs(nextCell.Z) > maxEx
				)
				) continue;

				Vector3 nextPos = CellToWorld(nextCell, _gridOrigin, ScanCellSize);
				if (nextPos.DistanceSquaredTo(_graphAnchor) > radiusSq) continue;

				bool hasNext = indexOf.ContainsKey(nextCell);
				if (!hasNext)
				{
					if (!IsPointFree(space, nextPos, exclusions)) continue;
					if (!IsSegmentClear(space, cellPos, nextPos, exclusions)) continue;

					indexOf[nextCell] = _graphPoints.Count;
					queue.Add(nextCell);
					_graphPoints.Add(nextPos);
					_graphEdges.Add([]);
					int newIdx = indexOf[nextCell];
					if (!EdgeExists(cellIdx, newIdx)) AddEdge(cellIdx, newIdx);
					continue;
				}
				else if (!IsSegmentClear(space, cellPos, nextPos, exclusions)) continue;

				int nextIdx = indexOf[nextCell];
				if (cellIdx == nextIdx || EdgeExists(cellIdx, nextIdx)) continue;
				if (_graphEdges[cellIdx].Count >= GraphNeighborLimit ||
				_graphEdges[nextIdx].Count >= GraphNeighborLimit) continue;
				AddEdge(cellIdx, nextIdx);
			}
		}
	}

	private Vector3 CellToWorld(Vector3I cell, Vector3 gridOrigin, float cellSize)
	{
		float step = cellSize * (1.0f - ScanCellInset);
		Vector3 result = new(cell.X, cell.Y, cell.Z);
		return gridOrigin + result * step;
	}

	private List<Vector3I> GetScanNeighborDirs(ScanNeighborModeEnum scanNeighborMode)
	{
		throw new NotImplementedException();
	}

	private void AddEdge(int a, int b)
	{
		List<int> aa = _graphEdges[a];
		aa.Add(b);
		_graphEdges[a] = aa;
		List<int> bb = _graphEdges[b];
		bb.Add(a);
		_graphEdges[b] = bb;
		_graphEdgeCount++;
	}

	private bool EdgeExists(int a, int b)
	{
		foreach (int edge in _graphEdges[a])
		{
			if (edge == b) return true;
		}
		return false;
	}

	private float[] FindDynamicLinks
	(
		PhysicsDirectSpaceState3D space,
		Vector3 worldOrigin,
		Vector3 worldTarget,
		Godot.Collections.Array<Rid> exclusions)
	{
		List<float> candidates = [];
		int keepCount = Math.Max(DynamicConnectionLimit * Math.Max(DynamicCanidateMultiplier, 1), DynamicConnectionLimit + 2);

		for (int i = 0; i < _graphPoints.Count; i++)
		{
			Vector3 point = _graphPoints[i];
			float score = worldOrigin.DistanceSquaredTo(point) + point.DistanceSquaredTo(worldTarget) * 0.25f;
			InsertSortedPair(candidates, score, i, keepCount);
		}

		foreach (int candi in candidates)
		{
			if (candidates.Count >= DynamicConnectionLimit) break;
			int idx = candi;
			if (IsSegmentClear(space, worldOrigin, _graphPoints[idx], exclusions)) candidates.Add(idx);
		}
		return [.. candidates];
	}

	private void InsertSortedPair(List<float> store, float score, int idx, int limit)
	{
		List<float> pair = [score, idx];
		bool inserted = false;
		for (int i = 0; i < store.Count; i++)
		{
			if (score < store[i])
			{
				store.InsertRange(i, pair);
				inserted = true;
				break;
			}
			if (!inserted)
			{
				if (store.Count < limit) store.AddRange(pair);
				else return;
			}
			if (store.Count > limit) store.Resize(limit);
		}
	}

	#endregion

	#region Utils - Nav

	private void ApplyNavigationProfilePreset()
	{
		switch (NavigationProfile)
		{
			case NavigationProfileEnum.Custom:
				return;

			case NavigationProfileEnum.OpenAreas:
				UseReachableScan = false;
				SamplePointCount = 128;
				MaxConnectionDistance = 11.0f;
				GraphNeighborLimit = 12;
				DynamicConnectionLimit = 8;
				DynamicCanidateMultiplier = 3;
				EdgeClearanceChecks = 0;
				GraphRecenterDistance = 3.5f;
				ScannedMaxExtent = 0;
				break;

			case NavigationProfileEnum.Hallways:
				UseReachableScan = true;
				ScanNeighborMode = ScanNeighborModeEnum.Axis_6;
				ScanCellSize = 0.9f;
				ScanedCellMax = 4096;
				ScannedMaxExtent = 28;
				ScanCellInset = 0.10f;
				MaxConnectionDistance = 8.0f;
				GraphNeighborLimit = 10;
				DynamicConnectionLimit = 6;
				DynamicCanidateMultiplier = 4;
				EdgeClearanceChecks = 1;
				GraphRecenterDistance = 1.5f;
				break;
		}

		_hasLastSolve = false;
		MarkGraphDirty();

		if (IsInsideTree() && !Engine.IsEditorHint()) RecomputePath();
	}

	// Rebuild navigation samples if needed.
	private void CheckRebuildSamples()
	{
		if (!_samplesDirty) return;

		_sampleOffsets.Clear();
		RandomNumberGenerator rng = new();
		rng.Seed = SampleSeed;
		for (int i = 0; i < SamplePointCount; i++)
		{
			_sampleOffsets.Add(RandomPointInSphere(rng, NavigationRadius));
		}
		_samplesDirty = false;
	}

	// Godot Array needed here instead of List due to ray query exclusions requirement
	private Godot.Collections.Array<Rid> BuildExclusions(Node3D target)
	{
		if (_exclusionsDirty)
		{
			_cachedBaseExclusions.Clear();
			Dictionary<Rid, bool> seen = [];
			AppendExclusionsRecursive(this, _cachedBaseExclusions, seen);
			foreach (Node3D node in ExcludedCollisionNodes)
			{
				AppendExclusionsRecursive(node, _cachedBaseExclusions, seen);
			}
			_exclusionsDirty = false;
		}

		if (!IgnoreListenerBody || target == null) return _cachedBaseExclusions;

		Godot.Collections.Array<Rid> result = _cachedBaseExclusions.Duplicate();
		Dictionary<Rid, bool> seenMerge = [];

		foreach (Rid rid in result) seenMerge[rid] = true;

		Node listenerRoot = FindCollisionAncestor(target);
		if (listenerRoot != null) AppendExclusionsRecursive(listenerRoot, result, seenMerge);

		return result;
	}

	private void AppendExclusionsRecursive(Node node, Godot.Collections.Array<Rid> result, Dictionary<Rid, bool> seen)
	{
		if (node == null) return;
		if (node is not CollisionObject3D collision) return;

		Rid rid = collision.GetRid();
		if (!seen.ContainsKey(rid)) { seen[rid] = true; result.Add(rid); }

		foreach (Node child in node.GetChildren()) AppendExclusionsRecursive(child, result, seen);
	}

	#endregion

	#region Utils - Proxy

	private void UpdateAudio(double delta)
	{
		if (AudioPlayerNode == null || !IsInstanceValid(AudioPlayerNode))
		{ GetAudioPlayer(); if (AudioPlayerNode == null) return; }

		Vector3 worldOrigin = ResolveWorldOrigin();
		Vector3 worldListener = _lastWorldTarget;
		if (_hasValidPath && _currentPath.Length > 0) worldListener = _currentPath[^1];
		else if (GetTargetNode() != null) worldListener = GetTargetNode().GlobalPosition;
		if (!MoveAudioPlayer)
		{
			_proxyInBackoff = false;
			_springArmReady = false;
			UpdateProxyInnerRadius(false, worldOrigin, worldOrigin);
			UpdateProxyOccl(false);
#if DEBUG
			UpdateExtNavDebug(false, worldOrigin, worldListener);
#endif
			if (RestoreAudioToOriginWhenDisabled)
			{
				AudioPlayerNode.GlobalPosition = worldOrigin;
				_currentProxy = worldOrigin;
				_targetProxy = worldOrigin;
				_proxyReady = true;
			}
			ApplyRefVolOffset(0.0f);
			return;
		}

		bool refActive = _hasValidPath && !_isDirectPath;
		Vector3 baseTar = GetProxyTarget(worldOrigin);
		if (refActive && ProxySpringArmEnabled) baseTar = ApplyProxySpringTarget(delta, baseTar);
		else _springArmReady = false;

		_targetProxy = baseTar + Vector3.Up * AudioProxyHeightOffset;
		if (!_proxyReady) { _currentProxy = _targetProxy; _proxyReady = true; }

		float lerpSpeed = AudioProxyLerpSpeed;
		if (_proxyInBackoff) lerpSpeed = AudioProxyBackoffLerpSpeed;
		if (lerpSpeed <= 0.0f) _currentProxy = _targetProxy;
		else
		{
			float lerpAmt = Math.Clamp((float)delta * lerpSpeed, 0.0f, 1.0f);
			_currentProxy = _currentProxy with
			{
				X = float.Lerp(_currentProxy.X, _targetProxy.X, lerpAmt),
				Y = float.Lerp(_currentProxy.X, _targetProxy.X, lerpAmt),
				Z = float.Lerp(_currentProxy.X, _targetProxy.X, lerpAmt)
			};
		}

		AudioPlayerNode.GlobalPosition = _currentProxy;
		UpdateRefMod(worldOrigin, _currentProxy);
		UpdateProxyInnerRadius(refActive, worldOrigin, _currentProxy);
		UpdateProxyOccl(refActive);
#if DEBUG
		UpdateExtNavDebug(false, worldOrigin, worldListener);
#endif
		EmitSignal(SignalName.AudioProxyPosUpdated, _currentProxy);
	}

	private void RefreshProxyRadius()
	{
		_proxyHasInner = false;
		_proxyInnerRad = -1.0f;
		_forceProxyInnerRad = false;
		if (AudioPlayerNode == null || !IsInstanceValid(AudioPlayerNode)) return;
		_proxyInnerRad = AudioPlayerNode.InnerRadius;
		_proxyHasInner = true;
	}

	private void UpdateProxyInnerRadius(bool reflectedActive, Vector3 worldOrigin, Vector3 worldProxy)
	{
		if (AudioPlayerNode == null || !IsInstanceValid(AudioPlayerNode) || !_proxyHasInner) return;
		_proxyInnerRad = AudioPlayerNode.InnerRadius;
		float srcInner = MathF.Max(_proxyInnerRad, 0.0f);
		bool outSrcInner = worldOrigin.DistanceTo(worldProxy) > srcInner + 0.001f;
		if (outSrcInner && !_forceProxyInnerRad)
		{
			AudioPlayerNode.InnerRadius = 0.0f;
			_forceProxyInnerRad = true;
		}
		else RestoreInnerRadForced();
	}

	private void RestoreInnerRadForced()
	{
		if (AudioPlayerNode == null || !IsInstanceValid(AudioPlayerNode))
		{ _forceProxyInnerRad = false; return; }

		if (_proxyHasInner && _forceProxyInnerRad && _proxyInnerRad >= 0.0f)
		{ AudioPlayerNode.InnerRadius = _proxyInnerRad; }

		_forceProxyInnerRad = true;
	}

	private Vector3 GetProxyTarget(Vector3 worldOrigin)
	{
		if (!_hasValidPath || _currentPath.Length < 2) { _proxyInBackoff = false; return worldOrigin; }
		if (ProxyOnlyWhenBlocked && _isDirectPath) { _proxyInBackoff = false; return worldOrigin; }

		int idx = Math.Min(Math.Max(ProxyWaypointIndex, 1), _currentPath.Length - 1);
		Vector3 tarPoint = _currentPath[idx];

		if (ProxySpringArmEnabled) { _proxyInBackoff = false; return tarPoint; }
		if (!EnableProxyListenerBackoff || _isDirectPath) { _proxyInBackoff = false; return tarPoint; }

		Vector3 listenerPos = _currentPath[^1];
		Vector3 proxyCheckPos = tarPoint;
		if (_proxyReady) proxyCheckPos = _currentProxy;
		float distToList = proxyCheckPos.DistanceTo(listenerPos);
		float releaseDist = MathF.Max(ProxyBackoffReleaseDistance, ProxyMinListenerDistance);

		if (_proxyInBackoff && distToList >= releaseDist) _proxyInBackoff = false;
		else if (distToList < ProxyMinListenerDistance) _proxyInBackoff = true;

		if (_proxyInBackoff)
		{
			float backoffDist = MathF.Max(ProxyBackoffPathDistance, ProxyMinListenerDistance);
			tarPoint = GetPointAlongPath(_currentPath, backoffDist); 
		}
		
		return tarPoint;
	}

	/// <summary>
	/// Update proxy occlusion transition support.
	/// </summary>
	private void UpdateProxyOccl(bool refActive)
	{
		if (AudioPlayerNode == null || !IsInstanceValid(AudioPlayerNode))
		{ _wasRefProxyActive = refActive; return; }

		if (!ProxyOcclusionTransitionSmoothing)
		{
			AudioPlayerNode.ClearExternalOcclusionHold();
			_wasRefProxyActive = refActive;
			return;
		}

		if (refActive) AudioPlayerNode.ClearExternalOcclusionHold();
		else if (_wasRefProxyActive && ProxyOcclusionHoldSeconds > 0.0f)
		{ AudioPlayerNode.SetExternalOcclusionHold(ProxyOcclusionHoldSeconds); }

		if (!refActive && _proxyReady && _currentProxy.DistanceTo(_targetProxy) > ProxyOcclusionHoldMoveThreshold)
		{ AudioPlayerNode.SetExternalOcclusionHold(ProxyOcclusionHoldSeconds); }

		_wasRefProxyActive = refActive;
	}

	private Vector3 ApplyProxySpringTarget(double delta, Vector3 baseTar)
	{
		if (_currentPath.Length < 2) { _springArmReady = false; return baseTar; }

		float pathTot = _currentPathLen;
		if (pathTot <= 0.001) { _springArmReady = false; return baseTar; }

		Vector3 listenerPos = _currentPath[^1];
		float baseAlong = GetDistAlongPath(_currentPath, baseTar);
		float desFromEnd = Math.Clamp(pathTot - baseAlong, 0.0f, pathTot);

		Vector3 probe = baseTar;
		if (_proxyReady) probe = _currentProxy;

		float curToList = probe.DistanceTo(listenerPos);
		float minDist = MathF.Max(ProxySpringMinDistance, 0.0f);

		if (curToList < minDist)
		{
			float breach = minDist - curToList;
			desFromEnd = MathF.Max(desFromEnd, minDist + breach * ProxySpringPushStrength);
		}

		if (!_springArmReady) { _springArmDistFromEnd = desFromEnd; _springArmReady = true; }
		else
		{
			float speed = ProxySpringReturnSpeed;
			if (desFromEnd > _springArmDistFromEnd) speed = ProxySpringPushSpeed;

			float lerpAmt = Math.Clamp((float)delta * speed, 0.0f, 1.0f);
			_springArmDistFromEnd = float.Lerp(_springArmDistFromEnd, desFromEnd, lerpAmt);
		}

		_springArmDistFromEnd = Math.Clamp(_springArmDistFromEnd, 0.0f, pathTot);
		return GetPointAlongPath(_currentPath, _springArmDistFromEnd);
	}

	#endregion

	#region Utils - Helpers

	private void MarkGraphDirty()
	{
		_graphDirty = true;
	}

	private void CommitSolveState(Vector3 origin, Vector3 target)
	{
		_lastSolveOrigin = origin;
		_lastSolveTarget = target;
		_hasLastSolve = true;
		_timeSinceRecompute = 0.0;
	}

	private SpatialAudioPlayer3D FindAudioPlayer(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is SpatialAudioPlayer3D result) return result;
		}

		return null;
	}

	private bool IsSpatialAudioPlayerFound()
	{
		if (FindAudioPlayer(this) is SpatialAudioPlayer3D result) return true;

		return false;
	}

	private Node FindCollisionAncestor(Node node)
	{
		Node cur = node;
		while (cur != null)
		{
			if (cur is CollisionObject3D) return cur;
			cur = cur.GetParent();
		}
		return null;
	}

	private Vector3 ResolveWorldOrigin()
	{
		switch (OriginMode)
		{
			case OriginModeEnum.NodePosition:
				return ResolveNodeOrigin();

			case OriginModeEnum.NodeWithLocalOffset:
				Node3D owner = this;
				if (OriginOverride != null) owner = OriginOverride;
				return owner.ToGlobal(OriginLocalOffset);

			case OriginModeEnum.FixedWorldPosition:
				return FixedWorldOrigin;
		}
		return GlobalPosition;
	}

	private Vector3 ResolveNodeOrigin()
	{
		if (OriginOverride != null) return OriginOverride.GlobalPosition;
		return GlobalPosition;
	}

	/// <summary>
	/// Get a point along the path from the end.
	/// </summary>
	private Vector3 GetPointAlongPath(Vector3[] worldPath, float backoffDist)
	{
		if (worldPath.IsEmpty()) return Vector3.Zero;
		if (worldPath.Length == 1) return worldPath[0];

		float remain = MathF.Max(backoffDist, 0.0f);

		for (int i = 0; i < worldPath.Length - 1; i++)
		{
			Vector3 segEnd = worldPath[i];
			Vector3 segStart = worldPath[i - 1];
			float segLen = segEnd.DistanceTo(segStart);
			if (segLen <= 0.0001f) continue;
			if (remain <= segLen)
			{
				float amt = remain / segLen;
				segEnd = segEnd with
				{
					X = float.Lerp(segEnd.X, segStart.X, amt),
					Y = float.Lerp(segEnd.Y, segStart.Y, amt),
					Z = float.Lerp(segEnd.Z, segStart.Z, amt),
				};
				return segEnd;
			}
			remain -= segLen;
		}
		return worldPath[0];
	}

	private float GetDistAlongPath(Vector3[] worldPath, Vector3 worldPoint)
	{
		if (worldPath.Length < 2) return 0.0f;

		float bestDist = float.PositiveInfinity;
		float bestAlong = 0.0f;
		float accum = 0.0f;

		for (int i = 0; i < worldPath.Length - 1; i++)
		{
			Vector3 a = worldPath[i];
			Vector3 b = worldPath[i + 1];
			Vector3 ab = b - a;
			float segLenSq = ab.LengthSquared();

			if (segLenSq <= 0.000001) continue;

			float t = Math.Clamp((worldPoint - a).Dot(ab) / segLenSq, 0.0f, 1.0f);
			Vector3 proj = a + ab * t;
			float d = proj.DistanceTo(worldPoint);
			float segLen = MathF.Sqrt(segLenSq);
			if (d < bestDist) { bestDist = d; bestAlong = accum + segLen * t; }
			accum += segLen;
		}
		return bestAlong;
	}

	/// <summary>
	/// Update the reflection audio modulation.
	/// </summary>
	private void UpdateRefMod(Vector3 worldOrigin, Vector3 currentProxy)
	{
		if (!ApplyRefectionVolumeLoss) { ApplyRefVolOffset(0.0f); return; }
		if (_isDirectPath || _currentPath.Length < 2) { ApplyRefVolOffset(0.0f); return; }

		float routeDist = GetDistAlongPath(_currentPath, currentProxy);
		float shapeDist = MathF.Pow(MathF.Max(routeDist, 0.0f), ReflectionLossPower);
		float lossDb = MathF.Min(shapeDist * ReflectionLossDbPerMeter, ReflectionMaxLossDb);
		ApplyRefVolOffset(-lossDb);
	}

	/// <summary>
	/// Apply the reflection volume offset dB.
	/// </summary>
	private void ApplyRefVolOffset(float dB)
	{
		if (AudioPlayerNode == null || !IsInstanceValid(AudioPlayerNode)) return;
		if (Math.Abs(dB - _lastRefVolOffsetDb) < 0.05f) return;
		_lastRefVolOffsetDb = dB;

		AudioPlayerNode.SetExternalVolumeDbOffset(dB);
	}

	private Node3D GetTargetNode()
	{
		if (TargetOverride != null) return TargetOverride;

		if (Engine.IsEditorHint())
		{
			Viewport vp = Plugin.GetEditorViewport3D();
			if (vp != null) return vp.GetCamera3D();
			return null;
		}
		return GetViewport().GetCamera3D();
	}
	
	private Vector3 RandomPointInSphere(RandomNumberGenerator rng, float navigationRadius)
	{
		Vector3 p = Vector3.Zero;
		do
		{
			p = new
			(
				rng.RandfRange(-1.0f, 1.0f),
				rng.RandfRange(-1.0f, 1.0f),
				rng.RandfRange(-1.0f, 1.0f)
			);
		} while (p.LengthSquared() >= 1.0f);

		return p;
	}

	#endregion

	#region Editor Config

	private void EditorReady()
	{
		if (!PreviewPathingInEditor) { SetProcess(true); ResetAudioProxyToOrigin(); }
		UpdateConfigurationWarnings();
		UpdateAudio(0.0f);
	}

	private bool IsEditorSelected()
	{
		if (!Engine.IsEditorHint()) return false;
		Godot.Collections.Array<Node> selections = Plugin.GetEditorSelected();
		foreach (Node n in selections)
		{
			if (n == this) return true;
		}
		return false;
	}

	/// <inheritdoc />
	public override void _Notification(int what)
	{
		if (what == NotificationChildOrderChanged)
		{
			_exclusionsDirty = true;
			UpdateConfigurationWarnings();
		}
	}

	/// <inheritdoc />
	public override string[] _GetConfigurationWarnings()
	{
		List<string> warnings = [];

		Node3D configuredAudio = GetAudioPlayer();
		bool foundSpatial = IsSpatialAudioPlayerFound();

		if (!foundSpatial)
		{
			warnings.Add($"No SpatialAudioPlayer3D was found. Add one as a child or assign one in {AudioPlayerNode} for reflected proxy playback.");
		}

		if (configuredAudio != null && configuredAudio is AudioStreamPlayer3D && configuredAudio is not SpatialAudioPlayer3D)
		{
			warnings.Add($"Detected audio player is {configuredAudio.Name}, this is not a compatable, " +
			"proxy reflection integration requires a SpatialAudioPlayer3D to function correctly.");
		}
		else if (configuredAudio != null && configuredAudio is not AudioStreamPlayer3D)
		{
			warnings.Add($"{nameof(AudioPlayerNode)} points to {configuredAudio.Name}, which is not a " +
			"SpatialAudioPlayer3D. Assign a SpatialAudioPlayer3D for reflected proxy playback.");
		}

		return [.. warnings];
	}

	private Node3D GetAudioPlayer()
	{
		if (AudioPlayerNode != null && IsInstanceValid(AudioPlayerNode)) return AudioPlayerNode;
		if (AutoFindAudioPlayerChild) return FindAudioPlayer(this);
		RefreshProxyRadius();
		return null;
	}

	/// <inheritdoc />
	public override void _ValidateProperty(Godot.Collections.Dictionary property)
	{
		if (NavigationProfile != NavigationProfileEnum.Custom)
		{
			foreach (StringName propName in _NAV_PROFILE_CUSTOM_ONLY_PROPS)
			{
				if ((StringName)property["name"] == propName)
				{
					property["usage"] = (int)~PropertyUsageFlags.Editor;
				}
			}
		}

		List<StringName> recomputeProps =
		[
			PropertyName.RecomputeOriginThreshold,
			PropertyName.RecomputeTargetThreshold,
			PropertyName.StaticRecomputeInterval,
		];
		if (!SkipRecomputeWhenStatic)
		{
			foreach (StringName prop in recomputeProps)
			{
				if ((StringName)property["name"] == prop)
				{
					property["usage"] = (int)~PropertyUsageFlags.Editor;
				}
			}
		}

		List<StringName> scanProps =
		[
			PropertyName.ScanCellSize,
			PropertyName.ScanNeighborMode, PropertyName.ScanedCellMax,
			PropertyName.ScannedMaxExtent, PropertyName.ScanCellInset,
		];
		if (!UseReachableScan)
		{
			foreach (StringName prop in scanProps)
			{
				if ((StringName)property["name"] == prop)
				{
					property["usage"] = (int)~PropertyUsageFlags.Editor;
				}
			}
		}

		List<StringName> unitProps =
		[ PropertyName.UnitCost ];
		if (!UseUnitCost)
		{
			foreach (StringName prop in unitProps)
			{
				if ((StringName)property["name"] == prop)
				{
					property["usage"] = (int)~PropertyUsageFlags.Editor;
				}
			}
		}

		List<StringName> reusePathProps =
		[
			PropertyName.ReuseOriginTolerance,
			PropertyName.ReuseTargetTolerance,
			PropertyName.ReuseMaxDetourRatio,
		];
		if (!ReuseLastPath)
		{
			foreach (StringName prop in reusePathProps)
			{
				if ((StringName)property["name"] == prop)
				{
					property["usage"] = (int)~PropertyUsageFlags.Editor;
				}
			}
		}

		List<StringName> audioPlayerProps =
		[
			PropertyName.AudioPlayerNode, PropertyName.AutoFindAudioPlayerChild, PropertyName.ProxyOnlyWhenBlocked,
			PropertyName.ProxyWaypointIndex, PropertyName.AudioProxyLerpSpeed, PropertyName.AudioProxyBackoffLerpSpeed,
			PropertyName.AudioProxyHeightOffset, PropertyName.RestoreAudioToOriginWhenDisabled, PropertyName.ProxyForceInnerRadiusOutsideSourceZone,
			PropertyName.EnableProxyListenerBackoff, PropertyName.ProxyMinListenerDistance, PropertyName.ProxyBackoffReleaseDistance,
			PropertyName.ProxyBackoffPathDistance, PropertyName.ProxySpringArmEnabled, PropertyName.ProxySpringMinDistance,
			PropertyName.ProxySpringPushStrength, PropertyName.ProxySpringPushSpeed, PropertyName.ProxySpringReturnSpeed,
			PropertyName.ApplyRefectionVolumeLoss, PropertyName.ReflectionLossDbPerMeter, PropertyName.ReflectionLossPower,
			PropertyName.ReflectionMaxLossDb, PropertyName.ProxyOcclusionTransitionSmoothing, PropertyName.ProxyOcclusionHoldSeconds,
			PropertyName.ProxyOcclusionHoldMoveThreshold, PropertyName.DebugDrawAudioProxy, PropertyName.DebugAudioProxyColor, PropertyName.DebugAudioProxyRadius,
		];
		if (!MoveAudioPlayer)
		{
			foreach (StringName prop in audioPlayerProps)
			{
				if ((StringName)property["name"] == prop)
				{
					property["usage"] = (int)~PropertyUsageFlags.Editor;
				}
			}
		}

		List<StringName> proxySpringProps =
		[
			PropertyName.EnableProxyListenerBackoff, PropertyName.ProxyMinListenerDistance, PropertyName.ProxyBackoffReleaseDistance, 
			PropertyName.ProxyBackoffPathDistance, PropertyName.AudioProxyBackoffLerpSpeed,
		];
		if (MoveAudioPlayer && ProxySpringArmEnabled)
		{
			foreach (StringName prop in proxySpringProps)
			{
				if ((StringName)property["name"] == prop)
				{
					property["usage"] = (int)~PropertyUsageFlags.Editor;
				}
			}
		}

		List<StringName> listenerBackProps =
		[
			PropertyName.ProxyMinListenerDistance, PropertyName.ProxyBackoffReleaseDistance,
			PropertyName.ProxyBackoffPathDistance, PropertyName.AudioProxyBackoffLerpSpeed,
		];
		if (MoveAudioPlayer && !ProxySpringArmEnabled && !EnableProxyListenerBackoff)
		{
			foreach (StringName prop in listenerBackProps)
			{
				if ((StringName)property["name"] == prop)
				{
					property["usage"] = (int)~PropertyUsageFlags.Editor;
				}
			}
		}

		List<StringName> noArmProps =
		[
			PropertyName.ProxySpringMinDistance, PropertyName.ProxySpringPushStrength,
			PropertyName.ProxySpringPushSpeed, PropertyName.ProxySpringReturnSpeed,
		];
		if (MoveAudioPlayer && !ProxySpringArmEnabled)
		{
			foreach (StringName prop in noArmProps)
			{
				if ((StringName)property["name"] == prop)
				{
					property["usage"] = (int)~PropertyUsageFlags.Editor;
				}
			}
		}

		List<StringName> noVolLossProps =
		[
			PropertyName.ReflectionLossDbPerMeter, PropertyName.ReflectionLossPower,
			PropertyName.ReflectionMaxLossDb,
		];
		if (MoveAudioPlayer && !ApplyRefectionVolumeLoss)
		{
			foreach (StringName prop in noVolLossProps)
			{
				if ((StringName)property["name"] == prop)
				{
					property["usage"] = (int)~PropertyUsageFlags.Editor;
				}
			}
		}

		List<StringName> noOccSmoothProps =
		[
			PropertyName.ProxyOcclusionHoldSeconds, PropertyName.ProxyOcclusionHoldMoveThreshold,
		];
		if (MoveAudioPlayer && !ProxyOcclusionTransitionSmoothing)
		{
			foreach (StringName prop in noOccSmoothProps)
			{
				if ((StringName)property["name"] == prop)
				{
					property["usage"] = (int)~PropertyUsageFlags.Editor;
				}
			}
		}

		List<StringName> debugPathProps =
		[
			PropertyName.DebugDrawDirectLineWhenBlocked,
		];
		if (!DebugDrawPath)
		{
			foreach (StringName prop in debugPathProps)
			{
				if ((StringName)property["name"] == prop)
				{
					property["usage"] = (int)~PropertyUsageFlags.Editor;
				}
			}
		}

		List<StringName> debugProxyProps =
		[
			PropertyName.DebugAudioProxyColor, PropertyName.DebugAudioProxyRadius,
		];
		if (!DebugDrawAudioProxy)
		{
			foreach (StringName prop in debugProxyProps)
			{
				if ((StringName)property["name"] == prop)
				{
					property["usage"] = (int)~PropertyUsageFlags.Editor;
				}
			}
		}
	}

	#endregion

#if DEBUG
	#region Debug

	// TODO: Debug

	private void SharedDebugMesh()
	{
		// TODO: SharedDebugMesh
	}

	private void DrawDebug(bool editorPreview = false)
	{
		// TODO: Draw debug
	}

	private void DrawPolyline()
	{
		// TODO: Draw polyline
	}

	private void DrawSegment()
	{
		// TODO: Draw segment
	}

	private void DrawCross()
	{
		// TODO: DrawCross
	}
	
	private void DrawWireframeSphere()
	{
		// TODO: Draw wireframe sphere
	}

	private void UpdateExtNavDebug(bool refActive, Vector3 worldOrigin, Vector3 worldListener)
	{
		// TODO: Update external navigation debug.
	}

	private void ResetAudioProxyToOrigin()
	{
		if (AudioPlayerNode == null || !IsInstanceValid(AudioPlayerNode))
		{ GetAudioPlayer(); if (AudioPlayerNode == null) return; }

		Vector3 worldOrigin = ResolveWorldOrigin();
		Vector3 worldListener = _lastWorldTarget;
		if (_hasValidPath && _currentPath.Length > 0) worldListener = _currentPath[^1];

		AudioPlayerNode.GlobalPosition = worldOrigin;
		_currentProxy = worldOrigin;
		_targetProxy = worldOrigin;
		_proxyReady = true;
		_proxyInBackoff = false;
		_springArmReady = false;
		UpdateProxyInnerRadius(false, worldOrigin, worldOrigin);
		UpdateProxyOccl(false);
		ApplyRefVolOffset(0.0f);
		UpdateExtNavDebug(false, worldOrigin, worldListener);
	}

	#endregion
#endif
}
