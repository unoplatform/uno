using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation.TestPages;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation;

[TestClass]
[RunsOnUIThread]
public class Given_TemplateBindingAfterAnimation
{
	[TestMethod]
	public async Task When_TemplateBinding_And_Animation_Set_Local_On_TemplatedParent()
	{
		// Scenario being tested:
		// - CustomButton style sets Foreground to Blue
		// - A visual state sets tb1.Foreground to Red (we call GoToState in OnApplyTemplate)
		// - tb1 Foreground has TemplateBinding to CustomButton's Foreground
		// NOTE: Current behavior doesn't exactly WinUI.
		var page = new TemplateBindingAfterAnimationPage();

		await UITestHelper.Load(page);

		var btn = page.customButton;
		var tb1 = btn.TextBlockTemplateChildBoundToForeground;

		Assert.AreEqual((btn.Foreground as SolidColorBrush).Color, Microsoft.UI.Colors.Blue);
		Assert.AreEqual((tb1.Foreground as SolidColorBrush).Color, Microsoft.UI.Colors.Red);

		btn.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green);

		Assert.AreEqual((btn.Foreground as SolidColorBrush).Color, Microsoft.UI.Colors.Green);
		Assert.AreEqual((tb1.Foreground as SolidColorBrush).Color, Microsoft.UI.Colors.Red);

		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual((btn.Foreground as SolidColorBrush).Color, Microsoft.UI.Colors.Green);
		Assert.AreEqual((tb1.Foreground as SolidColorBrush).Color, Microsoft.UI.Colors.Red);
	}

	[TestMethod]
	public async Task When_TemplateBinding_And_Animation_Change_Theme()
	{
		// Scenario being tested:
		// - CustomButton style sets Background to {ThemeResource TemplateBindingAfterAnimationThemeColor1} (Light=White, Dark=LightGray)
		// - A visual state sets tb2.Foreground to {ThemeResource TemplateBindingAfterAnimationThemeColor2} (Light=Brown, Dark=RosyBrown) (we call GoToState in OnApplyTemplate)
		// - tb2 Foreground has TemplateBinding to CustomButton's Background
		var page = new TemplateBindingAfterAnimationPage();

		await UITestHelper.Load(page);

		var btn = page.customButton;
		var tb2 = btn.TextBlockTemplateChildBoundToBackground;

		Assert.AreEqual((btn.Background as SolidColorBrush).Color, Microsoft.UI.Colors.White);
		Assert.AreEqual((tb2.Foreground as SolidColorBrush).Color, Microsoft.UI.Colors.Brown);

		using (ThemeHelper.UseDarkTheme())
		{
			Assert.AreEqual((btn.Background as SolidColorBrush).Color, Microsoft.UI.Colors.LightGray);

#if __ANDROID__ || __APPLE_UIKIT__
			// Android and iOS behavior is very wrong.
			// ResourceResolver.TryVisualTreeRetrieval fails to retrieve TemplateBindingAfterAnimationThemeColor2
			// So, we end up not updating the foreground.
			// TODO: Look into ResourceResolver.TryVisualTreeRetrieval and see whether it should
			// loop through all Sources instead of just the first one (as it's currently implemented)
			Assert.AreEqual((tb2.Foreground as SolidColorBrush).Color, Microsoft.UI.Colors.Brown);
#elif HAS_UNO
			Assert.AreEqual((tb2.Foreground as SolidColorBrush).Color, Microsoft.UI.Colors.RosyBrown);
#else
			Assert.AreEqual((tb2.Foreground as SolidColorBrush).Color, Microsoft.UI.Colors.LightGray);
#endif

			await TestServices.WindowHelper.WaitForIdle();

#if __ANDROID__ || __APPLE_UIKIT__
			Assert.AreEqual((tb2.Foreground as SolidColorBrush).Color, Microsoft.UI.Colors.Brown);
#else
			Assert.AreEqual((tb2.Foreground as SolidColorBrush).Color, Microsoft.UI.Colors.RosyBrown);
#endif
		}
	}
}
