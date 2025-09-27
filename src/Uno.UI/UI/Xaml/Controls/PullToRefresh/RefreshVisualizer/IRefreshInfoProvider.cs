// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference RefreshVisualizerPrivate.idl, commit c6174f1

#nullable enable

using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Microsoft.UI.Composition;

namespace Microsoft.UI.Private.Controls;

internal partial interface IRefreshInfoProvider
{
	void OnRefreshStarted();

	void OnRefreshCompleted();

	bool IsInteractingForRefresh { get; }

	CompositionPropertySet CompositionProperties { get; }

	string InteractionRatioCompositionProperty { get; }

	double ExecutionRatio { get; }

	event TypedEventHandler<IRefreshInfoProvider, object> IsInteractingForRefreshChanged;

	event TypedEventHandler<IRefreshInfoProvider, RefreshInteractionRatioChangedEventArgs> InteractionRatioChanged;

	event TypedEventHandler<IRefreshInfoProvider, object> RefreshStarted;

	event TypedEventHandler<IRefreshInfoProvider, object> RefreshCompleted;
}
