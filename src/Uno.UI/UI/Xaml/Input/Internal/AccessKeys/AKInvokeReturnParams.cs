// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\AccessKeys\inc\AKCommon.h, tag winui3/release/1.5.3

#nullable enable

using System;
using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Input.AccessKeys;

/// <summary>
/// Return parameters from an access key invoke operation.
/// </summary>
internal struct AKInvokeReturnParams
{
	/// <summary>
	/// When we've found an element within the Scope to invoke, we set this to true.
	/// </summary>
	internal bool InvokeAttempted;

	/// <summary>
	/// When we try to invoke the element, but we were unable to find a pattern, we set this to false.
	/// </summary>
	internal bool InvokeFoundValidPattern;

	/// <summary>
	/// The element we are trying to invoke.
	/// </summary>
	internal WeakReference<DependencyObject>? InvokedElement;

	/// <summary>
	/// Creates a new instance with default values.
	/// </summary>
	internal static AKInvokeReturnParams Default => new()
	{
		InvokeAttempted = false,
		InvokeFoundValidPattern = true, // default true - only set false if invoke attempted but no pattern found
		InvokedElement = null
	};
}
