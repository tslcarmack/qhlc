using System;
using Godot;
using Qhlc.Core.Public;

namespace Qhlc.Game;

public partial class SkinButton : Button
{
	[Export] public Texture2D? StripTexture { get; set; }
	[Export] public int FrameCount { get; set; } = 3;

	public override void _Ready()
	{
		if (StripTexture is null || FrameCount <= 0)
			return;

		var tw = StripTexture.GetWidth();
		var th = StripTexture.GetHeight();
		ApplyStyle("normal", 0, tw, th);
		ApplyStyle("hover", Math.Min(1, FrameCount - 1), tw, th);
		ApplyStyle("pressed", Math.Min(2, FrameCount - 1), tw, th);
	}

	private void ApplyStyle(StringName styleName, int frame, int tw, int th)
	{
		var (x, y, w, h) = SkinUiMath.VerticalStripFrame(tw, th, FrameCount, frame);
		var atlas = new AtlasTexture { Atlas = StripTexture, Region = new Rect2(x, y, w, h) };
		var box = new StyleBoxTexture { Texture = atlas };
		AddThemeStyleboxOverride(styleName, box);
		CustomMinimumSize = new Vector2(w, h);
	}
}
