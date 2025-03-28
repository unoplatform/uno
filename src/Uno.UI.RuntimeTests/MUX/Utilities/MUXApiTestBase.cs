// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Common;
using Uno.UI.RuntimeTests;
using Private.Infrastructure;
using System.Threading.Tasks;



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
		public async Task Setup()
		{
			await TestServices.WindowHelper.WaitForIdle();
			var hostLoaded = new UnoManualResetEvent(false);
			RunOnUIThread.Execute(() =>
			{
				_host = new Border();
				_host.Loaded += delegate { hostLoaded.Set(); };
				MUXControlsTestApp.App.TestContentRoot = _host;
			});
			Verify.IsTrue(await hostLoaded.WaitOne(DefaultWaitTimeInMS), "Waiting for loaded event");
		}

		[TestCleanup]
		public async Task Cleanup()
		{
			await TestUtilities.ClearVisualTreeRoot();
		}
	}
}
