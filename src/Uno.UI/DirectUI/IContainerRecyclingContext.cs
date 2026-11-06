// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference IContainerRecyclingContext.g.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using Microsoft.UI.Xaml;

namespace DirectUI;

internal interface IContainerRecyclingContext
{
	UIElement? SelectedContainer { get; set; }

	void PrepareForItemRecycling(object? item);

	bool IsCompatible(UIElement candidate);

	void ConfigureSelectedContainer(UIElement container);

	void StopRecycling();
}
