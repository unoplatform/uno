// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.Foundation;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;
using NotImplementedException = System.NotImplementedException;

namespace Windows.UI.Xaml.Controls
{
	public partial class CalendarViewDayItemChangingEventArgs
	{
		private bool m_inRecycleQueue;
		private CalendarViewDayItem m_pItem;
		private uint m_phase;
		//private bool m_wantsCallBack;
		//private TypedEventHandler<CalendarView, CalendarViewDayItemChangingEventArgs> m_pCallback;

		//private UIElement m_pContainerBackPointer;

		internal CalendarViewDayItemChangingEventArgs()
		{
			//m_pContainerBackPointer = null;
		}

		public bool InRecycleQueue
		{
			get => m_inRecycleQueue;
			internal set => m_inRecycleQueue = value;
		}

		public CalendarViewDayItem Item
		{
			get => m_pItem;
			internal set => m_pItem = value;
		}
		public uint Phase
		{
			get => m_phase;
			internal set => m_phase = value;
		}

		public void RegisterUpdateCallback(TypedEventHandler<CalendarView, CalendarViewDayItemChangingEventArgs> callback)
			=> RegisterUpdateCallbackImpl(callback);

		public void RegisterUpdateCallback(uint callbackPhase, TypedEventHandler<CalendarView, CalendarViewDayItemChangingEventArgs> callback)
			=> RegisterUpdateCallbackWithPhaseImpl(callbackPhase, callback);

		internal TypedEventHandler<CalendarView, CalendarViewDayItemChangingEventArgs> Callback { get; set; }
		internal bool WantsCallBack { get; set; }

		// reset the args in such a way that it is not holding on to any other structures
		private void ResetLifetimeImpl()
		{
			Callback = null;
			Item = null;
		}

		// Register this container for a callback on the next phase
		private void RegisterUpdateCallbackImpl(TypedEventHandler<CalendarView, CalendarViewDayItemChangingEventArgs> pCallback)
		{
			RegisterUpdateCallbackWithPhaseImpl(m_phase + 1, pCallback);
		}

		// Register this container for a callback on a specific phase
		private void RegisterUpdateCallbackWithPhaseImpl(uint callbackPhase, TypedEventHandler<CalendarView, CalendarViewDayItemChangingEventArgs> pCallback)
		{
			m_phase = callbackPhase;
			Callback = pCallback;
			WantsCallBack = true;
		}

		public void ResetLifetime()
			=> ResetLifetimeImpl();
	}
}
