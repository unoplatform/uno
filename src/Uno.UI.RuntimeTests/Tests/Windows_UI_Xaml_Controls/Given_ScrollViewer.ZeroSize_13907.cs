using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_ScrollViewer_ZeroSize_13907
	{
		// Reproduction for https://github.com/unoplatform/uno/issues/13907
		// On WinUI, a ScrollViewer hosted inside a 0x0 Border is still loaded
		// (Loaded fires, IsLoaded becomes true). Uno was reported to never
		// load the inner ScrollViewer in this configuration.
		[TestMethod]
		public async Task When_ScrollViewer_In_ZeroSize_Border_Should_Load_13907()
		{
			var scrollViewer = new ScrollViewer
			{
				VerticalScrollMode = ScrollMode.Enabled,
				HorizontalScrollMode = ScrollMode.Enabled,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
				Content = new TextBlock { Text = "content" },
			};

			var border = new Border
			{
				Width = 0,
				Height = 0,
				Child = scrollViewer,
			};

			WindowHelper.WindowContent = border;

			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(
				scrollViewer.IsLoaded,
				"ScrollViewer hosted inside a 0x0 Border should still be loaded (matches WinUI). " +
				"See https://github.com/unoplatform/uno/issues/13907");
		}
	}
}
