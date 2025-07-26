// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TeachingTip\TeachingTipClosedEventArgs.cpp, tag winui3/release/1.5.0, commit c8bd154c0

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides data for the Closed event.
/// </summary>
public partial class TeachingTipClosedEventArgs
{
	internal TeachingTipClosedEventArgs(TeachingTipCloseReason reason) =>
		Reason = reason;

	/// <summary>
	/// Gets a constant that specifies whether the cause of the Closed
	/// event was due to user interaction (Close button click), light-dismissal, or programmatic closure.
	/// </summary>
	public TeachingTipCloseReason Reason { get; } = TeachingTipCloseReason.CloseButton;
}
