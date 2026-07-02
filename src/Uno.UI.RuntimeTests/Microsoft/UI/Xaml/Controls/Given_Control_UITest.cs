#if HAS_INPUT_INJECTOR || WINAPPSDK

using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit.DevTools.Input;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices.WindowHelper;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
public class Given_Control_UITest
{
	// Migrated from SamplesApp.UITests Control_Tests.cs (Control_IsEnabled_Inheritance sample):
	// a disabled control (and its subtree) must not raise pointer events, and must start raising
	// them again once IsEnabled is toggled back on.
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS | RuntimeTestPlatforms.NativeWasm)]
	public async Task When_IsEnabled_Initially_False_And_Inherited()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			return;
		}

		var counter = 0;

		// A Border gives the ContentControl a guaranteed hit-testable surface; when the
		// ContentControl is disabled the inherited state must suppress hit-testing on it too.
		var hitSurface = new Border
		{
			Width = 200,
			Height = 80,
			Background = new SolidColorBrush(Microsoft.UI.Colors.SkyBlue),
		};

		var buttonUnderTest = new ContentControl
		{
			Content = hitSurface,
			IsEnabled = false,
		};
		buttonUnderTest.PointerPressed += (_, _) => counter++;

		var panel = new StackPanel { Children = { buttonUnderTest } };

		try
		{
			await UITestHelper.Load(panel);

			var injector = InputInjector.TryCreate();
			Assert.IsNotNull(injector);
			using var mouse = injector.GetMouse();

			async Task Tap()
			{
				var center = buttonUnderTest.TransformToVisual(TestServices.WindowHelper.XamlRoot.Content)
					.TransformPoint(new Point(buttonUnderTest.ActualWidth / 2, buttonUnderTest.ActualHeight / 2));
				mouse.Press(center);
				mouse.Release();
				await WaitForIdle();
			}

			// Initially disabled: tapping must not raise PointerPressed.
			Assert.AreEqual(0, counter);
			await Tap();
			Assert.AreEqual(0, counter);

			// Re-enable and confirm the state took effect.
			buttonUnderTest.IsEnabled = true;
			await WaitForIdle();
			Assert.IsTrue(buttonUnderTest.IsEnabled);

			// Now enabled: tapping raises PointerPressed exactly once.
			await Tap();
			Assert.AreEqual(1, counter);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}
}

#endif
