using Uno.UI.Composition.Composition;
using Windows.UI;
using Windows.UI.Xaml;

namespace Uno.UI.Helpers;

internal static class ElevationHelper
{
	internal static void SetElevation(DependencyObject element, double elevation, Color shadowColor)
	{
		if (element is not UIElement uiElement)
		{
			return;
		}

		var visual = uiElement.Visual;

		const float SHADOW_SIGMA_X_MODIFIER = 1f / 3.5f;
		const float SHADOW_SIGMA_Y_MODIFIER = 1f / 3.5f;
		float x = 0.3f;
		float y = 0.92f * 0.5f;

		var dx = (float)elevation * x;
		var dy = (float)elevation * y;
		var sigmaX = (float)elevation * SHADOW_SIGMA_X_MODIFIER;
		var sigmaY = (float)elevation * SHADOW_SIGMA_Y_MODIFIER;
		var shadow = new ShadowState(dx, dy, sigmaX, sigmaY, shadowColor);
		visual.ShadowState = shadow;
	}
}
