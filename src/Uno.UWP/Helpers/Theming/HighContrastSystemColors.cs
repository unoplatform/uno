#nullable enable

using Windows.UI;

namespace Uno.Helpers.Theming;

internal readonly record struct HighContrastSystemColors(
	Color ButtonFaceColor,
	Color ButtonTextColor,
	Color GrayTextColor,
	Color HighlightColor,
	Color HighlightTextColor,
	Color HotlightColor,
	Color WindowColor,
	Color WindowTextColor);