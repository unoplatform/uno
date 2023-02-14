#if __WASM__
using System.Linq;
using System.Threading.Tasks;
using Private.Infrastructure;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
[RequiresFullWindow]
public class Given_SystemFocusVisual
{
	[TestMethod]
	public async Task When_Focused_Element_Scrolled()
	{
		var scrollViewer = new ScrollViewer()
		{
			Height = 200,
			Margin = ThicknessHelper.FromUniformLength(30)
		};
		var button = new Button()
		{
			Content = "Test",
			FocusVisualPrimaryThickness = ThicknessHelper.FromUniformLength(10),
			FocusVisualSecondaryThickness = ThicknessHelper.FromUniformLength(10),
		};
		var topBorder = new Border() { Height = 150, Width = 300, Background = SolidColorBrushHelper.Blue };
		var bottomBorder = new Border() { Height = 300, Width = 300, Background = SolidColorBrushHelper.Red };
		var stackPanel = new StackPanel()
		{
			Children =
			{
				topBorder,
				button,
				bottomBorder,
			}
		};

		scrollViewer.Content = stackPanel;

		TestServices.WindowHelper.WindowContent = scrollViewer;
		await TestServices.WindowHelper.WaitForIdle();

		button.Focus(FocusState.Keyboard);
		await TestServices.WindowHelper.WaitForIdle();
		var visualTree = Uno.UI.Xaml.Core.CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRoot?.VisualTree;
		var focusVisualLayer = visualTree?.FocusVisualRoot;

		Assert.IsNotNull(focusVisualLayer);
		Assert.AreEqual(1, focusVisualLayer.Children.Count);

		var focusVisual = focusVisualLayer.Children.First();

		var transform = focusVisual.TransformToVisual(Microsoft.UI.Xaml.Window.Current.RootElement);
		var initialPoint = transform.TransformPoint(default);

		scrollViewer.ChangeView(null, 100, null, true);

		await TestServices.WindowHelper.WaitFor(() =>
		{
			transform = focusVisual.TransformToVisual(Microsoft.UI.Xaml.Window.Current.RootElement);
			var currentPoint = transform.TransformPoint(default);

			return currentPoint.Y < initialPoint.Y;
		});

		await TestServices.WindowHelper.WaitForIdle();

		transform = focusVisual.TransformToVisual(Microsoft.UI.Xaml.Window.Current.RootElement);
		var scrolledPoint = transform.TransformPoint(default);
		Assert.AreEqual(initialPoint.Y - 100, scrolledPoint.Y, 0.5);
	}
}
#endif
