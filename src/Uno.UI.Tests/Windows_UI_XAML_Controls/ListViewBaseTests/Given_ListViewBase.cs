using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Uno.UI.Tests.ListViewBaseTests
{
	[TestClass]
	public class Given_ListViewBase
	{
		[TestMethod]
		public void When_SingleSelectedItem()
		{
			var style = new Style(typeof(Windows.UI.Xaml.Controls.ListViewBase))
			{
				Setters =  {
					new Setter<ItemsControl>("Template", t =>
						t.Template = Funcs.Create(() =>
							new ItemsPresenter()
						)
					)
				}
			};

			var panel = new StackPanel();

			var SUT = new ListViewBase()
			{
				Style = style,
				ItemsPanel = new ItemsPanelTemplate(() => panel),
				Items = {
					new Border { Name = "b1" }
				}
			};

			// Search on the panel for now, as the name lookup is not properly
			// aligned on net46.
			Assert.IsNotNull(panel.FindName("b1"));

			SUT.SelectedIndex = 0;

			Assert.IsNotNull(SUT.SelectedItem);
			Assert.AreEqual(1, SUT.SelectedItems.Count);
		}

		[TestMethod]
		public void When_MultiSelectedItem()
		{
			var style = new Style(typeof(Windows.UI.Xaml.Controls.ListViewBase))
			{
				Setters =  {
					new Setter<ItemsControl>("Template", t =>
						t.Template = Funcs.Create(() =>
							new ItemsPresenter()
						)
					)
				}
			};

			var panel = new StackPanel();

			var SUT = new ListViewBase()
			{
				Style = style,
				ItemsPanel = new ItemsPanelTemplate(() => panel),
				Items = {
					new Border { Name = "b1" },
					new Border { Name = "b2" }
				}
			};

			// Search on the panel for now, as the name lookup is not properly
			// aligned on net46.
			Assert.IsNotNull(panel.FindName("b1"));
			Assert.IsNotNull(panel.FindName("b2"));

			SUT.SelectionMode = ListViewSelectionMode.Multiple;

			SUT.OnItemClicked(0);

			Assert.AreEqual(1, SUT.SelectedItems.Count);

			SUT.OnItemClicked(1);

			Assert.AreEqual(2, SUT.SelectedItems.Count);
		}

	}

	public class MyItemsControl : ItemsControl
	{
		public int OnItemsChangedCallCount { get; private set; }

		protected override void OnItemsChanged(object e)
		{
			OnItemsChangedCallCount++;
			base.OnItemsChanged(e);
		}
	}
}
