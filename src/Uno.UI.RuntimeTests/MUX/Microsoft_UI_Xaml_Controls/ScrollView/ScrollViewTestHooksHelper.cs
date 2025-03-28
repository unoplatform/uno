// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Common;
using Windows.UI.Xaml.Controls;
using Microsoft.UI.Private.Controls;

namespace MUXControlsTestApp.Utilities;

// Utility class used to set up ScrollView test hooks and automatically reset them when the instance gets disposed.
public class ScrollViewTestHooksHelper : IDisposable
{
	public ScrollViewTestHooksHelper(ScrollView scrollView, bool? autoHideScrollControllers = null)
	{
		RunOnUIThread.Execute(() =>
		{
			if (scrollView != null)
			{
				ScrollView = scrollView;

				ScrollViewTestHooks.SetAutoHideScrollControllers(scrollView, autoHideScrollControllers);
			}
		});
	}

	public ScrollView ScrollView
	{
		get;
		set;
	}

	public bool? AutoHideScrollControllers
	{
		get
		{
			return ScrollView == null ? true : ScrollViewTestHooks.GetAutoHideScrollControllers(ScrollView);
		}

		set
		{
			if (value != AutoHideScrollControllers)
			{
				Log.Comment($"ScrollViewTestHooksHelper: AutoHideScrollControllers set to {value}.");
				if (ScrollView != null)
				{
					ScrollViewTestHooks.SetAutoHideScrollControllers(ScrollView, value);
				}
			}
		}
	}

	public void Dispose()
	{
		RunOnUIThread.Execute(() =>
		{
			Log.Comment("PrivateLoggingHelper disposal: Resetting AutoHideScrollControllers.");
			AutoHideScrollControllers = null;
		});
	}
}
