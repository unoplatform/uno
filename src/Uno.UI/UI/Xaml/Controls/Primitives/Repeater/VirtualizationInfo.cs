// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.Foundation;
using Windows.UI.Xaml.Markup;
using Android.OS;

namespace Microsoft.UI.Xaml.Controls
{
	internal class VirtualizationInfo
	{
		private enum ElementOwner
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
		};

		private const int PhaseNotSpecified = int.MinValue;
		private const int PhaseReachedEnd = -1;

		private uint m_pinCounter ;
		private int m_index = -1;
		private string m_uniqueId;
		private ElementOwner m_owner = ElementOwner.ElementFactory;
		private Rect m_arrangeBounds;
		private int m_phase = PhaseNotSpecified;

		private WeakReference<object> m_data;
		private WeakReference<IDataTemplateComponent> m_dataTemplateComponent;

		VirtualizationInfo()
		{
			m_arrangeBounds = ItemsRepeater.InvalidRect;
		}

		private bool IsPinned => m_pinCounter > 0u;

		private bool IsHeldByLayout => m_owner == ElementOwner.Layout;

		private bool IsRealized => IsHeldByLayout || m_owner == ElementOwner.PinnedPool;

		private bool IsInUniqueIdResetPool => m_owner == ElementOwner.UniqueIdResetPool;

		private void UpdatePhasingInfo(int phase,  object data, IDataTemplateComponent component)
		{
			m_phase = phase;
			m_data = new WeakReference<object>(data);
			m_dataTemplateComponent = new WeakReference<IDataTemplateComponent>(component);
		}

		#region Ownership state machine

		private void MoveOwnershipToLayoutFromElementFactory(int index, string uniqueId)
		{
			global::System.Diagnostics.Debug.Assert(m_owner == ElementOwner.ElementFactory);

			m_owner = ElementOwner.Layout;
			m_index = index;
			m_uniqueId = uniqueId;
		}

		private void MoveOwnershipToLayoutFromUniqueIdResetPool()
		{
			global::System.Diagnostics.Debug.Assert(m_owner == ElementOwner.UniqueIdResetPool);

			m_owner = ElementOwner.Layout;
		}

		void MoveOwnershipToLayoutFromPinnedPool()
		{
			global::System.Diagnostics.Debug.Assert(m_owner == ElementOwner.PinnedPool);
			global::System.Diagnostics.Debug.Assert(IsPinned);

			m_owner = ElementOwner.Layout;
		}

		void MoveOwnershipToElementFactory()
		{
			global::System.Diagnostics.Debug.Assert(m_owner != ElementOwner.ElementFactory);

			m_owner = ElementOwner.ElementFactory;

			m_pinCounter = 0u;
			m_index = -1;
			m_uniqueId = null;
			m_arrangeBounds = ItemsRepeater.InvalidRect;
		}

		void MoveOwnershipToUniqueIdResetPoolFromLayout()
		{
			global::System.Diagnostics.Debug.Assert(m_owner == ElementOwner.Layout);

			m_owner = ElementOwner.UniqueIdResetPool;

			// Keep the pinCounter the same. If the container survives the reset
			// it can go on being pinned as if nothing happened.
		}

		void MoveOwnershipToAnimator()
		{
			// During a unique id reset, some elements might get removed.
			// Their ownership will go from the UniqueIdResetPool to the Animator.
			// The common path though is for ownership to go from Layout to Animator.
			global::System.Diagnostics.Debug.Assert(m_owner == ElementOwner.Layout || m_owner == ElementOwner.UniqueIdResetPool);

			m_owner = ElementOwner.Animator;
			m_index = -1;
			m_pinCounter = 0u;
		}

		void MoveOwnershipToPinnedPool()
		{
			global::System.Diagnostics.Debug.Assert(m_owner == ElementOwner.Layout);

			m_owner = ElementOwner.PinnedPool;
		}

		#endregion

		uint AddPin()
		{
			if (!IsRealized)

			{
				throw new InvalidOperationException("You can't pin an unrealized element.");
			}

			return ++m_pinCounter;
		}

		uint RemovePin()
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

		void UpdateIndex(int newIndex)
		{
			global::System.Diagnostics.Debug.Assert(IsRealized);

			m_index = newIndex;
		}

	}
}
