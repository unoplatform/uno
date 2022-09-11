// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference PullToRefreshHelperTestApi.cpp, commit d883cf3

using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Microsoft.UI.Private.Controls;

internal static partial class PullToRefreshHelperTestApi
{
	public static RefreshInteractionRatioChangedEventArgs CreateRefreshInteractionRatioChangedEventArgsInstance(double value) =>
		new RefreshInteractionRatioChangedEventArgs(value);

	public static RefreshStateChangedEventArgs CreateRefreshStateChangedEventArgsInstance(RefreshVisualizerState oldValue, RefreshVisualizerState newValue) =>
		new RefreshStateChangedEventArgs(oldValue, newValue);

	public static RefreshRequestedEventArgs CreateRefreshRequestedEventArgsInstance(Deferral handler) =>
		new RefreshRequestedEventArgs(handler);
}
