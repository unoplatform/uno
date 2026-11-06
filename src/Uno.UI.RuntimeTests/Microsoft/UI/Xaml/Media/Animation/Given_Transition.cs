using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation;

[TestClass]
[RunsOnUIThread]
public class Given_Transition
{
	[TestMethod]
	public async Task When_Element_Transitions_Has_EntranceThemeTransition()
	{
		var border = new Border
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Red),
			Transitions = new TransitionCollection { new EntranceThemeTransition() },
		};

		var minOpacity = double.MaxValue;
		EventHandler<object> onRendering = (_, _) => minOpacity = Math.Min(minOpacity, border.Opacity);

		CompositionTarget.Rendering += onRendering;
		try
		{
			TestServices.WindowHelper.WindowContent = border;
			await TestServices.WindowHelper.WaitForLoaded(border);
			await Task.Delay(500);
		}
		finally
		{
			CompositionTarget.Rendering -= onRendering;
		}

		Assert.IsTrue(minOpacity < 1, $"The element's own EntranceThemeTransition should fade it in (min opacity {minOpacity}).");
		Assert.AreEqual(1.0, border.Opacity, 0.001);
	}
}
