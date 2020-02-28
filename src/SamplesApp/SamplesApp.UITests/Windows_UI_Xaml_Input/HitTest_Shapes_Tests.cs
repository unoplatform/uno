using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Input
{
	[TestFixture]
	public partial class HitTest_Shapes_Tests : SampleControlUITestBase
	{
		private const string _sample = "UITests.Windows_UI_Input.PointersTests.HitTest_Shapes";

		[Test] [AutoRetry] public void When_RectangleFilled() => RunFilledTest();
		[Test] [AutoRetry] public void When_EllipseFilled() => RunFilledTest();
		[Test] [AutoRetry] public void When_LineFilled() => RunFilledTest();
		[Ignore("Path does not render with Uno")] [Test] [AutoRetry] public void When_PathFilled() => RunFilledTest();
		[Test] [AutoRetry] public void When_PolygonFilled() => RunFilledTest(offsetY: 10);
		[Test] [AutoRetry] public void When_PolylineFilled() => RunFilledTest(offsetY: 20);

		[Test] [AutoRetry] public void When_RectangleNotFilled() => RunNotFilledTest();
		[Test] [AutoRetry] public void When_EllipseNotFilled() => RunNotFilledTest();
		[ActivePlatforms(Platform.Browser, Platform.iOS)] [Test] [AutoRetry] public void When_LineNotFilled() => RunNotFilledTest();
		[Ignore("Path does not render with Uno")] [Test] [AutoRetry] public void When_PathNotFilled() => RunNotFilledTest();
		[Test] [AutoRetry] public void When_PolygonNotFilled() => RunNotFilledTest(offsetY: 10);
		[Test] [AutoRetry] public void When_PolylineNotFilled() => RunNotFilledTest(offsetY: 20);

		[Test] [AutoRetry] public void When_RectangleHidden() => RunHiddenTest();
		[Test] [AutoRetry] public void When_EllipseHidden() => RunHiddenTest();
		[Test] [AutoRetry] public void When_LineHidden() => RunHiddenTest();
		[Ignore("Path does not render with Uno")] [Test] [AutoRetry] public void When_PathHidden() => RunHiddenTest();
		[Test] [AutoRetry] public void When_PolygonHidden() => RunHiddenTest(offsetY: 10);
		[Test] [AutoRetry] public void When_PolylineHidden() => RunHiddenTest(offsetY: 20);


		private void RunFilledTest(int offsetX = 0, int offsetY = 0, [CallerMemberName] string testName = null)
			=> RunTest(offsetX, offsetY, testName.Substring("When_".Length), testName.Substring("When_".Length));

		private void RunNotFilledTest(int offsetX = 0, int offsetY = 0, [CallerMemberName] string testName = null)
			=> RunTest(offsetX, offsetY, testName.Substring("When_".Length), "Root");

		private void RunHiddenTest(int offsetX = 0, int offsetY = 0, [CallerMemberName] string testName = null)
			=> RunTest(offsetX, offsetY, testName.Substring("When_".Length) + "SubElement", testName.Substring("When_".Length) + "SubElement");

		private void RunTest(int offsetX, int offsetY, string element, string expected)
		{
			Run(_sample);

			var target = _app.WaitForElement(element).Single().Rect;
			
			_app.TapCoordinates(target.CenterX, target.CenterY);

			var result = _app.Marked("LastPressed").GetDependencyPropertyValue<string>("Text");

			result.Should().Be(expected);
		}
	}
}
