#if IS_RUNTIME_UI_TESTS
#define CAN_ASSERT_CONTENT
#if HAS_RENDER_TARGET_BITMAP
#define CAN_ASSERT_IMAGE
#endif
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
#else
#define CAN_ASSERT_IMAGE
#endif

#if !CAN_ASSERT_CONTENT && !CAN_ASSERT_IMAGE
#error No way to assert the test result
#endif

using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Input;
using SamplesApp.UITests.Extensions;
using Uno.Testing;

namespace SamplesApp.UITests.Windows_UI_Xaml.DragAndDropTests
{
	[TestFixture]
	public partial class DragDrop_TreeView_Reorder_Automated : SampleControlUITestBase
	{
		const int _itemHeight = 70;
		const int _offset = 30;
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)]
		[InjectedPointer(PointerDeviceType.Mouse)]
#if !IS_RUNTIME_UI_TESTS
		[Test]
		[Ignore("Flaky")]
#else
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaUIKit)] // https://github.com/unoplatform/uno-private/issues/809
#endif
		public async Task When_Dragging_TreeView_Item()
		{
			await RunAsync("UITests.Windows_UI_Xaml.DragAndDrop.DragDrop_TreeView", skipInitialScreenshot: true);

			var tv = App.Marked("tv");
			var bt0 = App.Marked("bt0");
			var bt1 = App.Marked("bt1");
			var bt2 = App.Marked("bt2");
			var bt3 = App.Marked("bt3");
			var focusbt = App.Marked("focusbt");
			var radio_disable = App.Marked("radio_disable");

			App.WaitForElement(radio_disable);
			App.Tap(radio_disable);

			App.WaitForElement(tv);

#if CAN_ASSERT_IMAGE
			App.Tap(bt1);
			App.Tap(focusbt);
			var case1 = await Screenshot("case1");

			App.Tap(bt2);
			App.Tap(focusbt);
			var case2 = await Screenshot("ase2");

			App.Tap(bt3);
			App.Tap(focusbt);
			var case3 = await Screenshot("case3");

			App.Tap(bt0);
#endif
#if CAN_ASSERT_CONTENT
			App.Tap(bt0);
			await UITestHelper.WaitForIdle();
			await UITestHelper.WaitForIdle();
			await UITestHelper.WaitForIdle();

			var treeView = (TreeView)App.Query(tv).Single().Element;
			Assert.IsNotNull(treeView);
			Assert.AreEqual(
				""""
				v aaa
					- bbb
				- ccc
				- ddd
				- eee
				"""".Trim().Replace("\r\n", "\n"),
				Dump(treeView));
#endif

			var tvBounds = App.Query("tv").Single().Rect;
			var fromX = tvBounds.X + 100;
			var fromY = tvBounds.Y + _itemHeight * 3 + _offset;
			var toX = tvBounds.X + 100;
			var toY = tvBounds.Y + _offset;

			await App.DragCoordinatesAsync(fromX, fromY, toX, toY);
			App.Tap(focusbt);

#if CAN_ASSERT_IMAGE
			var result = await Screenshot("result1");
			await ImageAssert.AreEqualAsync(case1, result);
#endif
#if CAN_ASSERT_CONTENT
			await UITestHelper.WaitForIdle();
			Assert.AreEqual(
				""""
				v aaa
					- bbb
					- ddd
				- ccc
				- eee
				"""".Trim().Replace("\r\n", "\n"),
				Dump(treeView));
#endif

			fromY = tvBounds.Y + _itemHeight * 1 + _offset;
			toY = tvBounds.Y + _itemHeight * 4 + _offset;
			await App.DragCoordinatesAsync(fromX, fromY, toX, toY);
			App.Tap(focusbt);

#if CAN_ASSERT_IMAGE
			result = await Screenshot("result2");
			await ImageAssert.AreEqualAsync(case2, result);
#endif
#if CAN_ASSERT_CONTENT
			await UITestHelper.WaitForIdle();
			Assert.AreEqual(
				""""
				v aaa
					- ddd
				- ccc
				> eee
				"""".Trim().Replace("\r\n", "\n"),
				Dump(treeView));
#endif

			fromY = tvBounds.Y + _itemHeight * 3 + _offset;
			toY = tvBounds.Y + _itemHeight - 5;
			await App.DragCoordinatesAsync(fromX, fromY, toX, toY);
			App.Tap(focusbt);

#if CAN_ASSERT_IMAGE
			result = await Screenshot("result3");
			await ImageAssert.AreEqualAsync(case3, result);
#endif
#if CAN_ASSERT_CONTENT
			await UITestHelper.WaitForIdle();
			Assert.AreEqual(
				""""
				v aaa
					> eee
					- ddd
				- ccc
				"""".Trim().Replace("\r\n", "\n"),
				Dump(treeView));
#endif

#if CAN_ASSERT_IMAGE
#if IS_RUNTIME_UI_TESTS
			async ValueTask<RawBitmap> Screenshot(string name) => await UITestHelper.ScreenShot((TreeView)App.Query(tv).Single().Element);
#else
			async ValueTask<FileInfo> Screenshot(string name) => await App.TakeScreenshotAsync(name);
#endif
#endif
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)]
		[InjectedPointer(PointerDeviceType.Mouse)]
		public async Task When_OnDragItemsCompleted()
		{
			await RunAsync("UITests.Windows_UI_Xaml.DragAndDrop.DragDrop_TreeView", skipInitialScreenshot: true);
			var tv = App.Marked("tv");
			var bt0 = App.Marked("bt0");
			var radio_enable = App.Marked("radio_enable");
			var tb = App.Marked("tb");

			App.WaitForElement(tv);

			App.WaitForElement(radio_enable);
			App.Tap(radio_enable);
			App.WaitForElement(bt0);
			App.Tap(bt0);

#if IS_RUNTIME_UI_TESTS
			await UITestHelper.WaitForIdle();
			await UITestHelper.WaitForIdle();
			await UITestHelper.WaitForIdle();
#endif

			var tvBounds = App.Query("tv").Single().Rect;
			var fromX = tvBounds.X + 100;
			var fromY = tvBounds.Y + _itemHeight * 3 + _offset;
			var toX = tvBounds.X + 100;
			var toY = tvBounds.Y + _offset;
			await App.DragCoordinatesAsync(fromX, fromY, toX, toY);

#if IS_RUNTIME_UI_TESTS
			await UITestHelper.WaitForIdle();
			await UITestHelper.WaitForIdle();
			await UITestHelper.WaitForIdle();
#endif

			string text = App.GetText("tb").Trim();
			Assert.AreEqual("DragItemsCompleted is triggered", text);
		}

#if CAN_ASSERT_CONTENT
		private string Dump(TreeView tv)
		{
			var dump = new StringBuilder();
			DumpCore(0, tv.RootNodes);
			return dump.ToString().Trim().Replace("\r\n", "\n");

			void DumpCore(int level, IList<TreeViewNode> nodes)
			{
				foreach (var node in nodes)
				{
					dump.Append(new string('\t', level));
					switch (node)
					{
						case { Children: null or { Count: 0 } }:
							dump.Append("- ");
							dump.AppendLine(node.Content?.ToString() ?? "--null--");
							break;

						case { IsExpanded: false }:
							dump.Append("> ");
							dump.AppendLine(node.Content?.ToString() ?? "--null--");
							break;

						default:
							dump.Append("v ");
							dump.AppendLine(node.Content?.ToString() ?? "--null--");
							DumpCore(level + 1, node.Children);
							break;
					}
				}
			}
		}
#endif
	}
}
