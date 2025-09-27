// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RefreshContainer.idl, commit c6174f1

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines constants that specify the direction to pull a RefreshContainer to initiate a refresh.
/// </summary>
public enum RefreshPullDirection
{
	/// <summary>
	/// Pull from left to right to initiate a refresh.
	/// </summary>
	LeftToRight = 0,

	/// <summary>
	/// Pull from top to bottom to initiate a refresh.
	/// </summary>
	TopToBottom = 1,

	/// <summary>
	/// Pull from right to left to initiate a refresh.
	/// </summary>
	RightToLeft = 2,

	/// <summary>
	/// Pull from bottom to top to initiate a refresh.
	/// </summary>
	BottomToTop = 3,
}
