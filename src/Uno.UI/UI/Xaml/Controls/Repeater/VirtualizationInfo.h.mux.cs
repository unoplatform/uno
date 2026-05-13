// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference VirtualizationInfo.h, commit 4b206bce3

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml.Markup;
using Uno.Extensions;

namespace Microsoft.UI.Xaml.Controls;

internal enum ElementOwner
{
	// All elements are originally owned by the view generator.
	ElementFactory,
	// Ownership is transferred to the layout when it calls GetElement.
	Layout,
	// Ownership is transferred to the pinned pool if the element is cleared (outside of
	// a 'remove' collection change of course).
	PinnedPool,
	// Ownership is transfered to the reset pool if the element is cleared by a reset and
	// the data source supports unique ids.
	UniqueIdResetPool,
	// Ownership is transfered to the animator if the element is cleared due to a
	// 'remove'-like collection change.
	Animator
}

partial class VirtualizationInfo
{
	public ElementOwner Owner => m_owner;
	public int Index => m_index;

	// Pinned means that the element is protected from getting cleared by layout.
	// A pinned element may still get cleared by a collection change.
	// IsPinned == true doesn't necessarly mean that the element is currently
	// owned by the PinnedPool, only that its ownership may be transferred to the
	// PinnedPool if it gets cleared by layout.

	// Info for phasing
	public int Phase
	{
		get => m_phase;
		set => m_phase = value;
	}
	public object Data => m_data?.GetTarget();
	public IDataTemplateComponent DataTemplateComponent => m_dataTemplateComponent?.GetTarget();

	public bool MustClearDataContext
	{
		get => m_mustClearDataContext;
		set => m_mustClearDataContext = value;
	}

	public const int PhaseNotSpecified = int.MinValue;
	public const int PhaseReachedEnd = -1;

	public Rect ArrangeBounds
	{
		get => m_arrangeBounds;
		set => m_arrangeBounds = value;
	}

	public string UniqueId => m_uniqueId;

	// #pragma region Keep element from being recycled

	public bool KeepAlive
	{
		get => m_keepAlive;
		set => m_keepAlive = value;
	}

	// #pragma endregion

	public bool AutoRecycleCandidate
	{
		get => m_autoRecycleCandidate;
		set => m_autoRecycleCandidate = value;
	}

#if DEBUG
	public static int GetLogItemIndex()
	{
		return s_logItemIndexDbg;
	}

	public static void SetLogItemIndex(int logItemIndex)
	{
		s_logItemIndexDbg = logItemIndex;
	}
#endif

	private uint m_pinCounter = 0u;
	private int m_index = -1;
	private string m_uniqueId;
	private ElementOwner m_owner = ElementOwner.ElementFactory;
	private Rect m_arrangeBounds;
	private int m_phase = PhaseNotSpecified;
	private bool m_keepAlive = false;
	private bool m_autoRecycleCandidate = false;
	// True when ViewManager set the element's DataContext during GetElementFromElementFactory.
	// Used by ClearElementToElementFactory to decide whether to clear DataContext back to null
	// to avoid holding on to stale bindings.
	private bool m_mustClearDataContext = false;

	private WeakReference<object> m_data;
	private WeakReference<IDataTemplateComponent> m_dataTemplateComponent;

#if DEBUG
	private static int s_logItemIndexDbg = -1;
#endif
}
