using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_Page
{
#if HAS_UNO
	[TestMethod]
	public async Task When_No_Content_Then_Templated_Child_Is_Laid_Out()
	{
		// Uno-only by design: WinUI's CUserControl::ApplyTemplate is a final no-op, so a WinUI Page
		// with no Content has no child at all and legitimately measures to 0x0. Uno expands the
		// template, so Page's layout overrides have to fall back to that child.
		var page = new Page
		{
			Width = 100,
			Height = 100,
			Template = (ControlTemplate)XamlReader.Load(
				"""
				<ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
					<Border Background="Red" />
				</ControlTemplate>
				"""),
		};

		await UITestHelper.Load(page);

		var root = (FrameworkElement)VisualTreeHelper.GetChild(page, 0);

		Assert.AreEqual(100, root.ActualWidth, 0.5, "Templated child width");
		Assert.AreEqual(100, root.ActualHeight, 0.5, "Templated child height");
	}
#endif
}
