#if HAS_INPUT_INJECTOR || WINAPPSDK

using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit.DevTools.Input;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices.WindowHelper;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Input;

// Migrated from SamplesApp.UITests Windows_UI_Xaml_Input.Focus_Tests (Focus_FocusState sample):
// tapping a control focuses it with FocusState.Pointer while sibling controls stay Unfocused.
[TestClass]
public class Given_FocusInput_UITest
{
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS | RuntimeTestPlatforms.NativeWasm)]
	public async Task When_Tapped_FocusState_Is_Pointer()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			return;
		}

		var button = new Button { Content = "Is button" };
		var contentControl = new ContentControl
		{
			Content = new Border
			{
				Background = new SolidColorBrush(Colors.Pink),
				Child = new TextBlock { Text = "Is ContentControl" }
			}
		};
		var textBox = new TextBox { Text = "Is TextBox" };

		var panel = new StackPanel { Children = { button, contentControl, textBox } };

		try
		{
			await UITestHelper.Load(panel);

			Assert.AreEqual(FocusState.Unfocused, button.FocusState);
			Assert.AreEqual(FocusState.Unfocused, contentControl.FocusState);
			Assert.AreEqual(FocusState.Unfocused, textBox.FocusState);

			var injector = InputInjector.TryCreate();
			Assert.IsNotNull(injector);
			using var mouse = injector.GetMouse();

			mouse.Tap(GetCenter(button));
			await WaitFor(() => button.FocusState == FocusState.Pointer);

			Assert.AreEqual(FocusState.Pointer, button.FocusState);
			Assert.AreEqual(FocusState.Unfocused, contentControl.FocusState);
			Assert.AreEqual(FocusState.Unfocused, textBox.FocusState);

			mouse.Tap(GetCenter(textBox));
			await WaitFor(() => textBox.FocusState == FocusState.Pointer);

			Assert.AreEqual(FocusState.Unfocused, button.FocusState);
			Assert.AreEqual(FocusState.Unfocused, contentControl.FocusState);
			Assert.AreEqual(FocusState.Pointer, textBox.FocusState);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	private static Point GetCenter(FrameworkElement element) =>
		element.TransformToVisual(TestServices.WindowHelper.XamlRoot.Content)
			.TransformPoint(new Point(element.ActualWidth / 2, element.ActualHeight / 2));
}

#endif
