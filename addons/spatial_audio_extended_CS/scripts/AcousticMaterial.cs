using Godot;
namespace SpatialAudioCS;

/// <summary>
/// Defines how a surface interacts with sound.
/// <para>Supports defining: </para>
/// <c>Absorption</c> - the fraction of sound energy absorbed on reflection. 
/// Used by reverb and room estimation. 
/// <para><c>1.0</c> = full absorption (less reverb). <c>0.0</c> = no absorption (full reverb).</para>
/// <para><c>Scattering</c> - how diffusely the surface reflects sound.</para>
/// <c>0.0</c> = perfect mirror. <c>1.0</c> = fully diffuse.
/// <para><c>Transmission</c> - fraction of sound energy that passed through the 
/// surface. Used by occlusion.</para> 
/// <c>0.0</c> = fully blocked. <c>1.0</c> = fully transparent.
/// <para>Material uses three band model for frequency dependent behavior.</para>
/// <c>Low ≤ 400 Hz</c> <c>Mid = 400-2500 Hz</c> <c>High ≥ 2500 Hz</c>
/// </summary>
[Tool, Icon("uid://cnhph2uchj8wu"), GlobalClass]
public partial class AcousticMaterial : Resource
{
	#region Exports

	[ExportGroup("Absorption")]

	private float _absorptionLow = 0.10f;
	/// <summary>
	/// Fraction of low-frequency energy absorbed on reflection (≤ 400 Hz).
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 1.0f, 0.01f")]
	public float AbsorptionLow
	{
		get => _absorptionLow;
		set => _absorptionLow = value;
	}

	private float _absorptionMid = 0.20f;
	/// <summary>
	/// Fraction of mid-frequency energy absorbed on reflection (400 - 2,500 Hz).
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 1.0f, 0.01f")]
	public float AbsorptionMid
	{
		get => _absorptionMid;
		set => _absorptionMid = value;
	}

	private float _absorptionHigh = 0.30f;
	/// <summary>
	/// Fraction of high-frequency energy absorbed on reflection (≥ 2,500 Hz).
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 1.0f, 0.01f")]
	public float AbsorptionHigh
	{
		get => _absorptionHigh;
		set => _absorptionHigh = value;
	}

	[ExportGroup("Scattering")]

	private float _scattering = 0.05f;
	/// <summary>
	/// How diffusely the surface reflects sound.
	/// <para><c>0.0</c> = specular reflection, <c>1.0</c> = fully scattered.</para>
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 1.0f, 0.01f")]
	public float Scattering
	{
		get => _scattering;
		set => _scattering = value;
	}

	[ExportGroup("Transmission")]

	private float _transmissionLow = 0.100f;
	/// <summary>
	/// Fraction of low-frequency energy that passes through (≤ 400 Hz).
	/// <para><c>0.0</c> = fully blocked, <c>1.0</c> = fully transparent.</para>
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 1.0f, 0.001f")]
	public float TransmissionLow
	{
		get => _transmissionLow;
		set => _transmissionLow = value;
	}

	private float _transmissionMid = 0.050f;
	/// <summary>
	/// Fraction of mid-frequency energy that passes through (400 - 2,500 Hz).
	/// <para><c>0.0</c> = fully blocked, <c>1.0</c> = fully transparent.</para>
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 1.0f, 0.001f")]
	public float TransmissionMid
	{
		get => _transmissionMid;
		set => _transmissionMid = value;
	}

	private float _transmissionHigh = 0.030f;
	/// <summary>
	/// Fraction of high-frequency energy that passes through (≥ 2,500 Hz).
	/// <para><c>0.0</c> = fully blocked, <c>1.0</c> = fully transparent.</para>
	/// </summary>
	[Export(PropertyHint.Range, "0.0f, 1.0f, 0.001f")]
	public float TransmissionHigh
	{
		get => _transmissionHigh;
		set => _transmissionHigh = value;
	}

	[ExportGroup("Special")]

	private bool _totalAbsorption = false;
	/// <summary>
	/// Treat this material as soundproof for direct-path occlusion and reverb.
	/// </summary>
	[Export]
	public bool TotalAbsorption
	{
		get => _totalAbsorption;
		set => _totalAbsorption = value;
	}

	private float _totalAbsorptionTransitionSpeed = 2.5f;
	/// <summary>
	/// Fade speed used when entering/exiting total-absorption occlusion.
	/// Lower values make soundproof transitions less abrupt.
	/// </summary>
	[Export(PropertyHint.Range, "0.1f, 20.0f, 0.1f")]
	public float TotalAbsorptionTransitionSpeed
	{
		get => _totalAbsorptionTransitionSpeed;
		set => _totalAbsorptionTransitionSpeed = value;
	}

	#endregion

	#region Preset Materials

	/// <summary>
	/// Generic material preset.
	/// </summary>
	/// <returns>A custom material resource.</returns>
	public static AcousticMaterial PresetGeneric()
	{
		AcousticMaterial material = new()
		{
			AbsorptionLow = 0.10f,
			AbsorptionMid = 0.20f,
			AbsorptionHigh = 0.30f,
			Scattering = 0.05f,
			TransmissionLow = 0.100f,
			TransmissionMid = 0.050f,
			TransmissionHigh = 0.030f
		};

		return material;
	}

	/// <summary>
	/// Brick material preset.
	/// </summary>
	/// <returns>A custom material resource.</returns>
	public static AcousticMaterial PresetBrick()
	{
		AcousticMaterial material = new()
		{
			AbsorptionLow = 0.03f,
			AbsorptionMid = 0.04f,
			AbsorptionHigh = 0.07f,
			Scattering = 0.05f,
			TransmissionLow = 0.025f,
			TransmissionMid = 0.019f,
			TransmissionHigh = 0.010f
		};

		return material;
	}

	/// <summary>
	/// Concrete material preset.
	/// </summary>
	/// <returns>A custom material resource.</returns>
	public static AcousticMaterial PresetConcrete()
	{
		AcousticMaterial material = new()
		{
			AbsorptionLow = 0.05f,
			AbsorptionMid = 0.07f,
			AbsorptionHigh = 0.08f,
			Scattering = 0.05f,
			TransmissionLow = 0.015f,
			TransmissionMid = 0.011f,
			TransmissionHigh = 0.008f
		};

		return material;
	}

	/// <summary>
	/// Ceramic material preset.
	/// </summary>
	/// <returns>A custom material resource.</returns>
	public static AcousticMaterial PresetCeramic()
	{
		AcousticMaterial material = new()
		{
			AbsorptionLow = 0.01f,
			AbsorptionMid = 0.02f,
			AbsorptionHigh = 0.02f,
			Scattering = 0.05f,
			TransmissionLow = 0.060f,
			TransmissionMid = 0.044f,
			TransmissionHigh = 0.011f
		};

		return material;
	}

	/// <summary>
	/// Gravel material preset.
	/// </summary>
	/// <returns>A custom material resource.</returns>
	public static AcousticMaterial PresetGravel()
	{
		AcousticMaterial material = new()
		{
			AbsorptionLow = 0.60f,
			AbsorptionMid = 0.70f,
			AbsorptionHigh = 0.80f,
			Scattering = 0.60f,
			TransmissionLow = 0.031f,
			TransmissionMid = 0.012f,
			TransmissionHigh = 0.008f
		};

		return material;
	}

	/// <summary>
	/// Carpet material preset.
	/// </summary>
	/// <returns>A custom material resource.</returns>
	public static AcousticMaterial PresetCarpet()
	{
		AcousticMaterial material = new()
		{
			AbsorptionLow = 0.24f,
			AbsorptionMid = 0.69f,
			AbsorptionHigh = 0.73f,
			Scattering = 0.57f,
			TransmissionLow = 0.020f,
			TransmissionMid = 0.005f,
			TransmissionHigh = 0.003f
		};

		return material;
	}

	/// <summary>
	/// Glass material preset.
	/// </summary>
	/// <returns>A custom material resource.</returns>
	public static AcousticMaterial PresetGlass()
	{
		AcousticMaterial material = new()
		{
			AbsorptionLow = 0.25f,
			AbsorptionMid = 0.06f,
			AbsorptionHigh = 0.03f,
			Scattering = 0.05f,
			TransmissionLow = 0.060f,
			TransmissionMid = 0.044f,
			TransmissionHigh = 0.011f
		};

		return material;
	}

	/// <summary>
	/// Plaster material preset.
	/// </summary>
	/// <returns>A custom material resource.</returns>
	public static AcousticMaterial PresetPlaster()
	{
		AcousticMaterial material = new()
		{
			AbsorptionLow = 0.12f,
			AbsorptionMid = 0.06f,
			AbsorptionHigh = 0.04f,
			Scattering = 0.05f,
			TransmissionLow = 0.056f,
			TransmissionMid = 0.028f,
			TransmissionHigh = 0.004f
		};

		return material;
	}

	/// <summary>
	/// Wood material preset.
	/// </summary>
	/// <returns>A custom material resource.</returns>
	public static AcousticMaterial PresetWood()
	{
		AcousticMaterial material = new()
		{
			AbsorptionLow = 0.11f,
			AbsorptionMid = 0.07f,
			AbsorptionHigh = 0.06f,
			Scattering = 0.05f,
			TransmissionLow = 0.200f,
			TransmissionMid = 0.025f,
			TransmissionHigh = 0.010f
		};

		return material;
	}

	/// <summary>
	/// Metal material preset.
	/// </summary>
	/// <returns>A custom material resource.</returns>
	public static AcousticMaterial PresetMetal()
	{
		AcousticMaterial material = new()
		{
			AbsorptionLow = 0.20f,
			AbsorptionMid = 0.07f,
			AbsorptionHigh = 0.06f,
			Scattering = 0.05f,
			TransmissionLow = 0.200f,
			TransmissionMid = 0.025f,
			TransmissionHigh = 0.010f
		};

		return material;
	}

	/// <summary>
	/// Rock material preset.
	/// </summary>
	/// <returns>A custom material resource.</returns>
	public static AcousticMaterial PresetRock()
	{
		AcousticMaterial material = new()
		{
			AbsorptionLow = 0.13f,
			AbsorptionMid = 0.20f,
			AbsorptionHigh = 0.24f,
			Scattering = 0.20f,
			TransmissionLow = 0.015f,
			TransmissionMid = 0.002f,
			TransmissionHigh = 0.001f
		};

		return material;
	}
	
	/// <summary>
	/// Acoustic foam material preset.
	/// </summary>
	/// <returns>A custom material resource.</returns>
	public static AcousticMaterial PresetAcousticFoam()
	{
		AcousticMaterial material = new()
		{
			AbsorptionLow = 1.00f,
			AbsorptionMid = 1.00f,
			AbsorptionHigh = 1.00f,
			Scattering = 0.60f,
			TransmissionLow = 0.000f,
			TransmissionMid = 0.000f,
			TransmissionHigh = 0.000f,
			TotalAbsorptionTransitionSpeed = 1.2f
		};

		return material;
	}

	#endregion
}
