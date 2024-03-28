using System.Reflection;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_ToggleSwitch
{
	[TestMethod]
	public async Task Knob_Translation()
	{
		var toggleSwitch = new ToggleSwitch() { IsOn = true };
		WindowHelper.WindowContent = toggleSwitch;
		await WindowHelper.WaitForLoaded(toggleSwitch);
		await WindowHelper.WaitForIdle();
		var minKnobTranslation = (double)typeof(ToggleSwitch).GetField("_minKnobTranslation", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(toggleSwitch);
		var maxKnobTranslation = (double)typeof(ToggleSwitch).GetField("_maxKnobTranslation", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(toggleSwitch);
		Assert.AreEqual(0, minKnobTranslation);
		Assert.AreEqual(24, maxKnobTranslation);
	}
}
