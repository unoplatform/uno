// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//
// This file is a C# translation of the DispatcherHelper.cpp file from WinUI controls.
//

using System;
using System.Collections.Generic;
using System.Text;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Uno.UI.Helpers.WinUI
{
	internal class DispatcherHelper
	{
		Windows.System.DispatcherQueue dispatcherQueue = null;
		CoreDispatcher coreDispatcher = null;

		public DispatcherHelper(DependencyObject dependencyObject = null)
		{
			if (SharedHelpers.IsDispatcherQueueAvailable())
			{
				dispatcherQueue = Windows.System.DispatcherQueue.GetForCurrentThread();
			}

			if (dispatcherQueue == null)
			{
				try
				{
					if (dependencyObject != null)
					{
						coreDispatcher = dependencyObject.Dispatcher;
					}
					else if (CoreApplication.GetCurrentView() is CoreApplicationView currentView)
					{
						coreDispatcher = currentView.Dispatcher;
					}
				}
				catch (Exception)
				{
					// CoreApplicationView might throw in XamlPresenter scenarios or in LogonUI.exe.
				}            
			}
		}

		public void RunAsync(Action func, bool fallbackToThisThread = false)
		{
			// TODO: Uno specific - dispatcher queue is not implemented yet
			if (false)//dispatcherQueue != null)
			{
				//var result = dispatcherQueue.TryEnqueue(() => func());
				//if (!result)
				//{
				//	if (fallbackToThisThread)
				//	{
				//		func();
				//	}
				//}
			}
			else if (coreDispatcher != null)
			{
				var asyncOp = coreDispatcher.TryRunAsync(CoreDispatcherPriority.Normal, () => func());

				asyncOp.Completed = (IAsyncOperation<bool> asyncInfo, AsyncStatus asyncStatus) =>
				{
					bool reRunOnThisThread = false;

					if (asyncStatus == AsyncStatus.Completed)
					{
						var succeeded = asyncInfo.GetResults();
						if (!succeeded)
						{
							if (fallbackToThisThread)
							{
								reRunOnThisThread = true;
							}
						}
					}

					if (reRunOnThisThread)
					{
						func();
					}
				};
			}
			else
			{
				if (fallbackToThisThread)
				{
					func();
				}
			}
		}
	}
}
