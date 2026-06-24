using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
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
		private static readonly ActivatableType[] _contentControlStyledDerivedTypes =
		{
			new(typeof(Button)),
			//new(typeof(AppBarButton)),
			//new(typeof(AppBar)),
			//new(typeof(CommandBar)),
			//new(typeof(SplitButton)),
			new(typeof(RepeatButton)),
			new(typeof(ToggleButton)),
			//new(typeof(AppBarToggleButton)),
			new(typeof(CheckBox)),
			new(typeof(RadioButton)),
			//new(typeof(SplitButton)),
			//new(typeof(ToggleSplitButton)),
			new(typeof(DropDownButton)),
		};

		public IEnumerable<ContentControl> DerivedStyledControlsInstances => _contentControlStyledDerivedTypes.Select(t => Activator.CreateInstance(t.Type) as ContentControl);

		[TestInitialize]
		public void Init()
		{
			_testsResources = new TestsResources();
		}

		private const DynamicallyAccessedMemberTypes ActivatorRequirements = DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;

		[TestMethod]
		[RunsOnUIThread]
		[DataRow(typeof(Grid))]
		[DataRow(typeof(StackPanel))]
		[DataRow(typeof(Border))]
		[DataRow(typeof(ContentPresenter))]
		public async Task When_SelfLoading([DynamicallyAccessedMembers(ActivatorRequirements)] Type type)
		{
			// Keep PreserveMetadata() calls in sync with the types in [DataRow] above.
			PreserveMetadata(typeof(Grid));
			PreserveMetadata(typeof(StackPanel));
			PreserveMetadata(typeof(Border));
			PreserveMetadata(typeof(ContentPresenter));

			var control = (FrameworkElement)Activator.CreateInstance(type);

			control.Width = 200;
			control.Height = 200;

			await UITestHelper.Load(control);

			static void PreserveMetadata([DynamicallyAccessedMembers(ActivatorRequirements)] Type type)
			{
			}
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
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/11926")]
		public async Task When_ListView_ItemTemplate_DataContext_Preserved_After_ContentTemplate_Change()
		{
			var expectedItems = new[] { "Apple", "Banana", "Cherry" };
			var viewModel = new ListViewDataContextViewModel { Items = expectedItems };

			ListView capturedListView = null;

			// DynamicDataTemplate is used (instead of the Uno-only `new DataTemplate(Func)`)
			// so the test also compiles against native WinUI.
			using var listViewItemTemplate = new DynamicDataTemplate(() =>
			{
				var border = new Border();
				border.SetBinding(FrameworkElement.DataContextProperty, new Binding());
				var tb = new TextBlock();
				tb.SetBinding(TextBlock.TextProperty, new Binding());
				border.Child = tb;
				return border;
			});

			using var listViewTemplate = new DynamicDataTemplate(() =>
			{
				var lv = new ListView();
				capturedListView = lv;
				lv.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Path = new PropertyPath("Items") });
				lv.ItemTemplate = listViewItemTemplate.Value;
				return lv;
			});

			using var itemsControlItemTemplate = new DynamicDataTemplate(() =>
			{
				var tb = new TextBlock();
				tb.SetBinding(TextBlock.TextProperty, new Binding());
				return tb;
			});

			using var itemsControlTemplate = new DynamicDataTemplate(() =>
			{
				var ic = new ItemsControl();
				ic.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Path = new PropertyPath("Items") });
				ic.ItemTemplate = itemsControlItemTemplate.Value;
				return ic;
			});

			var contentControl = new ContentControl
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				ContentTemplate = listViewTemplate.Value,
			};
			contentControl.SetBinding(ContentControl.ContentProperty, new Binding());

			var grid = new Grid { DataContext = viewModel };
			grid.Children.Add(contentControl);

			WindowHelper.WindowContent = grid;
			try
			{
				await WindowHelper.WaitForLoaded(grid);
				await WindowHelper.WaitForIdle();

				capturedListView = await WindowHelper.WaitForNonNull(
					() => capturedListView,
					message: "ListView should have been created by template");
				await WindowHelper.WaitFor(() => capturedListView.Items.Count == 3, message: "ListView should have 3 items initially");

				// Switch to ItemsControl template
				contentControl.ContentTemplate = itemsControlTemplate.Value;
				await WindowHelper.WaitForIdle();

				// Switch back to ListView template — captures a fresh ListView reference
				capturedListView = null;
				contentControl.ContentTemplate = listViewTemplate.Value;
				await WindowHelper.WaitForIdle();

				capturedListView = await WindowHelper.WaitForNonNull(
					() => capturedListView,
					message: "ListView should have been recreated by template");
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
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/11926")]
		public async Task When_ItemsControl_Items_Order_Preserved_After_ContentTemplate_Toggle()
		{
			var expectedItems = new[] { "First", "Second", "Third" };
			var viewModel = new ListViewDataContextViewModel { Items = expectedItems };

			ItemsControl capturedItemsControl = null;

			using var listViewItemTemplate = new DynamicDataTemplate(() =>
			{
				var tb = new TextBlock();
				tb.SetBinding(TextBlock.TextProperty, new Binding());
				return tb;
			});

			using var listViewTemplate = new DynamicDataTemplate(() =>
			{
				var lv = new ListView();
				lv.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Path = new PropertyPath("Items") });
				lv.ItemTemplate = listViewItemTemplate.Value;
				return lv;
			});

			using var itemsControlItemTemplate = new DynamicDataTemplate(() =>
			{
				var tb = new TextBlock();
				tb.SetBinding(TextBlock.TextProperty, new Binding());
				return tb;
			});

			using var itemsControlTemplate = new DynamicDataTemplate(() =>
			{
				var ic = new ItemsControl();
				capturedItemsControl = ic;
				ic.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Path = new PropertyPath("Items") });
				ic.ItemTemplate = itemsControlItemTemplate.Value;
				return ic;
			});

			var contentControl = new ContentControl
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				ContentTemplate = listViewTemplate.Value,
			};
			contentControl.SetBinding(ContentControl.ContentProperty, new Binding());

			var grid = new Grid { DataContext = viewModel };
			grid.Children.Add(contentControl);

			WindowHelper.WindowContent = grid;
			try
			{
				await WindowHelper.WaitForLoaded(grid);
				await WindowHelper.WaitForIdle();

				// Switch to ItemsControl template
				capturedItemsControl = null;
				contentControl.ContentTemplate = itemsControlTemplate.Value;
				await WindowHelper.WaitForIdle();

				capturedItemsControl = await WindowHelper.WaitForNonNull(
					() => capturedItemsControl,
					message: "ItemsControl should have been created");
				await WindowHelper.WaitFor(() => capturedItemsControl.Items.Count == 3, message: "ItemsControl should have 3 items");

				// Toggle back and forth (2x as described in the issue)
				contentControl.ContentTemplate = listViewTemplate.Value;
				await WindowHelper.WaitForIdle();

				capturedItemsControl = null;
				contentControl.ContentTemplate = itemsControlTemplate.Value;
				await WindowHelper.WaitForIdle();

				capturedItemsControl = await WindowHelper.WaitForNonNull(
					() => capturedItemsControl,
					message: "ItemsControl should have been recreated after double toggle");
				await WindowHelper.WaitFor(() => capturedItemsControl.Items.Count == 3, message: "ItemsControl should have 3 items after double toggle");

				// Issue #11926 reverses the *rendered* order after toggling. Items mirrors ItemsSource
				// (always source order), so assert on realized item visuals to actually catch a reversal.
				// Wait until all visuals are realized AND their text bindings resolved, capturing the
				// snapshot in the same tick so the assertion can't observe a half-bound (empty-text) state.
				string[] realizedTexts = null;
				await WindowHelper.WaitFor(
					() =>
					{
						var texts = GetRealizedTextBlocks(capturedItemsControl).Select(tb => tb.Text).ToArray();
						if (texts.Length != expectedItems.Length || texts.Any(string.IsNullOrEmpty))
						{
							return false;
						}

						realizedTexts = texts;
						return true;
					},
					message: "ItemsControl should realize all item visuals with bound text after double toggle");

				Assert.IsNotNull(realizedTexts);
				CollectionAssert.AreEqual(
					expectedItems,
					realizedTexts,
					$"Item visual order must be preserved. Expected [{string.Join(", ", expectedItems)}] but got [{string.Join(", ", realizedTexts)}]");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		private static IEnumerable<TextBlock> GetRealizedTextBlocks(DependencyObject root)
		{
			var count = VisualTreeHelper.GetChildrenCount(root);
			for (var i = 0; i < count; i++)
			{
				var child = VisualTreeHelper.GetChild(root, i);
				if (child is TextBlock tb)
				{
					yield return tb;
				}
				else
				{
					foreach (var nested in GetRealizedTextBlocks(child))
					{
						yield return nested;
					}
				}
			}
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

	struct ActivatableType
	{
		private const DynamicallyAccessedMemberTypes ActivatableRequirements = DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;

		[DynamicallyAccessedMembers(ActivatableRequirements)]
		public Type Type { get; }

		public ActivatableType([DynamicallyAccessedMembers(ActivatableRequirements)] Type type)
		{
			this.Type = type;
		}
	}
}
