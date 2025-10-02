using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Input;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.Testing;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Input;

[TestFixture]
public partial class Capture_Tests : SampleControlUITestBase
{
	[Test]
	[AutoRetry]
	[ActivePlatforms(Platform.iOS, Platform.Android)] // This fails with unit test
	[InjectedPointer(PointerDeviceType.Touch)]
	public async Task TestSimple()
		=> await RunTest("Simple", TouchAndMoveOut);

	[Test]
	[AutoRetry]
	[InjectedPointer(PointerDeviceType.Touch)]
	public async Task TestVisibility()
		=> await RunTest("Visibility");

	[Test]
	[AutoRetry]
	[InjectedPointer(PointerDeviceType.Touch)]
	public async Task TestNestedVisibility()
		=> await RunTest("NestedVisibility");

	[Test]
	[AutoRetry]
	[Ignore("Inconsistent behavior between manual and unit test")]
	[InjectedPointer(PointerDeviceType.Touch)]
	public async Task TestIsEnabled()
		=> await RunTest("IsEnabled");

	[Test]
	[AutoRetry]
	[Ignore("Inconsistent behavior between manual and unit test")]
	[ActivePlatforms(Platform.Browser)] // The IsEnabled property is not inherited on other platforms yet.
	[InjectedPointer(PointerDeviceType.Touch)]
	public async Task TestNestedIsEnabled()
		=> await RunTest("NestedIsEnabled");


	private readonly Action<QueryEx> TouchAndHold = element => /*element.TouchAndHold() not implemented ... we can use tap instead */ element.FastTap();
	private void TouchAndMoveOut(QueryEx element)
	{
		var rect = App.WaitForElement(element).Single().Rect;
		App.DragCoordinates(rect.X + 10, rect.Y + 10, rect.Right + 10, rect.Y + 10);
	}

	private async Task RunTest(string testName, Action<QueryEx> act = null)
	{
		act = act ?? TouchAndHold;

		await RunAsync("UITests.Shared.Windows_UI_Input.PointersTests.Capture");

		var target = App.Marked($"{testName}Target");
		var result = App.Marked($"{testName}Result");

		App.WaitForElement(target);
		act(target);

#if HAS_RENDER_TARGET_BITMAP
		await TakeScreenshotAsync("Result");
#endif

		result.GetDependencyPropertyValue<string>("Text").Should().Be("SUCCESS");
	}
}
