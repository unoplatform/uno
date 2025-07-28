using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_ColorAnimationUsingKeyFrames
	{
		[TestMethod]
		public async Task When_ThemeChanged_WithColorBinding()
		{
			var page = new TestPages.ColorAnimationPage();
			WindowHelper.WindowContent = page;
			await WindowHelper.WaitForLoaded(page);
			await WindowHelper.WaitForIdle();

			// minor delay added, so the "State1" visual-state applied on Page.Loaded have time to complete
			await Task.Delay(1000);

			Assert.AreEqual(Colors.Green, (page.Rect3.Fill as SolidColorBrush).Color);

			using (ThemeHelper.UseDarkTheme())
			{
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(Colors.Blue, (page.Rect3.Fill as SolidColorBrush).Color);
			}
		}

		[TestMethod]
		public async Task When_ThemeChanged_WithBrushBinding_AndColorPath()
		{
			var page = new TestPages.ColorAnimationPage();
			WindowHelper.WindowContent = page;
			await WindowHelper.WaitForLoaded(page);
			await WindowHelper.WaitForIdle();

			// minor delay added, so the "State1" visual-state applied on Page.Loaded have time to complete
			await Task.Delay(1000);

			Assert.AreEqual(Colors.Green, (page.Rect4.Fill as SolidColorBrush).Color);

			using (ThemeHelper.UseDarkTheme())
			{
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(Colors.Blue, (page.Rect4.Fill as SolidColorBrush).Color);
			}
		}
	}
}
