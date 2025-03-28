using Color = global::Windows.UI.Color;

namespace Windows.UI.Xaml;

public partial class UIElement
{
	private const float ShadowOffsetMax = 150;
	private const float ShadowAlphaFallback = 0.5f;
	private const float ShadowAlphaMultiplier = 0.18f;
	private const float ShadowOffsetAlphaModifier = 1f / 650f;
	private const float ShadowSigmaXModifier = 1f / 5f;
	private const float ShadowSigmaYModifier = 1f / 3.5f;

	partial void UnsetShadow()
	{
		Visual.ShadowState = null;
	}

	partial void SetShadow()
	{
		var translation = Translation;

		ComputeDropShadowValues(translation.Z, out var dx, out var dy, out var sigmaX, out var sigmaY, out var shadowColor);

		Visual.ShadowState = new(dx, dy, sigmaX, sigmaY, shadowColor);
	}

	private void ComputeDropShadowValues(float offsetZ, out float dx, out float dy, out float sigmaX, out float sigmaY, out Color shadowColor)
	{
		// Following math magic seems to follow UWP ThemeShadow quite nicely.
		float alpha;
		if (offsetZ <= ShadowOffsetMax)
		{
			// Alpha should slightly decrease as the offset increases
			alpha = 1.0f - (offsetZ * ShadowOffsetAlphaModifier);
		}
		else
		{
			alpha = ShadowAlphaFallback;
			offsetZ = ShadowOffsetMax;
		}

		// Make black less prominent
		alpha = alpha * ShadowAlphaMultiplier;

		dx = 0;
		dy = offsetZ / 2 - offsetZ * ShadowSigmaYModifier;
		sigmaX = offsetZ * ShadowSigmaXModifier;
		sigmaY = offsetZ * ShadowSigmaYModifier;
		shadowColor = Colors.Black.WithOpacity(alpha);
	}
}
