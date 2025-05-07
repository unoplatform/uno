// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TeachingTip\TeachingTip.idl, commit c8bd154c

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines constants that indicate the preferred location of the HeroContent within a teaching tip.
/// </summary>
public enum TeachingTipHeroContentPlacementMode
{
	/// <summary>
	/// The header of the teaching tip.
	/// The hero content might be moved to the footer to avoid intersecting with the tail of the targeted teaching tip.
	/// </summary>
	Auto,

	/// <summary>
	/// The header of the teaching tip.
	/// </summary>
	Top,

	/// <summary>
	/// The footer of the teaching tip.
	/// </summary>
	Bottom,
}
