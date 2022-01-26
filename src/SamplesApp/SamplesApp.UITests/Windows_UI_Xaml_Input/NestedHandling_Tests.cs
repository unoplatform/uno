using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Input;
using FluentAssertions;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.Testing;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Input
{
	[TestFixture]
	public partial class NestedHandling_Tests : SampleControlUITestBase
	{
		private const string _sample = "UITests.Windows_UI_Input.PointersTests.NestedHandling";

#if !__SKIA__
		[Test]
		[AutoRetry]
		[InjectedPointer(PointerDeviceType.Touch)]
		public async Task When_NestedHandlesPressed_Then_ContainerStillGetsSubsequentEvents()
		{
			await RunAsync(_sample);

			var target = App.WaitForElement("_nested").Single().Rect;
			App.DragCoordinates(target.X + 10, target.Y + 10, target.Right - 10, target.Bottom - 10);

			var result = App.Marked("_result").GetDependencyPropertyValue("Text");
			result.Should().Be("Pressed SUCCESS | Released SUCCESS");
		}
#endif

		[Test]
		[AutoRetry]
#if !__SKIA__
		[ActivePlatforms(Platform.Browser)] // Not supported on iOS and Android where dispatch is native and has "implicit capture".
#endif
		[InjectedPointer(PointerDeviceType.Touch)]
		public async Task When_Nested_Then_EnterAndExitedDoesNotBubble()
		{
			await RunAsync(_sample);

			var container = App.WaitForElement("_sample2_container").Single().Rect;
			var nested = App.WaitForElement("_sample2_nested").Single().Rect;

			if (CurrentPointerType is PointerDeviceType.Mouse)
			{
				// On CI we might miss moves on exit, so we split the movement in 2 gestures,
				// as it's a mouse, we won't have noisy 'exit' on pointer 'up'.
				App.DragCoordinates(container.X + 10, container.CenterY, container.Right - 10, nested.CenterY);
				App.DragCoordinates(nested.Right - 10, nested.CenterY, nested.Right + 10, container.CenterY);
			}
			else
			{
				App.DragCoordinates(container.X + 10, container.CenterY, container.Right - 10, container.CenterY);
			}

			var enterResult = App.Marked("_enterResult").GetDependencyPropertyValue<string>("Text").Trim();
			enterResult.Should().Be("ENTERED SUCCESS", "we should have received ENTER only on '_intermediate' which has subscribed to handled events too.");

			var exitResult = App.Marked("_exitResult").GetDependencyPropertyValue<string>("Text").Trim();
			exitResult.Should().Be("EXITED SUCCESS", "we should have received EXIT only on '_intermediate' which has subscribed to handled events too.");
		}
	}
}
