// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ViewManager.h, commit 4b206bce3

using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls;

// Manages elements on behalf of ItemsRepeater.
// ViewManager automatically pins focused elements.
partial class ViewManager
{
	struct PinnedElementInfo
	{
		private UIElement m_pinnedElement;

		// We hold on VirtualizationInfo to make sure we can
		// quickly access its content rather than go through
		// ItemsRepeater.GetVirtualizationInfo(element) which is
		// slower (assuming it's implemented using attached
		// properties).
		private VirtualizationInfo m_virtInfo;

		public PinnedElementInfo(UIElement element)
		{
			m_pinnedElement = element;
			m_virtInfo = ItemsRepeater.GetVirtualizationInfo(element);
		}

		public UIElement PinnedElement => m_pinnedElement;
		public VirtualizationInfo VirtualizationInfo => m_virtInfo;
	}

	ItemsRepeater m_owner;

	// Pinned elements that are currently owned by layout are *NOT* in this pool.
	List<PinnedElementInfo> m_pinnedPool = new List<PinnedElementInfo>();
	UniqueIdElementPool m_resetPool;

	// _lastFocusedElement is listed in _pinnedPool.
	// It has to be an element we own (i.e. a direct child).
	UIElement m_lastFocusedElement;
	bool m_isDataSourceStableResetPending;
	bool m_recycleWithoutOwner;

	// Event tokens (C# SerialDisposable replaces WinUI's auto_revoke pattern).
	private readonly SerialDisposable m_gotFocus = new SerialDisposable();
	private readonly SerialDisposable m_lostFocus = new SerialDisposable();

	Phaser m_phaser;

	// Cached generate/clear contexts to avoid cost of creation every time.
	ElementFactoryGetArgs m_ElementFactoryGetArgs;
	ElementFactoryRecycleArgs m_ElementFactoryRecycleArgs;

	// These are first/last indices requested by layout and not cleared yet.
	// These are also not truly first / last because they are a lower / upper bound on the known realized range.
	// For example, if we didn't have the optimization in ElementManager.cpp, m_lastRealizedElementIndexHeldByLayout
	// will not be accurate. Rather, it will be an upper bound on what we think is the last realized index.
	int m_firstRealizedElementIndexHeldByLayout = FirstRealizedElementIndexDefault;
	int m_lastRealizedElementIndexHeldByLayout = LastRealizedElementIndexDefault;
	const int FirstRealizedElementIndexDefault = int.MaxValue;
	const int LastRealizedElementIndexDefault = int.MinValue;
}
