using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.Disposables;
using Uno.UI.RuntimeTests.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_Toolkit
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_VisibleBoundsPadding
	{
#if __ANDROID__
		[TestMethod]
		public async Task Translucent_SystemBars()
		{
			ApplicationView.GetForCurrentView().ExitFullScreenMode();

			using var _ = UseFullWindow();
			using var __ = UseTranslucentBars();

			var redGrid = new Grid
			{
				Background = new SolidColorBrush(Colors.Red),
			};

			var blueGrid = new Grid
			{
				Background = new SolidColorBrush(Colors.Blue),
			};

			redGrid.Children.Add(blueGrid);

			VisibleBoundsPadding.SetPaddingMask(redGrid, VisibleBoundsPadding.PaddingMask.All);

			WindowHelper.WindowContent = redGrid;
			await WindowHelper.WaitForIdle();

			// wait for the system bars to re-layout
			await Task.Delay(100);

			var visibleBounds = ApplicationView.GetForCurrentView().VisibleBounds;
			var blueRect = blueGrid.TransformToVisual(redGrid).TransformBounds(new Windows.Foundation.Rect(0, 0, blueGrid.ActualWidth, blueGrid.ActualHeight));
			var redRect = redGrid.TransformToVisual(redGrid).TransformBounds(new Windows.Foundation.Rect(0, 0, redGrid.ActualWidth, redGrid.ActualHeight));

			var statusBarHeight = visibleBounds.Top - redRect.Top;
			var navAreaHeight = redRect.Bottom - visibleBounds.Bottom;

			Assert.AreEqual(blueRect.Top, statusBarHeight, message: $"Blue rect top: {blueRect.Top} should equal status bar height: {statusBarHeight}");
			Assert.AreEqual(blueRect.Bottom, redRect.Bottom - navAreaHeight, message: $"Blue rect bottom: {blueRect.Bottom} should be offset by nav area height: {navAreaHeight}");
			Assert.AreEqual(redGrid.Padding.Top, statusBarHeight, message: $"Red rect padding top: {redGrid.Padding.Top} should be equal to status bar height: {statusBarHeight}");
			Assert.AreEqual(redGrid.Padding.Bottom, navAreaHeight, message: $"Red rect padding bottom: {redGrid.Padding.Bottom} should be equal to nav area height: {navAreaHeight}");
		}

		[TestMethod]
		public async Task Translucent_SystemBars_Dynamic()
		{
			ApplicationView.GetForCurrentView().ExitFullScreenMode();

			using var _ = UseFullWindow();
			var tb = UseTranslucentBars();

			var redGrid = new Grid
			{
				Background = new SolidColorBrush(Colors.Red),
			};

			var blueGrid = new Grid
			{
				Background = new SolidColorBrush(Colors.Blue),
			};

			redGrid.Children.Add(blueGrid);
			VisibleBoundsPadding.SetPaddingMask(redGrid, VisibleBoundsPadding.PaddingMask.All);

			WindowHelper.WindowContent = redGrid;
			await WindowHelper.WaitForIdle();

			tb.Dispose();
			await WindowHelper.WaitForIdle();

			var blueWithOpaqueBars = blueGrid.TransformToVisual(redGrid).TransformBounds(new Windows.Foundation.Rect(0, 0, blueGrid.ActualWidth, blueGrid.ActualHeight));
			var redWithOpaqueBars = redGrid.TransformToVisual(redGrid).TransformBounds(new Windows.Foundation.Rect(0, 0, redGrid.ActualWidth, redGrid.ActualHeight));
			var visibleBoundsWithOpaqueBars = ApplicationView.GetForCurrentView().VisibleBounds;

			// before: windowWithOpaqueBars should be at (0, [statusBarHeight]) and the same size as visibleBoundsWithOpaqueBars
			using var __ = UseTranslucentBars();
			// after: windowWithTranslucentBars should be at (0, 0) and should differ from visibleBoundsWithTranslucentBars in height by [statusBarHeight] + [navAreaHeight]
			// wait for the system bars to re-layout
			await Task.Delay(100);
			await WindowHelper.WaitFor(() => redGrid.Padding.Top > 0);
			await WindowHelper.WaitForIdle();

			var blueWithTranslucentBars = blueGrid.TransformToVisual(redGrid).TransformBounds(new Windows.Foundation.Rect(0, 0, blueGrid.ActualWidth, blueGrid.ActualHeight));
			var redWithTranslucentBars = redGrid.TransformToVisual(redGrid).TransformBounds(new Windows.Foundation.Rect(0, 0, redGrid.ActualWidth, redGrid.ActualHeight));
			var visibleBoundsWithTranslucentBars = ApplicationView.GetForCurrentView().VisibleBounds;

			var statusBarHeight = visibleBoundsWithTranslucentBars.Top - redWithTranslucentBars.Top;
			var navAreaHeight = redWithTranslucentBars.Bottom - visibleBoundsWithTranslucentBars.Bottom;

			Assert.AreEqual(blueWithTranslucentBars.Top, statusBarHeight, message: $"Blue rect top: {blueWithTranslucentBars.Top} should equal status bar height: {statusBarHeight}");
			Assert.AreEqual(blueWithTranslucentBars.Bottom, redWithTranslucentBars.Bottom - navAreaHeight, message: $"Blue rect bottom: {blueWithTranslucentBars.Bottom} should be offset by nav area height: {navAreaHeight}");
			Assert.AreEqual(redGrid.Padding.Top, statusBarHeight, message: $"Red rect padding top: {redGrid.Padding.Top} should be equal to status bar height: {statusBarHeight}");
			Assert.AreEqual(redGrid.Padding.Bottom, navAreaHeight, message: $"Red rect padding bottom: {redGrid.Padding.Bottom} should be equal to nav area height: {navAreaHeight}");
		}


		private IDisposable UseTranslucentBars()
		{
			var activity = Uno.UI.ContextHelper.Current as Android.App.Activity;
			activity?.Window?.AddFlags(Android.Views.WindowManagerFlags.TranslucentNavigation | Android.Views.WindowManagerFlags.TranslucentStatus);

			return Disposable.Create(() =>
			{
				activity?.Window?.ClearFlags(Android.Views.WindowManagerFlags.TranslucentNavigation | Android.Views.WindowManagerFlags.TranslucentStatus);
			});
		}

		// The [RequiresFullWindow] attribute sets the app to fullscreen on Android, hiding all system bars.
		// This method maintains the fullscreen state, but shows the system bars for tests that need them.
		private IDisposable UseFullWindow()
		{
			WindowHelper.UseActualWindowRoot = true;
			WindowHelper.SaveOriginalWindowContent();

			return Disposable.Create(() =>
			{
				WindowHelper.RestoreOriginalWindowContent();
				WindowHelper.UseActualWindowRoot = false;
			});
		}
#endif
	}
}
