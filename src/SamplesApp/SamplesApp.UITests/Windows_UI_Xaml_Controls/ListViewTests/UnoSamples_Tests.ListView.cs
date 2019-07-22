using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.ListViewTests
{
	[TestFixture]
	public partial class ListViewTests_Tests : SampleControlUITestBase
	{
		[Test]
		public void RotatedListView_AddsToBottom()
		{
			Run("SamplesApp.Windows_UI_Xaml_Controls.ListView.RotatedListView_WithRotatedItems");

			_app.WaitForElement(_app.Marked("RotatedListView"));
			var listView = _app.Marked("RotatedListView");

			Assert.AreEqual("1", listView.GetDependencyPropertyValue("ItemsSource")?.ToString());

			var button = _app.Marked("theAddButton");
			_app.Tap(button);

			Assert.AreEqual("2", listView.GetDependencyPropertyValue("ItemsSource")?.ToString());
			Assert.AreNotEqual(listView.Sibling.AtIndex(1), listView.Sibling.AtIndex(0));
		}
	}
}
