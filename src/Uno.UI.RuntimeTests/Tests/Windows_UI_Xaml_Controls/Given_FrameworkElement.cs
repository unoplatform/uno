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

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_FrameworkElement
	{
		private async Task Dispatch(DispatchedHandler p)
		{
#if !NETFX_CORE
			await CoreApplication.GetCurrentView().Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, p);
#else
			await CoreApplication.MainView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, p);
#endif
		}

#if __WASM__
		// TODO Android does not handle measure invalidation properly
		[TestMethod]
		public async Task When_Measure_Once()
		{
			await Dispatch(() =>
			{
				var SUT = new MyControl01();

				SUT.Measure(new Size(10, 10));
				Assert.AreEqual(1, SUT.MeasureOverrides.Count);
				Assert.AreEqual(new Size(10, 10), SUT.MeasureOverrides[0]);

				SUT.Measure(new Size(10, 10));
				Assert.AreEqual(1, SUT.MeasureOverrides.Count);
			});
		}
#endif

		[TestMethod]
		public async Task When_Measure_And_Invalidate()
		{
			await Dispatch(() =>
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
		}

		[TestMethod]
		public async Task MeasureWithNan()
		{
			await Dispatch(() =>
			{

				var SUT = new MyControl01();

				SUT.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				Assert.AreEqual(new Size(double.PositiveInfinity, double.PositiveInfinity), SUT.MeasureOverrides.Last());
				Assert.AreEqual(new Size(0, 0), SUT.DesiredSize);

				Assert.ThrowsException<InvalidOperationException>(() => SUT.Measure(new Size(double.NaN, double.NaN)));
				Assert.ThrowsException<InvalidOperationException>(() => SUT.Measure(new Size(42.0, double.NaN)));
				Assert.ThrowsException<InvalidOperationException>(() => SUT.Measure(new Size(double.NaN, 42.0)));
			});
		}

		[TestMethod]
		public async Task MeasureOverrideWithNan()
		{
			await Dispatch(() =>
			{

				var SUT = new MyControl01();

				SUT.BaseAvailableSize = new Size(double.NaN, double.NaN);
				SUT.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				Assert.AreEqual(new Size(double.PositiveInfinity, double.PositiveInfinity), SUT.MeasureOverrides.Last());
				Assert.AreEqual(new Size(0, 0), SUT.DesiredSize);
			});
		}

		[TestMethod]
		public async Task MeasureOverride_With_Nan_In_Grid()
		{
			await Dispatch(() =>
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
		}

#if __WASM__
		// TODO Android does not handle measure invalidation properly
		[TestMethod]
		public async Task When_Grid_Measure_And_Invalidate()
		{
			await Dispatch(() =>
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
		}
#endif
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
