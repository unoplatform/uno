// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference ThemeTransitions.h

namespace Microsoft.UI.Xaml.Media.Animation;

/// <summary>
/// Internal enum used by NavigationTransitionInfo types to determine
/// which animation to play based on navigation direction.
/// </summary>
internal enum NavigationTrigger
{
	/// <summary>
	/// Forward navigation - animating the old page out.
	/// </summary>
	NavigatingAway = 0,

	/// <summary>
	/// Forward navigation - animating the new page in.
	/// </summary>
	NavigatingTo = 1,

	/// <summary>
	/// Back navigation - animating the current page out.
	/// </summary>
	BackNavigatingAway = 2,

	/// <summary>
	/// Back navigation - animating the previous page in.
	/// </summary>
	BackNavigatingTo = 3,
}
