#if !WINDOWS_UWP
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
    public class Given_XamlRoot
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Loaded_Matches_Window_Root()
		{
			var border = new Border();
			var completionSource = new TaskCompletionSource<XamlRoot>();
			border.Loaded += (s, e) =>
			{
				completionSource.SetResult(border.XamlRoot);
			};
			TestServices.WindowHelper.WindowContent = border;
			var xamlRoot = await completionSource.Task;
			Assert.AreEqual(Window.Current.Content, xamlRoot.Content);
			Assert.AreEqual(Window.Current.Content.RenderSize, xamlRoot.Size);
		}
	}
}
#endif
