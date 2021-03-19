// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma once

namespace Windows.UI.Xaml.Controls
{
	internal partial class SwipeTestHooks
	{

		class SwipeTestHooks : 

		public winrt.implementation.SwipeTestHooksT<SwipeTestHooks>
		{

		public:

		static com_ptr<SwipeTestHooks> GetGlobalTestHooks()
		{
			return s_testHooks;
		}

		static com_ptr<SwipeTestHooks> EnsureGlobalTestHooks();

		static winrt.SwipeControl GetLastInteractedWithSwipeControl();

		static bool GetIsOpen(winrt.SwipeControl& swipeControl);

		static bool GetIsIdle(winrt.SwipeControl& swipeControl);

		static void NotifyLastInteractedWithSwipeControlChanged();

		static winrt.event_token LastInteractedWithSwipeControlChanged(winrt.TypedEventHandler<winrt.DependencyObject, winrt.DependencyObject> & value);

		static void LastInteractedWithSwipeControlChanged(winrt.event_token & token);

		static void NotifyOpenedStatusChanged(winrt.SwipeControl& sender);

		static winrt.event_token OpenedStatusChanged(winrt.TypedEventHandler<winrt.SwipeControl, winrt.DependencyObject> & value);

		static void OpenedStatusChanged(winrt.event_token & token);

		static void NotifyIdleStatusChanged(winrt.SwipeControl& sender);

		static winrt.event_token IdleStatusChanged(winrt.TypedEventHandler<winrt.SwipeControl, winrt.DependencyObject> & value);

		static void IdleStatusChanged(winrt.event_token & token);

		private:

		static com_ptr<SwipeTestHooks> s_testHooks;
		winrt.event<winrt.TypedEventHandler<winrt.DependencyObject, winrt.DependencyObject>> m_lastInteractedWithSwipeControlChangedEventSource;

		winrt.event<winrt.TypedEventHandler<winrt.SwipeControl, winrt.DependencyObject>> m_openedStatusChangedEventSource;

		winrt.event<winrt.TypedEventHandler<winrt.SwipeControl, winrt.DependencyObject>> m_idleStatusChangedEventSource;
	};
}}
