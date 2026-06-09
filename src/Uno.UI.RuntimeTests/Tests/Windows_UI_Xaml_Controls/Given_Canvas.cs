using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_Canvas
{
	// Uno applies FrameworkPropertyMetadataOptions.AutoConvert, coercing string/int/float to the double
	// property type in SetValue; native WinUI throws when the value type doesn't match, so this is Uno-only.
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23409")]
	public void When_CanvasPropertyConvert()
	{
		var SUT = new Canvas();

		SUT.SetValue(Canvas.LeftProperty, "42");
		Assert.AreEqual(42d, SUT.GetValue(Canvas.LeftProperty));

		SUT.SetValue(Canvas.LeftProperty, 43);
		Assert.AreEqual(43d, SUT.GetValue(Canvas.LeftProperty));

		SUT.SetValue(Canvas.LeftProperty, 44f);
		Assert.AreEqual(44d, SUT.GetValue(Canvas.LeftProperty));

		SUT.SetValue(Canvas.TopProperty, "42");
		Assert.AreEqual(42d, SUT.GetValue(Canvas.TopProperty));

		SUT.SetValue(Canvas.ZIndexProperty, "42");
		Assert.AreEqual(42, SUT.GetValue(Canvas.ZIndexProperty));
	}
}
