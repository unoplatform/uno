using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SamplesApp.UITests;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
[RunsOnUIThread]
public class Given_VisualStateManager_UITest : SampleControlUITestBase
{
	// RunAsync loads samples from the SamplesApp head, which native WinUI does not host.
	private const RuntimeTestPlatforms UnsupportedPlatforms = RuntimeTestPlatforms.NativeWinUI;

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, UnsupportedPlatforms)]
	public async Task When_Testing_ComplexSetters()
	{
		using var _ = UITestHelper.ResetWindowContent();
		await RunAsync("UITests.Shared.Windows_UI_Xaml.VisualStateTests.VisualState_ComplexSetters_Automated");

		// The sample wires its "changeState" button to VisualStateManager.GoToState(this, "State01", true).
		// Drive that state change directly (no pointer injection).
		var page = WindowHelper.WindowContent.FindFirstDescendantOrThrow<UserControl>();
		VisualStateManager.GoToState(page, "State01", true);
		await WindowHelper.WaitForIdle();

		// State01 applies three setters with different value kinds:
		//  - border01_bound: {Binding Background, ElementName=border01} where border01 is Red
		//  - border02: {StaticResource myStaticResource} (a Purple SolidColorBrush)
		//  - border03: an inline complex SolidColorBrush (Orange)
		var border01Bound = page.FindFirstDescendantOrThrow<Border>("border01_bound");
		var border02 = page.FindFirstDescendantOrThrow<Border>("border02");
		var border03 = page.FindFirstDescendantOrThrow<Border>("border03");

		Assert.AreEqual(Colors.Red, ((SolidColorBrush)border01Bound.Background).Color);
		Assert.AreEqual(Colors.Purple, ((SolidColorBrush)border02.Background).Color);
		Assert.AreEqual(Colors.Orange, ((SolidColorBrush)border03.Background).Color);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, UnsupportedPlatforms)]
	public async Task When_Forever_Storyboard()
	{
		using var _ = UITestHelper.ResetWindowContent();
		await RunAsync("UITests.Shared.Windows_UI_Xaml.VisualStateTests.VisualState_Forever_Events");
		await WindowHelper.WaitForIdle();

		// The sample's Loaded handler subscribes to the template VisualStateGroup's
		// CurrentStateChanging/CurrentStateChanged events and logs each transition to LogsTextBlock.
		// OnClick calls VisualStateManager.GoToState(MyButton, "Blinking", true); drive it directly.
		var page = WindowHelper.WindowContent.FindFirstDescendantOrThrow<UserControl>();
		var myButton = page.FindFirstDescendantOrThrow<Control>("MyButton");
		var logs = page.FindFirstDescendantOrThrow<TextBlock>("LogsTextBlock");

		VisualStateManager.GoToState(myButton, "Blinking", true);

		await WindowHelper.WaitFor(
			() => logs.Text == "Changing to: Blinking\nChanged to: Blinking\n",
			timeoutMS: 3000,
			message: "VisualStateGroup state-change events were not logged as expected.");
	}
}
