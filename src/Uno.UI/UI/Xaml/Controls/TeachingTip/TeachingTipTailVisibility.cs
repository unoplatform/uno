// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TeachingTip\TeachingTip.idl, commit c8bd154c

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines constants that specify whether a teaching tip's Tail is visible or collapsed.
/// </summary>
public enum TeachingTipTailVisibility
{
	/// <summary>
	/// The teaching tip's tail is collapsed when non-targeted and visible when the targeted.
	/// </summary>
	Auto,

	/// <summary>
	/// The teaching tip's tail is visible.
	/// </summary>
	Visible,

	/// <summary>
	/// The teaching tip's tail is collapsed.
	/// </summary>
	Collapsed,
}
