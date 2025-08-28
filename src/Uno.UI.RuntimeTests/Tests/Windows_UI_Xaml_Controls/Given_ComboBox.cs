using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using SamplesApp.UITests;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.ComboBoxTests;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Storage.Pickers;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices;
using ComboBoxHelper = Microsoft.UI.Xaml.Tests.Common.ComboBoxHelper;
using Uno.UI.Extensions;
using Combinatorial.MSTest;

#if __APPLE_UIKIT__
using _UIViewController = UIKit.UIViewController;
using Uno.UI.Controls;

using Windows.UI.Core;
using Microsoft.UI.Xaml.Media.Animation;
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.MultiFrame;
using Microsoft.UI.Xaml.Controls.Primitives;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_ComboBox
	{
		private ResourceDictionary _testsResources;

		private Style CounterComboBoxContainerStyle => _testsResources["CounterComboBoxContainerStyle"] as Style;

		private Style ComboBoxItemContainerStyle => _testsResources["ComboBoxItemContainerStyle"] as Style;

		private Style ComboBoxWithSeparatorStyle => _testsResources["ComboBoxWithSeparatorStyle"] as Style;

		private DataTemplate CounterItemTemplate => _testsResources["CounterItemTemplate"] as DataTemplate;

		private ItemsPanelTemplate MeasureCountCarouselPanelTemplate => _testsResources["MeasureCountCarouselPanel"] as ItemsPanelTemplate;

		[TestInitialize]
		public void Init()
		{
			_testsResources = new TestsResources();

			CounterGrid.Reset();
			CounterGrid2.Reset();
			MeasureCountCarouselPanel.Reset();
		}

		const int BorderThicknessAdjustment = 2; // Deduct BorderThickness on PopupBorder

#if HAS_UNO
		[TestMethod]
		[DataRow(0)]
		[DataRow(1)]
		[DataRow(2)]
		public async Task When_ReOpened_Remains_Selected_VisualState(int selectedIndex)
		{
			var SUT = new ComboBox();
			var items = new[] { "First", "Second", "Third" };
			SUT.ItemsSource = items;
			await UITestHelper.Load(SUT);

			SUT.IsDropDownOpen = true;
			await TestServices.WindowHelper.WaitForIdle();
			SUT.SelectedIndex = selectedIndex;
			await TestServices.WindowHelper.WaitForIdle();
			SUT.IsDropDownOpen = false;
			await TestServices.WindowHelper.WaitForIdle();
			SUT.IsDropDownOpen = true;
			await TestServices.WindowHelper.WaitForIdle();
			var container = (ComboBoxItem)SUT.ContainerFromItem(items[selectedIndex]);
			Assert.AreEqual("Selected", VisualStateManager.GetCurrentState(container, "CommonStates").Name);
		}
#endif

		[TestMethod]
		public async Task When_IsEditable_False()
		{
			// EditableText is only available in fluent style.
			var SUT = new ComboBox();
			await UITestHelper.Load(SUT);

			Assert.IsFalse(SUT.IsEditable);
			Assert.AreEqual(Visibility.Collapsed, GetEditableText(SUT).Visibility);
		}

		[TestMethod]
		public async Task When_IsEditable_True()
		{
			// EditableText is only available in fluent style.
			var SUT = new ComboBox() { IsEditable = true };
			await UITestHelper.Load(SUT);

			Assert.IsTrue(SUT.IsEditable);
			Assert.AreEqual(Visibility.Visible, GetEditableText(SUT).Visibility);
		}

		[TestMethod]
		public async Task When_IsEditable_False_Changes_To_True()
		{
			if (!ApiInformation.IsPropertyPresent("Microsoft.UI.Xaml.Controls.ComboBox, Uno.UI", "IsEditable"))
			{
				Assert.Inconclusive();
			}

			// EditableText is only available in fluent style.
			var SUT = new ComboBox();
			await UITestHelper.Load(SUT);

			Assert.IsFalse(SUT.IsEditable);
			Assert.AreEqual(Visibility.Collapsed, GetEditableText(SUT).Visibility);

			SUT.IsEditable = true;

			Assert.IsTrue(SUT.IsEditable);
			Assert.AreEqual(Visibility.Visible, GetEditableText(SUT).Visibility);
		}

		private TextBox GetEditableText(ComboBox comboBox) => comboBox.FindFirstChild<TextBox>(c => c.Name == "EditableText");

		[TestMethod]
		public async Task When_ComboBox_MinWidth()
		{
			var source = Enumerable.Range(0, 5).ToArray();
			const int minWidth = 172;
			const int expectedItemWidth = minWidth - BorderThicknessAdjustment;
			var SUT = new ComboBox
			{
				MinWidth = minWidth,
				ItemsSource = source
			};

			try
			{
				WindowHelper.WindowContent = SUT;

				await WindowHelper.WaitForLoaded(SUT);

				await WindowHelper.WaitFor(() => SUT.ActualWidth == minWidth); // Needed for iOS where ComboBox may be initially too wide, for some reason

				SUT.IsDropDownOpen = true;

				ComboBoxItem cbi = null;
				foreach (var item in source)
				{
					await WindowHelper.WaitFor(() => (cbi = SUT.ContainerFromItem(item) as ComboBoxItem) != null);
					await WindowHelper.WaitForLoaded(cbi); // Required on Android
					Assert.AreEqual(expectedItemWidth, cbi.ActualWidth);
				}
			}
			finally
			{
				SUT.IsDropDownOpen = false;
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_ComboBox_Constrained_By_Parent()
		{
			var source = Enumerable.Range(0, 5).ToArray();
			const int width = 133;
			const int expectedItemWidth = width - BorderThicknessAdjustment;
			var SUT = new ComboBox
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				ItemsSource = source
			};
			var grid = new Grid
			{
				Width = width
			};
			grid.Children.Add(SUT);

			try
			{
				WindowHelper.WindowContent = grid;

				await WindowHelper.WaitForLoaded(SUT);

				SUT.IsDropDownOpen = true;

				ComboBoxItem cbi = null;
				foreach (var item in source)
				{
					await WindowHelper.WaitFor(() => (cbi = SUT.ContainerFromItem(item) as ComboBoxItem) != null);
					await WindowHelper.WaitForLoaded(cbi); // Required on Android
					Assert.AreEqual(expectedItemWidth, cbi.ActualWidth, 0.5); // Account for layout rounding
				}
			}
			finally
			{
				SUT.IsDropDownOpen = false;
				WindowHelper.WindowContent = null;
			}
		}

#if HAS_UNO
		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_PointerWheel()
		{
			var SUT = new ComboBox();
			var items = new[] { "First", "Second", "Third" };
			SUT.ItemsSource = items;
			var rect = await UITestHelper.Load(SUT);

			SUT.SelectedIndex = 0;
			SUT.Focus(FocusState.Programmatic);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			mouse.MoveTo(rect.X + 2, rect.Y + 2);

			mouse.WheelDown();

			Assert.AreEqual(1, SUT.SelectedIndex);

			await WindowHelper.WaitForIdle();

			mouse.WheelUp();

			Assert.AreEqual(0, SUT.SelectedIndex);

			await WindowHelper.WaitForIdle();

			mouse.WheelUp();

			Assert.AreEqual(0, SUT.SelectedIndex);
		}
#endif

		[TestMethod]
		public async Task Check_Creation_Count_Few_Items()
		{
			var source = Enumerable.Range(0, 5).ToArray();
			using (FeatureConfigurationHelper.UseTemplatePooling()) // If pooling is disabled, then the 'is template a container' check creates an extra template root
			{
				var SUT = new ComboBox
				{
					ItemsSource = source,
					ItemContainerStyle = CounterComboBoxContainerStyle,
					ItemTemplate = CounterItemTemplate
				};

				try
				{
					Assert.AreEqual(0, CounterGrid.CreationCount);
					Assert.AreEqual(0, CounterGrid2.CreationCount);
					WindowHelper.WindowContent = SUT;

					await WindowHelper.WaitForLoaded(SUT);

#if !__ANDROID__ && !__APPLE_UIKIT__ // This does not hold on Android or iOS, possibly because ComboBox is not virtualized
					Assert.AreEqual(0, CounterGrid.CreationCount);
					Assert.AreEqual(0, CounterGrid2.CreationCount);
#endif

					SUT.IsDropDownOpen = true;

					ComboBoxItem cbi = null;
					await WindowHelper.WaitFor(() => (cbi = SUT.ContainerFromItem(source[0]) as ComboBoxItem) != null);
					await WindowHelper.WaitForLoaded(cbi); // Required on Android

					Assert.AreEqual(5, CounterGrid.CreationCount);
					Assert.AreEqual(5, CounterGrid2.CreationCount);
					Assert.AreEqual(5, CounterGrid.BindCount);
					Assert.AreEqual(5, CounterGrid2.BindCount);
				}
				finally
				{
					SUT.IsDropDownOpen = false;
					WindowHelper.WindowContent = null;
				}
			}
		}

		[TestMethod]
#if __APPLE_UIKIT__ || __ANDROID__
		[Ignore("ComboBox is currently not virtualized on iOS and Android - #556")] // https://github.com/unoplatform/uno/issues/556
#endif
		public async Task Check_Creation_Count_Many_Items()
		{
			var source = Enumerable.Range(0, 500).ToArray();
			var SUT = new ComboBox
			{
				ItemsSource = source,
				ItemContainerStyle = CounterComboBoxContainerStyle,
				ItemTemplate = CounterItemTemplate
			};

			try
			{
				WindowHelper.WindowContent = SUT;

				await WindowHelper.WaitForLoaded(SUT);

				Assert.AreEqual(0, CounterGrid.CreationCount);
				Assert.AreEqual(0, CounterGrid2.CreationCount);

				SUT.IsDropDownOpen = true;

				ComboBoxItem cbi = null;
				await WindowHelper.WaitFor(() => source.Any(i => (cbi = SUT.ContainerFromItem(i) as ComboBoxItem) != null)); // Windows loads up CarouselPanel with no selected item around the middle, other platforms may not
																															 //await WindowHelper.WaitFor(() => (cbi = SUT.ContainerFromItem(source[0]) as ComboBoxItem) != null);
				await WindowHelper.WaitForLoaded(cbi); // Required on Android

				const int maxCount = 30;
				NumberAssert.Less(CounterGrid.CreationCount, maxCount);
				NumberAssert.Less(CounterGrid2.CreationCount, maxCount);
			}
			finally
			{
				SUT.IsDropDownOpen = false;
				WindowHelper.WindowContent = null;
			}
		}

#if __APPLE_UIKIT__
		[TestMethod]
		[RunsOnUIThread]
		public async Task Check_DropDown_Flyout_Margin_When_In_Modal()
		{
			MultiFrame multiFrame = new();
			var showModalButton = new Button();

			multiFrame.Children.Add(showModalButton);

			var source = Enumerable.Range(0, 6).ToArray();
			var SUT = new ComboBox
			{
				ItemsSource = source,
				Text = "Alignment",
				VerticalAlignment = VerticalAlignment.Center,
				PlaceholderText = "Testing",
				Style = ComboBoxWithSeparatorStyle
			};

			var modalPage = new Page();
			var gridContainer = new Grid()
			{
				Background = SolidColorBrushHelper.LightGreen
			};

			async void OpenModal(object sender, RoutedEventArgs e)
			{
				gridContainer.Children.Add(SUT);
				modalPage.Content = gridContainer;

				await multiFrame.OpenModal(FrameSectionsTransitionInfo.NativeiOSModal, modalPage);
			}

			try
			{
				var homePage = new Page();
				showModalButton.Click += OpenModal;
				homePage.Content = multiFrame;

				WindowHelper.WindowContent = homePage;

				await WindowHelper.WaitForLoaded(homePage);

				// Open Modal
				showModalButton.RaiseClick();

				await WindowHelper.WaitForLoaded(modalPage);

				SUT.IsDropDownOpen = true;

				await WindowHelper.WaitForIdle();

				var locationX = SUT.GetAbsoluteBoundsRect().Location.X;

				var popup = SUT.FindFirstChild<Popup>();
				var childX = popup?.Child?.Frame.X ?? 0;

				Assert.IsNotNull(ComboBoxWithSeparatorStyle);
				Assert.IsNotNull(popup);
				Assert.IsTrue(popup.IsOpen);
				Assert.AreEqual(locationX, childX, "ComboBox vs ComboBox.PopUp.Child Frame.X are not equal");
			}
			finally
			{
				showModalButton.Click -= OpenModal;
				SUT.IsDropDownOpen = false;
				await multiFrame.CloseModal();
				WindowHelper.WindowContent = null;
			}
		}
#endif

		[TestMethod]
		public async Task Check_Dropdown_Measure_Count()
		{
			var source = Enumerable.Range(0, 500).ToArray();
			var SUT = new ComboBox
			{
				ItemsSource = source,
				ItemsPanel = MeasureCountCarouselPanelTemplate
			};

			try
			{
				WindowHelper.WindowContent = SUT;

				await WindowHelper.WaitForLoaded(SUT);

				Assert.AreEqual(0, MeasureCountCarouselPanel.MeasureCount);
				Assert.AreEqual(0, MeasureCountCarouselPanel.ArrangeCount);

				SUT.IsDropDownOpen = true;

				ComboBoxItem cbi = null;
				await WindowHelper.WaitFor(() => source.Any(i => (cbi = SUT.ContainerFromItem(i) as ComboBoxItem) != null)); // Windows loads up CarouselPanel with no selected item around the middle, other platforms may not
				await WindowHelper.WaitForLoaded(cbi); // Required on Android

				NumberAssert.Greater(MeasureCountCarouselPanel.MeasureCount, 0);
				NumberAssert.Greater(MeasureCountCarouselPanel.ArrangeCount, 0);

#if __APPLE_UIKIT__
				const int MaxAllowedCount = 15; // TODO: figure out why iOS measures more times
#else
				const int MaxAllowedCount = 5;
#endif
				NumberAssert.Less(MeasureCountCarouselPanel.MeasureCount, MaxAllowedCount);
				NumberAssert.Less(MeasureCountCarouselPanel.ArrangeCount, MaxAllowedCount);
			}
			finally
			{
				SUT.IsDropDownOpen = false;
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_CB_Fluent_And_Theme_Changed()
		{
			var comboBox = new ComboBox
			{
				ItemsSource = new[] { 1, 2, 3 },
				PlaceholderText = "Select..."
			};

			WindowHelper.WindowContent = comboBox;
			await WindowHelper.WaitForLoaded(comboBox);

			var placeholderTextBlock = comboBox.FindFirstChild<TextBlock>(tb => tb.Name == "PlaceholderTextBlock");

			Assert.IsNotNull(placeholderTextBlock);

			var lightThemeForeground = TestsColorHelper.ToColor("#9E000000");
			var darkThemeForeground = TestsColorHelper.ToColor("#C5FFFFFF");

			Assert.AreEqual(lightThemeForeground, (placeholderTextBlock.Foreground as SolidColorBrush)?.Color);

			using (ThemeHelper.UseDarkTheme())
			{
				Assert.AreEqual(darkThemeForeground, (placeholderTextBlock.Foreground as SolidColorBrush)?.Color);
			}

			Assert.AreEqual(lightThemeForeground, (placeholderTextBlock.Foreground as SolidColorBrush)?.Color);
		}

		[TestMethod]
		public async Task When_SelectedItem_Set_Before_ItemsSource()
		{
			var SUT = new ComboBox();
			var items = Enumerable.Range(0, 10);

			await UITestHelper.Load(SUT);

			SUT.SelectedItem = 5;
			await WindowHelper.WaitForIdle();
			Assert.IsNull(SUT.SelectedItem);

			SUT.ItemsSource = items;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(5, SUT.SelectedItem);
		}

		[TestMethod]
		public async Task When_SelectedItem_Set_Then_SelectedIndex_Then_ItemsSource()
		{
			var SUT = new ComboBox();
			var items = Enumerable.Range(0, 10);

			await UITestHelper.Load(SUT);

			SUT.SelectedIndex = 3;
			await WindowHelper.WaitForIdle();
			Assert.IsNull(SUT.SelectedItem);
			Assert.AreEqual(-1, SUT.SelectedIndex);

			SUT.SelectedItem = 5;
			await WindowHelper.WaitForIdle();
			Assert.IsNull(SUT.SelectedItem);
			Assert.AreEqual(-1, SUT.SelectedIndex);

			SUT.ItemsSource = items;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(5, SUT.SelectedItem);
			Assert.AreEqual(5, SUT.SelectedIndex);
		}

		[TestMethod]
		public async Task When_Tabbed()
		{
			var SUT = new ComboBox
			{
				Items =
				{
					new ComboBoxItem { Content = new TextBox { Text = "item1" } },
					new ComboBoxItem { Content = new TextBox { Text = "item2" } }
				}
			};
			var btn = new Button();
			var grid = new Grid
			{
				Children =
				{
					SUT,
					btn
				}
			};

			WindowHelper.WindowContent = grid;
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(SUT, FocusManager.GetFocusedElement(SUT.XamlRoot));

			await KeyboardHelper.Tab();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(btn, FocusManager.GetFocusedElement(SUT.XamlRoot));
		}

		[TestMethod]
		public async Task When_Popup_Open_Tabbed()
		{
			var SUT = new ComboBox
			{
				Items =
				{
					new ComboBoxItem { Content = new TextBox { Text = "item1" } },
					new ComboBoxItem { Content = new TextBox { Text = "item2" } }
				}
			};
			var btn = new Button();
			var grid = new Grid
			{
				Children =
				{
					SUT,
					btn
				}
			};

			WindowHelper.WindowContent = grid;
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(SUT, FocusManager.GetFocusedElement(SUT.XamlRoot));

			await KeyboardHelper.Space();
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.IsDropDownOpen);
			Assert.IsTrue(FocusManager.GetFocusedElement(SUT.XamlRoot) is ComboBoxItem);

			await KeyboardHelper.Tab();
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.IsDropDownOpen);
			Assert.AreEqual(btn, FocusManager.GetFocusedElement(SUT.XamlRoot));
		}

		[TestMethod]
		public void When_Index_Is_Out_Of_Range_And_Later_Becomes_Valid()
		{
			var comboBox = new ComboBox();
			comboBox.SelectedIndex = 2;
			Assert.AreEqual(-1, comboBox.SelectedIndex);
			comboBox.Items.Add(new ComboBoxItem());
			Assert.AreEqual(-1, comboBox.SelectedIndex);
			comboBox.Items.Add(new ComboBoxItem());
			Assert.AreEqual(-1, comboBox.SelectedIndex);
			comboBox.Items.Add(new ComboBoxItem());
			Assert.AreEqual(2, comboBox.SelectedIndex);
		}

		[TestMethod]
		public void When_Index_Set_With_No_Items_Repeated()
		{
			var comboBox = new ComboBox();
			comboBox.SelectedIndex = 1;
			Assert.AreEqual(-1, comboBox.SelectedIndex);
			comboBox.Items.Add(new ComboBoxItem());
			Assert.AreEqual(-1, comboBox.SelectedIndex);
			comboBox.Items.Add(new ComboBoxItem());
			Assert.AreEqual(1, comboBox.SelectedIndex);
			comboBox.Items.Clear();
			Assert.AreEqual(-1, comboBox.SelectedIndex);
			comboBox.SelectedIndex = 2;
			comboBox.Items.Add(new ComboBoxItem());
			Assert.AreEqual(-1, comboBox.SelectedIndex);
			comboBox.Items.Add(new ComboBoxItem());
			Assert.AreEqual(-1, comboBox.SelectedIndex);
			comboBox.Items.Add(new ComboBoxItem());
			Assert.AreEqual(2, comboBox.SelectedIndex);
		}

		[TestMethod]
		public void When_Index_Set_Out_Of_Range_When_Items_Exist()
		{
			var comboBox = new ComboBox();
			comboBox.Items.Add(new ComboBoxItem());
			Assert.ThrowsExactly<ArgumentException>(() => comboBox.SelectedIndex = 2);
		}

		[TestMethod]
		public void When_Index_Set_Negative_Out_Of_Range_When_Items_Exist()
		{
			var comboBox = new ComboBox();
			comboBox.Items.Add(new ComboBoxItem());
			Assert.ThrowsExactly<ArgumentException>(() => comboBox.SelectedIndex = -2);
		}

		[TestMethod]
		public void When_Index_Set_Negative_Out_Of_Range_When_Items_Do_Not_Exist()
		{
			var comboBox = new ComboBox();
			comboBox.SelectedIndex = -2;
			Assert.AreEqual(-1, comboBox.SelectedIndex);
			comboBox.Items.Add(new ComboBoxItem());
			Assert.AreEqual(-1, comboBox.SelectedIndex);
		}

		[TestMethod]
		public void When_Index_Is_Explicitly_Set_To_Negative_After_Out_Of_Range_Value()
		{
			var comboBox = new ComboBox();
			comboBox.SelectedIndex = 2;
			Assert.AreEqual(-1, comboBox.SelectedIndex);
			comboBox.SelectedIndex = -1; // While SelectedIndex was already -1 (assert above), this *does* make a difference.
			comboBox.Items.Add(new ComboBoxItem());
			Assert.AreEqual(-1, comboBox.SelectedIndex);
			comboBox.Items.Add(new ComboBoxItem());
			Assert.AreEqual(-1, comboBox.SelectedIndex);
			comboBox.Items.Add(new ComboBoxItem());
			Assert.AreEqual(-1, comboBox.SelectedIndex); // Will no longer become 2
		}

		[TestMethod]
		public async Task When_Collection_Reset()
		{
			var SUT = new ComboBox();
			try
			{
				WindowHelper.WindowContent = SUT;

				var c = new MyObservableCollection<string>();
				c.Add("One");
				c.Add("Two");
				c.Add("Three");

				SUT.ItemsSource = c;

				await WindowHelper.WaitForIdle();

				Assert.AreEqual(3, SUT.Items.Count);

				using (c.BatchUpdate())
				{
					c.Add("Four");
					c.Add("Five");
				}

				SUT.IsDropDownOpen = true;

				// Items are materialized when the popup is opened
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(5, SUT.Items.Count);
				Assert.IsNotNull(SUT.ContainerFromItem("One"));
				Assert.IsNotNull(SUT.ContainerFromItem("Four"));
				Assert.IsNotNull(SUT.ContainerFromItem("Five"));
			}
			finally
			{
				SUT.IsDropDownOpen = false;
			}
		}

		[TestMethod]
		public async Task When_ComboBoxItem_DataContext_Cleared()
		{
			// Arrange
			var stackPanel = new StackPanel();
			stackPanel.DataContext = Guid.NewGuid().ToString();
			var comboBox = new ComboBox();
			var originalSource = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
			comboBox.ItemsSource = originalSource;
			comboBox.SelectedIndex = 0;
			stackPanel.Children.Add(comboBox);
			WindowHelper.WindowContent = stackPanel;
			await WindowHelper.WaitForLoaded(stackPanel);
			FrameworkElement itemContainer = null;

			// Act
			comboBox.IsDropDownOpen = true;
			await WindowHelper.WaitFor(() => (itemContainer = comboBox.ContainerFromIndex(0) as ComboBoxItem) is not null);
			itemContainer.DataContextChanged += (s, e) =>
			{
				// Assert
				Assert.AreNotEqual(stackPanel.DataContext, itemContainer.DataContext);
			};
			comboBox.IsDropDownOpen = false;
			await WindowHelper.WaitForIdle();
			comboBox.IsDropDownOpen = true;
			await WindowHelper.WaitForIdle();
			comboBox.IsDropDownOpen = false;
			var updatedSource = new List<int>() { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
			comboBox.ItemsSource = updatedSource;
			await WindowHelper.WaitForIdle();
			comboBox.IsDropDownOpen = true;
			await WindowHelper.WaitForIdle();
			comboBox.IsDropDownOpen = false;
		}

		[TestMethod]
		public async Task When_Binding_Change()
		{
			var SUT = new ComboBox();
			try
			{
				WindowHelper.WindowContent = SUT;

				var c = new ObservableCollection<string>();
				c.Add("One");
				c.Add("Two");
				c.Add("Three");
				c.Add("Four");
				c.Add("Five");
				c.Add("Six");
				c.Add("2-One");
				c.Add("2-Two");
				c.Add("2-Three");
				c.Add("2-Four");
				c.Add("2-Five");
				c.Add("2-Six");

				SUT.SetBinding(ComboBox.ItemsSourceProperty, new Microsoft.UI.Xaml.Data.Binding() { Path = new("MySource") });
				SUT.SetBinding(ComboBox.SelectedItemProperty, new Microsoft.UI.Xaml.Data.Binding() { Path = new("SelectedItem"), Mode = Microsoft.UI.Xaml.Data.BindingMode.TwoWay });

				SUT.DataContext = new { MySource = c, SelectedItem = "One" };

				await WindowHelper.WaitForIdle();

				SUT.IsDropDownOpen = true;

				await Task.Delay(100);

				WindowHelper.WindowContent = null;

				await Task.Delay(100);

				SUT.DataContext = null;

				await WindowHelper.WaitForIdle();

				WindowHelper.WindowContent = SUT;
				SUT.DataContext = new { MySource = c, SelectedItem = "Two" };

				Assert.AreEqual(12, SUT.Items.Count);
			}
			finally
			{
				SUT.IsDropDownOpen = false;
			}
		}

#if HAS_UNO
		[TestMethod]
		public async Task When_Full_Collection_Reset()
		{
			var SUT = new ComboBox();
			SUT.ItemTemplate = new DataTemplate(() =>
			{

				var tb = new TextBlock();
				tb.SetBinding(TextBlock.TextProperty, new Microsoft.UI.Xaml.Data.Binding { Path = "Text" });
				tb.SetBinding(TextBlock.NameProperty, new Microsoft.UI.Xaml.Data.Binding { Path = "Text" });

				return tb;
			});

			try
			{
				WindowHelper.WindowContent = SUT;

				var c = new MyObservableCollection<ItemModel>();
				c.Add(new ItemModel { Text = "One" });
				c.Add(new ItemModel { Text = "Two" });
				c.Add(new ItemModel { Text = "Three" });

				SUT.ItemsSource = c;

				await WindowHelper.WaitForIdle();
				SUT.IsDropDownOpen = true;

				await WindowHelper.WaitForIdle();
				SUT.IsDropDownOpen = false;

				// Not required on WinUI. Fixing this in Uno requires porting ComboBox.
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(3, SUT.Items.Count);

				using (c.BatchUpdate())
				{
					c.Clear();
					c.Add(new ItemModel { Text = "Five" });
					c.Add(new ItemModel { Text = "Six" });
					c.Add(new ItemModel { Text = "Seven" });
				}

				SUT.IsDropDownOpen = true;

				// Items are materialized when the popup is opened
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(3, SUT.Items.Count);
				Assert.IsNotNull(SUT.ContainerFromItem(c[0]));
				Assert.IsNotNull(SUT.ContainerFromItem(c[1]));
				Assert.IsNotNull(SUT.ContainerFromItem(c[2]));

				Assert.IsNotNull(SUT.FindName("Seven"));
				Assert.IsNotNull(SUT.FindName("Five"));
				Assert.IsNotNull(SUT.FindName("Six"));
			}
			finally
			{
				SUT.IsDropDownOpen = false;
			}
		}
#endif

#if HAS_UNO
		[TestMethod]
		public async Task When_Recycling_Explicit_Items()
		{
			var SUT = new ComboBox();
			SUT.ItemTemplate = new DataTemplate(() =>
			{

				var tb = new TextBlock();
				tb.SetBinding(TextBlock.TextProperty, new Microsoft.UI.Xaml.Data.Binding { Path = "Text" });
				tb.SetBinding(TextBlock.NameProperty, new Microsoft.UI.Xaml.Data.Binding { Path = "Text" });

				return tb;
			});

			try
			{
				WindowHelper.WindowContent = SUT;

				var c = new MyObservableCollection<ComboBoxItem>();
				c.Add(new ComboBoxItem { Content = "One" });
				c.Add(new ComboBoxItem { Content = "Two" });
				c.Add(new ComboBoxItem { Content = "Three" });

				SUT.ItemsSource = c;

				await WindowHelper.WaitForIdle();
				SUT.IsDropDownOpen = true;

				await WindowHelper.WaitForIdle();

				Assert.AreEqual(3, SUT.Items.Count);
				Assert.IsNotNull(SUT.ContainerFromItem(c[0]));
				Assert.IsNotNull(SUT.ContainerFromItem(c[1]));
				Assert.IsNotNull(SUT.ContainerFromItem(c[2]));
				Assert.AreEqual("One", c[0].Content);
				Assert.AreEqual("Two", c[1].Content);
				Assert.AreEqual("Three", c[2].Content);

				await WindowHelper.WaitForIdle();

				SUT.IsDropDownOpen = false;

				await WindowHelper.WaitForIdle();

				// Open again to ensure that the items are recycled
				SUT.IsDropDownOpen = true;

				await WindowHelper.WaitForIdle();

				Assert.AreEqual(3, SUT.Items.Count);
				Assert.IsNotNull(SUT.ContainerFromItem(c[0]));
				Assert.IsNotNull(SUT.ContainerFromItem(c[1]));
				Assert.IsNotNull(SUT.ContainerFromItem(c[2]));
				Assert.AreEqual("One", c[0].Content);
				Assert.AreEqual("Two", c[1].Content);
				Assert.AreEqual("Three", c[2].Content);

			}
			finally
			{
				SUT.IsDropDownOpen = false;
			}
		}
#endif

#if HAS_UNO
		[TestMethod]
		public async Task When_SelectedItem_TwoWay_Binding()
		{
			var itemsControl = new ItemsControl()
			{
				ItemsPanel = new ItemsPanelTemplate(() => new StackPanel()),
				ItemTemplate = new DataTemplate(() =>
				{
					var comboBox = new ComboBox();
					comboBox.Name = "combo";

					comboBox.SetBinding(ComboBox.ItemsSourceProperty, new Binding { Path = new("Numbers") });
					comboBox.SetBinding(
						ComboBox.SelectedItemProperty,
						new Binding { Path = new("SelectedNumber"), Mode = BindingMode.TwoWay });

					return comboBox;
				})
			};

			var test = new[] {
				new TwoWayBindingItem()
			};

			WindowHelper.WindowContent = itemsControl;

			itemsControl.ItemsSource = test;

			await WindowHelper.WaitForIdle();

			var comboBox = itemsControl.FindName("combo") as ComboBox;

			Assert.AreEqual(3, test[0].SelectedNumber);
			Assert.AreEqual(3, comboBox.SelectedItem);
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_SelectedItem_Active_VisualState()
		{
			var source = Enumerable.Range(0, 10).ToArray();
			var SUT = new ComboBox
			{
				ItemsSource = source,
				ItemContainerStyle = ComboBoxItemContainerStyle,
				ItemTemplate = CounterItemTemplate
			};

			SUT.SelectedItem = 2;
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			try
			{
				// force ItemsPanel to materialized, otherwise ContainerFromIndex will always return 0
				// and also while closed, the CBI's "CommonStates" will be forced to "Normal".
				// at least, _should be_ forced to "Normal" as winui suggests.
				SUT.IsDropDownOpen = true;
				await WindowHelper.WaitForIdle();

				var container2 = SUT.ContainerFromItem(SUT.SelectedItem) as SelectorItem;
				Assert.IsNotNull(container2, "failed to resolve container#2");
				var container2States = VisualStateHelper.GetCurrentVisualStateName(container2).ToArray();
				Assert.IsTrue(container2States.Any(x => x.Contains("Selected")), $"container#2 is not selected: states={container2States.JoinBy("|")}");

				// changing selection to 6
				SUT.SelectedItem = 6;
				await WindowHelper.WaitForIdle();

				var container6 = SUT.ContainerFromItem(SUT.SelectedItem) as SelectorItem;
				Assert.IsNotNull(container6, "failed to resolve container#6");
				var container2PostStates = VisualStateHelper.GetCurrentVisualStateName(container2).ToArray();
				var container6PostStates = VisualStateHelper.GetCurrentVisualStateName(container6).ToArray();
				Assert.IsFalse(container2PostStates.Any(x => x.Contains("Selected")), $"container#2 is still selected: states={container2PostStates.JoinBy("|")}");
				Assert.IsTrue(container6PostStates.Any(x => x.Contains("Selected")), $"container#6 is not selected: states={container6PostStates.JoinBy("|")}");
			}
			finally
			{
				SUT.IsDropDownOpen = false;
			}
		}

#if HAS_UNO
		[TestMethod]
		[RequiresFullWindow]
		[RunsOnUIThread]
		[Ignore("Test is not valid - the Popup is not actually rendered in the screenshots")]
		public async Task When_Mouse_Opened_And_Closed()
		{
			// Create a comboBox with some sample items
			ComboBox comboBox = new ComboBox();
			for (int i = 0; i < 10; i++)
			{
				comboBox.Items.Add(i);
			}
			var stackPanel = new StackPanel()
			{
				Orientation = Orientation.Horizontal,
				Spacing = 10,
				Padding = new Thickness(10),
				VerticalAlignment = VerticalAlignment.Top
			};

			var text = new TextBlock() { Text = "Click me", VerticalAlignment = VerticalAlignment.Top };
			stackPanel.Children.Add(comboBox);
			stackPanel.Children.Add(text);

			// Set the comboBox as Window content
			WindowHelper.WindowContent = stackPanel;

			// Wait for it to load
			await WindowHelper.WaitForLoaded(comboBox);

			comboBox.SelectedItem = 5;
			await WindowHelper.WaitForIdle();

			// Take a screenshot of the comboBox before opening
			var screenshotBefore = await TakeScreenshot(stackPanel);

			// Use input injection to tap the comboBox and open the popup
			var comboBoxCenter = comboBox.GetAbsoluteBounds().GetCenter();

			using var finger = InputInjector.TryCreate().GetFinger();
			finger.MoveTo(comboBoxCenter);
			finger.Press(comboBoxCenter);
			await WindowHelper.WaitForIdle();
			finger.Release();

			// Wait for the popup to load and render
			await WindowHelper.WaitFor(() => VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).Count > 0);
			await WindowHelper.WaitForIdle();

			// Take a screenshot of the UI after opening the comboBox
			var screenshotOpened = await TakeScreenshot(stackPanel);

			// Verify that the UI changed
			await ImageAssert.AreNotEqualAsync(screenshotBefore, screenshotOpened);

			var textCenter = text.GetAbsoluteBounds().GetCenter();
			finger.Press(textCenter.X, textCenter.Y + 100);
			finger.Release();

			await WindowHelper.WaitForIdle();

			// Wait for the popup to close and the UI to stabilize
			// Take a screenshot of the UI after closing the comboBox
			var screenshotAfter = await TakeScreenshot(stackPanel);

			// Verify that the UI looks the same as at the beginning
			await ImageAssert.AreSimilarAsync(screenshotBefore, screenshotAfter);
		}

		[TestMethod]
		[RequiresFullWindow]
		[RunsOnUIThread]
		[Ignore("Test is not valid - the Popup is not actually rendered in the screenshots")]
		public async Task When_Mouse_Opened_And_Closed_Uwp()
		{
			using var _ = StyleHelper.UseUwpStyles();
			await When_Mouse_Opened_And_Closed();
		}

		private async Task<RawBitmap> TakeScreenshot(FrameworkElement SUT)
		{
			var renderer = new RenderTargetBitmap();
			await renderer.RenderAsync(SUT);
			var result = await RawBitmap.From(renderer, SUT);
			return result;
		}
#endif

		[TestMethod]
		public async Task When_SelectedItem_TwoWay_Binding_Clear()
		{
			var root = new Grid();

			var comboBox = new ComboBox();

			root.Children.Add(comboBox);

			comboBox.SetBinding(ComboBox.ItemsSourceProperty, new Binding { Path = new("Items") });
			comboBox.SetBinding(ComboBox.SelectedItemProperty, new Binding { Path = new("Item"), Mode = BindingMode.TwoWay });

			WindowHelper.WindowContent = root;

			var dc = new TwoWayBindingClearViewModel();
			root.DataContext = dc;

			await WindowHelper.WaitForIdle();

			dc.Dispose();
			root.DataContext = null;

			Assert.AreEqual(1, dc.ItemGetCount);
			Assert.AreEqual(1, dc.ItemSetCount);
		}

		[TestMethod]
		[CombinatorialData]
		public async Task When_ComboBox_IsTextSearchEnabled_DropDown_Closed(bool isTextSearchEnabled)
		{
			var comboBox = new ComboBox();
			comboBox.Items.Add("Cat");
			comboBox.Items.Add("Dog");
			comboBox.Items.Add("Rabbit");
			comboBox.Items.Add("Elephant");

			// Assert the default value of IsTextSearchEnabled.
			Assert.IsTrue(comboBox.IsTextSearchEnabled);

			// Set the isTextSearchEnabled value being tested.
			comboBox.IsTextSearchEnabled = isTextSearchEnabled;

			await UITestHelper.Load(comboBox);

			comboBox.Focus(FocusState.Programmatic);

			Assert.AreEqual(-1, comboBox.SelectedIndex);
			Assert.IsFalse(comboBox.IsDropDownOpen);
			await KeyboardHelper.PressKeySequence("$d$_r#$u$_r");

			var expectedSelectedIndex = isTextSearchEnabled ? 2 : -1;
			var expectedSelectedItem = isTextSearchEnabled ? "Rabbit" : null;
			Assert.AreEqual(expectedSelectedIndex, comboBox.SelectedIndex);
			Assert.AreEqual(expectedSelectedItem, comboBox.SelectedItem);
		}

		[TestMethod]
		[CombinatorialData]
		public async Task When_ComboBox_IsTextSearchEnabled_DropDown_Opened(bool isTextSearchEnabled)
		{
			var comboBox = new ComboBox();
			comboBox.Items.Add("Cat");
			comboBox.Items.Add("Dog");
			comboBox.Items.Add("Rabbit");
			comboBox.Items.Add("Elephant");
			comboBox.SelectedIndex = 3;

			// Assert the default value of IsTextSearchEnabled.
			Assert.IsTrue(comboBox.IsTextSearchEnabled);

			// Set the isTextSearchEnabled value being tested.
			comboBox.IsTextSearchEnabled = isTextSearchEnabled;

			await UITestHelper.Load(comboBox);

			comboBox.Focus(FocusState.Programmatic);

			comboBox.IsDropDownOpen = true;

			await WindowHelper.WaitForIdle();

			Assert.AreEqual(3, comboBox.SelectedIndex);
			await KeyboardHelper.PressKeySequence("$d$_r#$u$_r");

			var expectedSelectedIndex = isTextSearchEnabled ? 2 : 3;
			var expectedSelectedItem = isTextSearchEnabled ? "Rabbit" : "Elephant";
			Assert.AreEqual(expectedSelectedIndex, comboBox.SelectedIndex);
			Assert.AreEqual(expectedSelectedItem, comboBox.SelectedItem);

			Assert.IsTrue(comboBox.IsDropDownOpen);
			await KeyboardHelper.Space();

			Assert.AreEqual(isTextSearchEnabled, comboBox.IsDropDownOpen);

			await Task.Delay(1100); // Make sure to wait enough so that HasSearchStringTimedOut becomes true.

			await KeyboardHelper.Space();

			Assert.AreEqual(!isTextSearchEnabled, comboBox.IsDropDownOpen);
		}

		[TestMethod]
		[RequiresFullWindow]
		[RunsOnUIThread]
		[DataRow(PopupPlacementMode.Bottom, 0)]
		[DataRow(PopupPlacementMode.Top, 0)]
		[DataRow(PopupPlacementMode.Bottom, 20)]
		[DataRow(PopupPlacementMode.Top, -20)]
		[GitHubWorkItem("https://github.com/unoplatform/nventive-private/issues/509")]
#if RUNTIME_NATIVE_AOT
		[Ignore("DataRowAttribute.GetData() wraps data in an extra array under NativeAOT; not yet understood why.")]
#endif  // RUNTIME_NATIVE_AOT
		public async Task When_Customized_Popup_Placement(PopupPlacementMode mode, double verticalOffset)
		{
			var grid = new Grid();
			var comboBox = new PopupPlacementComboBox();
			comboBox.Margin = new Thickness(150, 150, 0, 0);
			// Add items as itmes source
			comboBox.ItemsSource = new List<string> { "Cat", "Dog" };
			comboBox.DesiredPlacement = mode;
			comboBox.VerticalOffset = verticalOffset;
			grid.Children.Add(comboBox);
			try
			{
				TestServices.WindowHelper.WindowContent = grid;
				await TestServices.WindowHelper.WaitForLoaded(comboBox);

				comboBox.ApplyPlacement();
				comboBox.IsDropDownOpen = true;

				await WindowHelper.WaitForIdle();

				var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(comboBox.XamlRoot).FirstOrDefault();
				Assert.IsNotNull(popup);

				var child = (FrameworkElement)popup.Child;
				await WindowHelper.WaitFor(() => child.ActualHeight > 0);

				var popupBounds = child.TransformToVisual(null).TransformBounds(new Rect(0, 0, child.ActualWidth, child.ActualHeight));
				var comboBoxBounds = comboBox.TransformToVisual(null).TransformBounds(new Rect(0, 0, comboBox.ActualWidth, comboBox.ActualHeight));
				// For some reason WinUI's ComboBox popup border has a -1 vertical Margin, which pushes it up by 1 pixel, and can be inacurrate due to rounding
				double tolerance = 1.5;
				if (mode == PopupPlacementMode.Bottom)
				{
					Assert.AreEqual(comboBoxBounds.Bottom + verticalOffset, popupBounds.Top, tolerance);
				}
				else
				{
					Assert.AreEqual(comboBoxBounds.Top + verticalOffset, popupBounds.Bottom, tolerance);
				}
			}
			finally
			{
				comboBox.IsDropDownOpen = false;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Items_Are_Enum_Values()
		{
			var comboBox = new ComboBox();
			comboBox.ItemsSource = Enum.GetValues(typeof(PickerLocationId)).Cast<PickerLocationId>();
			comboBox.SelectedIndex = 0;
			TestServices.WindowHelper.WindowContent = comboBox;
			await TestServices.WindowHelper.WaitForLoaded(comboBox);

			await ComboBoxHelper.OpenComboBox(comboBox, ComboBoxHelper.OpenMethod.Programmatic);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsTrue(comboBox.IsDropDownOpen);

			await TestServices.WindowHelper.WaitForIdle();

			var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(comboBox.XamlRoot).FirstOrDefault();
			Assert.IsNotNull(popup);

			var child = (FrameworkElement)popup.Child;
			var comboBoxItems = child.GetAllChildren().OfType<ComboBoxItem>().ToArray();
			Assert.AreEqual(Enum.GetValues(typeof(PickerLocationId)).Length, comboBoxItems.Length);
		}

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ComboPopup_Rearrange_ScrollShouldNotReset()
		{
			var SUT = new ComboBox()
			{
				ItemsSource = Enumerable.Range(0, 50).Select(x => $"Item {x}").ToArray(),
				SelectedIndex = 0,
			};
			await UITestHelper.Load(SUT);

			// open down-drop
			SUT.IsDropDownOpen = true;
			var host = SUT.GetTemplateChild<Border>("PopupBorder") ?? throw new InvalidOperationException("Failed to find Border#PopupBorder");
			await WindowHelper.WaitForLoaded(host);
			await UITestHelper.WaitForIdle();

			// scroll to 2 screens away
			var sv = host.Child as ScrollViewer ?? throw new InvalidOperationException("Failed to find Border#PopupBorder>ScrollViewer");
			var origin = sv.VerticalOffset;
			var destination = origin + sv.ViewportHeight * 2;
			sv.ChangeView(null, verticalOffset: destination, null, disableAnimation: true);
			await UITestHelper.WaitForIdle();
			Assert.IsTrue(Math.Abs(destination - sv.VerticalOffset) < 1.0, $"Expect sv.VerticalOffset to be near {destination:0.##}, got: {sv.VerticalOffset:0.##}");

			// force an arrange
			var cbi = sv.FindFirstChild<ComboBoxItem>();
			cbi.InvalidateArrange();
			await UITestHelper.WaitForIdle();
			Assert.IsTrue(Math.Abs(destination - sv.VerticalOffset) < 1.0, $"Expect sv.VerticalOffset to be still near {destination:0.##}, got: {sv.VerticalOffset:0.##}");
		}
#endif

#if __SKIA__ // Requires input injection
		[TestMethod]
		[RequiresFullWindow]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/15531")]
		public async Task When_Tap_Twice()
		{
			var grid = new Grid();
			var comboBox = new PopupPlacementComboBox();
			comboBox.Margin = new Thickness(200);
			// Add items as itmes source
			comboBox.ItemsSource = new List<string> { "Cat", "Dog", "Rabbit", "Elephant" };
			comboBox.DesiredPlacement = PopupPlacementMode.Bottom;
			comboBox.VerticalOffset = 50;
			grid.Children.Add(comboBox);
			try
			{
				TestServices.WindowHelper.WindowContent = grid;
				await TestServices.WindowHelper.WaitForLoaded(comboBox);

				comboBox.ApplyPlacement();
				TestServices.InputHelper.Tap(comboBox);

				await WindowHelper.WaitForIdle();

				Assert.IsTrue(comboBox.IsDropDownOpen);

				TestServices.InputHelper.Tap(comboBox);

				await WindowHelper.WaitForIdle();

				Assert.IsFalse(comboBox.IsDropDownOpen);
			}
			finally
			{
				comboBox.IsDropDownOpen = false;
			}
		}
#endif

		[TestMethod]
		public async Task When_ComboBox_Popup_Dismissed()
		{
			// test case built against https://github.com/unoplatform/uno/issues/20014

			var sut = new ComboBox()
			{
				ItemsSource = Enumerable.Range(0, 3).ToArray(),
				Visibility = Visibility.Collapsed,
			};

			// load the collapsed control into the visual tree
			await UITestHelper.Load(sut, x => x.IsLoaded);

			// then let it become visible
			sut.Visibility = Visibility.Visible;
			await UITestHelper.WaitForLoaded(sut);

			var popup = sut.FindFirstDescendantOrThrow<Popup>("Popup");

			// focus the control
			// This is required, otherwise DropDownClosed would be triggered by focus shift instead, which is not what we are testing here.
			// [ComboBox > ComboBoxItem > ComboBox] doesnt trigger that mechanism, but [other-control > ComboBoxItem > ComboBox] will do.
			// We are trying to validate if the ComboBox properly subscribes to the Popup::Closed event, and in turn raises DropDownClosed.
			sut.Focus(FocusState.Programmatic);

			// open the popup
			sut.IsDropDownOpen = true;
			await UITestHelper.WaitFor(() => popup.IsOpen, timeoutMS: 2000, "timed out waiting on the popup to open");

			// track event firing
			var dropDownClosedFired = false;
			sut.DropDownClosed += (s, e) => dropDownClosedFired = true;

			// close the popup as if the user had light-dismissed it
			popup.IsOpen = false;
			await UITestHelper.WaitForIdle();

			Assert.IsFalse(sut.IsDropDownOpen, "ComboBox.IsDropDownOpen is still true");
			Assert.IsTrue(dropDownClosedFired, "DropDownClosed event was not fired");
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeIOS | RuntimeTestPlatforms.NativeAndroid)] // https://github.com/unoplatform/uno-private/issues/1297
		public Task When_ComboBox_ScrollIntoView_SelectedItem() => When_ComboBox_ScrollIntoView_Selection(viaIndex: false);

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeIOS | RuntimeTestPlatforms.NativeAndroid)] // https://github.com/unoplatform/uno-private/issues/1297
		public Task When_ComboBox_ScrollIntoView_SelectedIndex() => When_ComboBox_ScrollIntoView_Selection(viaIndex: true);

		private async Task When_ComboBox_ScrollIntoView_Selection(bool viaIndex)
		{
			var source = ( // 12x20=240 items, enough to overflow
				from a in "qwerasdfzxcv"
				from b in Enumerable.Range(0, 20)
				select string.Concat(a, b)
			).ToArray();
			var sut = new ComboBox
			{
				ItemsSource = source,
			};
			await UITestHelper.Load(sut, x => x.IsLoaded);

			var popup = sut.FindFirstDescendantOrThrow<Popup>("Popup");

			// open the popup
			sut.IsDropDownOpen = true;
			await UITestHelper.WaitFor(() => popup.IsOpen, timeoutMS: 2000, "timed out waiting on the popup to open");

			// wait until the dropdown panel is ready
			await UITestHelper.WaitFor(() => sut.ItemsPanelRoot is { }, timeoutMS: 2000, "timed out waiting on the ItemsPanelRoot");

			// set selection
			if (viaIndex)
			{
				sut.SelectedIndex = source.Length - 3;
			}
			else
			{
				sut.SelectedItem = source[^3];
			}
			await UITestHelper.WaitForIdle();

			// sanity check
			Assert.AreEqual(source.Length - 3, sut.SelectedIndex, "SelectedIndex should be the 3rd last");
			Assert.AreEqual(source[^3], sut.SelectedItem, "SelectedItem should be the 3rd last");

			var sv = sut.ItemsPanelRoot.FindFirstAncestorOrThrow<ScrollViewer>();
			var cbi = sut.ContainerFromIndex(sut.SelectedIndex) as FrameworkElement;
			Assert.IsNotNull(cbi, "Selected container should not be null");

			var cbiAbsRect = new Rect(cbi.ActualOffset.X, cbi.ActualOffset.Y, cbi.ActualWidth, cbi.ActualHeight);
			var viewportAbsRect = new Rect(sv.HorizontalOffset, sv.VerticalOffset, sv.ViewportWidth, sv.ViewportHeight);
			var intersection = viewportAbsRect;
			intersection.Intersect(cbiAbsRect);

			Assert.IsTrue(cbiAbsRect == intersection, $"Selected container should be fully within viewport: CBI={PrettyPrint.FormatRect(cbiAbsRect)}, VP={PrettyPrint.FormatRect(viewportAbsRect)}");
		}

		public sealed class TwoWayBindingClearViewModel : IDisposable
		{
			public enum Themes
			{
				Invalid,
				Day,
				Night
			}

			public TwoWayBindingClearViewModel()
			{
				_item = Items[0];
			}

			public Themes[] Items { get; } = new Themes[] { Themes.Day, Themes.Night };
			private Themes _item;
			private bool _isDisposed;
			public Themes Item
			{
				get
				{
					ItemGetCount++;

					if (_isDisposed)
					{
						return Themes.Invalid;
					}

					return _item;
				}
				set
				{
					ItemSetCount++;

					if (_isDisposed)
					{
						_item = Themes.Invalid;
						return;
					}

					_item = value;
				}
			}

			public int ItemGetCount { get; private set; }
			public int ItemSetCount { get; private set; }

			public void Dispose()
			{
				_isDisposed = true;
			}
		}

		public class TwoWayBindingItem : System.ComponentModel.INotifyPropertyChanged
		{
			private int _selectedNumber;

			public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

			public TwoWayBindingItem()
			{
				Numbers = new List<int> { 1, 2, 3, 4 };
				SelectedNumber = 3;
			}

			public List<int> Numbers { get; }

			public int SelectedNumber
			{
				get
				{
					return _selectedNumber;
				}
				set
				{
					_selectedNumber = value;
					PropertyChanged?.Invoke(this, new(nameof(Item)));
				}
			}
		}

		public class ItemModel
		{
			public string Text { get; set; }

			public override string ToString() => Text;
		}

		public class MyObservableCollection<TType> : ObservableCollection<TType>
		{
			private int _batchUpdateCount;

			public IDisposable BatchUpdate()
			{
				++_batchUpdateCount;

				return Uno.Disposables.Disposable.Create(Release);

				void Release()
				{
					if (--_batchUpdateCount <= 0)
					{
						OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
					}
				}
			}

			protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
			{
				if (_batchUpdateCount > 0)
				{
					return;
				}

				base.OnCollectionChanged(e);
			}

			public void Append(TType item) => Add(item);

			public TType GetAt(int index) => this[index];
		}

	}


#if __APPLE_UIKIT__
	#region "Helper classes for the iOS Modal Page (UIModalPresentationStyle.pageSheet)"
	public partial class MultiFrame : Grid
	{
		private readonly TaskCompletionSource<bool> _isReady = new TaskCompletionSource<bool>();

		private CoreDispatcher _dispatcher => Dispatcher;

		public MultiFrame()
		{
			Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			_isReady.TrySetResult(true);
		}

		public async Task OpenModal(FrameSectionsTransitionInfo transitionInfo, Page page) // Runs on background thread.
		{
			var uiViewController = new UiViewController(page);

			var rootController = UIKit.UIApplication.SharedApplication.KeyWindow.RootViewController;

			await rootController.PresentViewControllerAsync(uiViewController, animated: false);
		}

		public async Task CloseModal()
		{
			try
			{
				var rootController = UIKit.UIApplication.SharedApplication.KeyWindow.RootViewController;

				await rootController.DismissViewControllerAsync(false);
			}
			catch (Exception) { /* purposely */ }
		}

		public class UiViewController : _UIViewController
		{
			public UiViewController(Page frame)
			{
				View = frame;
			}

			public UIViewControllerSectionsTransitionInfo OpeningTransitionInfo { get; set; }

			public void SetTransitionInfo(UIViewControllerSectionsTransitionInfo transitionInfo)
			{
				ModalInPresentation = !transitionInfo.AllowDismissFromGesture;
				ModalPresentationStyle = transitionInfo.ModalPresentationStyle;
				ModalTransitionStyle = transitionInfo.ModalTransitionStyle;
			}
		}

		public abstract class FrameSectionsTransitionInfo : SectionsTransitionInfo
		{
			/// <summary>
			/// The type of <see cref="FrameSectionsTransitionInfo"/>.
			/// </summary>
			public abstract FrameSectionsTransitionInfoTypes Type { get; }

			/// <summary>
			/// Gets the transition info for a suppressed transition. There is not visual animation when using this transition info.
			/// </summary>
			public static DelegatingFrameSectionsTransitionInfo SuppressTransition { get; } = new DelegatingFrameSectionsTransitionInfo(ExecuteSuppressTransition);

			/// <summary>
			/// The new frame fades in or the previous frame fades out, depending on the layering.
			/// </summary>
			public static DelegatingFrameSectionsTransitionInfo FadeInOrFadeOut { get; } = new DelegatingFrameSectionsTransitionInfo(ExecuteFadeInOrFadeOut);

			/// <summary>
			/// The new frame slides up, hiding the previous frame.
			/// </summary>
			public static DelegatingFrameSectionsTransitionInfo SlideUp { get; } = new DelegatingFrameSectionsTransitionInfo(ExecuteSlideUp);

			/// <summary>
			/// The previous frame slides down, revealing the new frame.
			/// </summary>
			public static DelegatingFrameSectionsTransitionInfo SlideDown { get; } = new DelegatingFrameSectionsTransitionInfo(ExecuteSlideDown);

			/// <summary>
			/// The frames are animated using a UIViewController with the default configuration.
			/// </summary>
			public static UIViewControllerSectionsTransitionInfo NativeiOSModal { get; } = new UIViewControllerSectionsTransitionInfo();

			private static Task ExecuteSlideDown(Frame frameToHide, Frame frameToShow, bool frameToShowIsAboveFrameToHide)
			{
				return Animations.SlideFrame1DownToRevealFrame2(frameToHide, frameToShow);
			}

			private static Task ExecuteSlideUp(Frame frameToHide, Frame frameToShow, bool frameToShowIsAboveFrameToHide)
			{
				return Animations.SlideFrame2UpwardsToHideFrame1(frameToHide, frameToShow);
			}

			private static Task ExecuteFadeInOrFadeOut(Frame frameToHide, Frame frameToShow, bool frameToShowIsAboveFrameToHide)
			{
				if (frameToShowIsAboveFrameToHide)
				{
					return Animations.FadeInFrame2ToHideFrame1(frameToHide, frameToShow);
				}
				else
				{
					return Animations.FadeOutFrame1ToRevealFrame2(frameToHide, frameToShow);
				}
			}

			private static Task ExecuteSuppressTransition(Frame frameToHide, Frame frameToShow, bool frameToShowIsAboveFrameToHide)
			{
				return Animations.CollapseFrame1AndShowFrame2(frameToHide, frameToShow);
			}
		}

		public enum FrameSectionsTransitionInfoTypes
		{
			/// <summary>
			/// The transition is applied by changing properties or animating properties of <see cref="Frame"/> objects.
			/// This is associated with the <see cref="DelegatingFrameSectionsTransitionInfo"/> class.
			/// </summary>
			FrameBased,

			/// <summary>
			/// The transition is applied by using the native iOS transitions offered by UIKit.
			/// This is associated with the <see cref="UIViewControllerSectionsTransitionInfo"/> class.
			/// </summary>
			UIViewControllerBased
		}

		public class DelegatingFrameSectionsTransitionInfo : FrameSectionsTransitionInfo
		{
			private readonly FrameSectionsTransitionDelegate _frameTranstion;

			/// <summary>
			/// Creates a new instance of <see cref="DelegatingFrameSectionsTransitionInfo"/>.
			/// </summary>
			/// <param name="frameTranstion">The method describing the transition.</param>
			public DelegatingFrameSectionsTransitionInfo(FrameSectionsTransitionDelegate frameTranstion)
			{
				_frameTranstion = frameTranstion;
			}

			///<inheritdoc/>
			public override FrameSectionsTransitionInfoTypes Type => FrameSectionsTransitionInfoTypes.FrameBased;

			/// <summary>
			/// Runs the transition.
			/// </summary>
			/// <param name="frameToHide">The <see cref="Frame"/> that must be hidden after the transition.</param>
			/// <param name="frameToShow">The <see cref="Frame"/> that must be visible after the transition.</param>
			/// <param name="frameToShowIsAboveFrameToHide">Flag indicating whether the frame to show is above the frame to hide in their parent container.</param>
			/// <returns>Task running the transition operation.</returns>
			public Task Run(Frame frameToHide, Frame frameToShow, bool frameToShowIsAboveFrameToHide)
			{
				return _frameTranstion(frameToHide, frameToShow, frameToShowIsAboveFrameToHide);
			}
		}

		public delegate Task FrameSectionsTransitionDelegate(Frame frameToHide, Frame frameToShow, bool frameToShowIsAboveFrameToHide);

		public class UIViewControllerSectionsTransitionInfo : FrameSectionsTransitionInfo
		{
			public UIViewControllerSectionsTransitionInfo(bool allowDismissFromGesture = true, UIKit.UIModalPresentationStyle modalPresentationStyle = UIKit.UIModalPresentationStyle.PageSheet, UIKit.UIModalTransitionStyle modalTransitionStyle = UIKit.UIModalTransitionStyle.CoverVertical)
			{
				AllowDismissFromGesture = allowDismissFromGesture;
				ModalPresentationStyle = modalPresentationStyle;
				ModalTransitionStyle = modalTransitionStyle;
			}

			public bool AllowDismissFromGesture { get; }

			public UIKit.UIModalPresentationStyle ModalPresentationStyle { get; }

			public UIKit.UIModalTransitionStyle ModalTransitionStyle { get; }

			public override FrameSectionsTransitionInfoTypes Type => FrameSectionsTransitionInfoTypes.UIViewControllerBased;
		}

		public static class Animations
		{
			/// <summary>
			/// The default duration of built-in animations, in seconds.
			/// </summary>
			public const double DefaultDuration = 0.250;

			/// <summary>
			/// Fades out <paramref name="frame1"/> to reveal <paramref name="frame2"/>.
			/// </summary>
			public static Task FadeOutFrame1ToRevealFrame2(Frame frame1, Frame frame2)
			{
				// 1. Disable the currently visible frame during the animation.
				frame1.IsHitTestVisible = false;

				// 2. Make the next frame visible so that we see it as the previous frame fades out.
				frame2.Opacity = 1;

				frame2.Visibility = Visibility.Visible;
				frame2.IsHitTestVisible = true;

				// 3. Fade out the frame.
				var storyboard = new Storyboard();
				AddFadeOut(storyboard, frame1);
				storyboard.Begin();

				return Task.CompletedTask;
			}

			/// <summary>
			/// Fades in <paramref name="frame1"/> to hide <paramref name="frame2"/>.
			/// </summary>
			public static Task FadeInFrame2ToHideFrame1(Frame frame1, Frame frame2)
			{
				// 1. Disable the currently visible frame during the animation.
				frame1.IsHitTestVisible = false;

				// 2. Make the next frame visible, but transparent.
				frame2.Opacity = 0;
				frame2.Visibility = Visibility.Visible;

				// 3. Fade in the frame.
				var storyboard = new Storyboard();
				AddFadeIn(storyboard, frame2);
				storyboard.Begin();

				// 4. Once the next frame is visible, enable it.
				frame2.IsHitTestVisible = true;

				return Task.CompletedTask;
			}

			/// <summary>
			/// Slides <paramref name="frame2"/> upwards to hide <paramref name="frame1"/>.
			/// </summary>
			public static Task SlideFrame2UpwardsToHideFrame1(Frame frame1, Frame frame2)
			{
				frame1.IsHitTestVisible = false;
				((TranslateTransform)frame2.RenderTransform).Y = frame1.ActualHeight;
				frame2.Opacity = 1;
				frame2.Visibility = Visibility.Visible;

				var storyboard = new Storyboard();
				AddSlideInFromBottom(storyboard, (TranslateTransform)frame2.RenderTransform);
				storyboard.Begin();

				frame2.IsHitTestVisible = true;

				return Task.CompletedTask;
			}

			/// <summary>
			/// Slides down <paramref name="frame1"/> to releave <paramref name="frame2"/>.
			/// </summary>
			public static Task SlideFrame1DownToRevealFrame2(Frame frame1, Frame frame2)
			{
				frame1.IsHitTestVisible = false;
				frame2.Opacity = 1;
				frame2.Visibility = Visibility.Visible;

				var storyboard = new Storyboard();
				AddSlideBackToBottom(storyboard, (TranslateTransform)frame1.RenderTransform, frame2.ActualHeight);
				storyboard.Begin();

				frame2.IsHitTestVisible = true;

				return Task.CompletedTask;
			}

			/// <summary>
			/// Collapses <paramref name="frame1"/> and make <paramref name="frame2"/> visible.
			/// </summary>
			public static Task CollapseFrame1AndShowFrame2(Frame frame1, Frame frame2)
			{
				frame1.Visibility = Visibility.Collapsed;
				frame2.IsHitTestVisible = false;

				frame2.Visibility = Visibility.Visible;
				frame2.Opacity = 1;
				frame2.IsHitTestVisible = true;

				return Task.CompletedTask;
			}

			private static void AddFadeIn(Storyboard storyboard, DependencyObject target)
			{
				var animation = new DoubleAnimation()
				{
					To = 1,
					Duration = new Duration(TimeSpan.FromSeconds(DefaultDuration)),
					EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseInOut }
				};

				Storyboard.SetTarget(animation, target);
				Storyboard.SetTargetProperty(animation, "Opacity");

				storyboard.Children.Add(animation);
			}

			private static void AddFadeOut(Storyboard storyboard, DependencyObject target)
			{
				var animation = new DoubleAnimation()
				{
					To = 0,
					Duration = new Duration(TimeSpan.FromSeconds(DefaultDuration)),
					EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseInOut }
				};

				Storyboard.SetTarget(animation, target);
				Storyboard.SetTargetProperty(animation, "Opacity");

				storyboard.Children.Add(animation);
			}

			private static void AddSlideInFromBottom(Storyboard storyboard, TranslateTransform target)
			{
				var animation = new DoubleAnimation()
				{
					To = 0,
					Duration = new Duration(TimeSpan.FromSeconds(DefaultDuration)),
					EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseOut }
				};

				Storyboard.SetTarget(animation, target);
				Storyboard.SetTargetProperty(animation, "Y");

				storyboard.Children.Add(animation);
			}

			private static void AddSlideBackToBottom(Storyboard storyboard, TranslateTransform target, double translation)
			{
				var animation = new DoubleAnimation()
				{
					To = translation,
					Duration = new Duration(TimeSpan.FromSeconds(DefaultDuration)),
					EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseOut }
				};

				Storyboard.SetTarget(animation, target);
				Storyboard.SetTargetProperty(animation, "Y");

				storyboard.Children.Add(animation);
			}
		}

		public abstract class SectionsTransitionInfo
		{
		}
	}
	#endregion
#endif
}
