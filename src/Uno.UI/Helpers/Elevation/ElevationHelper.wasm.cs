namespace Uno.UI.Helpers;

internal static class ElevationHelper
{
	internal static void SetElevation(DependencyObject element, double elevation, Color shadowColor)
	{
		if (element is UIElement uiElement)
		{
			return;
		}

		if (elevation > 0)
		{
			// Values for 1dp elevation according to https://material.io/guidelines/resources/shadows.html#shadows-illustrator
			const double x = 0.25d;
			const double y = 0.92f * 0.5f; // Looks more accurate than the recommended 0.92f.
			const double blur = 0.5f;
			var color = Color.FromArgb((byte)(shadowColor.A * .35), shadowColor.R, shadowColor.G, shadowColor.B);

			var str = $"{(x * elevation).ToStringInvariant()}px {(y * elevation).ToStringInvariant()}px {(blur * elevation).ToStringInvariant()}px {color.ToCssString()}";
			uiElement.SetStyle("box-shadow", str);
			uiElement.SetCssClasses("noclip");
		}
		else
		{
			uiElement.ResetStyle("box-shadow");
			uiElement.UnsetCssClasses("noclip");
		}
	}
}
