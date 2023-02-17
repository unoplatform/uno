using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using static Private.Infrastructure.TestServices;
using System.Collections.ObjectModel;

#if NETFX_CORE
using Uno.UI.Extensions;
#elif __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#else
using Uno.UI;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_ComboBox
	{
		private ResourceDictionary _testsResources;

		private Style CounterComboBoxContainerStyle => _testsResources["CounterComboBoxContainerStyle"] as Style;

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

		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
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
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
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
					Assert.AreEqual(expectedItemWidth, cbi.ActualWidth);
				}
			}
			finally
			{
				SUT.IsDropDownOpen = false;
				WindowHelper.WindowContent = null;
			}
		}

		/// <remarks>
		/// This test is memory sensitive - if the memory usage exceeds FrameworkTemplatePool.HighMemoryThreshold,
		/// the test will most likely faily as the template will be recycled. Currently the Samples app uses
		/// HighMemoryThreshold of 0.9 (see App.ConfigureFeatureFlags).
		/// </remarks>
		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
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

#if !__ANDROID__ && !__IOS__ // This does not hold on Android or iOS, possibly because ComboBox is not virtualized
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
#if __IOS__ || __ANDROID__
		[Ignore("ComboBox is currently not virtualized on iOS and Android - #556")] // https://github.com/unoplatform/uno/issues/556
#endif
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
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

		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
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

#if __IOS__
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
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_Fluent_And_Theme_Changed()
		{
			using (StyleHelper.UseFluentStyles())
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
			Assert.ThrowsException<ArgumentException>(() => comboBox.SelectedIndex = 2);
		}

		[TestMethod]
		public void When_Index_Set_Negative_Out_Of_Range_When_Items_Exist()
		{
			var comboBox = new ComboBox();
			comboBox.Items.Add(new ComboBoxItem());
			Assert.ThrowsException<ArgumentException>(() => comboBox.SelectedIndex = -2);
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

				Assert.AreEqual(SUT.Items.Count, 3);

				using (c.BatchUpdate())
				{
					c.Add("Four");
					c.Add("Five");
				}

				SUT.IsDropDownOpen = true;

				// Items are materialized when the popup is opened
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(SUT.Items.Count, 5);
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

				SUT.SetBinding(ComboBox.ItemsSourceProperty, new Windows.UI.Xaml.Data.Binding() { Path = new("MySource") });
				SUT.SetBinding(ComboBox.SelectedItemProperty, new Windows.UI.Xaml.Data.Binding() { Path = new("SelectedItem"), Mode = Windows.UI.Xaml.Data.BindingMode.TwoWay });

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

				Assert.AreEqual(SUT.Items.Count, 12);
			}
			finally
			{
				SUT.IsDropDownOpen = false;
			}
		}

#if HAS_UNO
		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_Full_Collection_Reset()
		{
			var SUT = new ComboBox();
			SUT.ItemTemplate = new DataTemplate(() =>
			{

				var tb = new TextBlock();
				tb.SetBinding(TextBlock.TextProperty, new Windows.UI.Xaml.Data.Binding { Path = "Text" });
				tb.SetBinding(TextBlock.NameProperty, new Windows.UI.Xaml.Data.Binding { Path = "Text" });

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

				Assert.AreEqual(SUT.Items.Count, 3);

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
}
