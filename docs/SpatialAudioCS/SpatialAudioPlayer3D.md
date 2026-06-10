# SpatialAudioPlayer3D class

An advanced, drop-in relacement for Godot's AudioStreamPlayer3D that provides physically-inspired, real-time spatial audio effects for immersive 3D applications.

Usage:

1. Add this node in place of an AudioStreamPlayer3D for any 3D sound that needs advanced spatialization.
2. Configue `attenuation`, `reverb`, `occlusion` and `debug` via the inspector.
3. Connect to signals for gameplay, analytics, or UI feedback on audio events.

Key Inherited Parameters:

* VolumeDb: managed internally, editing it directly has no effect.
* MaxDb: controls the maximum loudness of the stream.
* UnitSize: the radius at which the sound plays at full volume.
* MaxDistance: the furthest distance at which the sound is audible. Should be equal to, or greater than [`MaxRaycastDistance`](./SpatialAudioPlayer3D/MaxRaycastDistance.md).

```csharp
public class SpatialAudioPlayer3D : AudioStreamPlayer3D
```

## Public Members

| name | description |
| --- | --- |
| [SpatialAudioPlayer3D](SpatialAudioPlayer3D/SpatialAudioPlayer3D.md)() | The default constructor. |
| [AbsorptionDampingInfluence](SpatialAudioPlayer3D/AbsorptionDampingInfluence.md) { get; set; } | How strongly surface absorption influences reverb damping. |
| [AbsorptionWetnessInfluence](SpatialAudioPlayer3D/AbsorptionWetnessInfluence.md) { get; set; } | How strongly surface absorption influences reverb wetness. |
| [AirAbsorptionCutoffFreqMax](SpatialAudioPlayer3D/AirAbsorptionCutoffFreqMax.md) { get; set; } | Lowpass cutoff frequency at the maximum distance (furthest from the source). Lower values produce heavier muffling at long range. |
| [AirAbsorptionCutoffFreqMin](SpatialAudioPlayer3D/AirAbsorptionCutoffFreqMin.md) { get; set; } | Lowpass cutoff frequency at the minimum distance (closest to the source). Higher values mean less filtering when nearby — recommended to keep close to 20 000 Hz. |
| [AirAbsorptionLogFreqScaling](SpatialAudioPlayer3D/AirAbsorptionLogFreqScaling.md) { get; set; } | When enabled, the filter cutoff interpolation uses a logarithmic frequency scale instead of linear. This produces a perceptually smoother sweep that better matches how we perceive pitch. |
| [AirAbsorptionMaxDistance](SpatialAudioPlayer3D/AirAbsorptionMaxDistance.md) { get; set; } | Distance from the emitter at which air absorption filtering reaches its maximum effect. The filter interpolates between min and max cutoff frequencies over this range. |
| [AirAbsorptionMinDistance](SpatialAudioPlayer3D/AirAbsorptionMinDistance.md) { get; set; } | Distance from the emitter at which air absorption filtering begins. Below this distance no filtering is applied. |
| [AttenuationFunction](SpatialAudioPlayer3D/AttenuationFunction.md) { get; set; } | The curve that controls how quickly volume drops between the inner radius and outer boundary. Select UserDefined to use a custom curve. |
| [AudioOcclusion](SpatialAudioPlayer3D/AudioOcclusion.md) { get; set; } | When enabled, a lowpass filter simulates sound being muffled by walls between the emitter and listener using a single target raycast. |
| [AutoplayFadeIn](SpatialAudioPlayer3D/AutoplayFadeIn.md) { get; set; } | When true and the node's `autoplay` is enabled, the player will start at `minimum_volume_db` on ready and lerp up once the first geometry scan completes. Toggle this to disable the automatic silent startup for autoplay. |
| [AutoplayFadeInSpeed](SpatialAudioPlayer3D/AutoplayFadeInSpeed.md) { get; set; } | Speed used specifically for fading the volume in when [`AutoplayFadeIn`](./SpatialAudioPlayer3D/AutoplayFadeIn.md) is active. Separate from [`LerpSpeed`](./SpatialAudioPlayer3D/LerpSpeed.md) so effect parameters can remain snappier while the audible fade is tuned independently. |
| [CustomListenerTarget](SpatialAudioPlayer3D/CustomListenerTarget.md) { get; set; } | Overrides the default listener target (the active `Camera3D`). |
| [EnableAirAbsorption](SpatialAudioPlayer3D/EnableAirAbsorption.md) { get; set; } | When enabled, a distance-based lowpass filter simulates how air absorbs high-frequency sound energy over distance. This is independent of wall occlution and stacks with it. |
| [EnableDebug](SpatialAudioPlayer3D/EnableDebug.md) { get; set; } | Toggle to add a debug node child to the audio player. |
| [EnableSoundDelay](SpatialAudioPlayer3D/EnableSoundDelay.md) { get; set; } | ## When enabled, playback is delayed based on the distance between the emitter and the listener divided by [param speed_of_sound], simulating the finite travel time of sound through air. |
| [EnableVolumeAttenuation](SpatialAudioPlayer3D/EnableVolumeAttenuation.md) { get; set; } | When enabled, volume is attenuated based on inner / outer radius and the chosen attenuation function. |
| [FallbackTransmission](SpatialAudioPlayer3D/FallbackTransmission.md) { get; set; } | Fraction of sound that passes through surfaces without an [`AcousticBody`](./AcousticBody.md). |
| [FalloffDistance](SpatialAudioPlayer3D/FalloffDistance.md) { get; set; } | The distance beyond the inner radius over which the sound fades from full volume to silence. The outer boundary equals [`InnerRadius`](./SpatialAudioPlayer3D/InnerRadius.md) + [`FalloffDistance`](./SpatialAudioPlayer3D/FalloffDistance.md). |
| [FibonacciRayCount](SpatialAudioPlayer3D/FibonacciRayCount.md) { get; set; } | Number of omni-directional rays when using FibonacciSphere distribution. More rays = better room-size estimation at higher CPU cost. |
| [FibonacciRayReflections](SpatialAudioPlayer3D/FibonacciRayReflections.md) { get; set; } | Number of times each Fibonacci ray can reflect (bounce) off surfaces. More reflections improve room-size estimation in complex geometry (L-shaped rooms, corridors, etc.) at higher CPU cost. |
| [FloorAngleThreshold](SpatialAudioPlayer3D/FloorAngleThreshold.md) { get; set; } | The angle in degrees from straight down withing which a ray is considered a "floor ray" and will be ignored when [`IgnoreFloor`](./SpatialAudioPlayer3D/IgnoreFloor.md) is enabled. |
| [IgnoreFloor](SpatialAudioPlayer3D/IgnoreFloor.md) { get; set; } | When enabled, rays pointing downward (below [`FloorAngleThreshold`](./SpatialAudioPlayer3D/FloorAngleThreshold.md)) are excluded from room-size and openness calculations. This prevents the floor (which is almost always present) from shrinking the perceived room size. |
| [IgnoreListenerBody](SpatialAudioPlayer3D/IgnoreListenerBody.md) { get; set; } | When enabled, the listener's `CharacterBody3D` (if any) is automatically excluded from occlusion raycasts. This prevents the player's own collision shapes from being detected as walls. |
| [InnerRadius](SpatialAudioPlayer3D/InnerRadius.md) { get; set; } | The radius around the emitter inside which the sound plays at full volume (completely unattenuated). |
| [InnerRadiusPanningStrength](SpatialAudioPlayer3D/InnerRadiusPanningStrength.md) { get; set; } | Panning strength multiplier applied when the listener is at the center of the inner radius. The value interpolates back to default `1.0` at the inner radius edge. |
| [LerpSpeed](SpatialAudioPlayer3D/LerpSpeed.md) { get; set; } | How quickly effect values (lowpass, reverb wet, room size) interpolate toward their targets each frame. Higher = snappier, lower = smoother. |
| [MaxOcclusionHits](SpatialAudioPlayer3D/MaxOcclusionHits.md) { get; set; } | Maximum number of walls (collisions) to detect between the emitter and the listener. Each additional wall multiplicatively reduces the lowpass cutoff, making the sound progressively more muffled. |
| [MaxOcclusionVolumeReduction](SpatialAudioPlayer3D/MaxOcclusionVolumeReduction.md) { get; set; } | Maximum combined volume reduction (in dB) that wall occlusion can apply. Prevents the sound from going completely silent behind many walls. |
| [MaxRaycastDistance](SpatialAudioPlayer3D/MaxRaycastDistance.md) { get; set; } | The maximum distance raycasts will travel to sense geometry. |
| [MaxReverbWetness](SpatialAudioPlayer3D/MaxReverbWetness.md) { get; set; } | The maximum reverb wetness that can be applied. Acts as a global ceiling on the wet signal regardless of how enclosed the space is. |
| [MinimumVolumeDb](SpatialAudioPlayer3D/MinimumVolumeDb.md) { get; set; } | The volume this emitter fades up from when it first becomes active, before the initial geometry scan completes. |
| [OccludedLowpassCutoffMinimum](SpatialAudioPlayer3D/OccludedLowpassCutoffMinimum.md) { get; set; } | Lowpass cutoff when the listener is fully occluded by a wall. Lower values produce a heavier, more muffled sound. |
| [OcclusionCollisionMask](SpatialAudioPlayer3D/OcclusionCollisionMask.md) { get; set; } | Physics layers the occlusion raycast collides with. Can differ from reverb if e.g. thin walls should occlude but not affect perceived room size. |
| [OcclusionStrength](SpatialAudioPlayer3D/OcclusionStrength.md) { get; set; } | How strongly the lowpass filter is applied when the listener is occluded. |
| [OcclusionVolumeStrength](SpatialAudioPlayer3D/OcclusionVolumeStrength.md) { get; set; } | How strongly walls reduce volume (in addition to AudioEffectLowPassFilter). |
| [OpenLowpassCutoff](SpatialAudioPlayer3D/OpenLowpassCutoff.md) { get; set; } | Lowpass cutoff when the listener has clear line of sight to the emitter. |
| [RayDistribution](SpatialAudioPlayer3D/RayDistribution.md) { get; set; } | How the room-sensing rays are arranged around the emitter. |
| [ReverbCollisionMask](SpatialAudioPlayer3D/ReverbCollisionMask.md) { get; set; } | Physics layers the room-sensing raycasts collide with. Should match the layers your level geometry occupies. |
| [RoomSizeReverb](SpatialAudioPlayer3D/RoomSizeReverb.md) { get; set; } | When enabled, reverb room size and wetness are calculated from surrounding geometry and applied automatically via omni-directional raycasts. |
| [ScatterShape](SpatialAudioPlayer3D/ScatterShape.md) { get; set; } | The collision-shape used as the scattering origin. Rays will be positioned around this node and fired outward. |
| [ShapeRayCount](SpatialAudioPlayer3D/ShapeRayCount.md) { get; set; } | Number of rays when using ShapeScatter random distribution. |
| [ShapeScatterRandomness](SpatialAudioPlayer3D/ShapeScatterRandomness.md) { get; set; } | How strongly the ray direction deviates from the center-to-surface direction. |
| [SpeedOfSound](SpatialAudioPlayer3D/SpeedOfSound.md) { get; set; } | The speed at which sound travels, in metres per second. Earth sea-level is ~343 m/s. Lower values exaggerate the delay. |
| [SurfaceAbsorption](SpatialAudioPlayer3D/SurfaceAbsorption.md) { get; set; } | When enabled, the absorption properties of [`AcousticMaterial`](./AcousticMaterial.md)s on surfaces hit by room-sensing rays are used to modulate reverb wetness and damping. |
| [UpdateFrequency](SpatialAudioPlayer3D/UpdateFrequency.md) { get; set; } | How often geometry is re-sampled and audio effects are recalculated, in seconds. Increase for static or slow-moving emitters to save CPU. |
| [UserAttenuationCurve](SpatialAudioPlayer3D/UserAttenuationCurve.md) { get; set; } | Custom attenuation curve used when UserDefined is selected. |
| [AudioBusPrefix](SpatialAudioPlayer3D/AudioBusPrefix.md) | Name prefix for the dynamically created audio bus. Will be a child of the selected bus in the AudioStreamPlayer3D settings. |
| event [AirAbsorptionUpdated](SpatialAudioPlayer3D/AirAbsorptionUpdated.md) |  |
| event [AirAbsorptionZoneChanged](SpatialAudioPlayer3D/AirAbsorptionZoneChanged.md) |  |
| event [AttenuationZoneEntered](SpatialAudioPlayer3D/AttenuationZoneEntered.md) |  |
| event [AttenuationZoneExited](SpatialAudioPlayer3D/AttenuationZoneExited.md) |  |
| event [AudioOccluded](SpatialAudioPlayer3D/AudioOccluded.md) |  |
| event [AudioUnoccluded](SpatialAudioPlayer3D/AudioUnoccluded.md) |  |
| event [EnableDebugToggled](SpatialAudioPlayer3D/EnableDebugToggled.md) |  |
| event [FalloffZoneEntered](SpatialAudioPlayer3D/FalloffZoneEntered.md) |  |
| event [FalloffZoneExited](SpatialAudioPlayer3D/FalloffZoneExited.md) |  |
| event [InnerRadiusEntered](SpatialAudioPlayer3D/InnerRadiusEntered.md) |  |
| event [InnerRadiusExited](SpatialAudioPlayer3D/InnerRadiusExited.md) |  |
| event [ListenerDistanceChanged](SpatialAudioPlayer3D/ListenerDistanceChanged.md) |  |
| event [OcclusionChanged](SpatialAudioPlayer3D/OcclusionChanged.md) |  |
| event [OcclusionRayCollided](SpatialAudioPlayer3D/OcclusionRayCollided.md) |  |
| event [ReverbRayCollided](SpatialAudioPlayer3D/ReverbRayCollided.md) |  |
| event [ReverbUpdated](SpatialAudioPlayer3D/ReverbUpdated.md) |  |
| event [ReverbZoneChanged](SpatialAudioPlayer3D/ReverbZoneChanged.md) |  |
| event [SpatialAudioPlaybackStarted](SpatialAudioPlayer3D/SpatialAudioPlaybackStarted.md) |  |
| event [SpatialAudioPlaybackStopped](SpatialAudioPlayer3D/SpatialAudioPlaybackStopped.md) |  |
| [PlayWithDelay](SpatialAudioPlayer3D/PlayWithDelay.md)(…) | Designed as an override to Single). |
| [Stop](SpatialAudioPlayer3D/Stop.md)() | Override for base Stop() to emit signal. |
| override [_EnterTree](SpatialAudioPlayer3D/_EnterTree.md)() |  |
| override [_ExitTree](SpatialAudioPlayer3D/_ExitTree.md)() |  |
| override [_PhysicsProcess](SpatialAudioPlayer3D/_PhysicsProcess.md)(…) |  |
| override [_Ready](SpatialAudioPlayer3D/_Ready.md)() | See: EditorReady for editor tool. |
| override [_ValidateProperty](SpatialAudioPlayer3D/_ValidateProperty.md)(…) | Hide/Show export properties based on status. |
| delegate [AirAbsorptionUpdatedEventHandler](SpatialAudioPlayer3D.AirAbsorptionUpdatedEventHandler.md) | Emitted when air-absorption lowpass cutoff changes. |
| delegate [AirAbsorptionZoneChangedEventHandler](SpatialAudioPlayer3D.AirAbsorptionZoneChangedEventHandler.md) | Emitted when the air-absorption zone changes (min/max thresholds crossed). |
| enum [AttenuationFunctionEnum](SpatialAudioPlayer3D.AttenuationFunctionEnum.md) | Options for the falloff curve used to calculate volume attenuation between the inner radius and the outer boundary. |
| delegate [AttenuationZoneEnteredEventHandler](SpatialAudioPlayer3D.AttenuationZoneEnteredEventHandler.md) | Emitted when a listener enters the outer boudary. |
| delegate [AttenuationZoneExitedEventHandler](SpatialAudioPlayer3D.AttenuationZoneExitedEventHandler.md) | Emitted when a listener exits the outer boudary. |
| delegate [AudioOccludedEventHandler](SpatialAudioPlayer3D.AudioOccludedEventHandler.md) | Emitted when the audio becomes occluded by one or more walls. |
| delegate [AudioUnoccludedEventHandler](SpatialAudioPlayer3D.AudioUnoccludedEventHandler.md) | Emitted when occlusion clears and the audio becomes unoccluded. |
| delegate [EnableDebugToggledEventHandler](SpatialAudioPlayer3D.EnableDebugToggledEventHandler.md) | Emitted when [`EnableDebug`](./SpatialAudioPlayer3D/EnableDebug.md) is toggled. |
| delegate [FalloffZoneEnteredEventHandler](SpatialAudioPlayer3D.FalloffZoneEnteredEventHandler.md) | Emitted when a listener enters the falloff zone (between inner and outer). |
| delegate [FalloffZoneExitedEventHandler](SpatialAudioPlayer3D.FalloffZoneExitedEventHandler.md) | Emitted when a listener exits the falloff zone (between inner and outer). |
| delegate [InnerRadiusEnteredEventHandler](SpatialAudioPlayer3D.InnerRadiusEnteredEventHandler.md) | Emitted when a listener enters the inner (full-volume) radius. |
| delegate [InnerRadiusExitedEventHandler](SpatialAudioPlayer3D.InnerRadiusExitedEventHandler.md) | Emitted when a listener leaves the inner (full-volume) radius. |
| delegate [ListenerDistanceChangedEventHandler](SpatialAudioPlayer3D.ListenerDistanceChangedEventHandler.md) | Emitted periodically when listener distance changes significantly. |
| class [MethodName](SpatialAudioPlayer3D.MethodName.md) | Cached StringNames for the methods contained in this class, for fast lookup. |
| delegate [OcclusionChangedEventHandler](SpatialAudioPlayer3D.OcclusionChangedEventHandler.md) | Emitted when occlusion parameters change. |
| delegate [OcclusionRayCollidedEventHandler](SpatialAudioPlayer3D.OcclusionRayCollidedEventHandler.md) | Emitted when an occlusion ray collides with a surface. |
| class [PropertyName](SpatialAudioPlayer3D.PropertyName.md) | Cached StringNames for the properties and fields contained in this class, for fast lookup. |
| enum [RayDistributionEnum](SpatialAudioPlayer3D.RayDistributionEnum.md) | Options for how the omni-directional room-sensing rays are distributed around the emitter. |
| delegate [ReverbRayCollidedEventHandler](SpatialAudioPlayer3D.ReverbRayCollidedEventHandler.md) | Emitted when a reverb/reflection ray collides with a surface. |
| delegate [ReverbUpdatedEventHandler](SpatialAudioPlayer3D.ReverbUpdatedEventHandler.md) | Emitted when reverb targets change. |
| delegate [ReverbZoneChangedEventHandler](SpatialAudioPlayer3D.ReverbZoneChangedEventHandler.md) | Emitted when room size or weness changes (higher-level reverb zone change). |
| class [SignalName](SpatialAudioPlayer3D.SignalName.md) | Cached StringNames for the signals contained in this class, for fast lookup. |
| delegate [SpatialAudioPlaybackStartedEventHandler](SpatialAudioPlayer3D.SpatialAudioPlaybackStartedEventHandler.md) | Emitted when playback actually starts (immediate or deferred). |
| delegate [SpatialAudioPlaybackStoppedEventHandler](SpatialAudioPlayer3D.SpatialAudioPlaybackStoppedEventHandler.md) | Emitted when playback is stopped. |

## Protected Members

| name | description |
| --- | --- |
| [EmitSignalAirAbsorptionUpdated](SpatialAudioPlayer3D/EmitSignalAirAbsorptionUpdated.md)(…) |  |
| [EmitSignalAirAbsorptionZoneChanged](SpatialAudioPlayer3D/EmitSignalAirAbsorptionZoneChanged.md)(…) |  |
| [EmitSignalAttenuationZoneEntered](SpatialAudioPlayer3D/EmitSignalAttenuationZoneEntered.md)(…) |  |
| [EmitSignalAttenuationZoneExited](SpatialAudioPlayer3D/EmitSignalAttenuationZoneExited.md)(…) |  |
| [EmitSignalAudioOccluded](SpatialAudioPlayer3D/EmitSignalAudioOccluded.md)(…) |  |
| [EmitSignalAudioUnoccluded](SpatialAudioPlayer3D/EmitSignalAudioUnoccluded.md)(…) |  |
| [EmitSignalEnableDebugToggled](SpatialAudioPlayer3D/EmitSignalEnableDebugToggled.md)(…) |  |
| [EmitSignalFalloffZoneEntered](SpatialAudioPlayer3D/EmitSignalFalloffZoneEntered.md)(…) |  |
| [EmitSignalFalloffZoneExited](SpatialAudioPlayer3D/EmitSignalFalloffZoneExited.md)(…) |  |
| [EmitSignalInnerRadiusEntered](SpatialAudioPlayer3D/EmitSignalInnerRadiusEntered.md)(…) |  |
| [EmitSignalInnerRadiusExited](SpatialAudioPlayer3D/EmitSignalInnerRadiusExited.md)(…) |  |
| [EmitSignalListenerDistanceChanged](SpatialAudioPlayer3D/EmitSignalListenerDistanceChanged.md)(…) |  |
| [EmitSignalOcclusionChanged](SpatialAudioPlayer3D/EmitSignalOcclusionChanged.md)(…) |  |
| [EmitSignalOcclusionRayCollided](SpatialAudioPlayer3D/EmitSignalOcclusionRayCollided.md)(…) |  |
| [EmitSignalReverbRayCollided](SpatialAudioPlayer3D/EmitSignalReverbRayCollided.md)(…) |  |
| [EmitSignalReverbUpdated](SpatialAudioPlayer3D/EmitSignalReverbUpdated.md)(…) |  |
| [EmitSignalReverbZoneChanged](SpatialAudioPlayer3D/EmitSignalReverbZoneChanged.md)(…) |  |
| [EmitSignalSpatialAudioPlaybackStarted](SpatialAudioPlayer3D/EmitSignalSpatialAudioPlaybackStarted.md)() |  |
| [EmitSignalSpatialAudioPlaybackStopped](SpatialAudioPlayer3D/EmitSignalSpatialAudioPlaybackStopped.md)() |  |
| override [GetGodotClassPropertyValue](SpatialAudioPlayer3D/GetGodotClassPropertyValue.md)(…) |  |
| override [HasGodotClassMethod](SpatialAudioPlayer3D/HasGodotClassMethod.md)(…) |  |
| override [HasGodotClassSignal](SpatialAudioPlayer3D/HasGodotClassSignal.md)(…) |  |
| override [InvokeGodotClassMethod](SpatialAudioPlayer3D/InvokeGodotClassMethod.md)(…) |  |
| override [RaiseGodotClassSignalCallbacks](SpatialAudioPlayer3D/RaiseGodotClassSignalCallbacks.md)(…) |  |
| override [RestoreGodotObjectData](SpatialAudioPlayer3D/RestoreGodotObjectData.md)(…) |  |
| override [SaveGodotObjectData](SpatialAudioPlayer3D/SaveGodotObjectData.md)(…) |  |
| override [SetGodotClassPropertyValue](SpatialAudioPlayer3D/SetGodotClassPropertyValue.md)(…) |  |

## See Also

* namespace [SpatialAudioCS](../spatial-audio-extended-csharp.md)

<!-- DO NOT EDIT: generated by xmldocmd for spatial-audio-extended-csharp.dll -->
