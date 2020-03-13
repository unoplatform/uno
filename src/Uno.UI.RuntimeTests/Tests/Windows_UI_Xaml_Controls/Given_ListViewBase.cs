using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_ListViewBase
	{
		[TestMethod]
		[RunsOnUIThread]
		public void ValidSelectionChange()
		{
			var source = Enumerable.Range(0, 10).ToArray();
			var list = new ListView { ItemsSource = source };
			list.SelectedItem = 3;
			Assert.AreEqual(list.SelectedItem, 3);
			list.SelectedItem = 5;
			Assert.AreEqual(list.SelectedItem, 5);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void InvalidSelectionChangeValidPrevious()
		{
			var source = Enumerable.Range(0, 10).ToArray();
			var list = new ListView { ItemsSource = source };
			list.SelectedItem = 3;
			Assert.AreEqual(list.SelectedItem, 3);
			list.SelectedItem = 17;
			Assert.AreEqual(list.SelectedItem, 3);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void InvalidSelectionChangeInvalidPrevious()
		{
			var source = Enumerable.Range(0, 10).ToArray();
			var list = new ListView { ItemsSource = source };
			list.SelectedItem = 3;
			Assert.AreEqual(list.SelectedItem, 3);
			source[3] = 13;
			list.SelectedItem = 17;
#if NETFX_CORE
			Assert.AreEqual(list.SelectedItem, 3);
#else
			Assert.IsNull(list.SelectedItem);
#endif
		}
	}
}
