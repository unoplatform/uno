// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TeachingTip\TeachingTip.idl, commit c8bd154c

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines constants that indicate the cause of the TeachingTip closure.
/// </summary>
public enum TeachingTipCloseReason
{
	/// <summary>
	/// The teaching tip was closed by the user clicking the close button.
	/// </summary>
	CloseButton,

	/// <summary>
	/// The teaching tip was closed by light-dismissal.
	/// </summary>
	LightDismiss,

	/// <summary>
	/// The teaching tip was programmatically closed.
	/// </summary>
	Programmatic,
}
