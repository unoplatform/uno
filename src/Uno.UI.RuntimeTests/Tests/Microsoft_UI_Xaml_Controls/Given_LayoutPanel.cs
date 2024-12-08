using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using Private.Infrastructure;

#if HAS_UNO && !HAS_UNO_WINUI
using Windows.UI.Xaml.Controls;
#endif

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
public partial class Given_LayoutPanel
{
#if !WINAPPSDK
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Padding_Set_In_SizeChanged()
	{
#pragma warning disable CS8305 // Type is for evaluation purposes only and is subject to change or removal in future updates.
		var SUT = new LayoutPanel()
		{
			Width = 300,
			Height = 300,
			VerticalAlignment = VerticalAlignment.Top,
			Children =
			{
				new Ellipse()
				{
					Fill = new SolidColorBrush(Colors.DarkOrange)
				}
			}
		};

		SUT.SizeChanged += (sender, args) => SUT.Padding = new Thickness(0, 200, 0, 0);
#pragma warning restore CS8305 // Type is for evaluation purposes only and is subject to change or removal in future updates.

		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitForLoaded(SUT);
		await TestServices.WindowHelper.WaitForIdle();

		// We have a problem on IOS and Android where SUT isn't relayouted after the padding
		// change even though IsMeasureDirty is true. This is a workaround to explicity relayout.
#if __IOS__ || __ANDROID__
		SUT.InvalidateMeasure();
		SUT.UpdateLayout();
#endif

		Assert.AreEqual(200, ((UIElement)VisualTreeHelper.GetChild(SUT, 0)).ActualOffset.Y);
	}
#endif
}
