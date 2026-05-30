using Godot;
namespace SpatialAudioCS;

/// <summary>
/// TODO: Documentation
/// </summary>
[Tool, Icon("uid://diaqpjedywsus"), GlobalClass]
public partial class SpatialAudioPlayer3D : AudioStreamPlayer3D
{
	#region Signals - Zones

	/// <summary>
	/// Emitted when a listener enters the inner (full-volume) radius.
	/// </summary>
	/// <param name="listener">The object that is receiving sound.</param>
	[Signal] public delegate void InnerRadiusEnteredEventHandler(Node3D listener);

	/// <summary>
	/// Emitted when a listener leaves the inner (full-volume) radius.
	/// </summary>
	/// <param name="listener">The object that is receiving sound.</param>
	[Signal] public delegate void InnerRadiusExitedEventHandler(Node3D listener);

	/// <summary>
	/// Emitted when a listener enters the falloff zone (between inner and outer).
	/// </summary>
	/// <param name="listener">The object that is receiving sound.</param>
	[Signal] public delegate void FalloffZoneEnteredEventHandler(Node3D listener);

	/// <summary>
	/// Emitted when a listener exits the falloff zone (between inner and outer).
	/// </summary>
	/// <param name="listener">The object that is receiving sound.</param>
	[Signal] public delegate void FalloffZoneExitedEventHandler(Node3D listener);

	/// <summary>
	/// Emitted when a listener enters the outer boudary.
	/// </summary>
	/// <param name="listener">The object that is receiving sound.</param>
	[Signal] public delegate void AttenuationZoneEnteredEventHandler(Node3D listener);

	/// <summary>
	/// Emitted when a listener exits the outer boudary.
	/// </summary>
	/// <param name="listener">The object that is receiving sound.</param>
	[Signal] public delegate void AttenuationZoneExitedEventHandler(Node3D listener);

	#endregion

	#region Signals - Occulusion

	/// <summary>
	/// Emitted when occlusion parameters change.
	/// </summary>
	/// <param name="wallCount">Number of walls involved.</param>
	/// <param name="cutoffHz">Only frequencies below this value can pass through to the listener.</param>
	[Signal] public delegate void OcclusionChangedEventHandler(int wallCount, float cutoffHz);

	/// <summary>
	/// Emitted when the audio becomes occluded by one or more walls.
	/// </summary>
	/// <param name="listener">The object that is receiving sound.</param>
	/// <param name="wallCount">Number of walls involved.</param>
	[Signal] public delegate void AudioOccludedEventHandler(Node3D listener, int wallCount);

	/// <summary>
	/// Emitted when occlusion clears and the audio becomes unoccluded.
	/// </summary>
	/// <param name="listener">The object that is receiving sound.</param>
	[Signal] public delegate void AudioUnoccludedEventHandler(Node3D listener);

	/// <summary>
	/// Emitted when an occlusion ray collides with a surface.
	/// </summary>
	/// <param name="hitPosition"></param>
	/// <param name="fromPosition"></param>
	/// <param name="collider"></param>
	/// <param name="listener">The object that is receiving sound.</param>
	[Signal] public delegate void OcclusionRayCollidedEventHandler(Vector3 hitPosition, Vector3 fromPosition, Node collider, Node listener);

	#endregion

	#region Signals - Reverb

	/// <summary>
	/// Emitted when reverb targets change.
	/// </summary>
	/// <param name="roomSize"></param>
	/// <param name="wetness">The ammount of reverb applied. 
	/// <para>Dry = Raw<para>Wet = Full</para><c>0.0 - 1.0</c></para></param>
	/// <param name="damping">How quickly high frequncies decay relative to low frequencies.
	/// <para>Dark/Warm = Highs fade faster <para>Bright/Airy = Highs persist with lows</para><c>0.0 - 1.0</c></para></param>
	[Signal] public delegate void ReverbUpdatedEventHandler(float roomSize, float wetness, float damping);

	/// <summary>
	/// Emitted when room size or weness changes (higher-level reverb zone change).
	/// </summary>
	/// <param name="roomSize"></param>
	/// <param name="wetness">The ammount of reverb applied. 
	/// <para>Dry = Raw<para>Wet = Full</para><c>0.0 - 1.0</c></para></param>
	[Signal] public delegate void ReverbZoneChangedEventHandler(float roomSize, float wetness);

	/// <summary>
	/// Emitted when a reverb/reflection ray collides with a surface.
	/// </summary>
	/// <param name="hitPosition"></param>
	/// <param name="fromPosition"></param>
	/// <param name="collider"></param>
	[Signal] public delegate void ReverbRayCollidedEventHandler(Vector3 hitPosition, Vector3 fromPosition, Node collider);

	#endregion

	#region Signals - Air Absorption

	/// <summary>
	/// Emitted when air-absorption lowpass cutoff changes.
	/// </summary>
	/// <param name="cutoffHz">Only frequencies below this value can pass through to the listener.</param>
	[Signal] public delegate void AirAbsorptionUpdatedEventHandler(float cutoffHz);

	/// <summary>
	/// Emitted when the air-absorption zone changes (min/max thresholds crossed).
	/// </summary>
	/// <param name="cutoffHz">Only frequencies below this value can pass through to the listener.</param>
	[Signal] public delegate void AirAbsorptionZoneChangedEventHandler(float cutoffHz);

	#endregion

	#region Signals - Playback

	/// <summary>
	/// Emitted periodically when listener distance changes significantly.
	/// </summary>
	/// <param name="distance"></param>
	[Signal] public delegate void ListenerDistanceChangedEventHandler(float distance);

	/// <summary>
	/// Emitted when playback actually starts (immediate or deferred).
	/// </summary>
	[Signal] public delegate void SpatialAudioPlaybackStartedEventHandler();

	/// <summary>
	/// Emitted when playback is stopped.
	/// </summary>
	[Signal] public delegate void SpatialAudioPlaybackStoppedEventHandler();

	#endregion

	#region Signals - Debug

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

	#region Exports - Rays

	/// <summary>
	/// Options for how the omni-directional
	/// room-sensing rays are distributed around the emitter.
	/// </summary>
	public enum EmitterRayDistribution
	{
		/// <summary>
		/// Ten (10) predefined rays (cardinal + diagonal + up/down).
		/// </summary>
		Classic,
		/// <summary>
		/// Evenly distributed rays using a Fibonacci sphere.
		/// </summary>
		FibonacciSphere,
		/// <summary>
		/// Rays scattered outward froma given collision shape.
		/// </summary>
		ShapeScatter,
	}
	private EmitterRayDistribution _rayDistribution = EmitterRayDistribution.Classic;
	/// <summary>
	/// How the room-sensing rays are arranged around the emitter.
	/// <para>See <see cref="EmitterRayDistribution"/> for options.</para>
	/// </summary>
	[Export]
	public EmitterRayDistribution RayDistribution
	{
		get => _rayDistribution;
		set
		{
			_rayDistribution = value;
#if TOOLS
			NotifyPropertyListChanged();
			// if (SetupComplete) RebuildRaycasts();
#endif
		}
	}

	private int _fibonacciRayCount = 8;
	/// <summary>
	/// Number of omni-directional rays when using 
	/// <see cref="EmitterRayDistribution.FibonacciSphere"/> distribution.
	/// More rays = better room-size estimation at higher CPU cost.
	/// </summary>
	[Export(PropertyHint.Range, "4, 128, 1")]
	public int FibonacciRayCount
	{
		get => _fibonacciRayCount;
		set
		{
			_fibonacciRayCount = value;
#if TOOLS
			// if (SetupComplete && RayDistribution == EmitterRayDistribution.FibonacciSphere)
			// {
			// 	RebuildRaycasts();
			// }
#endif
		}
	}

	private CollisionShape3D _scatterShape = null;
	/// <summary>
	/// The collision-shape used as the scattering origin.
	/// Rays will be positioned around this node and fired outward.
	/// <para>If empty, rays will be scattered around this node's global origin.</para>
	/// </summary>
	[Export]
	public CollisionShape3D ScatterShape
	{
		get => _scatterShape;
		set
		{
			_scatterShape = value;
#if TOOLS
			// if (SetupComplete && RayDistribution == EmitterRayDistribution.ShapeScatter)
			// {
			// 	RebuildRaycasts();
			// }
#endif
		}
	}

	private int _shapeRayCount = 32;
	/// <summary>
	/// Number of rays when using <see cref="EmitterRayDistribution.ShapeScatter"/> random distribution.
	/// </summary>
	[Export(PropertyHint.Range, "1, 256, 1")]
	public int ShapeRayCount
	{
		get => _shapeRayCount;
		set
		{
			_shapeRayCount = value;
#if TOOLS
			// if (SetupComplete && RayDistribution == EmitterRayDistribution.ShapeScatter)
			// {
			// 	RebuildRaycasts();
			// }
#endif
		}
	}

	private int _shapeScatterRandomness = 0;
	/// <summary>
	/// How strongly the ray direction deviates from the center-to-surface direction.
	/// <para><c>0</c> = rays point exactly away from the shape center</para>
	/// <c>50</c> = rays point in entirely random directions.
	/// </summary>
	[Export(PropertyHint.Range, "0, 50, 1, prefer_slider")]
	public int ShapeScatterRandomness
	{
		get => _shapeScatterRandomness;
		set
		{
			_shapeScatterRandomness = value;
#if TOOLS
			// if (SetupComplete && RayDistribution == EmitterRayDistribution.ShapeScatter)
			// {
			// 	RebuildRaycasts();
			// }
#endif
		}
	}

	private int _fibonacciRayReflections = 0;
	/// <summary>
	/// Number of times each Fibonacci ray can reflect (bounce) off surfaces.
	/// More reflections improve room-size estimation in complex geometry 
	/// (L-shaped rooms, corridors, etc.) at higher CPU cost.
	/// </summary>
	[Export(PropertyHint.Range, "0, 8, 1")]
	public int FibonacciRayReflections
	{
		get => _fibonacciRayReflections;
		set => _fibonacciRayReflections = value;
	}

	private float _maxRaycastDistance = 50.0f;

	[Export(PropertyHint.Range, "0.01f, 4096.0f, 0.01f, suffix:m")]
	public float MaxRaycastDistance
	{
		get => _maxRaycastDistance;
		set => _maxRaycastDistance = value;
	}

	#endregion

	#region Exports - Reverb

	[ExportGroup("Room Size Reverb")]

	private bool _roomSizeReverb = true;
	/// <summary>
	/// When enabled, reverb room size and wetness are calculated 
	/// from surrounding geometry and applied automatically via 
	/// omni-directional raycasts.
	/// </summary>
	[Export]
	public bool RoomSizeReverb
	{
		get => _roomSizeReverb;
		set
		{
			_roomSizeReverb = value;
#if TOOLS
			NotifyPropertyListChanged();
#endif
		}
	}

	private float _maxReverbWetness = 0.5f;
	/// <summary>
	/// The maximum reverb wetness that can be applied. 
	/// Acts as a global ceiling on the wet signal 
	/// regardless of how enclosed the space is.
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 1.0f, 0.01f")]
	public float MaxReverbWetness
	{
		get => _maxReverbWetness;
		set => _maxReverbWetness = value;
	}

	private bool _surfaceAbsorption = true;
	/// <summary>
	/// When enabled, the absorption properties of <see cref="AcousticMaterial"/>s
	/// on surfaces hit by room-sensing rays are used to modulate reverb wetness
	/// and damping. 
	/// <para>Highly absorptive surfaces (carpet, curtains) reduce reverb
	/// wetness and increase damping.</para>
	/// Reflective surfaces (concrete, glass) preserve reverb energy.
	/// <para>Surfaces without an <see cref="AcousticBody"/> / <see cref="AcousticMaterial"/>
	/// are ignored.</para>
	/// Only surfaces with explicit materials contribute to the absorption average.
	/// </summary>
	[Export]
	public bool SurfaceAbsorption
	{
		get => _surfaceAbsorption;
		set
		{
			_surfaceAbsorption = value;
#if TOOLS
			NotifyPropertyListChanged();
#endif
		}
	}

	private float _absorptionWetnessInfluence = 1.0f;
	/// <summary>
	/// How strongly surface absorption influences reverb wetness.
	/// <para><c>0.0</c> = absorption has no effect on wetness.</para>
	/// <c>1.0</c> = full physical effect.
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 2.0f, 0.01f")]
	public float AbsorptionWetnessInfluence
	{
		get => _absorptionWetnessInfluence;
		set => _absorptionWetnessInfluence = value;
	}

	private float _absorptionDampingInfluence = 1.0f;
	/// <summary>
	/// How strongly surface absorption influences reverb damping.
	/// <para><c>0.0</c> = absorption has no effect on damping.</para>
	/// <c>1.0</c> = full physical effect.
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 2.0f, 0.01f")]
	public float AbsorptionDampingInfluence
	{
		get => _absorptionDampingInfluence;
		set => _absorptionDampingInfluence = value;
	}

	private bool _ignoreFloor = false;
	/// <summary>
	/// When enabled, rays pointing downward (below <see cref="FloorAngleThreshold"/>)
	/// are excluded from room-size and openness calculations.
	/// This prevents the floor (which is almost always present) 
	/// from shrinking the perceived room size.
	/// </summary>
	[Export]
	public bool IgnoreFloor
	{
		get => _ignoreFloor;
		set
		{
			_ignoreFloor = value;
#if TOOLS
			NotifyPropertyListChanged();
#endif
		}
	}

	private float _floorAngleThreshold = 30.0f;
	/// <summary>
	/// The angle in degrees from straight down withing which a ray
	/// is considered a "floor ray" and will be ignored when
	/// <see cref="IgnoreFloor"/> is enabled.
	/// <para><c>30.0</c> = only nearly-vertical rays.</para>
	/// <c>60.0</c> = wider cone.
	/// </summary>
	[Export(PropertyHint.Range, "5.0f, 90.0f, 1.0f, suffix:deg")]
	public float FloorAngleThreshold
	{
		get => _floorAngleThreshold;
		set => _floorAngleThreshold = value;
	}

	private ulong _reverbCollisionMask = 1 << 0;
	/// <summary>
	/// Physics layers the room-sensing raycasts collide with.
	/// Should match the layers your level geometry occupies.
	/// </summary>
	[Export(PropertyHint.Layers3DPhysics)]
	public ulong ReverbCollisionMask
	{
		get => _reverbCollisionMask;
		set => _reverbCollisionMask = value;
	}

	#endregion

	#region Exports - Occlusion

	[ExportGroup("Occlusion")]

	private bool _audioOcclusion = true;
	/// <summary>
	/// When enabled, a lowpass filter simulates sound being muffled
	/// by walls between the emitter and listener using a single target
	/// raycast.
	/// </summary>
	[Export]
	public bool AudioOcclusion
	{
		get => _audioOcclusion;
		set
		{
			_audioOcclusion = value;
#if TOOLS
			NotifyPropertyListChanged();
#endif
		}
	}

	private float _occlusionStrength = 1.0f;
	/// <summary>
	/// How strongly the lowpass filter is applied when the listener is occluded.
	/// <para><c>1.0</c> = full effect (cutoff reaches <see cref="OccludedLowpassCutoffMinimum"/>)</para>
	/// <c>0.0</c> = no filtering at all regardless of occlusion.
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 5.0f, 0.01f")]
	public float OcclusionStrength
	{
		get => _occlusionStrength;
		set => _occlusionStrength = value;
	}

	private int _maxOcclusionHits = 4;
	/// <summary>
	/// Maximum number of walls (collisions) to detect between the emitter and the listener.
	/// Each additional wall multiplicatively reduces the lowpass cutoff,
	/// making the sound progressively more muffled.
	/// </summary>
	[Export(PropertyHint.Range, "1, 16, 1")]
	public int MaxOcclusionHits
	{
		get => _maxOcclusionHits;
		set => _maxOcclusionHits = value;
	}

	private float _fallbackTransmission = 0.030f;
	/// <summary>
	/// Fraction of sound that passes through surfaces without an <see cref="AcousticBody"/>.
	/// <para><c>0.0</c> = fully blocked</para>
	/// <c>1.0</c> = fully transparent
	/// <para>Mirrors the <see cref="AcousticMaterial.TransmissionHigh"/> band.</para>
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 1.0f, 0.001f")]
	public float FallbackTransmission
	{
		get => _fallbackTransmission;
		set => _fallbackTransmission = value;
	}

	private float _occlusionVolumeStrength = 0.35f;
	/// <summary>
	/// How strongly walls reduce volume (in addition to <see cref="AudioEffectLowPassFilter"/>).
	/// <para><c>0.0</c> = no volume reduction at all, only filtering.</para>
	/// <c>1.0</c> = full physically-derived dB loss per wall.
	/// <para>Values around <c>0.3</c>-<c>0.5</c> sound natural for most games.
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 1.0f, 0.01f")]
	public float OcclusionVolumeStrength
	{
		get => _occlusionVolumeStrength;
		set => _occlusionVolumeStrength = value;
	}

	private float _maxOcclusionVolumeReduction = 18.0f;
	/// <summary>
	/// Maximum combined volume reduction (in dB) that wall occlusion can apply.
	/// Prevents the sound from going completely silent behind many walls.
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 60.0f, 0.5f, suffix:dB")]
	public float MaxOcclusionVolumeReduction
	{
		get => _maxOcclusionVolumeReduction;
		set => _maxOcclusionVolumeReduction = value;
	}

	private ulong _occlusionCollisionMask = 1 << 0;
	/// <summary>
	/// Physics layers the occlusion raycast collides with.
	/// Can differ from reverb if e.g. thin walls should 
	/// occlude but not affect perceived room size.
	/// </summary>
	[Export(PropertyHint.Layers3DPhysics)]
	public ulong OcclusionCollisionMask
	{
		get => _occlusionCollisionMask;
		set => _occlusionCollisionMask = value;
	}

	private bool _ignoreListenerBody = true;
	/// <summary>
	/// When enabled, the listener's <c>CharacterBody3D</c> (if any) is
	/// automatically excluded from occlusion raycasts. 
	/// This prevents the player's own collision shapes from being detected as walls.
	/// <para>Detection walks up the scene tree from the active <c>Camera3D</c> 
	/// looking for the first <c>CharacterBody3D</c> ancestor, then excludes
	/// it and all of its collision shape children.</para>
	/// </summary>
	[Export]
	public bool IgnoreListenerBody
	{
		get => _ignoreListenerBody;
		set => _ignoreListenerBody = value;
	}

	#endregion

	#region Exports - Attenuation

	[ExportGroup("Attenuation")]

	private bool _enableVolumeAttenuation = true;
	/// <summary>
	/// When enabled, volume is attenuated based on inner / outer
	/// radius and the chosen attenuation function.
	/// <para><b>Note:</b> This overrides Godot's built-in distance attenuation model
	/// at runtime so there is no doulbe-falloff.</para>
	/// </summary>
	[Export]
	public bool EnableVolumeAttenuation
	{
		get => _enableVolumeAttenuation;
		set
		{
			_enableVolumeAttenuation = value;
#if TOOLS
			NotifyPropertyListChanged();
#endif
		}
	}

	private float _innerRadius = 2.0f;
	/// <summary>
	/// The radius around the emitter inside which the sound 
	/// plays at full volume (completely unattenuated).
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 4096.0f, 0.01f, suffix:m")]
	public float InnerRadius
	{
		get => _innerRadius;
		set => _innerRadius = value;
	}

	private float _falloffDistance = 2.0f;
	/// <summary>
	/// The distance beyond the inner radius over which the sound
	/// fades from full volume to silence. The outer boundary equals
	/// <see cref="InnerRadius"/> + <see cref="FalloffDistance"/>.
	/// </summary>
	[Export(PropertyHint.Range, "0.01f, 4096.0f, 0.01f, suffix:m")]
	public float FalloffDistance
	{
		get => _falloffDistance;
		set => _falloffDistance = value;
	}

	/// <summary>
	/// Options for the falloff curve used to calculate volume 
	/// attenuation between the inner radius and the outer boundary.
	/// </summary>
	public enum AttenuationFunctionType
	{
		/// <summary>
		/// Volume decreases at a constant rate with distance.
		/// </summary>
		Linear,
		/// <summary>
		/// Greater volume changes at close distances, lesser at far.
		/// </summary>
		Logarithmic,
		/// <summary>
		/// Like Log but more exaggerated; only audible very close.
		/// </summary>
		Inverse,
		/// <summary>
		/// Lesser volume changes close, dramatic changes far away.
		/// </summary>
		LogReverse,
		/// <summary>
		/// Middle-ground between Log and Inverse; closest to reality.
		/// </summary>
		NaturalSound,
		/// <summary>
		/// Use a custom attenuation curve provided by the user.
		/// </summary>
		UserDefined,
	}
	private AttenuationFunctionType _attenuationFunction = AttenuationFunctionType.Linear;
	/// <summary>
	/// The curve that controls how quickly volume drops between the
	/// inner radius and outer boundary. Select <see cref="AttenuationFunctionType.UserDefined"/>
	/// to use a custom curve.
	/// </summary>
	[Export]
	public AttenuationFunctionType AttenuationFunction
	{
		get => _attenuationFunction;
		set
		{
			_attenuationFunction = value;
#if TOOLS
			NotifyPropertyListChanged();
#endif
		}
	}

	private Curve _userAttenuationCurve = null;
	/// <summary>
	/// Custom attenuation curve used when <see cref="AttenuationFunctionType.UserDefined"/>
	/// is selected.
	/// <para>X Axis: Normalized distance <c>0</c> = inner, <c>1</c> = outer.</para>
	/// Y Axis: Volume <c>1</c> = full, <c>0</c> = silent.
	/// </summary>
	[Export]
	public Curve UserAttenuationCurve
	{
		get => _userAttenuationCurve;
		set => _userAttenuationCurve = value;
	}

	#endregion

	#region Exports - Air Absorption

	[ExportGroup("Air Absorption")]

	private bool _enableAirAbsorption = false;
	/// <summary>
	/// When enabled, a distance-based lowpass filter simulates 
	/// how air absorbs high-frequency sound energy over distance.
	/// This is independent of wall occlution and stacks with it.
	/// </summary>
	[Export]
	public bool EnableAirAbsorption
	{
		get => _enableAirAbsorption;
		set
		{
			_enableAirAbsorption = value;
#if TOOLS
			NotifyPropertyListChanged();
#endif
		}
	}

	private float _airAbsorptionMinDistance = 2.0f;
	/// <summary>
	/// Distance from the emitter at which air absorption filtering begins.
	/// Below this distance no filtering is applied.
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 4096.0f, 0.01f, suffix:m")]
	public float AirAbsorptionMinDistance
	{
		get => _airAbsorptionMinDistance;
		set => _airAbsorptionMinDistance = value;
	}

	private float _airAbsorptionMaxDistance = 100.0f;
	/// <summary>
	/// Distance from the emitter at which air absorption filtering reaches its
	/// maximum effect. The filter interpolates between min and max cutoff
	/// frequencies over this range.
	/// </summary>
	[Export(PropertyHint.Range, "0.01f, 4096.0f, 0.01f, suffix:m")]
	public float AirAbsorptionMaxDistance
	{
		get => _airAbsorptionMaxDistance;
		set => _airAbsorptionMaxDistance = value;
	}

	private int _airAbsorptionCutoffFreqMin = 20000;
	/// <summary>
	/// Lowpass cutoff frequency at the minimum distance (closest to the source).
	/// Higher values mean less filtering when nearby — recommended to keep
	/// close to 20 000 Hz.
	/// </summary>
	[Export(PropertyHint.None, "suffix:Hz")]
	public int AirAbsorptionCutoffFreqMin
	{
		get => _airAbsorptionCutoffFreqMin;
		set => _airAbsorptionCutoffFreqMin = value;
	}

	private int _airAbsorptionCutoffFreqMax = 4000;
	/// <summary>
	/// Lowpass cutoff frequency at the maximum distance (furthest from the source).
	/// Lower values produce heavier muffling at long range.
	/// </summary>
	[Export(PropertyHint.None, "suffix:Hz")]
	public int AirAbsorptionCutoffFreqMax
	{
		get => _airAbsorptionCutoffFreqMax;
		set => _airAbsorptionCutoffFreqMax = value;
	}

	private bool _airAbsorptionLogFreqScaling = true;
	/// <summary>
	/// When enabled, the filter cutoff interpolation uses a logarithmic frequency
	/// scale instead of linear. This produces a perceptually smoother sweep
	/// that better matches how we perceive pitch.
	/// </summary>
	[Export]
	public bool AirAbsorptionLogFreqScaling
	{
		get => _airAbsorptionLogFreqScaling;
		set => _airAbsorptionLogFreqScaling = value;
	}

	#endregion

	#region Exports - Sound Speed Delay

	[ExportGroup("Sound Speed Delay")]

	private bool _enableSoundDelay = false;
	/// <summary>
	/// ## When enabled, playback is delayed based on the distance between the
	/// emitter and the listener divided by [param speed_of_sound], simulating
	/// the finite travel time of sound through air.
	/// <para>
	/// Only affects the initial <c>Play()</c> call. Once playback has started
	/// it runs at normal speed. Best suited for one-shot sounds (gunshots,
	/// explosions, impacts).
	/// </para>
	/// </summary>
	[Export]
	public bool EnableSoundDelay
	{
		get => _enableSoundDelay;
		set
		{
			_enableSoundDelay = value;
#if TOOLS
			NotifyPropertyListChanged();
#endif
		}
	}

	private float _speedOfSound = 343.0f;
	/// <summary>
	/// The speed at which sound travels, in metres per second.
	/// Earth sea-level is ~343 m/s. Lower values exaggerate the delay.
	/// </summary>
	[Export(PropertyHint.Range, "10.0f, 2000.0f, 1.0f, suffix:m/s")]
	public float SpeedOfSound
	{
		get => _speedOfSound;
		set => _speedOfSound = value;
	}

	#endregion

	#region Exports - Advanced

	[ExportGroup("Advanced")]

	private float _minimumVolumeDb = -80.0f;
	/// <summary>
	/// The volume this emitter fades up from when it first becomes active, before
	/// the initial geometry scan completes.
	/// </summary>
	[Export(PropertyHint.Range, "-80.0f, 80.0f, 0.1f, suffix:dB")]
	public float MinimumVolumeDb
	{
		get => _minimumVolumeDb;
		set => _minimumVolumeDb = value;
	}

	private bool _autoplayFadeIn = true;
	/// <summary>
	/// When true and the node's <c>autoplay</c> is enabled, the player will start at
	/// <c>minimum_volume_db</c> on ready and lerp up once the first geometry scan
	/// completes. Toggle this to disable the automatic silent startup for autoplay.
	/// </summary>
	[Export]
	public bool AutoplayFadeIn
	{
		get => _autoplayFadeIn;
		set
		{
			_autoplayFadeIn = value;
#if TOOLS
			NotifyPropertyListChanged();
#endif
		}
	}

	private float _autoplayFadeInSpeed = 6.0f;
	/// <summary>
	/// Speed used specifically for fading the volume in when <see cref="AutoplayFadeIn"/>
	/// is active. Separate from <see cref="LerpSpeed"/> so effect parameters can remain
	/// snappier while the audible fade is tuned independently.
	/// </summary>
	[Export(PropertyHint.Range, "0.1f, 40.0f, 0.1f")]
	public float AutoplayFadeInSpeed
	{
		get => _autoplayFadeInSpeed;
		set => _autoplayFadeInSpeed = value;
	}

	private float _lerpSpeed = 15.0f;
	/// <summary>
	/// How quickly effect values (lowpass, reverb wet, room size) interpolate
	/// toward their targets each frame. Higher = snappier, lower = smoother.
	/// </summary>
	[Export(PropertyHint.Range, "1.0f, 20.0f, 0.1f")]
	public float LerpSpeed
	{
		get => _lerpSpeed;
		set => _lerpSpeed = value;
	}

	/// <summary>
	/// Overrides the default listener target (the active <c>Camera3D</c>).
	/// </summary>
	[Export]
	public Node3D CustomListenerTarget { get; set; }

	private int _occludedLowpassCutoffMinimum = 600;
	/// <summary>
	/// Lowpass cutoff when the listener is fully occluded by a wall.
	/// Lower values produce a heavier, more muffled sound.
	/// </summary>
	[Export(PropertyHint.None, "suffix:Hz")]
	public int OccludedLowpassCutoffMinimum
	{
		get => _occludedLowpassCutoffMinimum;
		set => _occludedLowpassCutoffMinimum = value;
	}

	private int _openLowpassCutoff = 20000;
	/// <summary>
	/// Lowpass cutoff when the listener has clear line of sight to the emitter.
	/// </summary>
	[Export(PropertyHint.None, "suffix:Hz")]
	public int OpenLowpassCutoff
	{
		get => _openLowpassCutoff;
		set => _openLowpassCutoff = value;
	}

	private float _updateFrequency = 0.2f;
	/// <summary>
	/// How often geometry is re-sampled and audio effects are recalculated, in
	/// seconds. Increase for static or slow-moving emitters to save CPU.
	/// </summary>
	[Export(PropertyHint.Range, "0.01f, 1.0f, 0.01f, suffix:s")]
	public float UpdateFrequency
	{
		get => _updateFrequency;
		set => _updateFrequency = value;
	}

	/// <summary>
	/// Name prefix for the dynamically created audio bus. Will be a child of the selected bus in the AudioStreamPlayer3D settings.
	/// </summary>
	[Export]
	public string AudioBusPrefix = "SpatialBus";

	#endregion

	#region Exports - Debug

	[ExportGroup("Debug")]

	private bool _debugDrawRays = false;
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

	private bool _debugDrawRadius = false;
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

	private bool _debugDrawPlayingState = false;
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

	private bool _displayDebugInfo = false;
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

	#endregion

	#region Internal State

	// TODO: Internal

	#endregion

	#region Sound Speed Delay

	// TODO: Sound Speed Delay

	#endregion

	#region Lifecycle

	// TODO: Lifecycle

	#endregion

	#region Physics Process

	// TODO: Physics Process

	#endregion

	#region Utils

	// TODO: Utils

	#endregion

	#region  Parameter Lerping

	// TODO: Parameter Lerping

	#endregion

	#region  Spatial Audio Update

	// TODO: Spatial Audio Update

	#endregion

	#region Volume Attenuation

	// TODO: Volume Attenuation

	#endregion

	#region Reverb

	// TODO: Reverb

	#endregion

	#region Occlusion

	// TODO: Occlusion

	#endregion

	#region Debug Overlay

	// TODO: Debug Overlay

	#endregion

	#region Debug Drawing

	// TODO: Debug Drawing

	#endregion
}
