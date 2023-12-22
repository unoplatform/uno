// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Common;
using Uno.UI.RuntimeTests;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

namespace MUXControlsTestApp.Utilities
{
	public class MUXApiTestBase
	{
		private Border _host;

		public const int DefaultWaitTimeInMS = 5000;

		// Set this content instead of using Window.Current.Content
		// because the latter requires you to tick the UI thread
		// before a layout pass can happen while you can directly call
		// UpdateLayout after the former, which is faster and less
		// sensitive to timing issues.
		public UIElement Content
		{
			get { return (UIElement)_host.Child; }
			set { _host.Child = value; }
		}

		[TestInitialize]
		public void Setup()
		{
			IdleSynchronizer.Wait();
			var hostLoaded = new ManualResetEvent(false);
			RunOnUIThread.Execute(() =>
			{
				_host = new Border();
				_host.Loaded += delegate { hostLoaded.Set(); };
				MUXControlsTestApp.App.TestContentRoot = _host;
			});
			Verify.IsTrue(hostLoaded.WaitOne(DefaultWaitTimeInMS), "Waiting for loaded event");
		}

		[TestCleanup]
		public void Cleanup()
		{
			TestUtilities.ClearVisualTreeRoot();
		}
	}
}
