using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_UserControl
	{
		[TestMethod]
		public void When_Inheritance_Then_Matches_WinUI_Hierarchy()
		{
			var userControl = new UserControl();
			Assert.IsInstanceOfType(userControl, typeof(Control));
			Assert.IsNotInstanceOfType(userControl, typeof(ContentControl));

			var page = new Page();
			Assert.IsInstanceOfType(page, typeof(UserControl));
			Assert.IsInstanceOfType(page, typeof(Control));
			Assert.IsNotInstanceOfType(page, typeof(ContentControl));
		}

		[TestMethod]
		public void When_ContentProperty_Then_Typed_UIElement()
		{
			Assert.AreEqual(typeof(UIElement), typeof(UserControl).GetProperty(nameof(UserControl.Content)).PropertyType);
		}

		[TestMethod]
		public async Task When_Content_Set_Then_Hosted_Without_ContentPresenter()
		{
			var child = new Border { Width = 100, Height = 50 };
			var userControl = new UserControl { Content = child };

			TestServices.WindowHelper.WindowContent = userControl;
			await TestServices.WindowHelper.WaitForLoaded(userControl);
			await TestServices.WindowHelper.WaitForIdle();

			// The content must be the direct, single visual child — no ContentPresenter layer.
			Assert.AreEqual(1, VisualTreeHelper.GetChildrenCount(userControl));
			Assert.AreSame(child, VisualTreeHelper.GetChild(userControl, 0));

			// And it must be measured/arranged through Control's single-child layout.
			// Tolerance accommodates sub-pixel layout rounding on fractional-DPI heads (e.g. WASM browser).
			Assert.AreEqual(100, child.ActualWidth, 1d);
			Assert.AreEqual(50, child.ActualHeight, 1d);
		}

		[TestMethod]
		public async Task When_Content_Reassigned_Then_Child_Swapped()
		{
			var first = new Border { Width = 100, Height = 50 };
			var second = new Border { Width = 80, Height = 40 };
			var userControl = new UserControl { Content = first };

			TestServices.WindowHelper.WindowContent = userControl;
			await TestServices.WindowHelper.WaitForLoaded(userControl);

			Assert.AreSame(first, VisualTreeHelper.GetChild(userControl, 0));

			userControl.Content = second;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(1, VisualTreeHelper.GetChildrenCount(userControl));
			Assert.AreSame(second, VisualTreeHelper.GetChild(userControl, 0));

			userControl.Content = null;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(0, VisualTreeHelper.GetChildrenCount(userControl));
		}

		[TestMethod]
		public async Task When_DataContext_Then_Flows_Through_Visual_Tree()
		{
			var textBlock = new TextBlock();
			textBlock.SetBinding(TextBlock.TextProperty, new Binding());
			var userControl = new UserControl { Content = textBlock, DataContext = "hello" };

			TestServices.WindowHelper.WindowContent = userControl;
			await TestServices.WindowHelper.WaitForLoaded(userControl);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual("hello", textBlock.DataContext);
			Assert.AreEqual("hello", textBlock.Text);
		}
	}
}
