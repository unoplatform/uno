#nullable enable
using System;

namespace Uno.UI;

/// <summary>
/// Font variants preloaded at startup, used by <see cref="FeatureConfiguration.Font.PreloadedVariants"/>.
/// </summary>
/// <remarks>
/// Weight flags select upright, normal-width faces. <see cref="Italic"/> and <see cref="Condensed"/>
/// widen that selection to the italic and condensed forms of the selected weights.
/// </remarks>
[Flags]
public enum FontPreloadVariants
{
	/// <summary>No font is preloaded; every face loads on first use.</summary>
	None = 0,

	Thin = 1 << 0,
	ExtraLight = 1 << 1,
	Light = 1 << 2,
	Normal = 1 << 3,
	Medium = 1 << 4,
	SemiBold = 1 << 5,
	Bold = 1 << 6,
	ExtraBold = 1 << 7,
	Black = 1 << 8,

	/// <summary>Also preload the italic form of each selected weight.</summary>
	Italic = 1 << 16,

	/// <summary>Also preload the condensed and semi-condensed widths of each selected weight.</summary>
	Condensed = 1 << 17,

	/// <summary>Every variant declared by the font manifest.</summary>
	All = ~0,
}
