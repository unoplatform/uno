using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Input;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.Testing;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Input
{
	[TestFixture]
	internal class Nested_Exit_Tests : SampleControlUITestBase
	{
		private const string _sample = "UITests.Windows_UI_Input.PointersTests.Nested_Exit";

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // Needs pointer over events, i.e mouse only
		[InjectedPointer(PointerDeviceType.Mouse)]
		public async Task When_ExitNestedWithIntermediateNonHitTestableElement()
		{
			await RunAsync(_sample);

			var blue = App.WaitForElement("Case1_Blue").Single().Rect;
			App.TapCoordinates(blue.CenterX, blue.CenterY);
			App.DragCoordinates(blue.CenterX, blue.CenterY, blue.Right + 10, blue.CenterY);

			var result = App.Marked("Case1_out").GetDependencyPropertyValue<string>("Text");

			result.Should().Be("Success");
		}
	}
}
