// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ThemeTransitions.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Media.Animation;

/// <summary>
/// Provides page navigation animations.
/// </summary>
[ContentProperty(Name = nameof(DefaultNavigationTransitionInfo))]
public partial class NavigationThemeTransition : Transition
{
	/// <summary>
	/// Initializes a new instance of the NavigationThemeTransition class.
	/// </summary>
	public NavigationThemeTransition()
	{
	}
}
