using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers.Queries;
using Query = System.Func<Uno.UITest.IAppQuery, Uno.UITest.IAppQuery>;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.SwipeControlTests
{
	[TestFixture]
	public partial class SwipeControlTests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS | Platform.Android)]
		public void When_SingleItem()
		{
			Run("UITests.Windows_UI_Xaml_Controls.SwipeControlTests.SwipeControl_Automated");

			QueryEx sut = "SUT";
			QueryEx output = "Output";

			var sutRect = _app.Query(sut).Single().Rect;

			_app.DragCoordinates(sutRect.CenterX, sutRect.CenterY, sutRect.Right - 10, sutRect.CenterY);

			var result = output.GetDependencyPropertyValue<string>("Text");

			Assert.AreEqual("Left_1", result);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS | Platform.Android)]
		public void When_MultipleItems()
		{
			Run("UITests.Windows_UI_Xaml_Controls.SwipeControlTests.SwipeControl_Automated");

			QueryEx sut = "SUT";
			QueryEx output = new QueryEx(q => q.All().Marked("Output"));

			var sutRect = _app.Query(sut).Single().Rect;

			_app.DragCoordinates(sutRect.CenterX, sutRect.CenterY, sutRect.X + 10, sutRect.CenterY);

			var result = output.GetDependencyPropertyValue<string>("Text");

			Assert.IsTrue(string.IsNullOrWhiteSpace(result));

			_app.TapCoordinates(sutRect.Right - 10, sutRect.CenterY);

			result = output.GetDependencyPropertyValue<string>("Text");

			Assert.AreEqual("Right_3", result);
		}
	}
}
