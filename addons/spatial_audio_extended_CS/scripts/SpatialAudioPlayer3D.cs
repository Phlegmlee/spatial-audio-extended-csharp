using System;
using System.Collections.Generic;
using Godot;
namespace SpatialAudioCS;

/// <summary>
/// TODO: Documentation
/// </summary>
[Tool, Icon("uid://diaqpjedywsus"), GlobalClass]
public partial class SpatialAudioPlayer3D : AudioStreamPlayer3D
{
	#region Public API

	/// <summary>
	/// Designed as an override to <see cref="AudioStreamPlayer3D.Play"/>.
	/// <para>When <see cref="EnableSoundDelay"/> is on, playback is delayed based on 
	/// listener distance and <see cref="SpeedOfSound"/></para>
	/// When <see cref="EnableSoundDelay"/>
	/// </summary>
	/// <param name="fromPosition">Position in seconds to start playing.</param>
	public void PlayWithDelay(float fromPosition = 0.0f)
	{
		if (Engine.IsEditorHint() || !EnableSoundDelay)
		{
			SetPlayInitiated();
			base.Play(fromPosition);
			EmitSignal(SignalName.SpatialAudioPlaybackStarted);
			return;
		}

		Node3D listener = GetListener();
		if (listener == null)
		{
			SetPlayInitiated();
			base.Play(fromPosition);
			return;
		}

		float distance = GlobalPosition.DistanceTo(listener.GlobalPosition);
		float delay = distance / SpeedOfSound;

		if (delay < 0.01)
		{
			SetPlayInitiated();
			base.Play(fromPosition);
			return;
		}

		SetPlayInitiated();

		if (pendingDelayTimer != null && pendingDelayTimer.TimeLeft > 0.0)
		{
			pendingDelayTimer.Timeout -= () => DeferredPlay(fromPosition);
		}

		pendingDelayTimer = GetTree().CreateTimer(delay);
		pendingDelayTimer.Timeout += () => DeferredPlay(fromPosition);
	}

	/// <summary>
	/// Override for base Stop() to emit signal.
	/// </summary>
	new public void Stop()
	{
		base.Stop();
		EmitSignal(SignalName.SpatialAudioPlaybackStopped);
	}

	#endregion

	#region Signals

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

#if DEBUG
	#region Signals - Debug

	/// <summary>
	/// Emitted when <see cref="EnableDebug"/> is toggled.
	/// </summary>
	/// <param name="value"></param>
	[Signal] public delegate void EnableDebugToggledEventHandler(bool value);

	#endregion
#endif
	#endregion

	#region Exports

	#region Exports - Rays

	/// <summary>
	/// Options for how the omni-directional
	/// room-sensing rays are distributed around the emitter.
	/// </summary>
	public enum RayDistributionEnum
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
	private RayDistributionEnum _rayDistribution = RayDistributionEnum.Classic;
	/// <summary>
	/// How the room-sensing rays are arranged around the emitter.
	/// <para>See <see cref="RayDistributionEnum"/> for options.</para>
	/// </summary>
	[Export]
	public RayDistributionEnum RayDistribution
	{
		get => _rayDistribution;
		set
		{
			_rayDistribution = value;
#if TOOLS
			NotifyPropertyListChanged();
			if (_setupComplete) RebuildRaycasts();
#endif
		}
	}

	private int _fibonacciRayCount = 8;
	/// <summary>
	/// Number of omni-directional rays when using 
	/// <see cref="RayDistributionEnum.FibonacciSphere"/> distribution.
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
			if (_setupComplete && RayDistribution == RayDistributionEnum.FibonacciSphere)
			{
				RebuildRaycasts();
			}
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
			if (_setupComplete && RayDistribution == RayDistributionEnum.ShapeScatter)
			{
				RebuildRaycasts();
			}
#endif
		}
	}

	private int _shapeRayCount = 32;
	/// <summary>
	/// Number of rays when using <see cref="RayDistributionEnum.ShapeScatter"/> random distribution.
	/// </summary>
	[Export(PropertyHint.Range, "1, 256, 1")]
	public int ShapeRayCount
	{
		get => _shapeRayCount;
		set
		{
			_shapeRayCount = value;
#if TOOLS
			if (_setupComplete && RayDistribution == RayDistributionEnum.ShapeScatter)
			{
				RebuildRaycasts();
			}
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
			if (_setupComplete && RayDistribution == RayDistributionEnum.ShapeScatter)
			{
				RebuildRaycasts();
			}
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
	/// <summary>
	/// The maximum distance raycasts will travel to sense geometry.
	/// <para>Keep this value close to <see cref="AudioStreamPlayer3D.MaxDistance"/>
	/// to avoid audible mismatches.</para>
	/// </summary>
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

	private uint _reverbCollisionMask = 1 << 0;
	/// <summary>
	/// Physics layers the room-sensing raycasts collide with.
	/// Should match the layers your level geometry occupies.
	/// </summary>
	[Export(PropertyHint.Layers3DPhysics)]
	public uint ReverbCollisionMask
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

	private uint _occlusionCollisionMask = 1 << 0;
	/// <summary>
	/// Physics layers the occlusion raycast collides with.
	/// Can differ from reverb if e.g. thin walls should 
	/// occlude but not affect perceived room size.
	/// </summary>
	[Export(PropertyHint.Layers3DPhysics)]
	public uint OcclusionCollisionMask
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
	public enum AttenuationFunctionEnum
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
	private AttenuationFunctionEnum _attenuationFunction = AttenuationFunctionEnum.Linear;
	/// <summary>
	/// The curve that controls how quickly volume drops between the
	/// inner radius and outer boundary. Select <see cref="AttenuationFunctionEnum.UserDefined"/>
	/// to use a custom curve.
	/// </summary>
	[Export]
	public AttenuationFunctionEnum AttenuationFunction
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
	/// Custom attenuation curve used when <see cref="AttenuationFunctionEnum.UserDefined"/>
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

	private float _innerRadiusPanningStrength = 1.0f;
	/// <summary>
	/// Panning strength multiplier applied when the listener is at the center 
	/// of the inner radius. The value interpolates back to default <c>1.0</c>
	/// at the inner radius edge.
	/// <para><c>0.0</c> = fully centered (no panning / non-directional).</para>
	/// <c>1.0</c> = default (no modification).
	/// <para><c>2.0</c> = exaggerated panning (full left/right).</para>
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 2.0f, 0.01f")]
	public float InnerRadiusPanningStrength
	{
		get => _innerRadiusPanningStrength;
		set => _innerRadiusPanningStrength = value;
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

#if DEBUG
	#region Exports - Debug

	[ExportGroup("Debug")]

	private bool _enableDebug = false;
	/// <summary>
	/// Toggle to add a debug node child to the audio player.
	/// </summary>
	[Export]
	public bool EnableDebug
	{
		get => _enableDebug;
		set
		{
			_enableDebug = value;
			EmitSignal(SignalName.EnableDebugToggled, value);
		}
	}

	#endregion
#endif
	#endregion

	#region Internal State

	internal List<RayCast3D> raycasts = [];
	internal List<float> distances = [];
	private List<string> _rayNames = [];

	private List<Vector3> _rayDirections = [];

	internal List<List<Vector3>> reflectionPaths = [];

	internal List<bool> reflectionEscaped = [];

	private List<float> _rayAbsorptions = [];

	private List<bool> _rayTotalAbsorption = [];

	private List<float> _rayTotalAbTrSpds = [];

	private List<string> _rayMaterialNames = [];

	internal RayCast3D targetRaycast = null;

	private const float _TotalAbsorpReverbWetCap = 0.05f;
	private const float _TotalAbsorpReverbDampFloor = 0.90f;
	private const float _TotalAbsorptionLowpassHz = 20.0f;
	private const float _TotalAbsorptionMuteDb = -120.0f;
	private const float _TotalAbsorpTransThresholdDb = -100.0f;
	private const float _DefaultTotalAbsorpTransSpeed = 2.5f;

	private readonly Dictionary<string, Vector3[]> _classicRays = new()
	{
		{ "Left", [Vector3.Right, Vector3.Zero]},
		{ "Right", [Vector3.Left, Vector3.Zero]},
		{ "Forward", [Vector3.Back, Vector3.Zero]},
		{ "ForwardLeft", [Vector3.Back, new Vector3(0, 45, 0)]},
		{ "ForwardRight", [Vector3.Back, new Vector3(0, -45, 0)]},
		{ "Backward", [Vector3.Forward, Vector3.Zero]},
		{ "BackwardLeft", [Vector3.Forward, new Vector3(0, -45, 0)]},
		{ "BackwardRight", [Vector3.Forward, new Vector3(0, 45, 0)]},
		{ "Up", [Vector3.Up, Vector3.Zero]},
		{ "Down", [Vector3.Down, Vector3.Zero]},
	};

	private double _lastUpdateTime = 0.0;
	private bool _setupComplete = false;
	private bool _initialScanDone = false;
	private bool _autoplayFadeActive = false;

	private bool _wasInsideInner = false;
	private bool _wasInFalloff = false;
	private bool _wasAudible = false;
	private float _lastListenerDistance = -1.0f;

	private float _lastReverbRoomSize = -1.0f;
	private float _lastReverbWetness = -1.0f;
	private float _lastReverbDamping = -1.0f;
	private float _lastAirAbsorptionCutoff = -1.0f;

	private string _busName = "";
	private int _busIndex = 1;
	internal AudioEffectReverb reverbEffect = null;
	internal AudioEffectLowPassFilter lowpassFilter = null;

	internal float targetLowpassCutoff = 20000.0f;
	internal float targetReverbRoomSize = 0.0f;
	internal float targetReverbWetness = 0.0f;
	internal float targetReverbDamping = 0.0f;
	internal float targetVolumeDb = 0.0f;

	private float _openness = 0.0f;

	private float _basePanningStrength = 1.0f;
	internal float targetPanningStrength = 1.0f;

	internal float targetAirAbsorpCutoff = 20000.0f;

	internal SceneTreeTimer pendingDelayTimer = null;

	private ulong _playInitiatedTime = 1;

	private int _playInitiatedDuration = 0;

	internal int lastWallCount = 0;

	internal string[] lastWallMaterials = [];

	internal float[] lastWallAbsorptions = [];

	private float _baseVolumeDb = 0.0f;

	private float _externalVolumeDbOffset = 0.0f;

	private ulong _externalOcclusionHoldUntilMsec = 0;

	private bool _hardMutedByTotalAbsorp = false;

	private float _activeTotalAbsorpTransSpeed = _DefaultTotalAbsorpTransSpeed;

#if TOOLS
	internal EditorInterface editorInterface = null;
#endif

	#endregion

	#region Internal API

	/// <summary>
	/// Allows external systems to apply extra dB reduction (loudness loss)
	/// without fighting <see cref="SpatialAudioPlayer3D"/>s internal attenuation.
	/// <para>See <see cref="SpatialReflectionNavigationAgent3D"/></para>
	/// </summary>
	/// <param name="offset">Extra dB reduction desired.</param>
	internal void SetExternalVolumeDbOffset(float offset)
	{
		_externalVolumeDbOffset = offset;
	}

	internal float GetExternalVolumeDbOffset()
	{
		return _externalVolumeDbOffset;
	}

	internal void ClearExternalVolumeDbOffset()
	{
		_externalVolumeDbOffset = 0.0f;
	}

	/// <summary>
	/// Temporarily forces occlusion open. 
	/// Intended for proxy-transition smoothing.
	/// </summary>
	/// <param name="seconds"></param>
	internal void SetExternalOcclusionHold(float seconds)
	{
		ulong durationMs = (ulong)Math.MaxMagnitude(seconds, 0.0) * 1000;
		if (durationMs <= 0) return;

		ulong until = Time.GetTicksMsec() + durationMs;
		_externalOcclusionHoldUntilMsec = Math.Max(_externalOcclusionHoldUntilMsec, until);
	}

	internal void ClearExternalOcclusionHold()
	{
		_externalOcclusionHoldUntilMsec = 0;
	}

	internal bool IsExternalOcclusionHeld()
	{
		return Time.GetTicksMsec() < _externalOcclusionHoldUntilMsec;
	}

	#endregion

	#region Gameplay Logic

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

		_baseVolumeDb = VolumeDb;
		targetVolumeDb = VolumeDb;
		_basePanningStrength = PanningStrength;
		targetPanningStrength = PanningStrength;

		if (Autoplay && AutoplayFadeIn)
		{
			VolumeDb = MinimumVolumeDb;
			_autoplayFadeActive = true;
		}

		if (EnableVolumeAttenuation)
		{
			AttenuationModel = AttenuationModelEnum.Disabled;
		}

		_busName = AudioBusPrefix + "#" + new Random().Next(1, 1000).ToString();
		AudioServer.AddBus();
		_busIndex = AudioServer.BusCount - 1;
		AudioServer.SetBusName(_busIndex, _busName);
		AudioServer.SetBusSend(_busIndex, Bus);
		this.Bus = _busName;

		AudioServer.AddBusEffect(_busIndex, new AudioEffectReverb(), 0);
		reverbEffect = AudioServer.GetBusEffect(_busIndex, 0) as AudioEffectReverb;

		AudioServer.AddBusEffect(_busIndex, new AudioEffectLowPassFilter(), 1);
		lowpassFilter = AudioServer.GetBusEffect(_busIndex, 1) as AudioEffectLowPassFilter;
	}

	private void RebuildRaycasts()
	{
		foreach (RayCast3D ray in raycasts)
		{
			if (ray != null && ray.IsInsideTree())
			{
				RemoveChild(ray);
				ray.QueueFree();
			}
		}

		ClearRayMetadata();

		// Generate rays based on RayDistribution
		switch (RayDistribution)
		{
			case RayDistributionEnum.Classic:
				foreach (string key in _classicRays.Keys)
				{
					RayCast3D ray = new()
					{
						Name = key,
						TargetPosition = _classicRays[key][0] * MaxRaycastDistance,
						RotationDegrees = _classicRays[key][1],
						CollisionMask = ReverbCollisionMask,
						DebugShapeThickness = 0,
					};

					// Compute world-space direction accounting for rotation.
					Vector3 rotDeg = _classicRays[key][1];
					Vector3 rotRad = DegToRad(rotDeg);

					Basis basis = Basis.FromEuler(rotRad);
					Vector3 dir = (basis * _classicRays[key][0]).Normalized();

					AddRayMetadata(ray, key, dir);
					AddChild(ray, false, InternalMode.Front);
				}
				break;

			case RayDistributionEnum.FibonacciSphere:
				Vector3[] fibDirs = GenerateFibonacciSphere(FibonacciRayCount);
				for (int i = 0; i < fibDirs.Length; i++)
				{
					string rayName = $"Fib_{i}";
					RayCast3D ray = new()
					{
						Name = $"Fib_{i}",
						TargetPosition = fibDirs[i] * MaxRaycastDistance,
						CollisionMask = ReverbCollisionMask,
						DebugShapeThickness = 0,
					};

					AddRayMetadata(ray, rayName, fibDirs[i].Normalized());
					AddChild(ray, false, InternalMode.Front);
				}
				break;

			case RayDistributionEnum.ShapeScatter:
				Vector3 shapeOrigin = GlobalPosition;
				float radius = 0.5f;
				if (ScatterShape != null && ScatterShape.IsInsideTree())
				{
					shapeOrigin = ScatterShape.GlobalPosition;
					Vector3 scale = ScatterShape.Scale;
					Vector3 absScale = new(Math.Abs(scale.X), Math.Abs(scale.Y), Math.Abs(scale.Z));
					radius *= Math.Max(Math.Max(absScale.X, absScale.Y), absScale.Z);
				}

				for (int i = 0; i < ShapeRayCount; i++)
				{
					Vector3 spawnDir = RandomUnitVector();
					Vector3 spawnOrigin = shapeOrigin + spawnDir * radius * new Random().NextSingle();

					Vector3 centerToPoint = (spawnOrigin - shapeOrigin).Normalized();

					Vector3 randDir = RandomUnitVector();

					float blend = (float)Math.Clamp(ShapeScatterRandomness / 50.0, 0.0, 1.0);
					Vector3 dir = (centerToPoint * (1.0f - blend) + randDir * blend).Normalized();

					string rayName = $"Shape_{i}";
					RayCast3D ray = new()
					{
						Name = rayName,
						GlobalTransform = new Transform3D(new Basis(), spawnOrigin),
						TargetPosition = dir * MaxRaycastDistance,
						CollisionMask = ReverbCollisionMask,
						DebugShapeThickness = 0,
					};

					AddRayMetadata(ray, rayName, dir);
					AddChild(ray, false, InternalMode.Front);
				}
				break;
		}

		// Target-ray, always added last, for wall-occlusion.
		RayCast3D targetRay = new()
		{
			Name = nameof(targetRay),
			TargetPosition = Vector3.Back * MaxRaycastDistance,
			CollisionMask = OcclusionCollisionMask,
			DebugShapeThickness = 0
		};

		targetRaycast = targetRay;
		AddRayMetadata(targetRay, nameof(targetRay), Vector3.Forward);
		AddChild(targetRay, false, InternalMode.Front);
	}

	#region Physics Process

	public override void _PhysicsProcess(double delta)
	{
		if (!_setupComplete) return;

		_lastUpdateTime += delta;

		if (_lastUpdateTime > UpdateFrequency)
		{
			if (RoomSizeReverb) UpdateReverbRays();
			Node3D listener = GetListener();
			if (listener != null) OnSpatialAudioUpdate(listener);
			if (!_initialScanDone && !Engine.IsEditorHint())
			{
				_initialScanDone = true;
				SnapParameters(!_autoplayFadeActive);
			}
			_lastUpdateTime = 0.0;
		}

		if (!Engine.IsEditorHint()) LerpParameters(delta);
	}

	private void UpdateReverbRays()
	{
		for (int i = 0; i < distances.Count; i++)
		{
			UpdateOmniDistance(raycasts[i], i);
		}
	}

	#endregion

	#region  Parameter Lerping

	private void LerpParameters(double delta)
	{
		float tDelta = (float)delta;
		float target = Math.Clamp(tDelta * LerpSpeed, 0.0f, 1.0f);
		float tTotalAbsorp = Math.Clamp(tDelta * _activeTotalAbsorpTransSpeed, 0.0f, 1.0f);
		bool totalAbsorpTrans = _hardMutedByTotalAbsorp
		|| targetVolumeDb <= _TotalAbsorpTransThresholdDb
		|| VolumeDb <= _TotalAbsorpTransThresholdDb;

		float tVolume = target;
		if (_autoplayFadeActive) tVolume = Math.Clamp(tDelta * AutoplayFadeInSpeed, 0.0f, 1.0f);
		else if (totalAbsorpTrans) tVolume = tTotalAbsorp;

		VolumeDb = float.Lerp(VolumeDb, targetVolumeDb, tVolume);
		PanningStrength = float.Lerp(PanningStrength, targetPanningStrength, target);

		if (lowpassFilter != null)
		{
			float combCutoff = MathF.Min(targetLowpassCutoff, targetAirAbsorpCutoff);
			float curCut = MathF.Max(1.0f, lowpassFilter.CutoffHz);
			float targetCut = MathF.Max(1.0f, combCutoff);
			float tFilter = target;
			if (totalAbsorpTrans) tFilter = tTotalAbsorp;
			lowpassFilter.CutoffHz = MathF.Exp(float.Lerp(MathF.Log(curCut), MathF.Log(targetCut), tFilter));
		}

		if (reverbEffect != null)
		{
			float tWet = 0.0f, tRvb = target;
			if (!_hardMutedByTotalAbsorp) tWet = targetReverbWetness * MaxReverbWetness;
			if (totalAbsorpTrans) tRvb = tTotalAbsorp;
			reverbEffect.Wet = float.Lerp(reverbEffect.Wet, tWet, tRvb);
			reverbEffect.RoomSize = float.Lerp(reverbEffect.RoomSize, targetReverbRoomSize, target);
			reverbEffect.Damping = float.Lerp(reverbEffect.Damping, targetReverbDamping, target);
		}

		if (_autoplayFadeActive)
		{
			if (MathF.Abs(VolumeDb - targetVolumeDb) < 0.1f)
			{
				VolumeDb = targetVolumeDb;
				_autoplayFadeActive = false;
			}
		}
	}

	private void SnapParameters(bool snapVolume = true)
	{
		if (snapVolume && _hardMutedByTotalAbsorp) VolumeDb = _TotalAbsorptionMuteDb;
		else VolumeDb = targetVolumeDb;

		PanningStrength = targetPanningStrength;

		if (lowpassFilter != null)
		{
			float combCutoff = MathF.Min(targetLowpassCutoff, targetAirAbsorpCutoff);
			lowpassFilter.CutoffHz = MathF.Max(1.0f, combCutoff);
		}

		if (reverbEffect != null)
		{
			if (_hardMutedByTotalAbsorp) reverbEffect.Wet = 0.0f;
			else reverbEffect.Wet = targetReverbWetness * MaxReverbWetness;
			reverbEffect.RoomSize = targetReverbRoomSize;
			reverbEffect.Damping = targetReverbDamping;
		}
	}

	#endregion

	#region  Spatial Audio Update

	private void OnSpatialAudioUpdate(Node3D listener)
	{
		float dist = listener.GlobalPosition.DistanceTo(GlobalPosition);

		UpdateVolumeAttenuation(listener, dist);
		UpdatePanningStrength(listener, dist);
		UpdateAirAbsorption(listener, dist);
		UpdateReverb();
		UpdateLowpass(listener);
	}

	#endregion

	#region Volume Attenuation

	private void UpdateVolumeAttenuation(Node3D listener, float distance)
	{
		if (!EnableVolumeAttenuation)
		{ targetVolumeDb = ApplyExternalVolumeOffset(_baseVolumeDb); return; }

		if (_lastListenerDistance < 0.0f || MathF.Abs(distance - _lastListenerDistance) >= 0.5f)
		{
			_lastListenerDistance = distance;
			EmitSignal(SignalName.ListenerDistanceChanged, distance);
		}

		float outerRadius = InnerRadius + FalloffDistance;
		bool insideInner = distance <= InnerRadius;
		bool inFalloff = distance > InnerRadius && distance <= outerRadius;
		bool audible = distance <= outerRadius;

		if (insideInner && !_wasInsideInner) EmitSignal(SignalName.InnerRadiusEntered, listener);
		if (!insideInner && _wasInsideInner) EmitSignal(SignalName.InnerRadiusExited, listener);

		if (inFalloff && !_wasInFalloff) EmitSignal(SignalName.FalloffZoneEntered, listener);
		if (!inFalloff && _wasInFalloff) EmitSignal(SignalName.FalloffZoneExited, listener);

		if (audible && !_wasAudible) EmitSignal(SignalName.AttenuationZoneEntered, listener);
		if (!audible && _wasAudible) EmitSignal(SignalName.FalloffZoneExited, listener);

		_wasInsideInner = insideInner;
		_wasInFalloff = inFalloff;
		_wasAudible = audible;

		if (insideInner) { targetVolumeDb = ApplyExternalVolumeOffset(_baseVolumeDb); return; }
		if (distance >= outerRadius) { targetVolumeDb = ApplyExternalVolumeOffset(MinimumVolumeDb); return; }

		float alpha = (distance - InnerRadius) / FalloffDistance;
		float atten = ApplyAttenuationFunction(alpha);

		float minGain = MathF.Pow(10.0f, MinimumVolumeDb / 20.0f);
		float baseGain = MathF.Pow(10.0f, _baseVolumeDb / 20.0f);
		float gain = float.Lerp(minGain, baseGain, atten);
		gain = MathF.Max(gain, (float)1e-8);
		targetVolumeDb = ApplyExternalVolumeOffset(20.0f * MathF.Log(gain) / MathF.Log(10.0f));
	}

	private void UpdatePanningStrength(Node3D listener, float distance)
	{
		if (!EnableVolumeAttenuation || Mathf.IsEqualApprox(InnerRadiusPanningStrength, 1.0f))
		{
			targetPanningStrength = _basePanningStrength;
			return;
		}

		if (distance >= InnerRadius || InnerRadius <= 0.0f)
		{
			targetPanningStrength = _basePanningStrength;
			return;
		}

		float ratio = distance / InnerRadius;
		float centerPan = Math.Clamp(_basePanningStrength * InnerRadiusPanningStrength, 0.0f, 2.0f);
		targetPanningStrength = float.Lerp(centerPan, _basePanningStrength, ratio);
	}

	private void UpdateAirAbsorption(Node3D listener, float distance)
	{
		if (!EnableAirAbsorption) { targetAirAbsorpCutoff = 20000.0f; return; }

		if (distance <= AirAbsorptionMinDistance)
		{ targetAirAbsorpCutoff = AirAbsorptionCutoffFreqMin; return; }

		if (distance >= AirAbsorptionMaxDistance)
		{ targetAirAbsorpCutoff = AirAbsorptionCutoffFreqMax; return; }

		float alpha = (distance - AirAbsorptionMinDistance) / MathF.Max(AirAbsorptionMaxDistance - AirAbsorptionMinDistance, 0.001f);

		if (AirAbsorptionLogFreqScaling)
		{
			float logMin = MathF.Log(MathF.Max(AirAbsorptionCutoffFreqMin, 1.0f));
			float logMax = MathF.Log(MathF.Max(AirAbsorptionCutoffFreqMax, 1.0f));
			targetAirAbsorpCutoff = MathF.Exp(float.Lerp(logMin, logMax, alpha));
		}
		else
		{
			targetAirAbsorpCutoff = float.Lerp(AirAbsorptionCutoffFreqMin, AirAbsorptionCutoffFreqMax, alpha);
		}

		if (MathF.Abs(targetAirAbsorpCutoff - _lastAirAbsorptionCutoff) > 1.0f)
		{
			EmitSignal(SignalName.AirAbsorptionUpdated, targetAirAbsorpCutoff);
			EmitSignal(SignalName.AirAbsorptionZoneChanged, targetAirAbsorpCutoff);
			_lastAirAbsorptionCutoff = targetAirAbsorpCutoff;
		}
	}

	private float ApplyAttenuationFunction(float alpha)
	{
		switch (AttenuationFunction)
		{
			case AttenuationFunctionEnum.Linear:
				return 1.0f - alpha;

			case AttenuationFunctionEnum.Logarithmic:
				return 1.0f - MathF.Log(alpha * 9.0f + 1.0f) / MathF.Log(10.0f);

			case AttenuationFunctionEnum.Inverse:
				return 1.0f / 1.0f + alpha * 9.0f;

			case AttenuationFunctionEnum.LogReverse:
				return MathF.Log(1.0f + (1.0f - alpha) * 9.0f) / MathF.Log(10.0f);

			case AttenuationFunctionEnum.NaturalSound:
				return MathF.Pow(1.0f - alpha, 1.5f);

			case AttenuationFunctionEnum.UserDefined:
				if (UserAttenuationCurve != null)
					return Math.Clamp(UserAttenuationCurve.Sample(Math.Clamp(alpha, 0.0f, 1.0f)), 0.0f, 1.0f);
				return 1.0f - alpha;

			default:
				return 1.0f - alpha;
		}
	}

	#endregion

	#region Reverb

	private void UpdateOmniDistance(RayCast3D ray, int index)
	{
		ray.ForceRaycastUpdate();
		reflectionPaths[index] = [];
		reflectionEscaped[index] = false;
		_rayAbsorptions[index] = -1.0f;

		if (ray.GetCollider() == null)
		{
			distances[index] = MaxRaycastDistance;
			reflectionEscaped[index] = true;
			ray.Enabled = false;
			return;
		}

		Vector3 firstHit = ray.GetCollisionPoint();
		float totalDist = GlobalPosition.DistanceTo(firstHit);

		// Sample absorption from first-hit surface (if it has one)
		float firstAbsorp = -1.0f;
		string firstMatName = "";
		Node collider = ray.GetCollider() as Node;
		AcousticBody ab = AcousticBody.FindForRayCollider(collider);
		if (ab != null && ab.AcousticMaterial != null)
		{
			AcousticMaterial mat = ab.AcousticMaterial;
			_rayTotalAbsorption[index] = mat.TotalAbsorption;
			if (mat.TotalAbsorption) _rayTotalAbTrSpds[index] = mat.TotalAbsorptionTransitionSpeed;

			if (SurfaceAbsorption)
			{
				firstAbsorp = (mat.AbsorptionLow + mat.AbsorptionMid + mat.AbsorptionHigh) / 3.0f;
				if (mat.TotalAbsorption) firstAbsorp = 1.0f;
			}

			if (mat.ResourceName == "")
			{
				firstMatName = mat.ResourcePath.GetFile().GetBaseName();
			}
			else firstMatName = mat.ResourceName;
		}
		_rayAbsorptions[index] = firstAbsorp;
		_rayMaterialNames[index] = firstMatName;

		// Reflections below, check if valid to continue.
		if (RayDistribution != RayDistributionEnum.FibonacciSphere || FibonacciRayReflections <= 0)
		{
			distances[index] = totalDist;
			ray.Enabled = false;
			return;
		}

		// Trace reflections using physics space
		PhysicsDirectSpaceState3D space = GetWorld3D().DirectSpaceState;
		PhysicsRayQueryParameters3D parameters = new() { CollisionMask = ReverbCollisionMask, };

		List<Vector3> path = [GlobalPosition, firstHit];
		Vector3 curPos = firstHit;
		Vector3 incomDir = (firstHit - GlobalPosition).Normalized();
		Vector3 curNorm = ray.GetCollisionNormal();
		float remainDist = MaxRaycastDistance - totalDist;

		// Accumulate absorp across all bounces
		float absropSum = MathF.Max(firstAbsorp, 0.0f);
		int absorpCount = 0;
		if (firstAbsorp >= 0.0f) absorpCount = 1;

		for (int i = 0; i < FibonacciRayReflections; i++)
		{
			if (remainDist <= 0.01f) break;

			// Reflect about surfact normal
			Vector3 reflectDir = (incomDir - 2.0f * incomDir.Dot(curNorm) * curNorm).Normalized();

			// Offset to avoid self-intersection.
			parameters.From = curPos + curNorm * 0.01f;
			parameters.To = parameters.From + reflectDir * remainDist;

			Godot.Collections.Dictionary result = space.IntersectRay(parameters);
			if (result.Count <= 0)
			{
				Vector3 escapeEnd = parameters.From + reflectDir * remainDist;
				path.Add(escapeEnd);
				reflectionEscaped[index] = true;
				break;
			}

			Vector3 hitPoint = (Vector3)result["position"];
			float segDist = curPos.DistanceTo(hitPoint);
			totalDist += segDist;
			remainDist -= segDist;
			path.Add(hitPoint);

			// Emit signal for reverb/reflection ray collision
			Node bounceCollider = (Node)result["collider"];
			EmitSignal(SignalName.ReverbRayCollided, hitPoint, curPos, bounceCollider);

			// Sample absorp from the bounced surface (if mat present)
			AcousticBody bounceAb = AcousticBody.FindForRayCollider(bounceCollider);
			if (bounceAb == null || bounceAb.AcousticMaterial == null) break;

			AcousticMaterial bounceMat = bounceAb.AcousticMaterial;
			if (bounceMat.TotalAbsorption)
			{
				_rayTotalAbsorption[index] = true;
				_rayTotalAbTrSpds[index] = MathF.Min
				(
					_rayTotalAbTrSpds[index],
					bounceMat.TotalAbsorptionTransitionSpeed
				);
			}

			if (SurfaceAbsorption)
			{
				float bAbsorp = (bounceMat.AbsorptionLow + bounceMat.AbsorptionMid + bounceMat.AbsorptionHigh) / 3.0f;
				if (bounceMat.TotalAbsorption) bAbsorp = 1.0f;
				absropSum += bAbsorp;
				absorpCount += 1;
			}

			incomDir = reflectDir;
			curNorm = (Vector3)result["normal"];
			curPos = hitPoint;
		}

		if (SurfaceAbsorption && absorpCount > 0)
		{ _rayAbsorptions[index] = absropSum / absorpCount; }

		distances[index] = totalDist;
		reflectionPaths[index] = path;
		ray.Enabled = false;
	}

	// FIXME: I changed A LOT about this function on rewrite, ensure it works as intended.
	private void UpdateReverb()
	{
		if (reverbEffect == null) return;

		if (!RoomSizeReverb)
		{
			targetReverbWetness = 0.0f;
			targetReverbRoomSize = 0.0f;
			targetReverbDamping = 0.0f;
			_openness = 0.0f;
			return;
		}


		int omniCount = Math.Max(distances.Count - 1, 1);
		int activeCount = 0;
		float roomSize = 0.0f, floorCosTh = MathF.Cos(FloorAngleThreshold * (MathF.PI / 180f));

		int abCount = 0;
		bool totalAbHit = false;
		float abSum = 0.0f, totalAbTrSp = _DefaultTotalAbsorpTransSpeed;

		float opennessSum = 0.0f;

		float activeF = 0.0f;

		for (int i = 0; i < omniCount; i++)
		{
			if (IgnoreFloor && i < _rayDirections.Count
				&& (_rayDirections[i].Y <= -floorCosTh)) continue;

			activeCount += 1;
			float d = distances[i];
			bool escaped = i < reflectionEscaped.Count && reflectionEscaped[i];
			totalAbHit = i < _rayTotalAbsorption.Count && _rayTotalAbsorption[i];

			activeF = MathF.Max(activeCount, 1);

			if (escaped)
			{
				opennessSum += 1.0f;
				roomSize += 1.0f / activeF;
			}
			else if (d > 0.0f)
			{
				opennessSum += d / MaxRaycastDistance;
				roomSize += (d / MaxRaycastDistance) / activeF;
			}

			if (totalAbHit && i < _rayTotalAbTrSpds.Count)
			{
				totalAbTrSp = MathF.Min(totalAbTrSp, _rayTotalAbTrSpds[i]);
			}

			if (SurfaceAbsorption && i < _rayAbsorptions.Count && _rayAbsorptions[i] >= 0.0f)
			{
				abSum += _rayAbsorptions[i];
				abCount += 1;
			}
		}

		_openness = opennessSum / activeF;
		roomSize = MathF.Min(roomSize, 1.0f);
		float wetness = MathF.Pow(1.0f - _openness, 2.0f);
		float damping = float.Lerp(0.0f, 1.0f, _openness);

		if (SurfaceAbsorption && abCount > 0)
		{
			float avgAb = abSum / abCount;
			wetness *= float.Lerp(1.0f, 1.0f - avgAb, AbsorptionWetnessInfluence);
			damping = Math.Clamp(damping + avgAb * AbsorptionDampingInfluence, 0.0f, 1.0f);
		}

		if (totalAbHit)
		{
			_activeTotalAbsorpTransSpeed = totalAbTrSp;
			wetness = MathF.Min(wetness, _TotalAbsorpReverbWetCap);
			damping = MathF.Max(damping, _TotalAbsorpReverbDampFloor);
		}

		float prevRoom = _lastReverbRoomSize, prevWet = _lastReverbWetness, prevDamp = _lastReverbDamping;

		targetReverbRoomSize = roomSize;
		targetReverbWetness = wetness;
		targetReverbDamping = damping;

		float chkRoom = MathF.Abs(targetReverbRoomSize - prevRoom);
		float chkWet = MathF.Abs(targetReverbWetness - prevWet);
		float chkDamp = MathF.Abs(targetReverbDamping - prevDamp);

		if (chkRoom > 0.01f || chkWet > 0.01f || chkDamp > 0.01f)
		{
			EmitSignal(SignalName.ReverbUpdated, targetReverbRoomSize, targetReverbWetness, targetReverbDamping);
			EmitSignal(SignalName.ReverbZoneChanged, targetReverbRoomSize, targetReverbWetness);
		}

		_lastReverbRoomSize = targetReverbRoomSize;
		_lastReverbWetness = targetReverbWetness;
		_lastReverbDamping = targetReverbDamping;
	}

	#endregion

	#region Occlusion
	
	private void UpdateLowpass(Node3D listener)
	{
		// TODO: UpdateLowpass
	}

	#endregion

	#endregion

	#region Utils

	internal Node3D GetListener()
	{
		if (CustomListenerTarget != null) return CustomListenerTarget;
#if TOOLS
		if (Engine.IsEditorHint())
		{
			Viewport viewport = editorInterface.GetEditorViewport3D();
			if (viewport != null) return viewport.GetCamera3D();
			return null;
		}
#endif
		return GetViewport().GetCamera3D();
	}

	private CharacterBody3D FindCharacterBody(Node node)
	{
		Node currentNode = node;
		while (currentNode != null)
		{
			if (currentNode is CharacterBody3D) return currentNode as CharacterBody3D;
			currentNode = currentNode.GetParent();
		}
		return null;
	}

	private Vector3 DegToRad(Vector3 rotation)
	{
		Vector3 result = new()
		{
			X = rotation.X * (MathF.PI / 180f),
			Y = rotation.Y * (MathF.PI / 180f),
			Z = rotation.Z * (MathF.PI / 180f),
		};
		return result;
	}

	private float ApplyExternalVolumeOffset(float volumeValue)
	{
		return (float)Math.MaxMagnitude(volumeValue + _externalVolumeDbOffset, _minimumVolumeDb);
	}

	private void SetPlayInitiated()
	{
		_playInitiatedTime = Time.GetTicksMsec();
		_playInitiatedDuration = (int)GetStreamLength() * 1000;
	}

	private double GetStreamLength()
	{
		double streamLength = 0.0;
		if (Stream != null) streamLength = Stream.GetLength();
		return streamLength;
	}

#if TOOLS
	private void AddRayMetadata(RayCast3D raycast, string rayName, Vector3 direction)
	{
		raycasts.Add(raycast);
		distances.Add(0.0f);
		_rayNames.Add(rayName);
		_rayDirections.Add(direction);
		reflectionPaths.Add([]);
		reflectionEscaped.Add(false);
		_rayAbsorptions.Add(0.0f);
		_rayTotalAbsorption.Add(false);
		_rayTotalAbTrSpds.Add(_DefaultTotalAbsorpTransSpeed);
		_rayMaterialNames.Add("");
	}

	private void ClearRayMetadata()
	{
		raycasts.Clear();
		distances.Clear();
		_rayNames.Clear();
		_rayDirections.Clear();
		reflectionPaths.Clear();
		reflectionEscaped.Clear();
		_rayAbsorptions.Clear();
		_rayTotalAbsorption.Clear();
		_rayTotalAbTrSpds.Clear();
		_rayMaterialNames.Clear();
		targetRaycast = null;
	}

	private Vector3[] GenerateFibonacciSphere(int count)
	{
		List<Vector3> directions = [];

		float goldenRatio = (1.0f + MathF.Sqrt(5.0f)) / 2.0f;

		for (int i = 0; i < count; i++)
		{
			// Polar angle
			float theta = MathF.Acos(1.0f - 2.0f * (i + 0.5f) / count);
			// Azimuthal angle
			float phi = MathF.Tau * i / goldenRatio;

			directions.Add(new Vector3
			(
				MathF.Sin(theta) * MathF.Cos(phi),
				MathF.Cos(theta),
				MathF.Sin(theta) * MathF.Sin(phi)
			));
		}

		return [.. directions];
	}

	private Vector3 RandomUnitVector()
	{
		float z = Random.Shared.NextSingle() * 2.0f - 1.00f;
		float theta = Random.Shared.NextSingle() * MathF.Tau;
		float r = MathF.Sqrt(MathF.Max(0.0f, 1.0f - z * z));
		return new Vector3(r * MathF.Cos(theta), r * MathF.Sin(theta), z);
	}

	private void AddChildInEditor(Node parent, Node node)
	{
		parent.AddChild(node, true);
		if (this.Owner == null)
		{
			node.Owner = this;
		}
		else
		{
			node.Owner = this.Owner;
		}
	}
#endif
	#endregion

	#region Event Handlers

	private void DeferredPlay(float fromPosition)
	{
		pendingDelayTimer = null;
		if (IsInsideTree())
		{
			base.Play(fromPosition);
			EmitSignal(SignalName.SpatialAudioPlaybackStarted);
		}
	}

#if DEBUG
	private void OnEnableDebugToggled(bool value)
	{
		if (!value) return;

		Node3D AcousticDebugger = new SpatialAudioDebug()
		{
			Name = nameof(AcousticDebugger)
		};

		AddChildInEditor(this, AcousticDebugger);
	}
#endif
	#endregion

#if TOOLS
	#region Editor Config

	public override void _EnterTree()
	{
		EnableDebugToggled += OnEnableDebugToggled;
	}

	/// <summary>
	/// This is <see cref="_Ready"/> but for the editor only.
	/// </summary>
	private void EditorReady()
	{
		if (!Engine.IsEditorHint()) return;

		RebuildRaycasts();

		editorInterface = EditorInterface.Singleton;

		_setupComplete = true;

		_lastListenerDistance = -1.0f;
		_wasInsideInner = false;
		_wasInFalloff = false;
		_wasAudible = false;
		_lastReverbRoomSize = targetReverbRoomSize;
		_lastReverbWetness = targetReverbWetness;
		_lastReverbDamping = targetReverbDamping;
		_lastAirAbsorptionCutoff = targetAirAbsorpCutoff;
	}

	public override void _ExitTree()
	{
		EnableDebugToggled -= OnEnableDebugToggled;

		if (!Engine.IsEditorHint())
		{
			int index = AudioServer.GetBusIndex(_busName);
			if (index >= 0) AudioServer.RemoveBus(index);
		}
	}

	/// <summary>
	/// Hide/Show export properties based on status.
	/// </summary>
	/// <param name="property"></param>
	public override void _ValidateProperty(Godot.Collections.Dictionary property)
	{
		List<StringName> fibonacciProperties =
		[
			PropertyName.FibonacciRayCount, PropertyName.FibonacciRayReflections,
		];
		if (RayDistribution != RayDistributionEnum.FibonacciSphere)
		{
			foreach (StringName propName in fibonacciProperties)
			{
				if ((StringName)property["name"] == propName)
				{
					property["usage"] = (int)~PropertyUsageFlags.Editor;
				}
			}
		}

		List<StringName> scatterProperties =
		[
			PropertyName.ScatterShape, PropertyName.ShapeRayCount, PropertyName.ShapeScatterRandomness,
		];
		if (RayDistribution != RayDistributionEnum.ShapeScatter)
		{
			foreach (StringName propName in scatterProperties)
			{
				if ((StringName)property["name"] == propName)
				{
					property["usage"] = (int)~PropertyUsageFlags.Editor;
				}
			}
		}

		List<StringName> reverbProperties =
		[
			PropertyName.MaxReverbWetness, PropertyName.SurfaceAbsorption,
			PropertyName.AbsorptionWetnessInfluence, PropertyName.AbsorptionDampingInfluence,
			PropertyName.IgnoreFloor, PropertyName.FloorAngleThreshold,
			PropertyName.ReverbCollisionMask,
		];
		if (!RoomSizeReverb)
		{
			foreach (StringName propName in reverbProperties)
			{
				if ((StringName)property["name"] == propName)
				{
					property["usage"] = (int)~PropertyUsageFlags.Editor;
				}
			}
		}

		List<StringName> surfaceAbsorptionProperties =
		[
			PropertyName.AbsorptionWetnessInfluence, PropertyName.AbsorptionDampingInfluence,
		];
		if (!SurfaceAbsorption)
		{
			foreach (StringName propName in surfaceAbsorptionProperties)
			{
				if ((StringName)property["name"] == propName)
				{
					property["usage"] = (int)~PropertyUsageFlags.Editor;
				}
			}
		}

		if (!IgnoreFloor && (StringName)property["name"] == PropertyName.FloorAngleThreshold)
		{
			property["usage"] = (int)~PropertyUsageFlags.Editor;
		}

		List<StringName> occlusionProperties =
		[
			PropertyName.OcclusionStrength, PropertyName.MaxOcclusionHits,
			PropertyName.FallbackTransmission, PropertyName.OcclusionVolumeStrength,
			PropertyName.MaxOcclusionVolumeReduction, PropertyName.OcclusionCollisionMask,
			PropertyName.IgnoreListenerBody,
		];
		if (!AudioOcclusion)
		{
			foreach (StringName propName in occlusionProperties)
			{
				if ((StringName)property["name"] == propName)
				{
					property["usage"] = (int)~PropertyUsageFlags.Editor;
				}
			}
		}

		List<StringName> attenuationProperties =
		[
			PropertyName.InnerRadius, PropertyName.FalloffDistance,
			PropertyName.AttenuationFunction, PropertyName.InnerRadiusPanningStrength,
			PropertyName.UserAttenuationCurve,
		];
		if (!EnableVolumeAttenuation)
		{
			foreach (StringName propName in attenuationProperties)
			{
				if ((StringName)property["name"] == propName)
				{
					property["usage"] = (int)~PropertyUsageFlags.Editor;
				}
			}
		}

		List<StringName> airAbsorptionProperties =
		[
			PropertyName.AirAbsorptionMinDistance, PropertyName.AirAbsorptionMaxDistance,
			PropertyName.AirAbsorptionCutoffFreqMin, PropertyName.AirAbsorptionCutoffFreqMax,
			PropertyName.AirAbsorptionLogFreqScaling,
		];
		if (!EnableAirAbsorption)
		{
			foreach (StringName propName in airAbsorptionProperties)
			{
				if ((StringName)property["name"] == propName)
				{
					property["usage"] = (int)~PropertyUsageFlags.Editor;
				}
			}
		}

		if (AttenuationFunction != AttenuationFunctionEnum.UserDefined && (StringName)property["name"] == PropertyName.UserAttenuationCurve)
		{
			property["usage"] = (int)~PropertyUsageFlags.Editor;
		}

		if (!EnableSoundDelay && (StringName)property["name"] == PropertyName.SpeedOfSound)
		{
			property["usage"] = (int)~PropertyUsageFlags.Editor;
		}

		if (!AutoplayFadeIn && (StringName)property["name"] == PropertyName.AutoplayFadeInSpeed)
		{
			property["usage"] = (int)~PropertyUsageFlags.Editor;
		}
	}

	#endregion
#endif
}
