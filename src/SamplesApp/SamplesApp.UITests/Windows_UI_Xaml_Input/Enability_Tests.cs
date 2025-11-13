using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Input
{
	[TestFixture]
	public partial class Enability_Tests : SampleControlUITestBase
	{
		private const string _sample = "UITests.Windows_UI_Input.PointersTests.Enability";

		[Test]
		[AutoRetry]
		public void When_ButtonDisabled_Then_NoPointerEvents()
		{
			var element = "DisabledButton";

			Run(_sample);

			var target = _app.WaitForElement(element).Single().Rect;

			_app.TapCoordinates(target.CenterX, target.CenterY);

			var result = _app.Marked("_output").GetDependencyPropertyValue<string>("Text");

			result.Should().BeNullOrWhiteSpace();
		}

		[Test]
		[AutoRetry]
		public void When_ButtonDisabling_Then_NoMorePointerEvents()
		{
			var element = "DisablingButton";

			Run(_sample);

			var target = _app.WaitForElement(element).Single().Rect;

			_app.TapCoordinates(target.CenterX, target.CenterY);
			_app.DragCoordinates(target.CenterX, target.CenterY, target.CenterX, target.CenterY + 20);

			var result = _app.Marked("_output").GetDependencyPropertyValue<string>("Text");

			result.Should().BeOneOf("Click", "Exited", "Released");
		}
	}
}
