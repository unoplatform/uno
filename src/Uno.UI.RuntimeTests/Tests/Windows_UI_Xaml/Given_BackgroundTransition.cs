using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
[RunsOnUIThread]
public class Given_BackgroundTransition
{
#if __SKIA__
	[TestMethod]
	[RunsOnUIThread]
	[DataRow(typeof(Grid))]
	[DataRow(typeof(StackPanel))]
	[DataRow(typeof(Border))]
	[DataRow(typeof(ContentPresenter))]
	[RequiresFullWindow] // https://github.com/unoplatform/uno/issues/17470
	public async Task When_Has_Brush_Transition(Type type)
	{
		var control = (FrameworkElement)Activator.CreateInstance(type);

		control.Width = 200;
		control.Height = 200;

		var transition = new BrushTransition()
		{
			Duration = TimeSpan.FromMilliseconds(2000),
		};

		Action<Brush> setBackground = null;
		if (control is Panel panel)
		{
			panel.BackgroundTransition = transition;
			setBackground = b => panel.Background = b;
		}
		else if (control is Border border)
		{
			border.BackgroundTransition = transition;
			setBackground = b => border.Background = b;
		}
		else if (control is ContentPresenter contentPresenter)
		{
			contentPresenter.BackgroundTransition = transition;
			setBackground = b => contentPresenter.Background = b;
		}
		else
		{
			Assert.Fail("Unexpected input");
		}

		setBackground(new SolidColorBrush(Windows.UI.Colors.Red));

		await UITestHelper.Load(control);

		setBackground(new SolidColorBrush(Windows.UI.Colors.Blue));

		await Task.Delay(1000);

		var bitmap = await UITestHelper.ScreenShot(control);

		ImageAssert.HasColorAt(bitmap, new Point(100, 100), new Color(255, 127, 0, 127), tolerance: 20);
	}
#endif
}
