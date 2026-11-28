using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using static Private.Infrastructure.TestServices;
using static Uno.UI.RuntimeTests.Helpers.TextInkHelper;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_TextBox
{
	[TestMethod]
	public async Task When_Rtl_TextBox_Latin_Keeps_Period_At_End()
	{
		var sut = new TextBox
		{
			Width = 300,
			FontSize = 20,
			FlowDirection = FlowDirection.RightToLeft,
			Text = "Hello world......",
			Foreground = new SolidColorBrush(Colors.Red),
			BorderThickness = new Thickness(0),
		};

		try
		{
			var (_, bitmap) = await Show(sut);
			var ink = Letters(bitmap, Colors.Red);

			// As on WinUI: left-to-right text keeps its trailing dots at its end, and stays on the left.
			Assert.IsTrue(DotsOnTheRight(ink), $"The dots should be right of the letters: ink {ink.Ink}, letters {ink.Letters}.");
			Assert.IsTrue(ink.Ink.Min < 60, $"The text should stay left-aligned: ink {ink.Ink}.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
