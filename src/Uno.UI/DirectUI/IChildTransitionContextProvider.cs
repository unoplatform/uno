// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference IChildTransitionContextProvider.g.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace DirectUI;

internal interface IChildTransitionContextProvider
{
	ThemeTransitionContext GetChildTransitionContext(UIElement element, int layoutTickId);

	Rect GetChildTransitionBounds(UIElement element);

	bool IsCollectionMutatingFast();
}
