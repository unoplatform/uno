#nullable enable

using System.Globalization;
using Microsoft.UI.Xaml.Shapes;

namespace Microsoft.UI.Xaml.Controls;

partial class AnimatedIcon
{
	// WinUI's AnimatedIcon derives from PathIcon natively (DeriveFromPathIconHelper_base), so CPathIcon::ApplyTemplate
	// creates its Path. Uno's AnimatedIcon derives from IconElement and mirrors it. OnApplyTemplate collapses
	// the Path and the fallback icon goes at index 1.
	private protected override void ApplyTemplate(out bool addedVisuals)
	{
		addedVisuals = false;

		if (GetChildElementNoRef() is null)
		{
			var path = new Path
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch,
			};
			AddChildElement(path);

			UpdateLinkedChildProperty(ForegroundProperty, Shape.FillProperty);

			addedVisuals = true;
		}
	}

	// The IconElement_Foreground case of CPathIcon::OnPropertyChanged.
	private protected override void OnForegroundChanged(DependencyPropertyChangedEventArgs e) =>
		UpdateLinkedChildProperty(ForegroundProperty, Shape.FillProperty);

	// Equivalent of `wcstof(str, &strEnd)` followed by `strEnd == str + size`: true when the whole string is a float.
	private static bool Wcstof(string value, out float result)
	{
		if (value.Length == 0)
		{
			// wcstof consumes nothing, so strEnd is both the start and the end of the empty string.
			result = 0.0f;
			return true;
		}

		return float.TryParse(
			value.TrimStart(),
			NumberStyles.Float & ~NumberStyles.AllowTrailingWhite,
			CultureInfo.InvariantCulture,
			out result);
	}

	private void OnIconUnloaded(object sender, RoutedEventArgs args)
	{
		m_foregroundColorPropertyChangedRevoker.Disposable = null;

		// m_progressPropertySet lives on the app-lifetime shared compositor, so a Progress
		// animation left running as the icon leaves the tree keeps it (via the property set's
		// Owner visual) registered there. Stopping it releases that registration.
		m_progressPropertySet?.StopAnimation(s_progressPropertyName);
	}
}
