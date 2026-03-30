// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBaseItemChrome.cpp, tag winui3/release/1.4.2

#nullable enable

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Constants used by ListViewBaseItemChrome for sizing and positioning chrome elements.
/// </summary>
internal static class ListViewBaseItemChromeConstants
{
	// Size of check mark visual (old style)
	public static readonly Size SelectionCheckMarkVisualSize = new(40.0f, 40.0f);

	// Focus rectangle thickness
	public static readonly Thickness FocusBorderThickness = new(2.0f, 2.0f, 2.0f, 2.0f);

	// Offset from top-right corner of control border bounds
	public static readonly Point CheckmarkOffset = new(-20.0f, 6.0f);

	// Offsets for swipe hint animations
	public static readonly Point SwipeHintOffset = new(-23.0f, 15.0f);

	// Opacity values
	public const float SwipingCheckSteadyStateOpacity = 0.5f;
	public const float SelectingSwipingCheckSteadyStateOpacity = 1.0f;
	public const float OpacityUnset = -1.0f;

	// Corner radii
	public const float GeneralCornerRadius = 4.0f;
	public const float InnerBorderCornerRadius = 3.0f;

	// Selection indicator
	public static readonly Size SelectionIndicatorSize = new(3.0f, 16.0f);
	public const float SelectionIndicatorHeightShrinkage = 6.0f;
	public static readonly Thickness SelectionIndicatorMargin = new(4.0f, 20.0f, 0.0f, 20.0f);

	// Multi-select checkbox
	public static readonly Size MultiSelectSquareSize = new(20.0f, 20.0f);
	public static readonly Thickness MultiSelectSquareThickness = new(2.0f);
	public static readonly Thickness MultiSelectRoundedSquareThickness = new(1.0f);
	public static readonly Thickness MultiSelectSquareInlineMargin = new(12.0f, 0.0f, 0.0f, 0.0f);
	public static readonly Thickness MultiSelectRoundedSquareInlineMargin = new(14.0f, 0.0f, 0.0f, 0.0f);
	public static readonly Thickness MultiSelectSquareOverlayMargin = new(0.0f, 2.0f, 2.0f, 0.0f);

	// Backplate/border
	public static readonly Thickness BackplateMargin = new(4.0f, 2.0f, 4.0f, 2.0f);
	public static readonly Thickness BorderThickness = new(1.0f);
	public static readonly Thickness InnerSelectionBorderThickness = new(1.0f);

	// Content offsets
	public const float ListViewItemMultiSelectContentOffset = 32.0f;
	public const float MultiSelectRoundedContentOffset = 28.0f;

	// CheckMark path points
	public static readonly Point[] CheckMarkPoints =
	[
		new(0.0f, 7.0f),
		new(2.2f, 4.3f),
		new(6.1f, 7.9f),
		new(12.4f, 0.0f),
		new(15.0f, 2.4f),
		new(6.6f, 13.0f)
	];

	// Focus border thickness
	public const float ListViewItemFocusBorderThickness = 1.0f;
	public const float GridViewItemFocusBorderThickness = 2.0f;

	// CheckMark glyph
	public const float CheckMarkGlyphFontSize = 16.0f;
	public const string CheckMarkGlyph = "\uE73E";

	// Default values for rounded chrome
	public static readonly CornerRadius DefaultSelectionIndicatorCornerRadius = new(1.5f);
	public static readonly CornerRadius DefaultCheckBoxCornerRadius = new(3.0f);
	public static readonly Thickness SelectedBorderThicknessRounded = new(2.0f);
	public static readonly Thickness SelectedBorderThickness = new(0.0f);

	// Animation durations (in milliseconds)
	public const int MultiSelectAnimationDuration = 250;
	public const int MultiSelectReturnAnimationDuration = 167;
	public const int SelectionIndicatorAnimationDuration = 150;

	// Animation target offsets
	public const float MultiSelectCheckBoxOffset = 28.0f;
}
