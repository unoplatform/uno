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
		public void When_Tapped_Then_ArgsLocationIsValid()
		{
			Run("UITests.Shared.Windows_UI_Input.GestureRecognizerTests.TappedTest");

			var root = _app.WaitForElement("WhenTappedThenArgsLocationIsValid_Root").Single();
			var target = _app.WaitForElement("WhenTappedThenArgsLocationIsValid_Target").Single();

			const int tapX = 10, tapY = 10;
			(int x, int y) targetToRoot = ((int)(target.Rect.X - root.Rect.X), (int)(target.Rect.Y - root.Rect.Y));

			_app.TapCoordinates(target.Rect.X + tapX, target.Rect.Y + tapY);

			var relativeToRoot = _app.Marked("WhenTappedThenArgsLocationIsValid_Result_RelativeToRoot").GetDependencyPropertyValue<string>("Text");
			var relativeToTarget = _app.Marked("WhenTappedThenArgsLocationIsValid_Result_RelativeToTarget").GetDependencyPropertyValue<string>("Text");

			relativeToTarget.Should().Be($"({tapX:D},{tapY:D})");
			relativeToRoot.Should().Be($"({tapX+targetToRoot.x:D},{tapY + targetToRoot.y:D})");
		}

		[Test]
		public void When_ChildHandlesPointers()
		{
			Run("UITests.Shared.Windows_UI_Input.GestureRecognizerTests.TappedTest");

			var target = _app.WaitForElement("WhenChildHandlesPointers_Target").Single();

			const int tapX = 10, tapY = 10;
			_app.TapCoordinates(target.Rect.X + tapX, target.Rect.Y + tapY);

			var result = _app.Marked("WhenChildHandlesPointers_Result").GetDependencyPropertyValue<string>("Text");
			result.Should().Be("Tapped");
		}
	}
}
