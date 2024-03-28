using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;

namespace Uno.UI.Tests.ItemsControlTests_CustomContainer
{
	[TestClass]
	public class Given_ListViewBase_CustomContainer
	{
		[TestMethod]
		public void When_TemplateRootIsOwnContainer()
		{
			var count = 0;
			var panel = new StackPanel();

			var SUT = new MyItemsControl()
			{
				ItemsPanelRoot = panel,
				ItemTemplate = new DataTemplate(() =>
				{
					count++;
					return new MyCustomItemContainer() { MyValue = 42 };
				}),
				Template = new ControlTemplate(() => new ItemsPresenter()),
			};

			SUT.ApplyTemplate();

			SUT.ItemsSource = new object[]
			{
				42
			};

			var container = SUT.ContainerFromIndex(0) as MyCustomItemContainer;

			Assert.IsNotNull(container);
			Assert.IsTrue(container.IsGeneratedContainer);
			Assert.IsFalse(container.ContentTemplateRoot is MyCustomItemContainer);
			Assert.AreEqual(42, container.DataContext);
		}

		[TestMethod]
		public void When_TemplateSelector_RootIsOwnContainer()
		{
			var count = 0;
			var panel = new StackPanel();

			var itemsPresenter = new MyItemsControl();

			var itemTemplate = new DataTemplate(() =>
			 {
				 count++;
				 return new MyCustomItemContainer() { MyValue = 42 };
			 });

			var SUT = new MyItemsControl()
			{
				ItemsPanelRoot = panel,
				ItemTemplateSelector = new MyDataTemplateSelector(i => itemTemplate),
				Template = new ControlTemplate(() => new ItemsPresenter()),
			};

			SUT.ApplyTemplate();

			SUT.ItemsSource = new object[]
			{
				42
			};

			var container = SUT.ContainerFromIndex(0) as MyCustomItemContainer;

			Assert.IsTrue(container is MyCustomItemContainer);
			Assert.IsTrue(container.IsGeneratedContainer);
			Assert.IsFalse(container.ContentTemplateRoot is MyCustomItemContainer);
			Assert.AreEqual(42, container.DataContext);
		}

		[TestMethod]
		public void When_ObservableCollection()
		{
			var count = 0;
			var panel = new StackPanel();

			var itemsPresenter = new MyItemsControl();

			var itemTemplate = new DataTemplate(() =>
			{
				count++;
				return new Border();
			});

			var SUT = new MyItemsControl()
			{
				ItemsPanelRoot = panel,
				ItemTemplate = itemTemplate,
				Template = new ControlTemplate(() => new ItemsPresenter()),
			};

			SUT.ApplyTemplate();

			Assert.AreEqual(0, count);

			var collection = new ObservableCollection<int>();
			SUT.ItemsSource = collection;

			Assert.AreEqual(0, count);

			collection.Add(1);

			Assert.AreEqual(FrameworkTemplatePool.IsPoolingEnabled ? 1 : 2, count);

			collection.Add(42);

			Assert.AreEqual(FrameworkTemplatePool.IsPoolingEnabled ? 2 : 4, count);

			collection.Add(43);

			Assert.AreEqual(FrameworkTemplatePool.IsPoolingEnabled ? 3 : 6, count);

			collection.RemoveAt(0);

			Assert.AreEqual(FrameworkTemplatePool.IsPoolingEnabled ? 3 : 6, count);

			collection[0] = 44;

			Assert.AreEqual(FeatureConfiguration.FrameworkTemplate.IsPoolingEnabled ? 3 : 8, count);
		}
	}

	public class MyDataTemplateSelector : DataTemplateSelector
	{
		private Func<object, DataTemplate> _selector;

		public MyDataTemplateSelector(Func<object, DataTemplate> selector) => _selector = selector;

		protected override DataTemplate SelectTemplateCore(object item) => _selector.Invoke(item);
	}


	public class MyItemsControl : ListView
	{
		protected override DependencyObject GetContainerForItemOverride()
			=> new MyCustomItemContainer();

		protected override bool IsItemItsOwnContainerOverride(object item)
			=> item is MyCustomItemContainer;
	}

	public class MyCustomItemContainer : SelectorItem
	{
		public int MyValue
		{
			get { return (int)GetValue(MyValueProperty); }
			set { SetValue(MyValueProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MyValue.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MyValueProperty =
			DependencyProperty.Register("MyValue", typeof(int), typeof(MyCustomItemContainer), new PropertyMetadata(0));


	}
}
