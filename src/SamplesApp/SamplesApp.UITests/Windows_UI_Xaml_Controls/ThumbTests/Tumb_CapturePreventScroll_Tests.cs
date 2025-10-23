using System;
using System.Linq;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.ThumbTests;

[TestFixture]
public partial class Tumb_CapturePreventScroll_Tests : SampleControlUITestBase
{
	private const string _sample = "UITests.Windows_UI_Xaml_Controls.ThumbTests.Thumb_CapturePreventScroll";

	[Test]
	[AutoRetry]
	[ActivePlatforms(Platform.iOS)] // TODO: Not available on Android https://github.com/unoplatform/uno/issues/9080
	public void When_Drag_Then_DoesNotScroll()
	{
		Run(_sample);

		var sut = _app.WaitForElement("SUT").Single().Rect;

		_app.DragCoordinates(
			sut.CenterX,
			sut.CenterY,
			sut.CenterX - 100,
			sut.CenterY - 100);

		var result = _app.Marked("Output").GetDependencyPropertyValue<string>("Text");

		result.Should().Be("--none--");

		// SANITY CHECK
		// To validate that test is effectively testing something, let try to scroll bu clicking out of the thumb
		_app.DragCoordinates(
			sut.Right + 10,
			sut.Bottom + 10,
			sut.CenterX - 100,
			sut.CenterY - 100);

		result.Should().NotBe("--none--");
	}
}
