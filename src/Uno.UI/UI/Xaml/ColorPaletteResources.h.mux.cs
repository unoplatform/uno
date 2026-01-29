// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\dxaml\xcp\components\resources\ScopedResources.cpp, tag winui3/release/1.4.2

using System.Collections.Generic;

namespace Microsoft.UI.Xaml;

partial class ColorPaletteResources
{
	/// <summary>
	/// Enumerates the color palette properties that map to system color resource keys.
	/// </summary>
	private enum ColorPaletteProperty
	{
		Accent,
		AltHigh,
		AltLow,
		AltMedium,
		AltMediumHigh,
		AltMediumLow,
		BaseHigh,
		BaseLow,
		BaseMedium,
		BaseMediumHigh,
		BaseMediumLow,
		ChromeAltLow,
		ChromeBlackHigh,
		ChromeBlackLow,
		ChromeBlackMedium,
		ChromeBlackMediumLow,
		ChromeDisabledHigh,
		ChromeDisabledLow,
		ChromeGray,
		ChromeHigh,
		ChromeLow,
		ChromeMedium,
		ChromeMediumLow,
		ChromeWhite,
		ErrorText,
		ListLow,
		ListMedium,
	}

	/// <summary>
	/// Maps color palette properties to their corresponding system resource keys.
	/// </summary>
	private static readonly Dictionary<ColorPaletteProperty, string> s_propertyToKeyMap = new()
	{
		{ ColorPaletteProperty.Accent, "SystemAccentColor" },
		{ ColorPaletteProperty.AltHigh, "SystemAltHighColor" },
		{ ColorPaletteProperty.AltLow, "SystemAltLowColor" },
		{ ColorPaletteProperty.AltMedium, "SystemAltMediumColor" },
		{ ColorPaletteProperty.AltMediumHigh, "SystemAltMediumHighColor" },
		{ ColorPaletteProperty.AltMediumLow, "SystemAltMediumLowColor" },
		{ ColorPaletteProperty.BaseHigh, "SystemBaseHighColor" },
		{ ColorPaletteProperty.BaseLow, "SystemBaseLowColor" },
		{ ColorPaletteProperty.BaseMedium, "SystemBaseMediumColor" },
		{ ColorPaletteProperty.BaseMediumHigh, "SystemBaseMediumHighColor" },
		{ ColorPaletteProperty.BaseMediumLow, "SystemBaseMediumLowColor" },
		{ ColorPaletteProperty.ChromeAltLow, "SystemChromeAltLowColor" },
		{ ColorPaletteProperty.ChromeBlackHigh, "SystemChromeBlackHighColor" },
		{ ColorPaletteProperty.ChromeBlackLow, "SystemChromeBlackLowColor" },
		{ ColorPaletteProperty.ChromeBlackMedium, "SystemChromeBlackMediumColor" },
		{ ColorPaletteProperty.ChromeBlackMediumLow, "SystemChromeBlackMediumLowColor" },
		{ ColorPaletteProperty.ChromeDisabledHigh, "SystemChromeDisabledHighColor" },
		{ ColorPaletteProperty.ChromeDisabledLow, "SystemChromeDisabledLowColor" },
		{ ColorPaletteProperty.ChromeGray, "SystemChromeGrayColor" },
		{ ColorPaletteProperty.ChromeHigh, "SystemChromeHighColor" },
		{ ColorPaletteProperty.ChromeLow, "SystemChromeLowColor" },
		{ ColorPaletteProperty.ChromeMedium, "SystemChromeMediumColor" },
		{ ColorPaletteProperty.ChromeMediumLow, "SystemChromeMediumLowColor" },
		{ ColorPaletteProperty.ChromeWhite, "SystemChromeWhiteColor" },
		{ ColorPaletteProperty.ErrorText, "SystemErrorTextColor" },
		{ ColorPaletteProperty.ListLow, "SystemListLowColor" },
		{ ColorPaletteProperty.ListMedium, "SystemListMediumColor" },
	};

	/// <summary>
	/// Tracks which properties have been set via the typed property accessors.
	/// Used for potential conflict detection when the same key is also set via x:Key.
	/// </summary>
	private readonly HashSet<ColorPaletteProperty> _overridesFromProperties = new();
}
