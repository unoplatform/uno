using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;

#if HAS_UNO_WINUI && !WINAPPSDK
using ProgressRing = Uno.UI.Controls.Legacy.ProgressRing;
#else
using ProgressRing = Windows.UI.Xaml.Controls.ProgressRing;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_ProgressRing
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ProgressRing_Visible()
		{
			const int expectedSize =
#if __ANDROID__
				48; //Natively set as 48 for Android; 20 for other platforms
#else
				20;
#endif
			var ring = new ProgressRing() { IsActive = true };
			try
			{
				var panel = new StackPanel();
				panel.Children.Add(ring);
				var border = new Border();
				border.Child = panel;

				TestServices.WindowHelper.WindowContent = border;
				await TestServices.WindowHelper.WaitForIdle();

				border.Measure(new Size(1000, 1000));
				border.Arrange(new Rect(0, 0, 1000, 1000));

				Assert.AreEqual(expectedSize, Math.Round(ring.ActualHeight));
			}
			finally
			{
				ring.IsActive = false;
			}
		}

		[TestMethod]
		public Task When_ProgressRing_Collapsed() =>
			RunOnUIThread.ExecuteAsync(() =>
			{
				var SUT = new ProgressRing
				{
					Visibility = Visibility.Collapsed
				};

				var spacerBorder = new Border
				{
					Width = 10,
					Height = 10,
					Margin = new Thickness(5)
				};

				var root = new Grid
				{
					Children =
					{
						spacerBorder,
						SUT
					}
				};

				root.Measure(new Size(1000, 1000));
				Assert.AreEqual(10d + 5d + 5d, root.DesiredSize.Height);
			});
	}
}
