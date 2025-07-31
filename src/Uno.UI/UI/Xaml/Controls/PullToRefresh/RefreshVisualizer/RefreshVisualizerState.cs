// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RefreshVisualizer.idl, commit c6174f1

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines constants that specify the state of a RefreshVisualizer.
/// </summary>
public enum RefreshVisualizerState
{
	/// <summary>
	/// The visualizer is idle.
	/// </summary>
	Idle = 0,

	/// <summary>
	/// The visualizer was pulled in the refresh direction from a position where
	/// a refresh is not allowed. Typically, the ScrollViewer was not at
	/// position 0 at the start of the pull.
	/// </summary>
	Peeking = 1,

	/// <summary>
	/// The user is interacting with the visualizer.
	/// </summary>
	Interacting = 2,

	/// <summary>
	/// The visualizer is pending.
	/// </summary>
	Pending = 3,

	/// <summary>
	/// The visualizer is being refreshed.
	/// </summary>
	Refreshing = 4,
}
