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
using Windows.UI.Xaml.Data;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Uno.UI.Tests.ItemsControlTests
{
#if !IS_UNIT_TESTS
	[RuntimeTests.RunsOnUIThread]
#endif
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

			SUT.ApplyTemplate();

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

#if IS_UNIT_TESTS
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

			//Assert.IsNull(SUT.ItemsPresenter);

			itemsPresenter.ForceLoaded();

			Assert.IsNotNull(SUT.ItemsPresenter);
		}
#endif

		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
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
		public void When_OnItemsSourceChanged_AfterRemove_ThenIndexesAreRecalculated()
		{
			void Operation(ObservableCollection<string> list)
			{
				list.RemoveAt(1);
			}

			CheckItemsSourceChanged(Operation);
		}

		[TestMethod]
		public void When_OnItemsSourceChanged_AfterInsert_ThenIndexesAreRecalculated()
		{
			void Operation(ObservableCollection<string> list)
			{
				list.Insert(2, "new item");
			}

			CheckItemsSourceChanged(Operation);
		}

		[TestMethod]
		public void When_OnItemsSourceChanged_AfterAdd_ThenIndexesAreRecalculated()
		{
			void Operation(ObservableCollection<string> list)
			{
				list.Add("new item at the end");
			}

			CheckItemsSourceChanged(Operation);
		}

		[TestMethod]
		public void When_OnItemsSourceChanged_AfterClear_ThenIndexesAreRecalculated()
		{
			void Operation(ObservableCollection<string> list)
			{
				list.Clear();
			}

			CheckItemsSourceChanged(Operation);
		}

		[TestMethod]
		public void When_OnItemsSourceChanged_AfterMove_ThenIndexesAreRecalculated()
		{
			void Operation(ObservableCollection<string> list)
			{
				list.Move(1, 3);
			}

			CheckItemsSourceChanged(Operation);
		}

		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public void When_OnItemsSourceChanged_AfterReplace_ThenIndexesAreRecalculated()
		{
			void Operation(ObservableCollection<string> list)
			{
				list[1] = "new index 1";
			}

			CheckItemsSourceChanged(Operation);
		}

		private static void CheckItemsSourceChanged(Action<ObservableCollection<string>> operation)
		{
			var source = new ObservableCollection<string>();

			source.Add("item 0");
			source.Add("item 1");
			source.Add("item 2");
			source.Add("item 3");
			source.Add("item 4");

			var panel = new StackPanel();
			var sut = new ItemsControl
			{
				ItemsPanelRoot = panel,
				InternalItemsPanelRoot = panel,
				ItemTemplate = new DataTemplate(() => { return new Border(); }),
				ItemsSource = source
			};

			using var _ = new AssertionScope();

			CheckIndexes();

			operation(source);

			CheckIndexes();

			void CheckIndexes()
			{
				panel.Children­.Should().HaveCount(source.Count);

				for (var i = 0; i < panel.Children.Count; i++)
				{
					var child = panel.Children[i];
					var index = child.GetValue(ItemsControl.IndexForItemContainerProperty);
					index.Should().Be(i);
				}
			}
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

#if IS_UNIT_TESTS
		[TestMethod]
		public void When_GroupedCollectionViewSource()
		{
			var count = 0;
			var panel = new StackPanel();

			var ungrouped = new[] { "Arg", "Aought", "Crab", "Crump", "Dart", "Fish", "Flash", "Fork", "Lion", "Louse", "Lemur" };
			var groups = ungrouped.GroupBy(s => s.First().ToString()).ToArray();
			var obsGroups = groups.Select(g => new Helpers.GroupedObservableCollection<string>(g)).ToArray();
			var source = new ObservableCollection<object>(obsGroups);

			var cvs = new CollectionViewSource
			{
				Source = source,
				IsSourceGrouped = true
			}.View;

			var SUT = new ItemsControl()
			{
				ItemsPanelRoot = panel,
				InternalItemsPanelRoot = panel,
				ItemTemplate = new DataTemplate(() =>
				{
					count++;
					return new Border();
				}),
			};

			SUT.ItemsSource = cvs;

			// This behavior is not the UWP one, this assertion
			// is present to freeze the behavior.
			Assert.AreEqual(0, count);
		}
#endif

		[TestMethod]
		public void When_ObservableVectorIntChanged()
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
			Assert.AreEqual(4, count);

			source.Remove(1);
			Assert.AreEqual(4, count);

			source[0] = 5;
			// Data template is not recreated because of pooling
			Assert.AreEqual(FrameworkTemplatePool.IsPoolingEnabled ? 4 : 5, count);
		}

		[TestMethod]
		public void When_ObservableVectorStringChanged()
		{
			var count = 0;
			var panel = new StackPanel();

			var source = new ObservableVector<string>() { "1", "2", "3" };

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

			source.Add("4");
			Assert.AreEqual(4, count);

			source.Remove("1");
			Assert.AreEqual(4, count);

			source[0] = "5";
			// Data template is not recreated because of pooling
			Assert.AreEqual(FrameworkTemplatePool.IsPoolingEnabled ? 4 : 5, count);
		}

		[TestMethod]
		public void When_ObservableCollectionChanged()
		{
			var count = 0;
			var panel = new StackPanel();

			var source = new ObservableCollection<int>() { 1, 2, 3 };

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
			Assert.AreEqual(3, SUT.Items.Count);
			Assert.AreEqual(3, SUT.ItemsPanelRoot.Children.Count);

			source.Add(4);
			Assert.AreEqual(4, count);
			Assert.AreEqual(4, SUT.Items.Count);
			Assert.AreEqual(4, SUT.ItemsPanelRoot.Children.Count);

			source.Remove(1);
			Assert.AreEqual(4, count);
			Assert.AreEqual(3, SUT.Items.Count);
			Assert.AreEqual(3, SUT.ItemsPanelRoot.Children.Count);

			source[0] = 5;
			// Data template is not recreated because of pooling
			Assert.AreEqual(FrameworkTemplatePool.IsPoolingEnabled ? 4 : 5, count);
			Assert.AreEqual(3, SUT.Items.Count);
			Assert.AreEqual(3, SUT.ItemsPanelRoot.Children.Count);
		}

		[TestMethod]
		public void When_ContainerStyleSet()
		{
			var count = 0;
			var panel = new StackPanel();

			var source = new ObservableVector<int>() { 1, 2, 3 };

			var SUT = new ItemsControl()
			{
				ItemsPanelRoot = panel,
				ItemContainerStyle = BuildBasicContainerStyle(),
				InternalItemsPanelRoot = panel,
				ItemTemplate = new DataTemplate(() =>
				{
					count++;
					return new Border();
				})
			};

			SUT.ApplyTemplate();

			Assert.AreEqual(0, count);

			SUT.ItemsSource = source;
			Assert.AreEqual(3, count);

			source.Add(4);
			Assert.AreEqual(4, count);

			source.Remove(1);
			Assert.AreEqual(4, count);
		}

		[TestMethod]
		public void When_ItemsSource_Changes_Items_VectorChanged_Triggered()
		{
			var listView = new ItemsControl();

			var triggerCount = 0;
			listView.Items.VectorChanged += (s, e) =>
			{
				triggerCount++;
			};

			listView.ItemsSource = new List<int>() { 1, 2 };

			Assert.AreEqual(1, triggerCount);
			Assert.AreEqual(2, listView.Items.Count);

			listView.ItemsSource = new List<int>() { 3, 4 };

			Assert.AreEqual(2, triggerCount);
			Assert.AreEqual(2, listView.Items.Count);

			listView.ItemsSource = null;

			Assert.AreEqual(3, triggerCount);
			Assert.AreEqual(0, listView.Items.Count);
		}

#if IS_UNIT_TESTS
		[TestMethod]
		public void When_Collection_Reset()
		{
			var count = 0;
			var panel = new StackPanel();

			var SUT = new ItemsControl()
			{
				ItemsPanelRoot = panel,
				ItemContainerStyle = BuildBasicContainerStyle(),
				InternalItemsPanelRoot = panel,
				ItemTemplate = new DataTemplate(() =>
				{
					count++;
					return new Border();
				})
			};
			SUT.ApplyTemplate();

			var c = new Helpers.ObservableCollectionEx<string>();
			c.Add("One");
			c.Add("Two");
			c.Add("Three");

			SUT.ItemsSource = c;
			Assert.AreEqual(count, 3);

			Assert.AreEqual(SUT.Items.Count, 3);

			using (c.BatchUpdate())
			{
				c.Add("Four");
				c.Add("Five");
			}

			Assert.AreEqual(SUT.Items.Count, 5);
			Assert.AreEqual(count, FrameworkTemplatePool.IsPoolingEnabled ? 5 : 8);
			Assert.IsNotNull(SUT.ContainerFromItem("One"));
			Assert.IsNotNull(SUT.ContainerFromItem("Four"));
			Assert.IsNotNull(SUT.ContainerFromItem("Five"));
		}
#endif

		[TestMethod]
		public void When_Collection_Append()
		{
			var count = 0;
			var panel = new StackPanel();

			var SUT = new ItemsControl()
			{
				ItemsPanelRoot = panel,
				ItemContainerStyle = BuildBasicContainerStyle(),
				InternalItemsPanelRoot = panel,
				ItemTemplate = new DataTemplate(() =>
				{
					count++;
					return new Border();
				})
			};
			SUT.ApplyTemplate();

			var c = new ObservableCollection<string>();

			SUT.ItemsSource = c;

			c.Add("One");
			Assert.AreEqual(count, 1);

			c.Add("Two");
			Assert.AreEqual(count, 2);

			c.Add("Three");
			Assert.AreEqual(count, 3);

			Assert.AreEqual(SUT.Items.Count, 3);

			c.Add("Four");

			Assert.AreEqual(SUT.Items.Count, 4);
			Assert.AreEqual(count, 4);
		}

		private Style BuildBasicContainerStyle() =>
			new Style(typeof(Windows.UI.Xaml.Controls.ListViewItem))
			{
				Setters =  {
					new Setter<ListViewItem>("Template", t =>
						t.Template = Funcs.Create(() =>
							new Grid
							{
								Children = {
									new ContentPresenter()
										.Apply(p => {
											p.SetBinding(ContentPresenter.ContentTemplateProperty, new Binding(){ Path = "ContentTemplate", RelativeSource = RelativeSource.TemplatedParent });
											p.SetBinding(ContentPresenter.ContentProperty, new Binding(){ Path = "Content", RelativeSource = RelativeSource.TemplatedParent });
										})
								}
							}
						)
					)
				}
			};

	}

	public partial class MyItemsControl : ItemsControl
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
