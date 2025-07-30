// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Private.Controls;

internal partial class ScrollViewTestHooks
{
	private static ScrollViewTestHooks s_testHooks;

	public static ScrollViewTestHooks EnsureGlobalTestHooks()
	{
		return s_testHooks ??= new();
	}

	public static bool? GetAutoHideScrollControllers(ScrollView scrollView)
	{
		if (scrollView is not null && s_testHooks is not null)
		{
			var hooks = EnsureGlobalTestHooks();

			if (hooks.m_autoHideScrollControllersMap.TryGetValue(scrollView, out var second))
			{
				return second;
			}
		}

		return null;
	}

	public static void SetAutoHideScrollControllers(ScrollView scrollView, bool? value)
	{
		if (scrollView is not null && (s_testHooks is not null || value is not null))
		{
			var hooks = EnsureGlobalTestHooks();

			if (hooks.m_autoHideScrollControllersMap.TryGetValue(scrollView, out _))
			{
				if (value is not null)
				{
					hooks.m_autoHideScrollControllersMap[scrollView] = value;
				}
				else
				{
					hooks.m_autoHideScrollControllersMap.Remove(scrollView, out _);
				}
			}
			else if (value is not null)
			{
				hooks.m_autoHideScrollControllersMap.Add(scrollView, value);
			}

			scrollView.ScrollControllersAutoHidingChanged();
		}
	}

	public ScrollPresenter GetScrollPresenterPart(ScrollView scrollView)
	{
		if (scrollView is not null)
		{
			return scrollView.GetScrollPresenterPart();
		}

		return null;
	}
}
