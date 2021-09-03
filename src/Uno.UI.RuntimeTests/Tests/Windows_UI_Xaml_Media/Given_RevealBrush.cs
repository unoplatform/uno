using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_RevealBrush
	{
		[TestMethod]
		public async Task When_RevealBrush_Assigned()
		{
			// Basic smoke test - RevealBrush is currently unimplemented, but it shouldn't break the layout

			var siblingBorder = new Border { Width = 43, Height = 22 };
			var gridWithRevealBackground = new Grid
			{
				Width = 57,
				Height = 34,
				Background = new RevealBackgroundBrush() { Color = Colors.Brown, FallbackColor = Colors.Brown },
			};
			var parentSP = new StackPanel
			{
				Children =
				{
					gridWithRevealBackground,
					siblingBorder
				}
			};

			WindowHelper.WindowContent = parentSP;
			await WindowHelper.WaitForLoaded(parentSP);

			await WindowHelper.WaitForEqual(57, () => gridWithRevealBackground.ActualWidth);
			Assert.AreEqual(34, gridWithRevealBackground.ActualHeight);

			Assert.AreEqual(43, siblingBorder.ActualWidth);
			Assert.AreEqual(22, siblingBorder.ActualHeight);
		}
	}
}
