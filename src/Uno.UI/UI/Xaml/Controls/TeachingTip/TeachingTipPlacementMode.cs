// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TeachingTip\TeachingTip.idl, commit c8bd154c

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines constants that indicate the preferred location of the TeachingTip teaching tip.
/// </summary>
public enum TeachingTipPlacementMode
{
	/// <summary>
	/// Along the bottom side of the xaml root when non-targeted and above the target element when targeted.
	/// </summary>
	Auto,

	/// <summary>
	/// Along the top side of the xaml root when non-targeted and above the target element when targeted.
	/// </summary>
	Top,

	/// <summary>
	/// Along the bottom side of the xaml root when non-targeted and below the target element when targeted.
	/// </summary>
	Bottom,

	/// <summary>
	/// Along the left side of the xaml root when non-targeted and left of the target element when targeted.
	/// </summary>
	Left,

	/// <summary>
	/// Along the right side of the xaml root when non-targeted and right of the target element when targeted.
	/// </summary>
	Right,

	/// <summary>
	/// The top right corner of the xaml root when non-targeted and above the target element expanding rightward when targeted.
	/// </summary>
	TopRight,

	/// <summary>
	/// The top left corner of the xaml root when non-targeted and above the target element expanding leftward when targeted.
	/// </summary>
	TopLeft,

	/// <summary>
	/// The bottom right corner of the xaml root when non-targeted and below the target element expanding rightward when targeted.
	/// </summary>
	BottomRight,

	/// <summary>
	/// The bottom left corner of the xaml root when non-targeted and below the target element expanding leftward when targeted.
	/// </summary>
	BottomLeft,

	/// <summary>
	/// The top left corner of the xaml root when non-targeted and left of the target element expanding upward when targeted.
	/// </summary>
	LeftTop,

	/// <summary>
	/// The bottom left corner of the xaml root when non-targeted and left of the target element expanding downward when targeted.
	/// </summary>
	LeftBottom,

	/// <summary>
	/// The top right corner of the xaml root when non-targeted and right of the target element expanding upward when targeted.
	/// </summary>
	RightTop,

	/// <summary>
	/// The bottom right corner of the xaml root when non-targeted and right of the target element expanding downward when targeted.
	/// </summary>
	RightBottom,

	/// <summary>
	/// The center of the xaml root when non-targeted and pointing at the center of the target element when targeted.
	/// </summary>
	Center
}
