using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Uno.UI.Tests.ItemsControlTests
{
	[TestClass]
	public class Given_ItemsControl
	{
		[TestMethod]
		public void When_EarlyItems()
		{
			var style = new Style(typeof(Windows.UI.Xaml.Controls.ItemsControl))
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

			var SUT = new ItemsControl()
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
		}

		[TestMethod]
		public void When_ItemsChanged()
		{
			var SUT = new MyItemsControl();

			int onVectorChanged = 0;

			SUT.Items.VectorChanged += (s, e) => onVectorChanged++;

			Assert.AreEqual(0, SUT.OnItemsChangedCallCount);
			Assert.AreEqual(0, onVectorChanged);

			SUT.Items.Add(new Border());

			Assert.AreEqual(1, SUT.OnItemsChangedCallCount);
			Assert.AreEqual(1, onVectorChanged);
			Assert.IsNotNull(SUT.ItemsChangedArgs as IVectorChangedEventArgs);

			SUT.Items.RemoveAt(0);

			Assert.AreEqual(2, SUT.OnItemsChangedCallCount);
			Assert.AreEqual(2, onVectorChanged);
			Assert.AreEqual(0, SUT.Items.Count);
		}

		[TestMethod]
		public void When_TemplatedParent_Before_Loading()
		{
			var itemsPresenter = new ItemsPresenter();

			var style = new Style(typeof(ItemsControl))
			{
				Setters =  {
					new Setter<ItemsControl>("Template", t =>
						t.Template = Funcs.Create(() => itemsPresenter)
					)
				}
			};

			var panel = new StackPanel();

			var SUT = new ItemsControl()
			{
				ItemsPanel = new ItemsPanelTemplate(() => panel),
				Items = {
					new Border { Name = "b1" }
				},
				Style = style
			};

			Assert.IsNull(SUT.ItemsPresenter);

			itemsPresenter.ForceLoaded();

			Assert.IsNotNull(SUT.ItemsPresenter);
		}

		[TestMethod]
		public void When_OnItemsSourceChanged()
		{
			var count = 0;
			var panel = new StackPanel();

			var SUT = new ItemsControl()
			{
				ItemsPanelRoot = panel,
				InternalItemsPanelRoot = panel,
				ItemTemplate = new DataTemplate(() =>
				{
					count++;
					return new Border();
				}),
				ItemsSource = new[]
				{
					"Item1"
				}
			};
			
			Assert.AreEqual(1, count);
		}

		[TestMethod]
		public void When_CollectionViewSource()
		{
			var count = 0;
			var panel = new StackPanel();

			var cvs = new CollectionViewSource();

			var SUT = new ItemsControl()
			{
				ItemsPanelRoot = panel,
				InternalItemsPanelRoot = panel,
				ItemTemplate = new DataTemplate(() =>
				{
					count++;
					return new Border();
				}),
				ItemsSource = cvs
			};

			Assert.AreEqual(0, count);

			cvs.Source = new[] { 42 };

			Assert.AreEqual(1, count);
		}

		[TestMethod]
		public void When_ObservableVectorChanged()
		{
			var count = 0;
			var panel = new StackPanel();

			var source = new ObservableVector<int>() { 1, 2, 3 };

			var SUT = new ItemsControl()
			{
				ItemsPanelRoot = panel,
				InternalItemsPanelRoot = panel,
				ItemTemplate = new DataTemplate(() =>
				{
					count++;
					return new Border();
				})
			};

			Assert.AreEqual(0, count);

			SUT.ItemsSource = source;
			Assert.AreEqual(3, count);

			source.Add(4);
			Assert.AreEqual(7, count);

			source.Remove(1);
			Assert.AreEqual(13, count);
		}
	}

	public class MyItemsControl : ItemsControl
	{
		public int OnItemsChangedCallCount { get; private set; }
		public object ItemsChangedArgs { get; private set; }

		protected override void OnItemsChanged(object e)
		{
			OnItemsChangedCallCount++;
			ItemsChangedArgs = e;
			base.OnItemsChanged(e);
		}
	}
}
