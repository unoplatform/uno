// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ContainerContentChangingEventArgs_Partial.h, ContainerContentChangingEventArgs.g.h, tag winui3/release/2.5.1, commit ba3a8d59e6d


#nullable enable

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial class ContainerContentChangingEventArgs
{
	internal const int NoBindingNeededToken = -1;

	private UIElement? m_pContainerBackPointer;
	private int m_xbindingPhase = NoBindingNeededToken;

	// ContainerContentChangingEventArgs.g.h backing fields.
	private uint m_phase;
	private bool m_wantsCallBack;
	private bool m_contentShouldBeSet;
	private TypedEventHandler<ListViewBase, ContainerContentChangingEventArgs>? m_callback;

	internal void RegisterBackpointerToContainer(UIElement pOwner) => m_pContainerBackPointer = pOwner;

	internal int GetBindingPhase() => m_xbindingPhase;

	internal void SetBindingPhase(int phase) => m_xbindingPhase = phase;

	internal bool GetWantsBindingPhaseCallback() => m_xbindingPhase != NoBindingNeededToken;

	internal uint GetLowestWantedPhase()
	{
		uint lowestPhase = unchecked((uint)(GetWantsBindingPhaseCallback() ? m_xbindingPhase : -1)); // wants _public_ callback, which is what m_phase is indicating
		if ((m_wantsCallBack && (GetWantsBindingPhaseCallback() && m_phase < (ulong)m_xbindingPhase)) || !GetWantsBindingPhaseCallback())
		{
			lowestPhase = m_phase;
		}
		return lowestPhase;
	}

	public uint Phase
	{
		get => m_phase;
		internal set => m_phase = value;
	}

	internal bool WantsCallBack
	{
		get => m_wantsCallBack;
		set => m_wantsCallBack = value;
	}

	internal bool ContentShouldBeSet
	{
		get => m_contentShouldBeSet;
		set => m_contentShouldBeSet = value;
	}

	internal TypedEventHandler<ListViewBase, ContainerContentChangingEventArgs>? Callback
	{
		get => m_callback;
		set => m_callback = value;
	}
}

