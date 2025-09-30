using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.FrameTests;

[TestFixture]
internal class Given_Frame : SampleControlUITestBase
{
	[Test]
	[AutoRetry]
	public void When_PageIsTransparent_Then_DontSeeStackedPaged()
	{
		Run("UITests.Windows_UI_Xaml_Controls.FrameTests.Frame_PageStacking", skipInitialScreenshot: true);

		var frame = _app.Query("MyFrame").Single().Rect;

		_app.Marked("NavRed").FastTap();
		_app.Marked("NavTransparent").FastTap();

		var result = TakeScreenshot("post_nav", ignoreInSnapshotCompare: true);

		ImageAssert.HasColorAt(result, frame.CenterX, frame.CenterY, Color.Purple);
	}

	[Test]
	[AutoRetry]
	public void When_PageHasNoContent_Then_PointerPassThrough()
	{
		Run("UITests.Windows_UI_Xaml_Controls.FrameTests.Frame_PageStacking", skipInitialScreenshot: true);

		_app.Marked("NavRed").FastTap();
		_app.Marked("NavTransparent").FastTap();

		_app.FastTap("MyFrame");

		QueryEx output = "TappedResult";
		var result = output.GetDependencyPropertyValue<string>("Text");

		// We expect a "tapped" as:
		// 1. The red should be collapsed so it should not have receive the tapped event, so it has not flagged it as handled.
		// 2. Even if the bg of the TransparentPage is not null (i.e. Transparent),
		//	  the Page should not get any pointer event as its content is completely empty.
		result.Should().Be("tapped");
	}
}
