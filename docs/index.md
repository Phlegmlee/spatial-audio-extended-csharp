# SpatialAudioCS Documentation

Welcome to the `SpatialAudioCS` documentation. Select the class you would like to explore or read more below.

| public type | description |
| --- | --- |
| class [SpatialAudioPlayer3D](./SpatialAudioCS/SpatialAudioPlayer3D.md) | An advanced, drop-in relacement for Godot's AudioStreamPlayer3D that provides physically-inspired, real-time spatial audio effects for immersive 3D applications. |
| class [SpatialAudioDebug](./SpatialAudioCS/SpatialAudioDebug.md) | This class is used to debug a [`SpatialAudioPlayer3D`](./SpatialAudioCS/SpatialAudioPlayer3D.md). A debug child will be automatically added when [`EnableDebug`](./SpatialAudioCS/SpatialAudioPlayer3D/EnableDebug.md) is toggled. |
| class [AcousticBody](./SpatialAudioCS/AcousticBody.md) | Attach as a child to any `CollisionObject3D` to give the surface acoustic properties using an [`AcousticMaterial`](./SpatialAudioCS/AcousticMaterial.md). |
| class [AcousticMaterial](./SpatialAudioCS/AcousticMaterial.md) | Defines how a surface interacts with sound. |
| class [SpatialReflectionNavigationAgent3D](./SpatialAudioCS/SpatialReflectionNavigationAgent3D.md) | TODO: Documentation |

## Features

- **Volume Attenuation** — Multiple falloff curves (Linear, Logarithmic, Inverse, Natural Sound, and user-defined) with configurable inner and outer radii.
- **Room Size Reverb** — Omni-directional raycasts estimate room geometry and apply reverb automatically. Works in real time as the player moves between spaces.
- **Wall Occlusion** — Multi-wall detection with material-based lowpass filtering and volume reduction. Sounds become progressively more muffled behind each wall.
- **Air Absorption** — Distance-based high-frequency rolloff simulating how air attenuates sound over long distances.
- **Sound Speed Delay** — Realistic propagation delay for one-shot sounds. Fire a gunshot across a valley and hear it arrive a moment later.
- **Acoustic Materials** — Physics-based surface properties (absorption, scattering, transmission) with 12 built-in presets covering brick, concrete, carpet, glass, wood, metal, acoustic foam, and more.
- **Debug Overlay** — On-screen HUD, in-editor ray visualisation, radius wireframes, and a live A/B effect toggle.
- **Rich Signals** — Zone transitions, occlusion events, reverb updates, air absorption changes, playback state, and diagnostic data.
- **Reflection Navigation Agent (Experimental)** — `SpatialReflectionNavigationAgent3D` routes reflected proxy audio around corners in 3D space and integrates with `SpatialAudioPlayer3D`.


---

## Node Overview

| Node | Description |
|---|---|
| `SpatialAudioPlayer3D` | The main audio player. Drop-in for `AudioStreamPlayer3D`. |
| `SpatialReflectionNavigationAgent3D` | 3D path-routing agent for reflected/proxy audio around occluders. |
| `AcousticBody` | Attach to collision geometry to define its acoustic surface properties. |
| `AcousticMaterial` | Resource defining absorption, scattering, and transmission per frequency band. |

---

## Acoustic Material Presets

12 presets are included, based on real-world acoustic data:

`brick` · `concrete` · `ceramic` · `carpet` · `glass` · `gravel` · `metal` · `plaster` · `rock` · `wood` · `generic` · `acoustic_foam`

The material resource files can be found in the `addons/spatial_audio_extended_CS/materials` folder.

```csharp
// Code usage examples

// Load resource file from disk
AcousticMaterial loadMat = GD.Load("res://addons/spatial_audio_extended_CS/materials/concrete.tres") as AcousticMaterial;

// Or access the static constructor
AcousticMaterial mat = AcousticMaterial.PresetConcrete;
```

---