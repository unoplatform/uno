// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference VirtualizationInfo.cpp, commit 4b206bce3

using System;
using Microsoft.UI.Xaml.Markup;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

partial class VirtualizationInfo
{
	public VirtualizationInfo()
	{
		m_arrangeBounds = ItemsRepeater.InvalidRect;
	}

	public bool IsPinned => m_pinCounter > 0u;

	public bool IsHeldByLayout => m_owner == ElementOwner.Layout;

	public bool IsInPinnedPool => m_owner == ElementOwner.PinnedPool;

	public bool IsInUniqueIdResetPool => m_owner == ElementOwner.UniqueIdResetPool;

	public bool IsRealized => IsHeldByLayout || IsInPinnedPool;

	public void UpdatePhasingInfo(int phase, object data, IDataTemplateComponent component)
	{
		m_phase = phase;
		m_data = new WeakReference<object>(data);
		m_dataTemplateComponent = new WeakReference<IDataTemplateComponent>(component);
	}

	// #pragma region Ownership state machine

	public void MoveOwnershipToLayoutFromElementFactory(int index, string uniqueId)
	{
		// TODO Uno: this assert is failing - issue #4691
		// MUX_ASSERT(m_owner == ElementOwner.ElementFactory);
		m_owner = ElementOwner.Layout;
		m_index = index;
		m_uniqueId = uniqueId;
	}

	public void MoveOwnershipToLayoutFromUniqueIdResetPool()
	{
		MUX_ASSERT(m_owner == ElementOwner.UniqueIdResetPool);
		m_owner = ElementOwner.Layout;
	}

	public void MoveOwnershipToLayoutFromPinnedPool()
	{
		MUX_ASSERT(m_owner == ElementOwner.PinnedPool);
		MUX_ASSERT(IsPinned);
		m_owner = ElementOwner.Layout;
	}

	public void MoveOwnershipToElementFactory()
	{
		MUX_ASSERT(m_owner != ElementOwner.ElementFactory);
		m_owner = ElementOwner.ElementFactory;
		m_pinCounter = 0u;
		m_index = -1;
		m_uniqueId = null;
		m_arrangeBounds = ItemsRepeater.InvalidRect;
	}

	public void MoveOwnershipToUniqueIdResetPoolFromLayout()
	{
		MUX_ASSERT(m_owner == ElementOwner.Layout);
		m_owner = ElementOwner.UniqueIdResetPool;
		// Keep the pinCounter the same. If the container survives the reset
		// it can go on being pinned as if nothing happened.
	}

	public void MoveOwnershipToAnimator()
	{
		// During a unique id reset, some elements might get removed.
		// Their ownership will go from the UniqueIdResetPool to the Animator.
		// The common path though is for ownership to go from Layout to Animator.
		MUX_ASSERT(m_owner == ElementOwner.Layout || m_owner == ElementOwner.UniqueIdResetPool);
		m_owner = ElementOwner.Animator;
		m_index = -1;
		m_pinCounter = 0u;
	}

	public void MoveOwnershipToPinnedPool()
	{
		MUX_ASSERT(m_owner == ElementOwner.Layout);
		m_owner = ElementOwner.PinnedPool;
	}

	// #pragma endregion

	public uint AddPin()
	{
		if (!IsRealized)
		{
			throw new InvalidOperationException("You can't pin an unrealized element.");
		}

		return ++m_pinCounter;
	}

	public uint RemovePin()
	{
		if (!IsRealized)
		{
			throw new InvalidOperationException("You can't unpin an unrealized element.");
		}

		if (!IsPinned)
		{
			throw new InvalidOperationException("UnpinElement was called more often than PinElement.");
		}

		return --m_pinCounter;
	}

	public void UpdateIndex(int newIndex)
	{
		MUX_ASSERT(IsRealized);
		m_index = newIndex;
	}
}
