using System.Threading.Tasks;
using Windows.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	[RequiresFullWindow]
	public partial class Given_RefreshContainer
	{
		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_Stretch_Child()
		{
			var grid = new Grid();
			var refreshContainer = new Microsoft.UI.Xaml.Controls.RefreshContainer();
			var child = new Border()
			{
				Background = new SolidColorBrush(Colors.Red),
				HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch,
				VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch,
			};
			var grandChild = new Border()
			{
				Background = new SolidColorBrush(Colors.Blue),
				Width = 10,
				Height = 10,
			};

			child.Child = grandChild;
			refreshContainer.Content = child;
			grid.Children.Add(refreshContainer);

			WindowHelper.WindowContent = grid;

			await WindowHelper.WaitForLoaded(grandChild);
			await WindowHelper.WaitForLoaded(refreshContainer);

			await WindowHelper.WaitForIdle();

			Assert.IsTrue(child.ActualWidth > 50);
			Assert.IsTrue(child.ActualHeight > 50);
		}
	}
}
