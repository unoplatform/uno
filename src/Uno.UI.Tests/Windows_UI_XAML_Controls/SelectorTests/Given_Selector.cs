using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls.Primitives;

namespace Uno.UI.Tests.Windows_UI_XAML_Controls.SelectorTests
{
	[TestClass]
	public class Given_Selector
	{
		[TestMethod]
		public void When_Empty_SelectedValuePath()
		{
			var SUT = new Selector();
			SUT.ItemsSource = new[] { 1, 2, 3 };

			SUT.SelectedIndex = 0;
			Assert.AreEqual(1, SUT.SelectedValue);

			SUT.SelectedIndex = 1;
			Assert.AreEqual(2, SUT.SelectedValue);
		}

		[TestMethod]
		public void When_Single_SelectedValuePath()
		{
			var SUT = new Selector();
			SUT.ItemsSource = new[] { Tuple.Create("1", 1), Tuple.Create("2", 2), Tuple.Create("3", 3) };
			SUT.SelectedValuePath = "Item1";

			SUT.SelectedIndex = 0;
			Assert.AreEqual("1", SUT.SelectedValue);

			SUT.SelectedIndex = 1;
			Assert.AreEqual("2", SUT.SelectedValue);
		}

		[TestMethod]
		public void When_Single_SelectedValuePath_Changed()
		{
			var SUT = new Selector();
			var items = new[] { Tuple.Create("1", 1), Tuple.Create("2", 2), Tuple.Create("3", 3) };
			SUT.ItemsSource = items;

			SUT.SelectedIndex = 0;
			Assert.AreEqual(items[0], SUT.SelectedValue);

			SUT.SelectedValuePath = "Item1";

			Assert.AreEqual(items[0].Item1, SUT.SelectedValue);

			SUT.SelectedValuePath = "";

			Assert.AreEqual(items[0], SUT.SelectedValue);
		}

		[TestMethod]
		public void When_Invalid_SelectedValuePath()
		{
			var SUT = new Selector();
			var items = new[] { Tuple.Create("1", 1), Tuple.Create("2", 2), Tuple.Create("3", 3) };
			SUT.ItemsSource = items;

			SUT.SelectedIndex = 0;
			Assert.AreEqual(items[0], SUT.SelectedValue);

			SUT.SelectedValuePath = "Item42";
			Assert.IsNull(SUT.SelectedValue);

			SUT.SelectedValuePath = "Item2";
			Assert.AreEqual(1, SUT.SelectedValue);
		}

		[TestMethod]
		public void When_Null_SelectedValuePath()
		{
			var SUT = new Selector();
			var items = new[] { Tuple.Create("1", 1), Tuple.Create("2", 2), Tuple.Create("3", 3) };
			SUT.ItemsSource = items;

			SUT.SelectedIndex = 0;
			Assert.AreEqual(items[0], SUT.SelectedValue);

			SUT.SelectedValuePath = null;
			Assert.AreEqual(items[0], SUT.SelectedValue);

			SUT.SelectedValuePath = "Item2";
			Assert.AreEqual(1, SUT.SelectedValue);
		}

		[TestMethod]
		public void When_Double_SelectedValuePath()
		{
			var SUT = new Selector();

			SUT.ItemsSource = new[] {
				Tuple.Create("1", Tuple.Create("11", 1)),
				Tuple.Create("2", Tuple.Create("22", 2)),
				Tuple.Create("3", Tuple.Create("22", 3))
			};

			SUT.SelectedValuePath = "Item2.Item1";

			SUT.SelectedIndex = 0;
			Assert.AreEqual("11", SUT.SelectedValue);

			SUT.SelectedIndex = 1;
			Assert.AreEqual("22", SUT.SelectedValue);
		}
	}
}
