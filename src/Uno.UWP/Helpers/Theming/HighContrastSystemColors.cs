#nullable enable

using Windows.UI;

namespace Uno.Helpers.Theming;

// MUX Reference FrameworkTheming.cpp, tag winui3/release/1.4.2
// Maps to the system color resources created in RebuildColorAndBrushResources.
internal readonly record struct HighContrastSystemColors(
	Color ButtonFaceColor,
	Color ButtonTextColor,
	Color GrayTextColor,
	Color HighlightColor,
	Color HighlightTextColor,
	Color HotlightColor,
	Color WindowColor,
	Color WindowTextColor,
	Color ActiveCaptionColor,
	Color BackgroundColor,
	Color CaptionTextColor,
	Color InactiveCaptionColor,
	Color InactiveCaptionTextColor,
	Color DisabledTextColor);