// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.Foundation;
using Windows.UI.Xaml.Markup;
using Uno.Extensions;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	internal partial class VirtualizationInfo
	{
		public const int PhaseNotSpecified = int.MinValue;
		public const int PhaseReachedEnd = -1;

		private uint m_pinCounter;
		private int m_index = -1;
		private string m_uniqueId;
		private ElementOwner m_owner = ElementOwner.ElementFactory;
		private Rect m_arrangeBounds;
		private int m_phase = PhaseNotSpecified;
		private bool m_keepAlive;
		private bool m_autoRecycleCandidate;

		private WeakReference<object> m_data;
		private WeakReference<IDataTemplateComponent> m_dataTemplateComponent;

		public VirtualizationInfo()
		{
			m_arrangeBounds = ItemsRepeater.InvalidRect;
		}

		public Rect ArrangeBounds
		{
			get => m_arrangeBounds;
			set => m_arrangeBounds = value;
		}

		public string UniqueId => m_uniqueId;

		public bool KeepAlive
		{
			get => m_keepAlive;
			set => m_keepAlive = value;
		}

		public bool AutoRecycleCandidate
		{
			get => m_autoRecycleCandidate;
			set => m_autoRecycleCandidate = value;
		}

		public ElementOwner Owner => m_owner;

		public int Index => m_index;

		// Pinned means that the element is protected from getting cleared by layout.
		// A pinned element may still get cleared by a collection change.
		// IsPinned == true doesn't necessarly mean that the element is currently
		// owned by the PinnedPool, only that its ownership may be transferred to the
		// PinnedPool if it gets cleared by layout.
		public bool IsPinned => m_pinCounter > 0u;

		public bool IsHeldByLayout => m_owner == ElementOwner.Layout;

		public bool IsRealized => IsHeldByLayout || m_owner == ElementOwner.PinnedPool;

		public bool IsInUniqueIdResetPool => m_owner == ElementOwner.UniqueIdResetPool;

		public int Phase
		{
			get => m_phase;
			set => m_phase = value;
		}

		public object Data => m_data?.GetTarget();

		public IDataTemplateComponent DataTemplateComponent => m_dataTemplateComponent?.GetTarget();

		public void UpdatePhasingInfo(int phase, object data, IDataTemplateComponent component)
		{
			m_phase = phase;
			m_data = new WeakReference<object>(data);
			m_dataTemplateComponent = new WeakReference<IDataTemplateComponent>(component);
		}

		#region Ownership state machine
		public void MoveOwnershipToLayoutFromElementFactory(int index, string uniqueId)
		{
			// Uno specific - this assert is failing - issue #4691
			//global::System.Diagnostics.Debug.Assert(m_owner == ElementOwner.ElementFactory);

			m_owner = ElementOwner.Layout;
			m_index = index;
			m_uniqueId = uniqueId;
		}

		public void MoveOwnershipToLayoutFromUniqueIdResetPool()
		{
			global::System.Diagnostics.Debug.Assert(m_owner == ElementOwner.UniqueIdResetPool);

			m_owner = ElementOwner.Layout;
		}

		public void MoveOwnershipToLayoutFromPinnedPool()
		{
			global::System.Diagnostics.Debug.Assert(m_owner == ElementOwner.PinnedPool);
			global::System.Diagnostics.Debug.Assert(IsPinned);

			m_owner = ElementOwner.Layout;
		}

		public void MoveOwnershipToElementFactory()
		{
			global::System.Diagnostics.Debug.Assert(m_owner != ElementOwner.ElementFactory);

			m_owner = ElementOwner.ElementFactory;

			m_pinCounter = 0u;
			m_index = -1;
			m_uniqueId = null;
			m_arrangeBounds = ItemsRepeater.InvalidRect;
		}

		public void MoveOwnershipToUniqueIdResetPoolFromLayout()
		{
			global::System.Diagnostics.Debug.Assert(m_owner == ElementOwner.Layout);

			m_owner = ElementOwner.UniqueIdResetPool;

			// Keep the pinCounter the same. If the container survives the reset
			// it can go on being pinned as if nothing happened.
		}

		public void MoveOwnershipToAnimator()
		{
			// During a unique id reset, some elements might get removed.
			// Their ownership will go from the UniqueIdResetPool to the Animator.
			// The common path though is for ownership to go from Layout to Animator.
			global::System.Diagnostics.Debug.Assert(m_owner == ElementOwner.Layout || m_owner == ElementOwner.UniqueIdResetPool);

			m_owner = ElementOwner.Animator;
			m_index = -1;
			m_pinCounter = 0u;
		}

		public void MoveOwnershipToPinnedPool()
		{
			global::System.Diagnostics.Debug.Assert(m_owner == ElementOwner.Layout);

			m_owner = ElementOwner.PinnedPool;
		}

		#endregion

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
			global::System.Diagnostics.Debug.Assert(IsRealized);

			m_index = newIndex;
		}
	}
}
