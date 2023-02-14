// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ScrollViewerIRefreshInfoProviderAdapter.idl, commit c6174f1

using Microsoft.UI.Composition.Interactions;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Private.Controls;

internal interface IAdapterAnimationHandler
{
	void InteractionTrackerAnimation(UIElement refreshVisualizer, UIElement infoProvider, InteractionTracker interactionTracker);

	void RefreshRequestedAnimation(UIElement refreshVisualizer, UIElement infoProvider, double executionRatio);

	void RefreshCompletedAnimation(UIElement refreshVisualizer, UIElement infoProvider);
}
