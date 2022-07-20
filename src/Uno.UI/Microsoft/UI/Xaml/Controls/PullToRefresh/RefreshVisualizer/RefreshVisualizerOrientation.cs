// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RefreshVisualizer.idl, commit c6174f1

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines constants that specify the starting position of a RefreshVisualizer 's progress spinner.
/// </summary>
public enum RefreshVisualizerOrientation
{
	/// <summary>
	/// The progress spinner automatically rotates so the arrow starts in the appropriate position for the PullDirection.
	/// </summary>
	Auto = 0,

	/// <summary>
	/// The progress spinner default position.
	/// </summary>
	Normal = 1,

	/// <summary>
	/// The progress spinner is rotated 90 degrees counter-clockwise from Normal.
	/// </summary>
	Rotate90DegreesCounterclockwise = 2,

	/// <summary>
	/// The progress spinner is rotated 270 degrees counter-clockwise from Normal.
	/// </summary>
	Rotate270DegreesCounterclockwise = 3,
}
