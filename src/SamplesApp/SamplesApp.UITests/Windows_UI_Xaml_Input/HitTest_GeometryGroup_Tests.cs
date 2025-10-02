using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Input
{
	[TestFixture]
	public partial class HitTest_GeometryGroup_Tests : SampleControlUITestBase
	{
		private const string _sample = "UITests.Windows_UI_Input.PointersTests.HitTest_GeometryGroup";

		[Test]
		[AutoRetry]
		public void When_HollowCircle2()
		{
			Run(_sample);

			var element = "HollowCircle2";

			// tap on the hollow circle at the center of 12-o-clock position
			var rect = _app.WaitForElement(element).Single().Rect;
			var target = new PointF(
				rect.CenterX,
				rect.Bottom - rect.Height * (7f / 8f)
			);
			_app.TapCoordinates(target.X, target.Y);

			var result = _app.Marked("LastPressed").GetDependencyPropertyValue<string>("Text");
			var resultSrc = _app.Marked("LastPressedSrc").GetDependencyPropertyValue<string>("Text");

			result.Should().Be(element);
			resultSrc.Should().Be(element);
		}
	}
}
