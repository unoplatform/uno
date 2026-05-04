// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextFormatting.cpp, tag winui3/release/1.8.1, line 181

using System;
using Uno.UI;

namespace Uno.UI.Xaml.Core;

/// <summary>
/// Provides the WinUI text scaling formula that applies a logarithmic
/// scaling function to font sizes based on the OS text scale factor.
/// </summary>
internal static class TextScaleHelper
{
	/// <summary>
	/// Computes the scaled font size using the WinUI logarithmic formula.
	/// Smaller fonts receive more scaling than larger fonts.
	/// </summary>
	/// <param name="inputFontSize">The original font size.</param>
	/// <param name="fontScale">The OS text scale factor (1.0 = no scaling).</param>
	/// <param name="isTextScaleFactorEnabled">Whether text scaling is enabled for this element.</param>
	/// <returns>The scaled font size.</returns>
	internal static double GetScaledFontSize(double inputFontSize, double fontScale, bool isTextScaleFactorEnabled)
	{
		if (!isTextScaleFactorEnabled || inputFontSize <= 0)
		{
			return inputFontSize;
		}

		// WinUI only supports scale factors >= 1.0. Clamp to avoid reducing font sizes
		// or producing negative values (which can occur on Android/iOS with scale < 1.0).
		if (fontScale <= 1.0)
		{
			return inputFontSize;
		}

		double capped = Math.Max(inputFontSize, 1.0);

		// s_o = s_i + max(-e * ln(s_i) + 18, 0) * (f - 1)
		return capped + Math.Max(-Math.E * Math.Log(capped) + 18, 0.0) * (fontScale - 1);
	}

	/// <summary>
	/// Gets the effective font scale factor, applying FeatureConfiguration overrides and clamping.
	/// </summary>
	internal static double GetEffectiveFontScale(double osFontScale)
	{
		if (FeatureConfiguration.Font.IgnoreTextScaleFactor)
		{
			return 1.0;
		}

		var scale = FeatureConfiguration.Font.TextScaleFactor ?? osFontScale;

		if (FeatureConfiguration.Font.MaximumTextScaleFactor is { } max && scale > max)
		{
			scale = max;
		}

		return Math.Max(scale, 1.0);
	}
}
