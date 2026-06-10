# SpatialAudioDebug class

This class is used to debug a [`SpatialAudioPlayer3D`](./SpatialAudioPlayer3D.md). A debug child will be automatically added when [`EnableDebug`](./SpatialAudioPlayer3D/EnableDebug.md) is toggled.

```csharp
public class SpatialAudioDebug : Node3D
```

## Public Members

| name | description |
| --- | --- |
| [SpatialAudioDebug](SpatialAudioDebug/SpatialAudioDebug.md)() | The default constructor. |
| [DebugDrawPlayingState](SpatialAudioDebug/DebugDrawPlayingState.md) { get; set; } | Draws a small wireframe sphere at the emitter origin that indicates playback state: |
| [DebugDrawRadius](SpatialAudioDebug/DebugDrawRadius.md) { get; set; } | Draws wireframe spheres for the inner radius (cyan) and outer boundary (orange) at runtime (in-game) when volume attenuation is enabled. |
| [DebugDrawRays](SpatialAudioDebug/DebugDrawRays.md) { get; set; } | Draws coloured lines for every raycast at runtime (in-game). |
| [DebugToggleEffectsKey](SpatialAudioDebug/DebugToggleEffectsKey.md) { get; set; } | While the debug overlay is visible, press this key to toggle all spatial-audio effects on/off so you can A/B compare the difference. |
| [DebugToggleShapesKey](SpatialAudioDebug/DebugToggleShapesKey.md) { get; set; } | While the debug overlay is visible, press this key to toggle debug shape drawing (rays, spheres) on/off at runtime. |
| [DisplayDebugInfo](SpatialAudioDebug/DisplayDebugInfo.md) { get; set; } | Displays key spatial-audio diagnostics as an on-screen overlay every update cycle while within the radius of the source. |
| override [_ExitTree](SpatialAudioDebug/_ExitTree.md)() | Disconnect from signal and remove per-instance debug nodes from the shared container. |
| override [_Process](SpatialAudioDebug/_Process.md)(…) |  |
| override [_Ready](SpatialAudioDebug/_Ready.md)() | See: EditorReady for editor tool. |
| override [_UnhandledInput](SpatialAudioDebug/_UnhandledInput.md)(…) | Look for unhandled input for the debug pannel interactions. |
| override [_ValidateProperty](SpatialAudioDebug/_ValidateProperty.md)(…) |  |
| class [MethodName](SpatialAudioDebug.MethodName.md) | Cached StringNames for the methods contained in this class, for fast lookup. |
| class [PropertyName](SpatialAudioDebug.PropertyName.md) | Cached StringNames for the properties and fields contained in this class, for fast lookup. |
| class [SignalName](SpatialAudioDebug.SignalName.md) | Cached StringNames for the signals contained in this class, for fast lookup. |


## See Also

* namespace [SpatialAudioCS](../spatial-audio-extended-csharp.md)

