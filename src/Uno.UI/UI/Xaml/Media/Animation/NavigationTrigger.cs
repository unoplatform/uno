// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference src/dxaml/xcp/dxaml/idl/winrt/main/microsoft.ui.xaml.private.idl, tag winui3/release/2.5.1, commit ba3a8d59e

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
