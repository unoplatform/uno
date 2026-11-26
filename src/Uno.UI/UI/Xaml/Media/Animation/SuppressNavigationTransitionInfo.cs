// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference ThemeTransitions.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Media.Animation;

/// <summary>
/// Specifies that animated transitions are suppressed during navigation.
/// </summary>
public partial class SuppressNavigationTransitionInfo : NavigationTransitionInfo
{
	public SuppressNavigationTransitionInfo() : base() { }

	/// <inheritdoc />
	protected override string GetNavigationStateCore() => "0";

	/// <inheritdoc />
	protected override void SetNavigationStateCore(string navigationState)
	{
	}

	// The transition uses an empty storyboard.
	private protected override IList<Storyboard> CreateStoryboardsCore(UIElement element, NavigationTrigger trigger)
		=> new List<Storyboard>();
}
