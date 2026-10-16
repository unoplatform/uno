using System;
using UIKit;

namespace Windows.UI.ViewManagement;

public partial class UISettings
{
	// The body size at the default content size category: 17pt on iOS, but 29pt on tvOS.
	private static readonly Lazy<double> _defaultBodyPointSize = new(() =>
		UIFont.GetPreferredFontForTextStyle(UIFontTextStyle.Body, UITraitCollection.Create(UIContentSizeCategory.Large)).PointSize);

	public double TextScaleFactor
	{
		get => GetTextScaleFactorValue();
	}

	internal static double GetTextScaleFactorValue() => UIFont.PreferredBody.PointSize / _defaultBodyPointSize.Value;
}
