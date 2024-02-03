// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;
using Common;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

using RefreshVisualizer = Microsoft/* UWP don't rename */.UI.Xaml.Controls.RefreshVisualizer;
using RefreshVisualizerState = Microsoft/* UWP don't rename */.UI.Xaml.Controls.RefreshVisualizerState;
using RefreshRequestedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.RefreshRequestedEventArgs;
using RefreshInteractionRatioChangedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.RefreshInteractionRatioChangedEventArgs;
using RefreshStateChangedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.RefreshStateChangedEventArgs;
using RefreshPullDirection = Microsoft/* UWP don't rename */.UI.Xaml.Controls.RefreshPullDirection;
using Uno.UI.Samples.Controls;
using Private.Infrastructure;
#if HAS_UNO
using IRefreshContainerPrivate = Microsoft.UI.Private.Controls.IRefreshContainerPrivate;
using IRefreshInfoProvider = Microsoft.UI.Private.Controls.IRefreshInfoProvider;
using IRefreshVisualizerPrivate = Microsoft.UI.Private.Controls.IRefreshVisualizerPrivate;
using System.Threading.Tasks;
#endif

namespace MUXControlsTestApp
{
	[Sample("MUX", "WinUI", "PullToRefresh")]
	public sealed partial class RefreshContainerNoScroll : TestPage
	{
		public RefreshContainerNoScroll()
		{
			this.InitializeComponent();
		}

		private async void RefreshContainer_RefreshRequested(object sender, RefreshRequestedEventArgs e)
		{
			var deferral = e.GetDeferral();
			await Task.Delay(3000);
			deferral.Complete();
		}

		private void Refresh_Click(object sender, RoutedEventArgs args)
		{
			RefreshContainer.RequestRefresh();
		}
	}
}
