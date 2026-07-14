#if HAS_INPUT_INJECTOR || WINAPPSDK

using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit.DevTools.Input;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices.WindowHelper;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Input;

// Migrated from SamplesApp.UITests Windows_UI_Xaml_Input.Enability_Tests — a disabled control
// (and a control disabling itself mid-gesture) must not keep receiving pointer events.
[TestClass]
public class Given_Enability_UITest
{
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS | RuntimeTestPlatforms.NativeWasm)]
	public async Task When_ButtonDisabled_Then_NoPointerEvents()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			return;
		}

		var output = new TextBlock();
		var button = new Button { Content = "Disabled button", IsEnabled = false };
		RegisterEvents(button, output);

		var panel = new StackPanel { Children = { output, button } };

		try
		{
			await UITestHelper.Load(panel);

			var injector = InputInjector.TryCreate();
			Assert.IsNotNull(injector);
			using var mouse = injector.GetMouse();

			var center = GetCenter(button);
			mouse.Tap(center);
			await WaitForIdle();

			Assert.IsTrue(string.IsNullOrWhiteSpace(output.Text));
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS | RuntimeTestPlatforms.NativeWasm)]
	public async Task When_ButtonDisabling_Then_NoMorePointerEvents()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			return;
		}

		var output = new TextBlock();
		var button = new Button { Content = "Disabling button" };
		RegisterEvents(button, output);
		button.Click += (snd, e) => button.IsEnabled = false;

		var panel = new StackPanel { Children = { output, button } };

		try
		{
			await UITestHelper.Load(panel);

			var injector = InputInjector.TryCreate();
			Assert.IsNotNull(injector);
			using var mouse = injector.GetMouse();

			var center = GetCenter(button);

			// Tap invokes Click, which disables the button.
			mouse.Tap(center);
			await WaitForIdle();

			// Dragging off a now-disabled button must not resurrect Pressed/Moved/Entered.
			mouse.Drag(center, new Point(center.X, center.Y + 20));
			await WaitForIdle();

			CollectionAssert.Contains(new[] { "Click", "Exited", "Released" }, output.Text);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	private static void RegisterEvents(Button button, TextBlock output)
	{
		button.Click += (snd, e) => output.Text = "Click";
		button.PointerPressed += (snd, e) => output.Text = "Pressed";
		button.PointerReleased += (snd, e) => output.Text = "Released";
		button.PointerMoved += (snd, e) => output.Text = "Moved";
		button.PointerEntered += (snd, e) => output.Text = "Entered";
		button.PointerExited += (snd, e) => output.Text = "Exited";
	}

	private static Point GetCenter(FrameworkElement element) =>
		element.TransformToVisual(TestServices.WindowHelper.XamlRoot.Content)
			.TransformPoint(new Point(element.ActualWidth / 2, element.ActualHeight / 2));
}

#endif
