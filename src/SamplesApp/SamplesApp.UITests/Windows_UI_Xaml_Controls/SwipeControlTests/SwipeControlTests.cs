using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.Extensions;
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

			_app.DragCoordinates(sutRect.CenterX, sutRect.CenterY, sutRect.Right - 10, sutRect.CenterY - 20);

			_app.WaitForDependencyPropertyValue(output, "Text", "Left_1");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS | Platform.Android)]
		public async Task When_MultipleItems()
		{
			Run("UITests.Windows_UI_Xaml_Controls.SwipeControlTests.SwipeControl_Automated");

			QueryEx sut = "SUT";
			QueryEx output = new QueryEx(q => q.All().Marked("Output"));

			var sutRect = _app.Query(sut).Single().Rect;

			_app.DragCoordinates(sutRect.Right - 10, sutRect.CenterY, sutRect.X + 10, sutRect.CenterY);

			var result = output.GetDependencyPropertyValue<string>("Text");

			await Task.Delay(1000); // We cannot detect the animation ...

			Assert.AreEqual("** none **", result);

			_app.TapCoordinates(sutRect.Right - 10, sutRect.CenterY);

			result = output.GetDependencyPropertyValue<string>("Text");

			Assert.AreEqual("Right_3", result);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS | Platform.Android)]
		public Task When_InListView()
			=> When_InScrollableContainer("UITests.Windows_UI_Xaml_Controls.SwipeControlTests.SwipeControl_ListView");

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS | Platform.Android)]
		public Task When_InScrollViewer()
			=> When_InScrollableContainer("UITests.Windows_UI_Xaml_Controls.SwipeControlTests.SwipeControl_ScrollViewer");

		private async Task When_InScrollableContainer(string testName)
		{
			QueryEx sut = new QueryEx(q => q.All().Marked("SUT"));
			QueryEx output = new QueryEx(q => q.All().Marked("Output"));

			Run(testName, skipInitialScreenshot: true);

			var sutPhyRect = _app.GetPhysicalRect(sut);
			var item2PhyPosition = new Point((int)sutPhyRect.X + 150, (int)sutPhyRect.Y + 150).LogicalToPhysicalPixels(_app);

			// Validate initial state
			var initial = TakeScreenshot("initial");
			ImageAssert.HasColorAt(initial, item2PhyPosition.X, item2PhyPosition.Y, "#FFFFA52C");

			// Execute left command on item 2
			_app.DragCoordinates(item2PhyPosition.X, item2PhyPosition.Y, item2PhyPosition.X + 300.LogicalToPhysicalPixels(_app), item2PhyPosition.Y);
			await Task.Delay(1000); // We cannot detect the animation ...

			var swippedItem = output.GetDependencyPropertyValue<string>("Text");
			Assert.AreEqual("#FFFFA52C", swippedItem);

			// Scroll up
			_app.DragCoordinates(sutPhyRect.CenterX, sutPhyRect.Bottom - 10.LogicalToPhysicalPixels(_app), sutPhyRect.CenterX, sutPhyRect.Y + 10.LogicalToPhysicalPixels(_app));

			// Validate scrolled successfully
			var postScroll = TakeScreenshot("after scroll");
			ImageAssert.DoesNotHaveColorAt(postScroll, item2PhyPosition.X, item2PhyPosition.Y, "#FFFFA52C");

			// Execute left command on item that is now at item 2 location
			_app.DragCoordinates(item2PhyPosition.X, item2PhyPosition.Y, item2PhyPosition.X + 300.LogicalToPhysicalPixels(_app), item2PhyPosition.Y);
			await Task.Delay(1000); // We cannot detect the animation ...

			swippedItem = output.GetDependencyPropertyValue<string>("Text");
			Assert.AreNotEqual("#FFFFA52C", swippedItem);
		}
	}
}
