#nullable enable

using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;

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

	// On native WinUI a non-finite Top leaves the children after the bad one unarranged
	// (CCanvas::ArrangeOverride stops at the first failing Arrange), so the sibling checks are Uno-only.
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26371")]
	[DataRow(double.NaN, 0d)]
	[DataRow(0d, double.NaN)]
	[DataRow(double.PositiveInfinity, 0d)]
	[DataRow(0d, double.NegativeInfinity)]
	public async Task When_Child_Offset_Is_Not_Finite(double left, double top)
	{
		var good1 = new Border { Width = 10, Height = 10, Background = new SolidColorBrush(Colors.Green) };
		var bad = new Border { Width = 10, Height = 10, Background = new SolidColorBrush(Colors.Red) };
		var good2 = new Border { Width = 10, Height = 10, Background = new SolidColorBrush(Colors.Blue) };
		Canvas.SetLeft(good1, 20);
		Canvas.SetLeft(bad, left);
		Canvas.SetTop(bad, top);
		Canvas.SetLeft(good2, 60);

		var SUT = new Canvas
		{
			Width = 100,
			Height = 20,
			Background = new SolidColorBrush(Colors.White),
			Children = { good1, bad, good2 },
		};

		await UITestHelper.Load(SUT);

		Assert.AreEqual(new Vector3(20, 0, 0), good1.ActualOffset);
		Assert.AreEqual(new Vector3(60, 0, 0), good2.ActualOffset);
		Assert.AreEqual(new Vector2(10, 10), good2.ActualSize);
		Assert.AreEqual(new Vector2(10, 10), bad.ActualSize);

		var screenshot = await UITestHelper.ScreenShot(SUT);
		ImageAssert.HasColorAt(screenshot, 25, 5, Colors.Green, tolerance: 5);
		ImageAssert.HasColorAt(screenshot, 65, 5, Colors.Blue, tolerance: 5);
		ImageAssert.HasColorAt(screenshot, 5, 5, Colors.White, tolerance: 5);
	}
}
