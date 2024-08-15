using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
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

		Action<Brush> setBackground = null;
		Action<BrushTransition> setTransition = null;
		if (control is Panel panel)
		{
			setBackground = b => panel.Background = b;
			setTransition = t => panel.BackgroundTransition = t;
		}
		else if (control is Border border)
		{
			setBackground = b => border.Background = b;
			setTransition = t => border.BackgroundTransition = t;
		}
		else if (control is ContentPresenter contentPresenter)
		{
			setBackground = b => contentPresenter.Background = b;
			setTransition = t => contentPresenter.BackgroundTransition = t;
		}
		else
		{
			Assert.Fail("Unexpected input");
		}

		setBackground(new SolidColorBrush(Microsoft.UI.Colors.Red));

		setTransition(new BrushTransition { Duration = TimeSpan.FromMilliseconds(2000) });

		await UITestHelper.Load(control);

		setBackground(new SolidColorBrush(Microsoft.UI.Colors.Blue));

		await Task.Delay(1000);

		var bitmap = await UITestHelper.ScreenShot(control);

		ImageAssert.HasColorAt(bitmap, new Point(100, 100), new Color(255, 127, 0, 127), tolerance: 20);
	}
#endif
}
