// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Microsoft.UI.Xaml.Controls.cs, tag winui3/release/1.8.4, commit dc46907e92

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides event data for the ViewChangeStarted and ViewChangeCompleted events.
/// </summary>
public partial class SemanticZoomViewChangedEventArgs
{
	/// <summary>
	/// Initializes a new instance of the SemanticZoomViewChangedEventArgs class.
	/// </summary>
	public SemanticZoomViewChangedEventArgs()
	{
	}

	/// <summary>
	/// Provides information about the item and its bounds, once the view change is complete.
	/// </summary>
	public SemanticZoomLocation DestinationItem { get; set; } = null!;

	/// <summary>
	/// Gets or sets a value that indicates whether the starting view is the ZoomedInView.
	/// </summary>
	public bool IsSourceZoomedInView { get; set; }

	/// <summary>
	/// Provides information about the item and its bounds, for the item as represented in the previous view.
	/// </summary>
	public SemanticZoomLocation SourceItem { get; set; } = null!;
}
