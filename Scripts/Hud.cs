using Godot;
using System;

public partial class Hud : CanvasLayer
{
	// Link these to the TextureProgressBars in the Inspector
	[Export] private TextureProgressBar[] _cooldownOverlays;

	public void OnPlayerSkillActivated(int slot, float cooldown)
	{
		GD.Print($"Signal received! Slot: {slot}, Cooldown: {cooldown}");
		if (slot < 0 || slot >= _cooldownOverlays.Length){
			GD.Print($"ERROR: Slot {slot} is out of range!");
			return;	
		} 

		var overlay = _cooldownOverlays[slot];
	  	if (overlay == null)
		{
			GD.Print($"ERROR: overlay at slot {slot} is null!");
			return;
		}
		
		// Set to 100% (fully covering the icon)
		GD.Print($"Starting cooldown animation for slot {slot}");
		overlay.Value = 100;

		// Professional approach: Use a Tween to animate the countdown
		Tween tween = GetTree().CreateTween();
		
		// This smoothly reduces the progress bar value to 0 over the 'cooldown' duration
		tween.TweenProperty(overlay, "value", 0.0f, cooldown);
		GD.Print($"Tween created successfully!");
	}
}
