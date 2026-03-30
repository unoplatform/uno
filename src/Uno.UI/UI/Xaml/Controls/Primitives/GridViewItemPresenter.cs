// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference GridViewItemPresenter_Partial.h, GridViewItemPresenter_Partial.cpp, tag winui3/release/1.4.2

#nullable enable

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Represents the visual elements of a GridViewItem. GridViewItemPresenter is designed to handle
/// the visual representation of a grid item and can include visual states, selection visuals,
/// and other presentation features.
/// </summary>
public partial class GridViewItemPresenter
{
	// Chrome helper for state management and animations (lazy initialized to avoid constructor conflicts)
	private GridViewItemPresenterChrome? _chrome;
	private GridViewItemPresenterChrome Chrome => _chrome ??= new GridViewItemPresenterChrome(this);

	/// <summary>
	/// Called when the VisualStateManager requests a visual state change.
	/// </summary>
	protected override bool GoToElementStateCore(string stateName, bool useTransitions)
	{
		// Delegate to the chrome's state management
		var wentToState = Chrome.GoToChromedState(stateName, useTransitions);

		// Process any queued animation commands
		Chrome.ProcessAnimationCommands();

		return wentToState;
	}
}
