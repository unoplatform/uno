// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\FlyoutShowOptions_partial.cpp, tag winui3/release/1.4.3, commit 685d2bf

using Uno.UI.UI.Xaml.Controls.Flyout.Mux;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls.Primitives;

/// <summary>
/// Represents the options used to show a flyout.
/// </summary>
public partial class FlyoutShowOptions
{
	/// <summary>
	/// Initializes a new instance of the FlyoutShowOptions class.
	/// </summary>
	public FlyoutShowOptions()
	{
	}

	/// <summary>
	/// Gets or sets a rectangular area that the flyout tries to not overlap.
	/// </summary>
	/// <remarks>
	/// In some cases, the flyout will not be able to honor the exclusion rect;
	/// for example, if there is not sufficient space between the exclusion rect and the window edge.
	/// </remarks>
	public Rect? ExclusionRect { get; set; }
	
	/// <summary>
	/// Gets or sets the position where the flyout opens.
	/// </summary>
	public Point? Position { get; set; }

	/// <summary>
	/// Gets or sets a value that indicates where the flyout is placed in relation to its target element.
	/// </summary>
	public FlyoutPlacementMode Placement { get; set; } = FlyoutPlacementMode.Auto;

	/// <summary>
	/// Gets or sets a value that indicates how the flyout behaves when opened.
	/// </summary>
	public FlyoutShowMode ShowMode { get; set; } = FlyoutShowMode.Auto;
}
