using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	public partial class Given_UIElement
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_StartBringIntoView_In_Live_Tree_Before_Loaded()
		{
			var panel = new StackPanel();
			var scrollViewer = new ScrollViewer { Height = 100, Content = panel };
			await UITestHelper.Load(scrollViewer);

			var raised = false;
			panel.BringIntoViewRequested += (_, _) => raised = true;

			var element = new Border { Width = 50, Height = 50 };
			panel.Children.Add(element);
			panel.UpdateLayout();

			// WinUI only requires the element to be in the live tree, not Loaded.
			Assert.IsFalse(element.IsLoaded);

			element.StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false });

			// WinUI routes BringIntoViewRequested asynchronously.
			await TestServices.WindowHelper.WaitFor(() => raised);
		}
	}
}
