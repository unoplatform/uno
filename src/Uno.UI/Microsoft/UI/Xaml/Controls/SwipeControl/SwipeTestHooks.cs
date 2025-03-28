// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// Imported in uno on 2021/03/21 from commit 307bd99682cccaa128483036b764c0b7c862d666
// https://github.com/microsoft/microsoft-ui-xaml/blob/307bd99682cccaa128483036b764c0b7c862d666/dev/SwipeControl/SwipeTestHooks.cpp

using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	internal partial class SwipeTestHooks
	{
		//com_ptr<SwipeTestHooks> SwipeTestHooks.s_testHooks { };

		public static SwipeTestHooks EnsureGlobalTestHooks()
		{
			return s_testHooks ?? new SwipeTestHooks();
		}

		public SwipeControl GetLastInteractedWithSwipeControl()
		{
			return SwipeControl.GetLastInteractedWithSwipeControl();
		}

		public bool GetIsOpen(SwipeControl swipeControl)
		{
			if (swipeControl is { })
			{
				return ((SwipeControl)swipeControl).GetIsOpen();
			}
			else
			{
				var lastInteractedWithSwipeControl = SwipeControl.GetLastInteractedWithSwipeControl();
				if (lastInteractedWithSwipeControl is { })
				{
					return ((SwipeControl)lastInteractedWithSwipeControl).GetIsOpen();
				}
				return false;
			}
		}

		public bool GetIsIdle(SwipeControl swipeControl)
		{
			if (swipeControl is { })
			{
				return ((SwipeControl)swipeControl).GetIsIdle();
			}
			else
			{
				var lastInteractedWithSwipeControl = SwipeControl.GetLastInteractedWithSwipeControl();
				if (lastInteractedWithSwipeControl is { })
				{
					return ((SwipeControl)lastInteractedWithSwipeControl).GetIsIdle();
				}
				return false;
			}
		}

		public void NotifyLastInteractedWithSwipeControlChanged()
		{
			var hooks = EnsureGlobalTestHooks();
			if (hooks.m_lastInteractedWithSwipeControlChangedEventSource is { })
			{
				hooks.m_lastInteractedWithSwipeControlChangedEventSource(null, null);
			}
		}

		public event TypedEventHandler<DependencyObject, DependencyObject> LastInteractedWithSwipeControlChanged
		{
			add
			{
				var hooks = EnsureGlobalTestHooks();
				hooks.m_lastInteractedWithSwipeControlChangedEventSource += value;
			}
			remove
			{
				var hooks = EnsureGlobalTestHooks();
				hooks.m_lastInteractedWithSwipeControlChangedEventSource -= value;
			}
		}

		public void NotifyOpenedStatusChanged(SwipeControl sender)
		{
			var hooks = EnsureGlobalTestHooks();
			if (hooks.m_openedStatusChangedEventSource is { })
			{
				hooks.m_openedStatusChangedEventSource(sender, null);
			}
		}

		public event TypedEventHandler<SwipeControl, DependencyObject> OpenedStatusChanged
		{
			add
			{
				var hooks = EnsureGlobalTestHooks();
				hooks.m_openedStatusChangedEventSource += value;
			}
			remove
			{
				var hooks = EnsureGlobalTestHooks();
				hooks.m_openedStatusChangedEventSource -= value;
			}
		}

		public void NotifyIdleStatusChanged(SwipeControl sender)
		{
			var hooks = EnsureGlobalTestHooks();
			if (hooks.m_idleStatusChangedEventSource is { })
			{
				hooks.m_idleStatusChangedEventSource(sender, null);
			}
		}

		public event TypedEventHandler<SwipeControl, DependencyObject> IdleStatusChanged
		{
			add
			{
				var hooks = EnsureGlobalTestHooks();
				hooks.m_idleStatusChangedEventSource += value;
			}
			remove
			{
				var hooks = EnsureGlobalTestHooks();
				hooks.m_idleStatusChangedEventSource -= value;
			}
		}
	}
}
