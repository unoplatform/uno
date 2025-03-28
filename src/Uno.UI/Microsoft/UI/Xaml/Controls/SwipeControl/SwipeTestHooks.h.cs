// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// Imported in uno on 2021/03/21 from commit 307bd99682cccaa128483036b764c0b7c862d666
// https://github.com/microsoft/microsoft-ui-xaml/blob/307bd99682cccaa128483036b764c0b7c862d666/dev/SwipeControl/SwipeTestHooks.h

using Windows.Foundation;
using Windows.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	internal partial class SwipeTestHooks
	{
		//class SwipeTestHooks :

		//public implementation.SwipeTestHooksT<SwipeTestHooks>
		//{

		//public:

		public static SwipeTestHooks GetGlobalTestHooks()
		{
			return s_testHooks;
		}

		//static com_ptr<SwipeTestHooks> EnsureGlobalTestHooks();

		//static SwipeControl GetLastInteractedWithSwipeControl();

		//static bool GetIsOpen(SwipeControl& swipeControl);

		//static bool GetIsIdle(SwipeControl& swipeControl);

		//static void NotifyLastInteractedWithSwipeControlChanged();

		//static event_token LastInteractedWithSwipeControlChanged(TypedEventHandler<DependencyObject, DependencyObject> & value);

		//static void LastInteractedWithSwipeControlChanged(event_token & token);

		//static void NotifyOpenedStatusChanged(SwipeControl& sender);

		//static event_token OpenedStatusChanged(TypedEventHandler<SwipeControl, DependencyObject> & value);

		//static void OpenedStatusChanged(event_token & token);

		//static void NotifyIdleStatusChanged(SwipeControl& sender);

		//static event_token IdleStatusChanged(TypedEventHandler<SwipeControl, DependencyObject> & value);

		//static void IdleStatusChanged(event_token & token);

		//private:

		private static SwipeTestHooks s_testHooks;

		private event TypedEventHandler<DependencyObject, DependencyObject> m_lastInteractedWithSwipeControlChangedEventSource;

		private event TypedEventHandler<SwipeControl, DependencyObject> m_openedStatusChangedEventSource;

		private event TypedEventHandler<SwipeControl, DependencyObject> m_idleStatusChangedEventSource;
	}
}
