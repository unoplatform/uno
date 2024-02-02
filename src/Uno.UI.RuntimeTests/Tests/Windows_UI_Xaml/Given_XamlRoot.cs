using System.Threading.Tasks;
using Private.Infrastructure;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_XamlRoot
	{
		[TestMethod]
		[RunsOnUIThread]
#if HAS_UNO_WINUI
		[Ignore("Window.Current is null on WinUI")]
#endif
		public async Task When_Loaded_Matches_Window_Root()
		{
			if (TestServices.WindowHelper.IsXamlIsland)
			{
				return;
			}

			var border = new Border();
			var completionSource = new TaskCompletionSource<XamlRoot>();
			border.Loaded += (s, e) =>
			{
				completionSource.SetResult(border.XamlRoot);
			};
			TestServices.WindowHelper.WindowContent = border;
			var xamlRoot = await completionSource.Task;
			Assert.AreEqual(Window.Current.Content, xamlRoot.Content);
			Assert.AreEqual(Window.Current.Content!.RenderSize, xamlRoot.Size);
		}
	}
}
