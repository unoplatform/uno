// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Microsoft.UI.Xaml.Controls.cs, tag winui3/release/1.8.4, commit dc46907e92

#nullable enable

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Communicates information for items and view state in a SemanticZoom.
/// </summary>
public partial class SemanticZoomLocation
{
	/// <summary>
	/// Initializes a new instance of the SemanticZoomLocation class.
	/// </summary>
	public SemanticZoomLocation()
	{
	}

	/// <summary>
	/// Gets or sets the sizing bounds of the item as it exists in the current view of a SemanticZoom.
	/// </summary>
	public Rect Bounds { get; set; }

	/// <summary>
	/// Gets or sets the display item as it exists in the current view of a SemanticZoom.
	/// </summary>
	public object? Item { get; set; }

	internal Point ZoomPoint { get; set; }

	internal Rect Remainder { get; set; }

	internal bool IsBottomAlignment { get; set; }
}
