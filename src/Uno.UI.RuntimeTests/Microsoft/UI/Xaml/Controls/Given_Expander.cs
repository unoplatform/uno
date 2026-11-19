using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_Expander
{
	[TestMethod]
	public async Task When_Content_Translated_Over_Header_Then_Clipped()
	{
		var SUT = new Expander
		{
			Header = "Header",
			Content = new Border { Height = 100 },
			Background = new SolidColorBrush(Colors.Red),
			IsExpanded = true,
			Width = 200,
		};

		await UITestHelper.Load(SUT);

		var clip = FindBorder(SUT, "ExpanderContentClip");
		var content = FindBorder(SUT, "ExpanderContent");

		// The expand/collapse animations slide the content like this; the clip keeps it out of the header.
		((CompositeTransform)content.RenderTransform).TranslateY = -40;
		await TestServices.WindowHelper.WaitForIdle();

		var clipTop = clip.TransformToVisual(SUT).TransformPoint(new Point(0, 0)).Y;
		var screenshot = await UITestHelper.ScreenShot(SUT);

		ImageAssert.HasColorAt(screenshot, 100, (float)clipTop + 20, Colors.Red, tolerance: 25);
		ImageAssert.DoesNotHaveColorAt(screenshot, 100, (float)clipTop - 10, Colors.Red, tolerance: 25);
	}

	private static Border FindBorder(DependencyObject parent, string name)
	{
		for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
		{
			var child = VisualTreeHelper.GetChild(parent, i);
			var result = child is Border { Name: var n } border && n == name ? border : FindBorder(child, name);
			if (result is not null)
			{
				return result;
			}
		}

		return null;
	}
}
