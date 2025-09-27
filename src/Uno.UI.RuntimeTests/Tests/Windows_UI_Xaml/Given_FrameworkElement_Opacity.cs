using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using FluentAssertions;
using FluentAssertions.Execution;
using Private.Infrastructure;
using MUXControlsTestApp.Utilities;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using SamplesApp.UITests.TestFramework;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]

	public class Given_FrameworkElement_Opacity
	{
#if __SKIA__
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Opacity()
		{
			if (OperatingSystem.IsAndroid())
			{
				Assert.Inconclusive("Fails on Android Skia, likely because the system doesn't have an available font for full block unicode character (\u2588)");
			}

			var SUT = new FrameworkElement_Opacity();

			await UITestHelper.Load(SUT);

			var si = await UITestHelper.ScreenShot(SUT);

			var width = SUT.tbOpacity1_0.ActualWidth;
			var height = SUT.tbOpacity1_0.ActualHeight;
			ImageAssert.HasColorAtChild(si, SUT.tbOpacity1_0, (width / 4) * 3, height / 2, Colors.Black);

			width = SUT.tbOpacity0_5.ActualWidth;
			height = SUT.tbOpacity0_5.ActualHeight;
			ImageAssert.HasColorAtChild(si, SUT.tbOpacity0_5, (width / 4) * 3, height / 2, "#FF808080");

			width = SUT.tbOpacity0_1.ActualWidth;
			height = SUT.tbOpacity0_1.ActualHeight;
			ImageAssert.HasColorAtChild(si, SUT.tbOpacity0_1, (width / 4) * 3, height / 2, "#FFE6E6E6");

			width = SUT.border0_5.ActualWidth;
			height = SUT.border0_5.ActualHeight;
			ImageAssert.HasColorAtChild(si, SUT.border0_5, 2, 2, "#FFFF8080");

			width = SUT.ImageOpacity0_5.ActualWidth;
			height = SUT.ImageOpacity0_5.ActualHeight;
			ImageAssert.HasColorAtChild(si, SUT.ImageOpacity0_5, width / 2, height / 2, "#FFFEF3C2");
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaMacOS | RuntimeTestPlatforms.SkiaIOS)]
		public async Task When_Opacity_Inner()
		{
			if (OperatingSystem.IsAndroid())
			{
				Assert.Inconclusive("Fails on Android Skia, likely because the system doesn't have an available font for full block unicode character (\u2588)");
			}

			var SUT = new FrameworkElement_Opacity();

			await UITestHelper.Load(SUT);

			var si = await UITestHelper.ScreenShot(SUT);

			var width = SUT.tbInnerOpacity1_0.ActualWidth;
			var height = SUT.tbInnerOpacity1_0.ActualHeight;
			ImageAssert.HasColorAtChild(si, SUT.tbInnerOpacity1_0, (width / 4) * 3.3, height / 2, "#FF808080");

			width = SUT.tbInnerOpacity0_5.ActualWidth;
			height = SUT.tbInnerOpacity0_5.ActualHeight;
			ImageAssert.HasColorAtChild(si, SUT.tbInnerOpacity0_5, (width / 4) * 3.3, height / 2, "#FFC0C0C0");

			width = SUT.tbInnerOpacity0_1.ActualWidth;
			height = SUT.tbInnerOpacity0_1.ActualHeight;
			ImageAssert.HasColorAtChild(si, SUT.tbInnerOpacity0_1, (width / 4) * 3.3, height / 2, "#FFF3F3F3");

			width = SUT.BorderInnerOpacity0_5.ActualWidth;
			height = SUT.BorderInnerOpacity0_5.ActualHeight;
			ImageAssert.HasColorAtChild(si, SUT.BorderInnerOpacity0_5, 2, 2, "#FFFFC0C0");

			width = SUT.tbBorderInnerOpacity0_5.ActualWidth;
			height = SUT.tbBorderInnerOpacity0_5.ActualHeight;
			ImageAssert.HasColorAtChild(si, SUT.tbBorderInnerOpacity0_5, (width / 4) * 3.3, height / 2, "#FFC09090", tolerance: 1);

			width = SUT.ImageInner0_5.ActualWidth;
			height = SUT.ImageInner0_5.ActualHeight;
			ImageAssert.HasColorAtChild(si, SUT.ImageInner0_5, width / 2, height / 2, "#FFFEF9E1", tolerance: 1);
		}
#endif
	}
}
