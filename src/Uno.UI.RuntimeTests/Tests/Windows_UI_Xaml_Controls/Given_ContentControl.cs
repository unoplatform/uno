using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Private.Infrastructure;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Windows_UI_Xaml_Controls;
using static Private.Infrastructure.TestServices;
#if WINAPPSDK
using Uno.UI.Extensions;
#elif __APPLE_UIKIT__
using UIKit;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_ContentControl
	{
		private ResourceDictionary _testsResources;

		public DataTemplate DataContextBindingDataTemplate => _testsResources["DataContextBindingDataTemplate"] as DataTemplate;
		public DataTemplate ContentControlComboTemplate => _testsResources["ContentControlComboTemplate"] as DataTemplate;

		private DataTemplate SelectableItemTemplateA => _testsResources["SelectableItemTemplateA"] as DataTemplate;
		private DataTemplate SelectableItemTemplateB => _testsResources["SelectableItemTemplateB"] as DataTemplate;
		private DataTemplate SelectableItemTemplateC => _testsResources["SelectableItemTemplateC"] as DataTemplate;

		// Commented types don't seem to use ContentTemplateSelector on UWP
		private static readonly Type[] _contentControlStyledDerivedTypes = new[]
		{
			typeof(Button),
			//typeof(AppBarButton),
			//typeof(AppBar),
			//typeof(CommandBar),
			//typeof(SplitButton),
			typeof(RepeatButton),
			typeof(ToggleButton),
			//typeof(AppBarToggleButton),
			typeof(CheckBox),
			typeof(RadioButton),
			//typeof(SplitButton),
			//typeof(ToggleSplitButton),
			typeof(DropDownButton)
		};

		public IEnumerable<ContentControl> DerivedStyledControlsInstances => _contentControlStyledDerivedTypes.Select(t => Activator.CreateInstance(t) as ContentControl);

		[TestInitialize]
		public void Init()
		{
			_testsResources = new TestsResources();
		}

		[TestMethod]
		[RunsOnUIThread]
		[DataRow(typeof(Grid))]
		[DataRow(typeof(StackPanel))]
		[DataRow(typeof(Border))]
		[DataRow(typeof(ContentPresenter))]
		public async Task When_SelfLoading(Type type)
		{
			var control = (FrameworkElement)Activator.CreateInstance(type);

			control.Width = 200;
			control.Height = 200;

			await UITestHelper.Load(control);
		}

		[TestMethod]
		public async Task When_Binding_Within_Control_Template()
		{
			var contentControl = new ContentControl
			{
				ContentTemplate = DataContextBindingDataTemplate,
			}.Apply(cc => cc.SetBinding(ContentControl.ContentProperty, new Binding()));

			var grid = new Grid()
			{
				DataContext = new ViewModel(),
			};

			grid.Children.Add(contentControl);
			TestServices.WindowHelper.WindowContent = grid;

			await TestServices.WindowHelper.WaitForLoaded(grid);

			var tb = grid.FindFirstChild<TextBlock>();

			Assert.IsNotNull(tb);

			await TestServices.WindowHelper.WaitFor(() => tb.Text == "Steve");
		}

		[TestMethod]
		public async Task When_ContentTemplateSelector_And_Default_Style()
		{
			var items = new[] { "item 1", "item 2", "item 3" };
			foreach (var control in DerivedStyledControlsInstances)
			{
				var templateSelector = new Given_ListViewBase.KeyedTemplateSelector
				{
					Templates =
					{
						{ items[0], SelectableItemTemplateA },
						{ items[1], SelectableItemTemplateB },
						{ items[2], SelectableItemTemplateC },
					}
				};

				control.ContentTemplateSelector = templateSelector;
				control.Content = "Dummy";

				WindowHelper.WindowContent = control;
				await WindowHelper.WaitForLoaded(control);
				control.Content = items[0];
				var text1 = await WindowHelper.WaitForNonNull(() => control.FindFirstChild<TextBlock>(tb => tb.Name == "TextBlockInTemplate"), message: $"Template selector not applied for {control.GetType()}");
				Assert.AreEqual("Selectable A", text1.Text, $"Template selector not applied for {control.GetType()}");

				control.Content = items[1];
				var text2 = await WindowHelper.WaitForNonNull(() => control.FindFirstChild<TextBlock>(tb => tb.Name == "TextBlockInTemplate"));
				Assert.AreEqual("Selectable B", text2.Text);

				control.Content = items[2];
				var text3 = await WindowHelper.WaitForNonNull(() => control.FindFirstChild<TextBlock>(tb => tb.Name == "TextBlockInTemplate"));
				Assert.AreEqual("Selectable C", text3.Text);
			}
		}


		[TestMethod]
		public async Task When_ContentTemplateSelector_And_Default_Style_And_Uwp()
		{
			using var _ = StyleHelper.UseUwpStyles();
			await When_ContentTemplateSelector_And_Default_Style();
		}

		[TestMethod]
		public async Task When_Template_Applied_On_Loading_DataContext_Propagation()
		{
			var page = new Template_Loading_DataContext_Page();
			WindowHelper.WindowContent = page;
			await WindowHelper.WaitForLoaded(page);
			var comboBox = page.SpawnedButtonHost.PseudoContent as ComboBox;

			var dataContextChangedCounter = 0;
			var itemsSourceChangedCounter = 0;
			comboBox.DataContextChanged += (_, __) => dataContextChangedCounter++;
			comboBox.RegisterPropertyChangedCallback(ItemsControl.ItemsSourceProperty, (_, __) => itemsSourceChangedCounter++);

			page.SpawnedButtonHost.SpawnButton();
			Assert.IsNotNull(comboBox);

			await WindowHelper.WaitForLoaded(comboBox);
			Assert.AreEqual("Froot", comboBox.SelectedItem);
			Assert.AreEqual(1, dataContextChangedCounter);
			Assert.AreEqual(1, itemsSourceChangedCounter);
		}

		[TestMethod]
		public async Task When_Content_Set_Null_ComboBox()
		{
			var contentControl = new ContentControl
			{
				ContentTemplate = ContentControlComboTemplate,
			}.Apply(cc => cc.SetBinding(ContentControl.ContentProperty, new Binding()));

			var grid = new Grid()
			{
				DataContext = new ViewModel()
			};

			grid.Children.Add(contentControl);
			WindowHelper.WindowContent = grid;

			await WindowHelper.WaitForLoaded(grid);

			var comboBox = grid.FindFirstChild<ComboBox>();


			Assert.IsNotNull(comboBox);
			Assert.HasCount(3, comboBox.Items);

			contentControl.Content = null;

#if __APPLE_UIKIT__ || __ANDROID__
			Assert.IsEmpty(comboBox.Items);
			Assert.AreEqual(-1, comboBox.SelectedIndex);
#else // this is correct
			Assert.HasCount(3, comboBox.Items);
			Assert.AreEqual(1, comboBox.SelectedIndex);
#endif
		}

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_FindName_ContentControl_Without_ContentTemplate()
		{
			var sut = new ContentControl
			{
				Width = 100,
				Height = 100,
				Content = new TextBox()
			};

			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);

			Assert.IsNotNull(sut.FindName("ContentElement"));
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_FindName_ContentControl_With_ContentTemplate()
		{
			var sut = new ContentControl
			{
				Width = 100,
				Height = 100,
				Content = new TextBox(),
				ContentTemplate = new DataTemplate(() => new TextBlock())
			};

			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);

			Assert.IsNull(sut.FindName("ContentElement"));
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/11926")]
		public async Task When_ListView_ItemTemplate_DataContext_Preserved_After_ContentTemplate_Change()
		{
			var expectedItems = new[] { "Apple", "Banana", "Cherry" };
			var viewModel = new ListViewDataContextViewModel { Items = expectedItems };

			ListView capturedListView = null;

			var listViewTemplate = new DataTemplate(() =>
			{
				var lv = new ListView();
				capturedListView = lv;
				lv.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Path = new PropertyPath("Items") });
				lv.ItemTemplate = new DataTemplate(() =>
				{
					var border = new Border();
					border.SetBinding(FrameworkElement.DataContextProperty, new Binding());
					var tb = new TextBlock();
					tb.SetBinding(TextBlock.TextProperty, new Binding());
					border.Child = tb;
					return border;
				});
				return lv;
			});

			var itemsControlTemplate = new DataTemplate(() =>
			{
				var ic = new ItemsControl();
				ic.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Path = new PropertyPath("Items") });
				ic.ItemTemplate = new DataTemplate(() =>
				{
					var tb = new TextBlock();
					tb.SetBinding(TextBlock.TextProperty, new Binding());
					return tb;
				});
				return ic;
			});

			var contentControl = new ContentControl
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				ContentTemplate = listViewTemplate,
			};
			contentControl.SetBinding(ContentControl.ContentProperty, new Binding());

			var grid = new Grid { DataContext = viewModel };
			grid.Children.Add(contentControl);

			WindowHelper.WindowContent = grid;
			await WindowHelper.WaitForLoaded(grid);
			await WindowHelper.WaitForIdle();

			Assert.IsNotNull(capturedListView, "ListView should have been created by template");
			await WindowHelper.WaitFor(() => capturedListView.Items.Count == 3, message: "ListView should have 3 items initially");

			// Switch to ItemsControl template
			contentControl.ContentTemplate = itemsControlTemplate;
			await WindowHelper.WaitForIdle();

			// Switch back to ListView template — captures a fresh ListView reference
			capturedListView = null;
			contentControl.ContentTemplate = listViewTemplate;
			await WindowHelper.WaitForIdle();

			Assert.IsNotNull(capturedListView, "ListView should have been recreated by template");
			await WindowHelper.WaitFor(() => capturedListView.Items.Count == 3, message: "ListView should still have 3 items after template switch");

			// Each container's DataContext should still be the original string item, not the ViewModel
			for (int i = 0; i < expectedItems.Length; i++)
			{
				var index = i;
				var container = await WindowHelper.WaitForNonNull(
					() => capturedListView.ContainerFromIndex(index) as ListViewItem,
					message: $"Container {index} should exist after template switch");
				Assert.AreEqual(
					expectedItems[index],
					container.DataContext,
					$"Item[{index}] DataContext was altered. Expected '{expectedItems[index]}' but got: {container.DataContext?.GetType().Name}: '{container.DataContext}'");
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/11926")]
		public async Task When_ItemsControl_Items_Order_Preserved_After_ContentTemplate_Toggle()
		{
			var expectedItems = new[] { "First", "Second", "Third" };
			var viewModel = new ListViewDataContextViewModel { Items = expectedItems };

			ItemsControl capturedItemsControl = null;

			var listViewTemplate = new DataTemplate(() =>
			{
				var lv = new ListView();
				lv.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Path = new PropertyPath("Items") });
				lv.ItemTemplate = new DataTemplate(() =>
				{
					var tb = new TextBlock();
					tb.SetBinding(TextBlock.TextProperty, new Binding());
					return tb;
				});
				return lv;
			});

			var itemsControlTemplate = new DataTemplate(() =>
			{
				var ic = new ItemsControl();
				capturedItemsControl = ic;
				ic.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Path = new PropertyPath("Items") });
				ic.ItemTemplate = new DataTemplate(() =>
				{
					var tb = new TextBlock();
					tb.SetBinding(TextBlock.TextProperty, new Binding());
					return tb;
				});
				return ic;
			});

			var contentControl = new ContentControl
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				ContentTemplate = listViewTemplate,
			};
			contentControl.SetBinding(ContentControl.ContentProperty, new Binding());

			var grid = new Grid { DataContext = viewModel };
			grid.Children.Add(contentControl);

			WindowHelper.WindowContent = grid;
			await WindowHelper.WaitForLoaded(grid);
			await WindowHelper.WaitForIdle();

			// Switch to ItemsControl template
			capturedItemsControl = null;
			contentControl.ContentTemplate = itemsControlTemplate;
			await WindowHelper.WaitForIdle();

			Assert.IsNotNull(capturedItemsControl, "ItemsControl should have been created");
			await WindowHelper.WaitFor(() => capturedItemsControl.Items.Count == 3, message: "ItemsControl should have 3 items");

			// Toggle back and forth (2x as described in the issue)
			contentControl.ContentTemplate = listViewTemplate;
			await WindowHelper.WaitForIdle();

			capturedItemsControl = null;
			contentControl.ContentTemplate = itemsControlTemplate;
			await WindowHelper.WaitForIdle();

			Assert.IsNotNull(capturedItemsControl, "ItemsControl should have been recreated after double toggle");
			await WindowHelper.WaitFor(() => capturedItemsControl.Items.Count == 3, message: "ItemsControl should have 3 items after double toggle");

			// Items should be in original order: First, Second, Third
			Assert.AreEqual(expectedItems[0], capturedItemsControl.Items[0], "First item order must be preserved");
			Assert.AreEqual(expectedItems[1], capturedItemsControl.Items[1], "Second item order must be preserved");
			Assert.AreEqual(expectedItems[2], capturedItemsControl.Items[2], "Third item order must be preserved");
		}

		private class SignInViewModel
		{
			public string UserName { get; set; } = "Steve";
		}

		public sealed class Item
		{
			public string DisplayName { get; init; }
		}

		private class ViewModel
		{
			public SignInViewModel SignIn { get; set; } = new SignInViewModel();

			List<Item> _items = new()
			{
				new Item { DisplayName = "Test1" },
				new Item { DisplayName = "Test2" },
				new Item { DisplayName = "Test3" },
			};

			public IEnumerable<Item> Items => _items;

			public int SelectedIndex { get; set; } = 1;
		}

		private class ListViewDataContextViewModel
		{
			public string[] Items { get; set; }
		}
	}
}
