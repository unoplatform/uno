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

namespace SamplesApp.UITests.Windows_UI_Xaml_Input
{
	[TestFixture]
	internal class HitTest_Clipping_Tests : SampleControlUITestBase
	{
		private const string _sample = "UITests.Windows_UI_Input.PointersTests.HitTest_Clipping";

		[Test]
		[AutoRetry]
		[InjectedPointer(PointerDeviceType.Touch)]
		public async Task When_Scroller_Then_ElementsAboveAreStillTouchable()
		{
			await RunAsync(_sample);

			QueryEx arrange = "Scroll_Prepare";
			QueryEx act = "Scroll_Target";

			arrange.FastTap();
			await Task.Delay(100);

			act.FastTap();

			var result = App.Marked("The_Output").GetDependencyPropertyValue("Text");
			result.Should().Be("Scroll_Target");
		}

		[Test]
		[AutoRetry]
		[InjectedPointer(PointerDeviceType.Touch)]
		public async Task When_ClippedElement_Then_ElementsAboveAreTouchable()
		{
			await RunAsync(_sample);

			QueryEx act = "Clipped_Target";

			act.FastTap();

			var result = App.Marked("The_Output").GetDependencyPropertyValue("Text");
			result.Should().Be("Clipped_Target");
		}
	}
}
