#if !NETFX_CORE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AwesomeAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Uno.UI.Helpers;

namespace Uno.UI.Tests.ListViewBaseTests
{
	[TestClass]
	public class Given_ListViewBase
	{
		[TestInitialize] public void Init() => UnitTestsApp.App.EnsureApplication();

		[TestMethod]
		public void When_MultiSelectedItem()
		{
			var panel = new StackPanel();

			var SUT = new ListViewBase()
			{
				Style = null,
				Template = new ControlTemplate(() => new ItemsPresenter()),
				ItemContainerStyle = BuildBasicContainerStyle(),
				ItemsPanel = new ItemsPanelTemplate(() => panel),
				Items = {
					new Border { Name = "b1" },
					new Border { Name = "b2" }
				}
			};

			SUT.ForceLoaded();

			// Search on the panel for now, as the name lookup is not properly
			// aligned on net46.
			Assert.IsNotNull(panel.FindName("b1"));
			Assert.IsNotNull(panel.FindName("b2"));

			SUT.SelectionMode = ListViewSelectionMode.Multiple;

			SUT.OnItemClicked(0, VirtualKeyModifiers.None);

			Assert.AreEqual(1, SUT.SelectedItems.Count);

			SUT.OnItemClicked(1, VirtualKeyModifiers.None);

			Assert.AreEqual(2, SUT.SelectedItems.Count);
		}

		[TestMethod]
		public void When_SingleSelectedItem_Event()
		{
			var panel = new StackPanel();

			var item = new Border { Name = "b1" };
			var SUT = new ListViewBase()
			{
				Style = null,
				Template = new ControlTemplate(() => new ItemsPresenter()),
				ItemsPanel = new ItemsPanelTemplate(() => panel),
				ItemContainerStyle = BuildBasicContainerStyle(),
				Items = {
					item
				}
			};

			var model = new MyModel
			{
				SelectedItem = (object)null
			};

			SUT.SetBinding(
				Selector.SelectedItemProperty,
				new Binding()
				{
					Path = "SelectedItem",
					Source = model,
					Mode = BindingMode.TwoWay
				}
			);

			SUT.ForceLoaded();

			// Search on the panel for now, as the name lookup is not properly
			// aligned on net46.
			Assert.IsNotNull(panel.FindName("b1"));

			var selectionChanged = 0;

			SUT.SelectionChanged += (s, e) =>
			{
				selectionChanged++;

				Assert.AreEqual(item, SUT.SelectedItem);
				Assert.AreEqual(item, model.SelectedItem);
			};

			SUT.SelectedIndex = 0;

			Assert.AreEqual(item, model.SelectedItem);

			Assert.IsNotNull(SUT.SelectedItem);
			Assert.AreEqual(1, selectionChanged);
			Assert.AreEqual(1, SUT.SelectedItems.Count);
		}

		[TestMethod]
		public void When_ResetItemsSource()
		{
			var panel = new StackPanel();

			var SUT = new ListView()
			{
				Style = null,
				Template = new ControlTemplate(() => new ItemsPresenter()),
				ItemContainerStyle = BuildBasicContainerStyle(),
				ItemsPanel = new ItemsPanelTemplate(() => panel),
				SelectionMode = ListViewSelectionMode.Single,
			};

			SUT.ItemsSource = new int[] { 1, 2, 3 };

			SUT.OnItemClicked(0, VirtualKeyModifiers.None);

			SUT.ItemsSource = null;
		}

		[TestMethod]
		public void When_SelectionChanged_Changes_Selection()
		{
			var list = new ListView()
			{
				Style = null,
				ItemContainerStyle = BuildBasicContainerStyle(),
			};
			list.ItemsSource = Enumerable.Range(0, 20);
			var callbackCount = 0;

			list.SelectionChanged += OnSelectionChanged;
			list.SelectedItem = 7;

			using (new AssertionScope())
			{
				list.SelectedItem.Should().Be(14);
				list.SelectedIndex.Should().Be(14);
				callbackCount.Should().Be(2);
			}

			void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
			{
				var l = sender as ListViewBase;
				l.SelectedItem = 14;

				if (callbackCount++ > 100)
				{
					throw new InvalidOperationException("callbackCount >100... clearly broken.");
				}
			}
		}

		[TestMethod]
		public void When_SelectionChanged_Changes_Selection_Repeated()
		{
			var list = new ListView()
			{
				Style = null,
				ItemContainerStyle = BuildBasicContainerStyle(),
			};
			list.ItemsSource = Enumerable.Range(0, 20);
			var callbackCount = 0;

			list.SelectionChanged += OnSelectionChanged;
			list.SelectedItem = 7;

			using (new AssertionScope())
			{
				list.SelectedItem.Should().Be(14);
				list.SelectedIndex.Should().Be(14);
				callbackCount.Should().Be(8); //Unlike eg TextBox.TextChanged there is no guard on reentrant modification
			}

			void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
			{
				callbackCount++;
				var l = sender as ListViewBase;
				var selected = (int)l.SelectedItem;
				if (selected < 14)
				{
					selected++;
					l.SelectedItem = selected;
				}

				if (callbackCount > 100)
				{
					throw new InvalidOperationException("callbackCount >100... clearly broken.");
				}
			}
		}

		[TestMethod]
		public void When_SelectionChanged_Changes_Order()
		{
			var list = new ListView()
			{
				Style = null,
				ItemContainerStyle = BuildBasicContainerStyle(),
			};
			list.ItemsSource = Enumerable.Range(0, 20);
			var callbackCount = 0;

			list.SelectionChanged += OnSelectionChanged;
			list.SelectedItem = 7;

			using (new AssertionScope())
			{
				list.SelectedItem.Should().Be(7);
				list.SelectedIndex.Should().Be(7);
				list.SelectedValue.Should().Be(7);
				callbackCount.Should().Be(1); //Unlike eg TextBox.TextChanged there is no guard on reentrant modification
			}

			void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
			{
				callbackCount++;

				using (new AssertionScope())
				{
					list.SelectedItem.Should().Be(7);
					list.SelectedIndex.Should().Be(7);
					list.SelectedValue.Should().Be(7);
					callbackCount.Should().Be(1);
				}
			}
		}

		[TestMethod]
		public void When_ViewModelSource_SelectionChanged_Changes_Selection()
		{
			var SUT = new ListView()
			{
				ItemsPanel = new ItemsPanelTemplate(() => new StackPanel()),
				ItemContainerStyle = BuildBasicContainerStyle(),
				Template = new ControlTemplate(() => new ItemsPresenter()),
				SelectionMode = ListViewSelectionMode.Single,
			};

			var source = Enumerable.Range(0, 20).Select(i => new MyViewModel { MyValue = i }).ToArray();
			SUT.ItemsSource = source;

			Assert.IsNull(SUT.SelectedItem);
			Assert.AreEqual(-1, SUT.SelectedIndex);

			SUT.ForceLoaded();

			var selectionChanged = new List<SelectionChangedEventArgs>();
			SUT.SelectionChanged += (s, e) => selectionChanged.Add(e);

			SUT.SelectedItem = source[1];

			Assert.AreEqual(1, SUT.SelectedIndex);
			Assert.AreEqual(1, selectionChanged.Count);

			if (SUT.ContainerFromIndex(1) is ListViewItem s1)
			{
				Assert.IsTrue(s1.IsSelected);
			}
			else
			{
				Assert.Fail("Container should be a ListViewItem");
			}
		}

		[TestMethod]
		public void When_Single_SelectionChanged_And_SelectorItem_IsSelected_Changed()
		{
			var SUT = new ListView()
			{
				Style = null,
				ItemsPanel = new ItemsPanelTemplate(() => new StackPanel()),
				ItemContainerStyle = BuildBasicContainerStyle(),
				Template = new ControlTemplate(() => new ItemsPresenter()),
				SelectionMode = ListViewSelectionMode.Single,
			};
			SUT.ForceLoaded();

			var selectionChanged = new List<SelectionChangedEventArgs>();

			SUT.SelectionChanged += (s, e) =>
			{
				selectionChanged.Add(e);
			};

			Assert.AreEqual(-1, SUT.SelectedIndex);

			var source = new[] {
				new ListViewItem(){ Content = "item 1" },
				new ListViewItem(){ Content = "item 2" },
				new ListViewItem(){ Content = "item 3" },
				new ListViewItem(){ Content = "item 4" },
			};

			SUT.ItemsSource = source;

			Assert.AreEqual(-1, SUT.SelectedIndex);
			Assert.AreEqual(0, selectionChanged.Count);

			SUT.SelectedItem = source[0];

			Assert.AreEqual(source[0], SUT.SelectedValue);
			Assert.AreEqual(1, selectionChanged.Count);
			Assert.AreEqual(source[0], selectionChanged[0].AddedItems[0]);
			Assert.AreEqual(0, selectionChanged[0].RemovedItems.Count);
			Assert.IsTrue(source[0].IsSelected);

			source[1].IsSelected = true;

			Assert.AreEqual(source[1], SUT.SelectedItem);
			Assert.AreEqual(2, selectionChanged.Count);
			Assert.AreEqual(source[1], selectionChanged.Last().AddedItems[0]);
			Assert.AreEqual(source[0], selectionChanged.Last().RemovedItems[0]);
			Assert.IsFalse(source[0].IsSelected);

			source[2].IsSelected = true;

			Assert.AreEqual(source[2], SUT.SelectedItem);
			Assert.AreEqual(3, selectionChanged.Count);
			Assert.AreEqual(source[2], selectionChanged.Last().AddedItems[0]);
			Assert.AreEqual(source[1], selectionChanged.Last().RemovedItems[0]);
			Assert.IsTrue(source[2].IsSelected);
			Assert.IsFalse(source[1].IsSelected);
			Assert.IsFalse(source[0].IsSelected);

			source[2].IsSelected = false;

			Assert.IsNull(SUT.SelectedItem);
			Assert.AreEqual(4, selectionChanged.Count);
			Assert.AreEqual(source[2], selectionChanged.Last().RemovedItems[0]);
			Assert.AreEqual(0, selectionChanged.Last().AddedItems.Count);
			Assert.IsFalse(source[0].IsSelected);
			Assert.IsFalse(source[1].IsSelected);
			Assert.IsFalse(source[2].IsSelected);
		}

		[TestMethod]
		public void When_Multi_SelectionChanged_And_SelectorItem_IsSelected_Changed()
		{
			var SUT = new ListView()
			{
				Style = null,
				ItemsPanel = new ItemsPanelTemplate(() => new StackPanel()),
				ItemContainerStyle = BuildBasicContainerStyle(),
				Template = new ControlTemplate(() => new ItemsPresenter()),
				SelectionMode = ListViewSelectionMode.Multiple,
			};

			SUT.ForceLoaded();

			var selectionChanged = new List<SelectionChangedEventArgs>();

			SUT.SelectionChanged += (s, e) =>
			{
				selectionChanged.Add(e);
			};

			Assert.AreEqual(-1, SUT.SelectedIndex);

			var source = new[] {
				new ListViewItem(){ Content = "item 1" },
				new ListViewItem(){ Content = "item 2" },
				new ListViewItem(){ Content = "item 3" },
				new ListViewItem(){ Content = "item 4" },
			};

			SUT.ItemsSource = source;

			Assert.AreEqual(-1, SUT.SelectedIndex);
			Assert.AreEqual(0, selectionChanged.Count);

			SUT.SelectedItem = source[0];

			Assert.IsNull(SUT.SelectedValue);
			Assert.AreEqual(1, selectionChanged.Count);
			Assert.AreEqual(source[0], selectionChanged[0].AddedItems[0]);
			Assert.AreEqual(0, selectionChanged[0].RemovedItems.Count);
			Assert.IsTrue(source[0].IsSelected);

			source[1].IsSelected = true;

			Assert.AreEqual(source[0], SUT.SelectedItem);
			Assert.AreEqual(2, selectionChanged.Count);
			Assert.AreEqual(source[1], selectionChanged.Last().AddedItems[0]);
			Assert.AreEqual(0, selectionChanged.Last().RemovedItems.Count);
			Assert.IsTrue(source[0].IsSelected);
			Assert.IsTrue(source[1].IsSelected);

			source[2].IsSelected = true;

			Assert.AreEqual(source[0], SUT.SelectedItem);
			Assert.AreEqual(3, selectionChanged.Count);
			Assert.AreEqual(source[2], selectionChanged.Last().AddedItems[0]);
			Assert.AreEqual(0, selectionChanged.Last().RemovedItems.Count);
			Assert.IsTrue(source[0].IsSelected);
			Assert.IsTrue(source[1].IsSelected);
			Assert.IsTrue(source[2].IsSelected);

			source[2].IsSelected = false;

			Assert.AreEqual(4, selectionChanged.Count);
			Assert.AreEqual(source[2], selectionChanged.Last().RemovedItems[0]);
			Assert.AreEqual(0, selectionChanged.Last().AddedItems.Count);
			Assert.IsTrue(source[0].IsSelected);
			Assert.IsTrue(source[1].IsSelected);
			Assert.IsFalse(source[2].IsSelected);
		}

		[TestMethod]
		public void When_Single_IsSelected_Changed_And_String_Items()
		{
			var SUT = new ListView()
			{
				Style = null,
				ItemsPanel = new ItemsPanelTemplate(() => new StackPanel()),
				ItemContainerStyle = BuildBasicContainerStyle(),
				Template = new ControlTemplate(() => new ItemsPresenter()),
				SelectionMode = ListViewSelectionMode.Single,
			};

			SUT.ForceLoaded();

			var selectionChanged = new List<SelectionChangedEventArgs>();

			SUT.SelectionChanged += (s, e) =>
			{
				selectionChanged.Add(e);
			};

			var source = Enumerable.Range(0, 10).Select(v => v.ToString()).ToArray();
			SUT.ItemsSource = source;

			Assert.IsNull(SUT.SelectedItem);

			SUT.SelectedItem = "1";

			Assert.AreEqual(1, selectionChanged.Count);

			if (SUT.ContainerFromIndex(2) is ListViewItem s1)
			{
				s1.IsSelected = true;
			}
			else
			{
				Assert.Fail("Container should be a ListViewItem");
			}

			Assert.AreEqual(1, SUT.SelectedItems.Count);
			Assert.AreEqual("2", SUT.SelectedItem);
			Assert.AreEqual(2, selectionChanged.Count);
		}

		[TestMethod]
		public void When_Multi_IsSelected_Changed_And_String_Items()
		{
			var SUT = new ListView()
			{
				ItemsPanel = new ItemsPanelTemplate(() => new StackPanel()),
				ItemContainerStyle = BuildBasicContainerStyle(),
				Template = new ControlTemplate(() => new ItemsPresenter()),
				SelectionMode = ListViewSelectionMode.Multiple,
			};

			SUT.ForceLoaded();

			var selectionChanged = new List<SelectionChangedEventArgs>();

			SUT.SelectionChanged += (s, e) =>
			{
				selectionChanged.Add(e);
			};

			var source = Enumerable.Range(0, 10).Select(v => v.ToString()).ToArray();
			SUT.ItemsSource = source;

			Assert.IsNull(SUT.SelectedItem);

			SUT.SelectedItem = "1";

			Assert.AreEqual(1, selectionChanged.Count);

			if (SUT.ContainerFromIndex(2) is ListViewItem s1)
			{
				s1.IsSelected = true;
			}
			else
			{
				Assert.Fail("Container should be a ListViewItem");
			}

			Assert.AreEqual(2, SUT.SelectedItems.Count);
			Assert.AreEqual("1", SUT.SelectedItem);
			Assert.AreEqual(2, selectionChanged.Count);

			if (SUT.ContainerFromIndex(3) is ListViewItem s2)
			{
				s2.IsSelected = true;
			}
			else
			{
				Assert.Fail("Container should be a ListViewItem");
			}

			Assert.AreEqual(3, SUT.SelectedItems.Count);
			Assert.AreEqual("1", SUT.SelectedItem);
			Assert.AreEqual(3, selectionChanged.Count);

			if (SUT.ContainerFromIndex(2) is ListViewItem s3)
			{
				s3.IsSelected = false;
			}
			else
			{
				Assert.Fail("Container should be a ListViewItem");
			}

			Assert.AreEqual(2, SUT.SelectedItems.Count);
		}

		[TestMethod]
		public void When_Multi_SelectionModeChanged()
		{
			var SUT = new ListView()
			{
				ItemsPanel = new ItemsPanelTemplate(() => new StackPanel()),
				ItemContainerStyle = BuildBasicContainerStyle(),
				Template = new ControlTemplate(() => new ItemsPresenter()),
				SelectionMode = ListViewSelectionMode.Multiple,
			};

			SUT.ForceLoaded();

			var source = Enumerable.Range(0, 10).Select(v => v.ToString()).ToArray();
			SUT.ItemsSource = source;

			Assert.IsNull(SUT.SelectedItem);

			if (SUT.ContainerFromIndex(2) is ListViewItem s1)
			{
				s1.IsSelected = true;
			}
			else
			{
				Assert.Fail("Container should be a ListViewItem");
			}

			if (SUT.ContainerFromIndex(3) is ListViewItem s2)
			{
				s2.IsSelected = true;
			}
			else
			{
				Assert.Fail("Container should be a ListViewItem");
			}

			Assert.AreEqual(2, SUT.SelectedItems.Count);

			SUT.SelectionMode = ListViewSelectionMode.None;

			Assert.AreEqual(0, SUT.SelectedItems.Count);
		}

		[TestMethod]
		public void When_ItemClick()
		{
			var SUT = new ListView()
			{
				ItemsPanel = new ItemsPanelTemplate(() => new StackPanel()),
				ItemContainerStyle = BuildBasicContainerStyle(),
				Template = new ControlTemplate(() => new ItemsPresenter()),
				SelectionMode = ListViewSelectionMode.Single,
			};

			SUT.ForceLoaded();

			var selectionChanged = new List<SelectionChangedEventArgs>();

			SUT.SelectionChanged += (s, e) =>
			{
				selectionChanged.Add(e);
			};

			Assert.AreEqual(-1, SUT.SelectedIndex);

			var source = new[] {
				"item 0",
				"item 1",
				"item 2",
				"item 3",
				"item 4"
			};

			SUT.ItemsSource = source;

			Assert.AreEqual(-1, SUT.SelectedIndex);

			if (SUT.ContainerFromItem(source[1]) is SelectorItem si)
			{
				SUT?.OnItemClicked(si, VirtualKeyModifiers.None);
			}

			Assert.AreEqual(source[1], SUT.SelectedValue);
			Assert.AreEqual(1, selectionChanged.Count);
			Assert.AreEqual(source[1], selectionChanged[0].AddedItems[0]);
			Assert.AreEqual(0, selectionChanged[0].RemovedItems.Count);
		}

		[TestMethod]
		public void When_ContainerSet_Then_ContentShouldBeSet()
		{
			var SUT = new ListView()
			{
				ItemsPanel = new ItemsPanelTemplate(() => new StackPanel()),
				ItemContainerStyle = BuildBasicContainerStyle(),
				ItemTemplate = new DataTemplate(() =>
				{
					var tb = new TextBlock();
					tb.SetBinding(TextBlock.TextProperty, new Binding());
					return tb;
				}),
				Template = new ControlTemplate(() => new ItemsPresenter()),
			};

			SUT.ForceLoaded();

			var source = new[] {
				"item 0",
			};

			SUT.ItemsSource = source;

			Assert.AreEqual(-1, SUT.SelectedIndex);

			var si = SUT.ContainerFromItem(source[0]) as SelectorItem;
			Assert.IsNotNull(si);

			var tb = si.FindFirstChild<TextBlock>();
			Assert.IsNotNull(tb);
			Assert.AreEqual("item 0", tb.Text);
		}

		[TestMethod]
		public void When_IsItsOwnItemContainer_FromSource()
		{
			var SUT = new ListView()
			{
				Style = null,
				ItemsPanel = new ItemsPanelTemplate(() => new StackPanel()),
				ItemContainerStyle = BuildBasicContainerStyle(),
				Template = new ControlTemplate(() => new ItemsPresenter()),
				SelectionMode = ListViewSelectionMode.Single,
			};

			SUT.ForceLoaded();

			var source = new[] {
				new ListViewItem(){ Content = "item 1" },
				new ListViewItem(){ Content = "item 2" },
				new ListViewItem(){ Content = "item 3" },
				new ListViewItem(){ Content = "item 4" },
			};

			SUT.ItemsSource = source;

			var si = SUT.ContainerFromItem(source[0]) as ListViewItem;
			Assert.IsNotNull(si);
			Assert.AreEqual("item 1", si.Content);
		}

		[TestMethod]
		public void When_NoItemTemplate()
		{
			var SUT = new ListView()
			{
				Style = null,
				ItemsPanel = XamlHelper.LoadXaml<ItemsPanelTemplate>("<ItemsPanelTemplate><StackPanel /></ItemsPanelTemplate>"),
				ItemContainerStyle = BuildBasicContainerStyle(),
				ItemTemplate = null,
				ItemTemplateSelector = null,
				Template = XamlHelper.LoadXaml<ControlTemplate>("<ControlTemplate><ItemsPresenter /></ControlTemplate>"),
			};
			SUT.ForceLoaded();

			var source = new[] {
				"Item 1"
			};
			SUT.ItemsSource = source;

			var si = SUT.ContainerFromItem(source[0]) as ListViewItem;
			Assert.IsNotNull(si);
			Assert.AreEqual("Item 1", si.Content);
			var cp = si.FindFirstChild<ContentPresenter>();
			Assert.IsInstanceOfType(cp.ContentTemplateRoot, typeof(ImplicitTextBlock));
			Assert.AreEqual("Item 1", (cp.ContentTemplateRoot as TextBlock).Text);
		}

		[TestMethod]
		public void When_IsItsOwnItemContainer_FromSource_With_DataTemplate()
		{
			var SUT = new ListView()
			{
				Style = null,
				ItemsPanel = new ItemsPanelTemplate(() => new StackPanel()),
				ItemContainerStyle = BuildBasicContainerStyle(),
				ItemTemplate = new DataTemplate(() =>
				{
					var tb = new TextBlock();
					tb.SetBinding(TextBlock.TextProperty, new Binding());
					return tb;
				}),
				Template = new ControlTemplate(() => new ItemsPresenter()),
				SelectionMode = ListViewSelectionMode.Single,
			};

			SUT.ForceLoaded();

			var source = new object[] {
				new ListViewItem(){ Content = "item 1" },
				"item 2"
			};

			SUT.ItemsSource = source;

			var si = SUT.ContainerFromItem(source[0]) as ListViewItem;
			Assert.IsNotNull(si);
			Assert.AreEqual("item 1", si.Content);
			Assert.AreSame(si, source[0]);
			Assert.IsFalse(si.IsGeneratedContainer);

			var si2 = SUT.ContainerFromItem(source[1]) as ListViewItem;
			Assert.IsNotNull(si2);
			Assert.AreNotSame(si, source[1]);
			Assert.AreEqual("item 2", si2.DataContext);
			Assert.AreEqual("item 2", si2.Content);
			Assert.IsTrue(si2.IsGeneratedContainer);
		}

		[TestMethod]
		public void When_ObservableVectorStringChanged()
		{
			var count = 0;
			var containerCount = 0;
			using var dispose1 = SelfCountingGrid.HookCtor(() => count++);
			using var dispose2 = SelfCountingGrid2.HookCtor(() => containerCount++);

			var panel = new StackPanel();
			panel.ForceLoaded();

			var source = new ObservableVector<string>() { "1", "2", "3" };

			var xmlnses = new Dictionary<string, string>
			{
				[string.Empty] = "http://schemas.microsoft.com/winfx/2006/xaml/presentation",
				["x"] = "http://schemas.microsoft.com/winfx/2006/xaml",
				["local"] = $"using:{typeof(SelfCountingGrid).Namespace}",
			};
			Style BuildContainerStyle() => XamlHelper.LoadXaml<Style>($$"""
				<Style TargetType="ListViewItem">
					<Setter Property="Template">
						<Setter.Value>
							<ControlTemplate>
								<local:SelfCountingGrid2>
									<ContentPresenter
										ContentTemplate="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=ContentTemplate}"
										Content="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=Content}"
										/>
								</local:SelfCountingGrid2>
							</ControlTemplate>
						</Setter.Value>
					</Setter>
				</Style>
			""", xmlnses);

			var SUT = new ListView()
			{
				ItemsPanelRoot = panel,
				InternalItemsPanelRoot = panel,
				ItemContainerStyle = BuildContainerStyle(),
				ItemTemplate = XamlHelper.LoadXaml<DataTemplate>("<DataTemplate><local:SelfCountingGrid /></DataTemplate>", xmlnses),
			};

			Assert.AreEqual(0, count);
			Assert.AreEqual(0, containerCount);
			Assert.AreEqual(0, containerCount);

			SUT.ItemsSource = source;
			Assert.AreEqual(FrameworkTemplatePool.IsPoolingEnabled ? 3 : 4, count);
			Assert.AreEqual(3, containerCount);
			Assert.AreEqual(3, containerCount);

			source.Add("4");
			Assert.AreEqual(FrameworkTemplatePool.IsPoolingEnabled ? 4 : 6, count);
			Assert.AreEqual(4, containerCount);
			Assert.AreEqual(4, containerCount);

			source.Remove("1");
			Assert.AreEqual(FrameworkTemplatePool.IsPoolingEnabled ? 4 : 6, count);
			Assert.AreEqual(4, containerCount);
			Assert.AreEqual(4, containerCount);

			source[0] = "5";
			// Data template is not recreated because of pooling
			Assert.AreEqual(FrameworkTemplatePool.IsPoolingEnabled ? 4 : 8, count);
			// The item container style is reapplied (not cached)
			Assert.AreEqual(5, containerCount);
		}

		private Style BuildBasicContainerStyle() => XamlHelper.LoadXaml<Style>("""
			<Style TargetType="ListViewItem">
				<Setter Property="Template">
					<Setter.Value>
						<ControlTemplate>
							<Grid>
								<ContentPresenter
									ContentTemplate="{Binding Path=ContentTemplate, RelativeSource={RelativeSource TemplatedParent}}"
									Content="{Binding Path=Content, RelativeSource={RelativeSource TemplatedParent}}"
									/>
							</Grid>
						</ControlTemplate>
					</Setter.Value>
				</Setter>
			</Style>
		""");
	}

	public class MyModel
	{
		public object SelectedItem { get; set; }
	}

	public class MyViewModel
	{
		public int MyValue { get; set; }
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

	public class SelfCountingGrid : Grid
	{
		public static int Count { get; private set; }
		public static Action CtorCallback = null;

		public SelfCountingGrid()
		{
			Count++;
			CtorCallback?.Invoke();
		}

		public static IDisposable RestartCounter() => HookCtor(null);
		public static IDisposable HookCtor(Action callback)
		{
			(Count, CtorCallback) = (0, callback);
			return new DisposableAction(() => (Count, CtorCallback) = (0, null));
		}
	}
	public class SelfCountingGrid2 : Grid
	{
		public static int Count { get; private set; }
		public static Action CtorCallback = null;

		public SelfCountingGrid2()
		{
			Count++;
			CtorCallback?.Invoke();
		}

		public static IDisposable RestartCounter() => HookCtor(null);
		public static IDisposable HookCtor(Action callback)
		{
			(Count, CtorCallback) = (0, callback);
			return new DisposableAction(() => (Count, CtorCallback) = (0, null));
		}
	}
}
#endif
