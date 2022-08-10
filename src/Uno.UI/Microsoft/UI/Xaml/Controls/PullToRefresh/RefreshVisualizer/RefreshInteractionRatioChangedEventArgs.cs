// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RefreshInteractionRatioChangedEventArgs.cpp, commit d883cf3

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides event data.
/// </summary>
public sealed partial class RefreshInteractionRatioChangedEventArgs
{
	internal RefreshInteractionRatioChangedEventArgs(double interactionRatio)
	{
		InteractionRatio = interactionRatio;
	}

	/// <summary>
	/// Gets the interaction ratio value.
	/// </summary>
	public double InteractionRatio { get; }
}
