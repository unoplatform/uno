// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference DispatcherHelper.h, tag winui3/release/1.4.2

using System;
using Windows.UI.Xaml;

namespace Uno.UI.Helpers.WinUI;

internal class DispatcherHelper
{
	private Windows.System.DispatcherQueue dispatcherQueue;

	public DispatcherHelper(DependencyObject dependencyObject = null)
	{
		dispatcherQueue = Windows.System.DispatcherQueue.GetForCurrentThread();
	}

	public void RunAsync(Action func, bool fallbackToThisThread = false)
	{
		var result = dispatcherQueue?.TryEnqueue(() => func()) ?? false;
		if (!result)
		{
			if (fallbackToThisThread)
			{
				func();
			}
		}
	}
}
