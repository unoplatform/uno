using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Input
{
	public class Tapped_Tests : SampleControlUITestBase
	{
		[Test]
		public void TappedLocationTest()
		{
			Run("UITests.Shared.Windows_UI_Input.GestureRecognizerTests.TappedTest");

			var root = _app.WaitForElement("LocationTestRoot").Single();
			var target = _app.WaitForElement("LocationTestTarget").Single();

			int tapX = 10, tapY = 10;
			(int x, int y) targetToRoot = ((int)(target.Rect.X - root.Rect.X), (int)(target.Rect.Y - root.Rect.Y));

			_app.TapCoordinates(target.Rect.X + tapX, target.Rect.Y + tapY);

			var relativeToRoot = _app.Marked("LocationTestRelativeToRootLocation").GetDependencyPropertyValue<string>("Text");
			var relativeToTarget = _app.Marked("LocationTestRelativeToTargetLocation").GetDependencyPropertyValue<string>("Text");

			relativeToTarget.Should().Be($"({tapX:D},{tapY:D})");
			relativeToRoot.Should().Be($"({tapX+targetToRoot.x:D},{tapY + targetToRoot.y:D})");
		}
	}
}
