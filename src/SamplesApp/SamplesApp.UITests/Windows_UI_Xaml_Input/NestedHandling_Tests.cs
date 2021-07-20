using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SamplesApp.UITests.Extensions;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Input
{
	[TestFixture]
	public partial class NestedHandling_Tests : SampleControlUITestBase
	{
		private const string _sample = "UITests.Windows_UI_Input.PointersTests.NestedHandling";

		[Test]
		[AutoRetry]
		public void When_NestedHandlesPressed_Then_ContainerStillGetsSubsequentEvents()
		{
			Run(_sample);

			var target = _app.WaitForElement("_nested").Single().Rect;
			_app.DragCoordinates(target.X + 10, target.Y + 10, target.Right - 10, target.Bottom - 10);

			var result = _app.Marked("_result").GetDependencyPropertyValue("Text");
			result.Should().Be("Pressed SUCCESS | Released SUCCESS");
		}
	}
}
