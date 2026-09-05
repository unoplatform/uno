using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SamplesApp.UITests;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
[RunsOnUIThread]
public class Given_VisualStateManager_UITest : SampleControlUITestBase
{
	// RunAsync loads samples from the SamplesApp.Skia / SamplesApp.Wasm heads, so these tests
	// only apply to Skia (all variants) and native WebAssembly; the native mobile / WinUI heads
	// are excluded (RunAsync throws PlatformNotSupportedException there).
	// RunAsync loads samples only on Skia/native-WASM hosts and needs the input injector,
	// so exclude every native head (native WASM included).
	private const RuntimeTestPlatforms UnsupportedPlatforms = RuntimeTestPlatforms.Native;

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, UnsupportedPlatforms)]
	public async Task When_Testing_ComplexSetters()
	{
		try
		{
			await RunAsync("UITests.Shared.Windows_UI_Xaml.VisualStateTests.VisualState_ComplexSetters_Automated");

			// The sample wires its "changeState" button to VisualStateManager.GoToState(this, "State01", true).
			// Drive that state change directly (no pointer injection) so the test is valid on Skia and WebAssembly.
			DependencyObject node = App.Query("changeState").Single().Element;
			while (node is not null and not UserControl)
			{
				node = VisualTreeHelper.GetParent(node);
			}

			Assert.IsNotNull(node, "Could not locate the sample UserControl.");
			VisualStateManager.GoToState((Control)node, "State01", true);
			await WindowHelper.WaitForIdle();

			// State01 applies three setters with different value kinds:
			//  - border01_bound: {Binding Background, ElementName=border01} where border01 is Red
			//  - border02: {StaticResource myStaticResource} (a Purple SolidColorBrush)
			//  - border03: an inline complex SolidColorBrush (Orange)
			var border01Bound = (Border)App.Query("border01_bound").Single().Element;
			var border02 = (Border)App.Query("border02").Single().Element;
			var border03 = (Border)App.Query("border03").Single().Element;

			Assert.AreEqual(Colors.Red, ((SolidColorBrush)border01Bound.Background).Color);
			Assert.AreEqual(Colors.Purple, ((SolidColorBrush)border02.Background).Color);
			Assert.AreEqual(Colors.Orange, ((SolidColorBrush)border03.Background).Color);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, UnsupportedPlatforms)]
	public async Task When_Forever_Storyboard()
	{
		try
		{
			await RunAsync("UITests.Shared.Windows_UI_Xaml.VisualStateTests.VisualState_Forever_Events");
			await WindowHelper.WaitForIdle();

			// The sample's Loaded handler subscribes to the template VisualStateGroup's
			// CurrentStateChanging/CurrentStateChanged events and logs each transition to LogsTextBlock.
			// OnClick calls VisualStateManager.GoToState(MyButton, "Blinking", true); drive it directly.
			var myButton = (Control)App.Query("MyButton").Single().Element;
			var logs = (TextBlock)App.Query("LogsTextBlock").Single().Element;

			VisualStateManager.GoToState(myButton, "Blinking", true);

			await WindowHelper.WaitFor(
				() => logs.Text == "Changing to: Blinking\nChanged to: Blinking\n",
				timeoutMS: 3000,
				message: "VisualStateGroup state-change events were not logged as expected.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
