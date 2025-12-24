using Godot;

namespace Combat;

/// <summary>
/// Creates a red vignette (darkened edges) effect when the player takes damage.
/// Add this as a child of the Player node.
/// </summary>
public partial class DamageVignette : CanvasLayer
{
	[ExportGroup("Vignette Settings")]
	[Export] public Color VignetteColor { get; set; } = new Color(0.8f, 0.0f, 0.0f, 0.6f);
	[Export] public float FadeInDuration { get; set; } = 0.1f;
	[Export] public float FadeOutDuration { get; set; } = 0.4f;
	
	[ExportGroup("Intensity Scaling")]
	/// <summary>If true, vignette intensity scales with damage taken.</summary>
	[Export] public bool ScaleWithDamage { get; set; } = true;
	/// <summary>Damage amount that produces maximum intensity.</summary>
	[Export] public float MaxDamageReference { get; set; } = 50f;
	
	private ColorRect _vignetteRect;
	private CombatStats _stats;
	private Tween _currentTween;
	private ShaderMaterial _vignetteMaterial;

	public override void _Ready()
	{
		// Create the vignette overlay
		CreateVignetteOverlay();
		
		// Find CombatStats in parent
		_stats = GetParent()?.GetNodeOrNull<CombatStats>("CombatStats");
		if (_stats != null)
		{
			_stats.DamageTaken += OnDamageTaken;
		}
		else
		{
			GD.PrintErr("DamageVignette: Could not find CombatStats in parent!");
		}
	}

	private void CreateVignetteOverlay()
	{
		// Set this CanvasLayer to render on top
		Layer = 100;
		
		// Create a ColorRect that covers the screen
		_vignetteRect = new ColorRect();
		_vignetteRect.Name = "VignetteRect";
		_vignetteRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_vignetteRect.MouseFilter = Control.MouseFilterEnum.Ignore;
		
		// Create and apply the vignette shader
		var shader = new Shader();
		shader.Code = @"
shader_type canvas_item;

uniform vec4 vignette_color : source_color = vec4(0.8, 0.0, 0.0, 0.6);
uniform float intensity : hint_range(0.0, 1.0) = 0.0;
uniform float softness : hint_range(0.0, 1.0) = 0.4;
uniform float radius : hint_range(0.0, 1.0) = 0.7;

void fragment() {
    vec2 uv = UV - 0.5;
    float dist = length(uv * vec2(1.0, 0.6)); // Slightly oval shape
    float vignette = smoothstep(radius, radius - softness, dist);
    float edge_alpha = (1.0 - vignette) * intensity;
    COLOR = vec4(vignette_color.rgb, vignette_color.a * edge_alpha);
}
";
		
		_vignetteMaterial = new ShaderMaterial();
		_vignetteMaterial.Shader = shader;
		_vignetteMaterial.SetShaderParameter("vignette_color", VignetteColor);
		_vignetteMaterial.SetShaderParameter("intensity", 0.0f);
		
		_vignetteRect.Material = _vignetteMaterial;
		AddChild(_vignetteRect);
	}

	private void OnDamageTaken(DamageInfo info)
	{
		// Calculate intensity based on damage
		float intensity = 1.0f;
		if (ScaleWithDamage && MaxDamageReference > 0)
		{
			intensity = Mathf.Clamp(info.FinalDamage / MaxDamageReference, 0.3f, 1.0f);
		}
		
		PlayVignetteEffect(intensity);
	}

	/// <summary>
	/// Play the vignette effect with the given intensity (0-1).
	/// </summary>
	public void PlayVignetteEffect(float intensity = 1.0f)
	{
		// Kill any existing tween
		_currentTween?.Kill();
		
		// Create new tween sequence
		_currentTween = CreateTween();
		
		// Fade in
		_currentTween.TweenMethod(
			Callable.From<float>(SetVignetteIntensity),
			0.0f,
			intensity,
			FadeInDuration
		);
		
		// Fade out
		_currentTween.TweenMethod(
			Callable.From<float>(SetVignetteIntensity),
			intensity,
			0.0f,
			FadeOutDuration
		);
	}

	private void SetVignetteIntensity(float value)
	{
		_vignetteMaterial?.SetShaderParameter("intensity", value);
	}
}
