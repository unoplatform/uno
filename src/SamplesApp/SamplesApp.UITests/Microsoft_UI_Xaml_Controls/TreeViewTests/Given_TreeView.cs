using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Query = System.Func<Uno.UITest.IAppQuery, Uno.UITest.IAppQuery>;

namespace SamplesApp.UITests.Microsoft_UI_Xaml_Controls.TreeViewTests
{
	public class Given_TreeView : SampleControlUITestBase
	{
		[SetUp]
		public void TestSetup()
		{
			Run("UITests.Shared.Microsoft_UI_Xaml_Controls.TreeViewTests.TreeViewPage");
		}

		private void ExpandCollapseTest(bool isContentMode = false)
		{
			{
				SetContentMode(isContentMode);

				var itemRoot = LabelFirstItem();

				ClickButton("GetItemCount");
				Assert.AreEqual("1", ReadResult());

				itemRoot.Tap();

				// Should be expanded now
				ClickButton("GetItemCount");
				Assert.AreEqual("4", ReadResult());

				ClickButton("AddSecondLevelOfNodes");
				ClickButton("LabelItems");

				var root0 = _app.Marked("Root.0");
				root0.Tap();
				ClickButton("GetItemCount");
				Assert.AreEqual("5", ReadResult());

				itemRoot.Tap();
				ClickButton("GetItemCount");
				Assert.AreEqual("1", ReadResult());

				itemRoot.Tap();
				ClickButton("GetItemCount");
				Assert.AreEqual("5", ReadResult());


				// TODO: Uno specific! needed to ensure automation ids
				// are assigned to correct item containers
				ClickButton("LabelItems"); 


				var root1 = _app.Marked("Root.1");
				root1.Tap();
				ClickButton("GetItemCount");
				Assert.AreEqual("8", ReadResult());

				// Collapse and expand
				itemRoot.Tap();
				itemRoot.Tap();
				ClickButton("GetItemCount");
				Assert.AreEqual("8", ReadResult());
			}
		}

		[Test]
		[AutoRetry]
		public void ExpandCollapseTest_NodeMode()
		{
			ExpandCollapseTest();
		}

		[Test]
		[AutoRetry]
		public void ExpandCollapseTest_ContentMode()
		{
			ExpandCollapseTest(isContentMode: true);
		}

		private void SetContentMode(bool isContentMode)
		{
			if (isContentMode)
			{
				ClickButton("SetContentMode");
			}
		}

		private void ClickButton(string buttonName)
		{
			var button = _app.Marked(buttonName);
			button.Tap();
			// Wait.ForIdle();
		}

		private string ReadResult()
		{
			var textBlock = _app.Marked("Results");
			return textBlock.GetText();
		}

		private QueryEx LabelFirstItem()
		{
			ClickButton("LabelItems");
			Console.WriteLine("Retrieve first item as generic UIElement");
			var ItemRoot = _app.Marked("Root");
			Assert.IsNotNull(ItemRoot, "Verifying that we found a UIElement called Root");
			return ItemRoot;
		}
	}
}
