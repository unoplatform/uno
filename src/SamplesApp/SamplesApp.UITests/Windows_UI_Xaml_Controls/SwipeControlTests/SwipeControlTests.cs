using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Input;
using NUnit.Framework;
using SamplesApp.UITests.Extensions;
using SamplesApp.UITests.TestFramework;
using Uno.Testing;
using Uno.UI.RuntimeTests;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.SwipeControlTests
{
	[TestFixture]
	public partial class SwipeControlTests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		[InjectedPointer(PointerDeviceType.Touch)]
		public async Task When_SingleItem()
		{
			await RunAsync("UITests.Windows_UI_Xaml_Controls.SwipeControlTests.SwipeControl_Automated");

			QueryEx sut = "SUT";
			QueryEx output = "Output";

			var sutRect = App.Query(sut).Single().Rect;

			App.DragCoordinates(sutRect.CenterX, sutRect.CenterY, sutRect.Right - 10, sutRect.CenterY - 20);

			await App.WaitForDependencyPropertyValueAsync(output, "Text", "** none **");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)] // ignore on android `Left_1` is sometimes ** none ** https://github.com/unoplatform/uno/issues/9080
		[InjectedPointer(PointerDeviceType.Touch)]
#if __SKIA__
		[Ignore("Invalid layout of items")]
#endif
		public async Task When_MultipleItems()
		{
			await RunAsync("UITests.Windows_UI_Xaml_Controls.SwipeControlTests.SwipeControl_Automated");

			QueryEx sut = "SUT";
			QueryEx output = new QueryEx(q => q.All().Marked("Output"));

			var sutRect = App.Query(sut).Single().Rect;

			App.DragCoordinates(sutRect.Right - 10, sutRect.CenterY, sutRect.X + 10, sutRect.CenterY);

			var result = output.GetDependencyPropertyValue<string>("Text");

			await Task.Delay(1000); // We cannot detect the animation ...

			Assert.AreEqual("** none **", result);

			App.TapCoordinates(sutRect.Right - 10, sutRect.CenterY);

			result = output.GetDependencyPropertyValue<string>("Text");

			Assert.AreEqual("Right_3", result);
		}


#if !__SKIA__ // No screenshot on skia
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public Task When_InListView()
			=> When_InScrollableContainer("UITests.Windows_UI_Xaml_Controls.SwipeControlTests.SwipeControl_ListView");

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
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
#endif

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)] // Swipe possible only with Touch in release
		[InjectedPointer(PointerDeviceType.Touch)]
		public async Task When_SwipeInListView_Then_TriggerOnlySwipeItem()
		{
			await RunAsync("UITests.Windows_UI_Xaml_Controls.SwipeControlTests.SwipeControl_ListView_ItemClick");

			QueryEx sut = "SUT";
			QueryEx clicked = "LastClicked";
			QueryEx selected = "LastSelected";
			QueryEx swiped = "LastSwipeInvoked";

			var sutRect = App.GetPhysicalRect(sut);

			App.DragCoordinates(sutRect.X + 100, sutRect.Y + 20, sutRect.Right - 20, sutRect.Y + 20);

			await Task.Delay(1000); // We cannot detect the animation ...

			Assert.AreEqual("** none **", clicked.GetDependencyPropertyValue<string>("Text"));
			Assert.AreEqual("** none **", selected.GetDependencyPropertyValue<string>("Text"));
			Assert.AreEqual("0.Left_1", swiped.GetDependencyPropertyValue<string>("Text"));
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)] // Swipe possible only with Touch in release
		[InjectedPointer(PointerDeviceType.Touch)]
		public async Task When_TapInListView_Then_TriggerClickAndSelection()
		{
			await RunAsync("UITests.Windows_UI_Xaml_Controls.SwipeControlTests.SwipeControl_ListView_ItemClick");

			QueryEx sut = "SUT";
			QueryEx clicked = "LastClicked";
			QueryEx selected = "LastSelected";
			QueryEx swiped = "LastSwipeInvoked";

			var sutRect = App.GetPhysicalRect(sut);

			App.TapCoordinates(sutRect.X + 100, sutRect.Y + 20);

			await Task.Delay(1000); // We cannot detect the animation ...

			Assert.AreEqual("0", clicked.GetDependencyPropertyValue<string>("Text"));
			Assert.AreEqual("0", selected.GetDependencyPropertyValue<string>("Text"));
			Assert.AreEqual("** none **", swiped.GetDependencyPropertyValue<string>("Text"));
		}
	}
}
