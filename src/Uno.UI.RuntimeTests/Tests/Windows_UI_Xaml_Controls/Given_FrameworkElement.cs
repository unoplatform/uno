#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Private.Infrastructure;
using MUXControlsTestApp.Utilities;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_FrameworkElement
	{
#if __WASM__
		// TODO Android does not handle measure invalidation properly
		[TestMethod]
		public Task When_Measure_Once() =>
			RunOnUIThread.Execute(() =>
			{
				var SUT = new MyControl01();

				SUT.Measure(new Size(10, 10));
				Assert.AreEqual(1, SUT.MeasureOverrides.Count);
				Assert.AreEqual(new Size(10, 10), SUT.MeasureOverrides[0]);

				SUT.Measure(new Size(10, 10));
				Assert.AreEqual(1, SUT.MeasureOverrides.Count);
			});
#endif

		[TestMethod]
		public Task When_Measure_And_Invalidate() =>
			RunOnUIThread.Execute(() =>
			{
				var SUT = new MyControl01();

				SUT.Measure(new Size(10, 10));
				Assert.AreEqual(1, SUT.MeasureOverrides.Count);
				Assert.AreEqual(new Size(10, 10), SUT.MeasureOverrides[0]);

				SUT.InvalidateMeasure();

				SUT.Measure(new Size(10, 10));
				Assert.AreEqual(2, SUT.MeasureOverrides.Count);
				Assert.AreEqual(new Size(10, 10), SUT.MeasureOverrides[1]);
			});

		[TestMethod]
		public Task MeasureWithNan() =>
			RunOnUIThread.Execute(() =>
			{

				var SUT = new MyControl01();

				SUT.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				Assert.AreEqual(new Size(double.PositiveInfinity, double.PositiveInfinity), SUT.MeasureOverrides.Last());
				Assert.AreEqual(new Size(0, 0), SUT.DesiredSize);

				Assert.ThrowsException<InvalidOperationException>(() => SUT.Measure(new Size(double.NaN, double.NaN)));
				Assert.ThrowsException<InvalidOperationException>(() => SUT.Measure(new Size(42.0, double.NaN)));
				Assert.ThrowsException<InvalidOperationException>(() => SUT.Measure(new Size(double.NaN, 42.0)));
			});

		[TestMethod]
		public Task MeasureOverrideWithNan() =>
			RunOnUIThread.Execute(() =>
			{

				var SUT = new MyControl01();

				SUT.BaseAvailableSize = new Size(double.NaN, double.NaN);
				SUT.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				Assert.AreEqual(new Size(double.PositiveInfinity, double.PositiveInfinity), SUT.MeasureOverrides.Last());
				Assert.AreEqual(new Size(0, 0), SUT.DesiredSize);
			});

		[TestMethod]
#if __WASM__
		[Ignore] // Failing on WASM - https://github.com/unoplatform/uno/issues/2314
#endif
		public Task MeasureOverride_With_Nan_In_Grid() =>
			RunOnUIThread.Execute(() =>
			{
				var grid = new Grid();

				var SUT = new MyControl02();
				SUT.Content = new Grid();
				grid.Children.Add(SUT);

				SUT.BaseAvailableSize = new Size(double.NaN, double.NaN);
				grid.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				Assert.AreEqual(new Size(double.PositiveInfinity, double.PositiveInfinity), SUT.MeasureOverrides.Last());
				Assert.AreEqual(new Size(0, 0), SUT.DesiredSize);
			});

#if __WASM__
		// TODO Android does not handle measure invalidation properly
		[TestMethod]
		public Task When_Grid_Measure_And_Invalidate() =>
			RunOnUIThread.Execute(() =>
			{
				var grid = new Grid();
				var SUT = new MyControl01();

				grid.Children.Add(SUT);

				grid.Measure(new Size(10, 10));
				Assert.AreEqual(1, SUT.MeasureOverrides.Count);
				Assert.AreEqual(new Size(10, 10), SUT.MeasureOverrides[0]);

				grid.InvalidateMeasure();

				grid.Measure(new Size(10, 10));
				Assert.AreEqual(1, SUT.MeasureOverrides.Count);
			});
#endif

		[TestMethod]
#if __WASM__
		[Ignore] // Failing on WASM - https://github.com/unoplatform/uno/issues/2314
#endif
		public async Task When_MinWidth_SmallerThan_AvailableSize()
		{
			Border content = null;
			ContentControl contentCtl = null;
			Grid grid = null;

			await RunOnUIThread.Execute(() =>
			{
				content = new Border { Width = 100, Height = 15 };

				contentCtl = new ContentControl { MinWidth = 110, Content = content };

				grid = new Grid() { MinWidth = 120 };

				grid.Children.Add(contentCtl);

				grid.Measure(new Size(50, 50));
#if NETFX_CORE || __WASM__ // TODO: align all platforms with Windows here
				Assert.AreEqual(new Size(50, 15), grid.DesiredSize);
				Assert.AreEqual(new Size(110, 15), contentCtl.DesiredSize);
				Assert.AreEqual(new Size(100, 15), content.DesiredSize);
#endif

				grid.Arrange(new Rect(default, new Size(50, 50)));

				TestServices.WindowHelper.WindowContent = new Border { Child = grid, Width = 50, Height = 50 };
			});

			await TestServices.WindowHelper.WaitForIdle();
			await RunOnUIThread.Execute(() => { });
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.Execute(() =>
			{
				var ls1 = LayoutInformation.GetLayoutSlot(grid);
				Assert.AreEqual(new Rect(0, 0, 50, 50), ls1);
#if NETFX_CORE || __WASM__ // TODO: align all platforms with Windows here
				var ls2 = LayoutInformation.GetLayoutSlot(contentCtl);
				Assert.AreEqual(new Rect(0, 0, 120, 50), ls2);
				var ls3 = LayoutInformation.GetLayoutSlot(content);
				Assert.AreEqual(new Rect(0, 0, 100, 15), ls3);
#endif
			});
		}

		[TestMethod]
		[RunsOnUIThread]
		public void Check_ActualWidth_After_Measure()
		{
			var SUT = new Border { Width = 75, Height = 32 };
			var size = new Size(1000, 1000);
			SUT.Measure(size);
			Assert.AreEqual(75, SUT.DesiredSize.Width);
			Assert.AreEqual(32, SUT.DesiredSize.Height);

			Assert.AreEqual(0, SUT.ActualWidth);
			Assert.AreEqual(0, SUT.ActualHeight);
		}
	}

	public partial class MyControl01 : FrameworkElement
	{
		public List<Size> MeasureOverrides { get; } = new List<Size>();

		public Size? BaseAvailableSize;

		protected override Size MeasureOverride(Size availableSize)
		{
			MeasureOverrides.Add(availableSize);
			return base.MeasureOverride(BaseAvailableSize ?? availableSize);
		}
	}

	public partial class MyControl02 : ContentControl
	{
		public List<Size> MeasureOverrides { get; } = new List<Size>();

		public Size? BaseAvailableSize;

		protected override Size MeasureOverride(Size availableSize)
		{
			MeasureOverrides.Add(availableSize);
			return base.MeasureOverride(BaseAvailableSize ?? availableSize);
		}
	}
}
