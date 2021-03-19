// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


com_ptr<SwipeTestHooks> SwipeTestHooks.s_testHooks{};

com_ptr<SwipeTestHooks> EnsureGlobalTestHooks()
{
    static bool s_initialized = []() {
        s_testHooks = winrt.new SwipeTestHooks();
        return true;
    }();
    return s_testHooks;
}

winrt.SwipeControl GetLastInteractedWithSwipeControl()
{
    return SwipeControl.GetLastInteractedWithSwipeControl();
}

bool GetIsOpen( winrt.SwipeControl& swipeControl)
{
    if (swipeControl)
    {
        return winrt.get_self<SwipeControl>(swipeControl).GetIsOpen();
    }
    else
    {
        if (var lastInteractedWithSwipeControl = SwipeControl.GetLastInteractedWithSwipeControl())
        {
            return winrt.get_self<SwipeControl>(lastInteractedWithSwipeControl).GetIsOpen();
        }
        return false;
    }
}

bool GetIsIdle( winrt.SwipeControl& swipeControl)
{
    if (swipeControl)
    {
        return winrt.get_self<SwipeControl>(swipeControl).GetIsIdle();
    }
    else
    {
        if (var lastInteractedWithSwipeControl = SwipeControl.GetLastInteractedWithSwipeControl())
        {
            return winrt.get_self<SwipeControl>(lastInteractedWithSwipeControl).GetIsIdle();
        }
        return false;
    }
}

void NotifyLastInteractedWithSwipeControlChanged()
{
    var hooks = EnsureGlobalTestHooks();
    if (hooks.m_lastInteractedWithSwipeControlChangedEventSource)
    {
        hooks.m_lastInteractedWithSwipeControlChangedEventSource(null, null);
    }
}

winrt.event_token LastInteractedWithSwipeControlChanged(winrt.TypedEventHandler<winrt.DependencyObject, winrt.DependencyObject> & value)
{
    var hooks = EnsureGlobalTestHooks();
    return hooks.m_lastInteractedWithSwipeControlChangedEventSource.add(value);
}

void LastInteractedWithSwipeControlChanged(winrt.event_token & token)
{
    var hooks = EnsureGlobalTestHooks();
    hooks.m_lastInteractedWithSwipeControlChangedEventSource.remove(token);
}

void NotifyOpenedStatusChanged( winrt.SwipeControl& sender)
{
    var hooks = EnsureGlobalTestHooks();
    if (hooks.m_openedStatusChangedEventSource)
    {
        hooks.m_openedStatusChangedEventSource(sender, null);
    }
}

winrt.event_token OpenedStatusChanged(winrt.TypedEventHandler<winrt.SwipeControl, winrt.DependencyObject> & value)
{
    var hooks = EnsureGlobalTestHooks();
    return hooks.m_openedStatusChangedEventSource.add(value);
}

void OpenedStatusChanged(winrt.event_token & token)
{
    var hooks = EnsureGlobalTestHooks();
    hooks.m_openedStatusChangedEventSource.remove(token);
}

void NotifyIdleStatusChanged( winrt.SwipeControl& sender)
{
    var hooks = EnsureGlobalTestHooks();
    if (hooks.m_idleStatusChangedEventSource)
    {
        hooks.m_idleStatusChangedEventSource(sender, null);
    }
}

winrt.event_token IdleStatusChanged(winrt.TypedEventHandler<winrt.SwipeControl, winrt.DependencyObject> & value)
{
    var hooks = EnsureGlobalTestHooks();
    return hooks.m_idleStatusChangedEventSource.add(value);
}

void IdleStatusChanged(winrt.event_token & token)
{
    var hooks = EnsureGlobalTestHooks();
    hooks.m_idleStatusChangedEventSource.remove(token);
}
