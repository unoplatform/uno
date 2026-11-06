// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ContainerContentChangingEventArgs_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d


#nullable enable

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial class ContainerContentChangingEventArgs
{
	// reset the args in such a way that it is not holding on to any other structures
	internal void ResetLifetime()
	{
		Callback = null;
		Item = null!;
	}

	// Register this container for a callback on the next phase
	public void RegisterUpdateCallback(TypedEventHandler<ListViewBase, ContainerContentChangingEventArgs> callback)
		=> RegisterUpdateCallback(m_phase + 1, callback);

	// Register this container for a callback on a specific phase
	public void RegisterUpdateCallback(uint callbackPhase, TypedEventHandler<ListViewBase, ContainerContentChangingEventArgs> callback)
	{
		m_phase = callbackPhase;
		Callback = callback;
		WantsCallBack = true;
	}
}

