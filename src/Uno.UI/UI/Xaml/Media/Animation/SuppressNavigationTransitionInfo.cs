// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference ThemeTransitions.cpp, tag winui3/release/1.7-stable

using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Media.Animation;

/// <summary>
/// Specifies that animated transitions are suppressed during navigation.
/// </summary>
public partial class SuppressNavigationTransitionInfo : NavigationTransitionInfo
{
	public SuppressNavigationTransitionInfo() : base() { }

	/// <summary>
	/// The SuppressNavigationTransitionInfo uses an empty storyboard list,
	/// effectively suppressing any navigation animation.
	/// </summary>
	protected override IList<Storyboard> CreateStoryboardsCore(UIElement element, NavigationTrigger trigger)
		=> new List<Storyboard>();
}

