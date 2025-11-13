using System;
using System.Collections.Generic;
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
	public partial class HitTest_Image_Tests : SampleControlUITestBase
	{
		private const string _sample = "UITests.Windows_UI_Input.PointersTests.HitTest_Image";

		[Test]
		[AutoRetry]
		public void When_Image()
		{
			var element = "TheImage";

			Run(_sample);

			var target = _app.WaitForElement(element).Single().Rect;

			_app.TapCoordinates(target.CenterX, target.CenterY);

			var result = _app.Marked("LastPressed").GetDependencyPropertyValue<string>("Text");
			var resultSrc = _app.Marked("LastPressedSrc").GetDependencyPropertyValue<string>("Text");

			result.Should().Be(element);
			resultSrc.Should().Be(element);
		}
	}
}
