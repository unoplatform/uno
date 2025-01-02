// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Common;
using MUXControlsTestApp.Utilities;

using System;
using System.Numerics;
using System.Threading;

using Windows.Foundation;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Private.Infrastructure;
using System.Threading.Tasks;

using TwoPaneView = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TwoPaneView;
using TwoPaneViewPriority = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TwoPaneViewPriority;
using TwoPaneViewMode = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TwoPaneViewMode;
using TwoPaneViewWideModeConfiguration = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TwoPaneViewWideModeConfiguration;
using TwoPaneViewTallModeConfiguration = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TwoPaneViewTallModeConfiguration;

//using WEX.TestExecution;
//using WEX.TestExecution.Markup;
//using WEX.Logging.Interop;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests
{
	[TestClass]
	public class TwoPaneViewTests : MUXApiTestBase
	{
		private const double c_defaultMinWideModeWidth = 641.0;
		private const double c_defaultMinTallModeHeight = 641.0;

		[TestMethod]
		[Description("Verifies the TwoPaneView default properties.")]
		public async Task VerifyDefaultsAndBasicSetting()
		{
			TwoPaneView twoPaneView = null;
			UIElement pane1 = null;
			UIElement pane2 = null;

			RunOnUIThread.Execute(() =>
			{
				pane1 = new Grid();
				pane2 = new Grid();

				twoPaneView = new TwoPaneView();
				Verify.IsNotNull(twoPaneView);

				// Verify Defaults
				Verify.IsNull(twoPaneView.Pane1);
				Verify.IsNull(twoPaneView.Pane2);
				Verify.AreEqual(TwoPaneViewPriority.Pane1, twoPaneView.PanePriority);
				Verify.AreEqual(TwoPaneViewMode.SinglePane, twoPaneView.Mode);
				Verify.AreEqual(TwoPaneViewWideModeConfiguration.LeftRight, twoPaneView.WideModeConfiguration);
				Verify.AreEqual(TwoPaneViewTallModeConfiguration.TopBottom, twoPaneView.TallModeConfiguration);
				Verify.AreEqual(c_defaultMinWideModeWidth, twoPaneView.MinWideModeWidth);
				Verify.AreEqual(c_defaultMinTallModeHeight, twoPaneView.MinTallModeHeight);

				// Verify basic setters
				twoPaneView.Pane1 = pane1;
				twoPaneView.Pane2 = pane2;
				twoPaneView.PanePriority = TwoPaneViewPriority.Pane2;
				twoPaneView.WideModeConfiguration = TwoPaneViewWideModeConfiguration.RightLeft;
				twoPaneView.TallModeConfiguration = TwoPaneViewTallModeConfiguration.BottomTop;
				twoPaneView.MinWideModeWidth = 500;
				twoPaneView.MinTallModeHeight = 700;
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(pane1, twoPaneView.Pane1);
				Verify.AreEqual(pane2, twoPaneView.Pane2);
				Verify.AreEqual(TwoPaneViewPriority.Pane2, twoPaneView.PanePriority);
				Verify.AreEqual(TwoPaneViewWideModeConfiguration.RightLeft, twoPaneView.WideModeConfiguration);
				Verify.AreEqual(TwoPaneViewTallModeConfiguration.BottomTop, twoPaneView.TallModeConfiguration);
				Verify.AreEqual(500, twoPaneView.MinWideModeWidth);
				Verify.AreEqual(700, twoPaneView.MinTallModeHeight);

				// Verify nullable values
				twoPaneView.Pane1 = null;
				twoPaneView.Pane2 = null;
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Verify.IsNull(twoPaneView.Pane1);
				Verify.IsNull(twoPaneView.Pane2);
			});
		}
	}
}
