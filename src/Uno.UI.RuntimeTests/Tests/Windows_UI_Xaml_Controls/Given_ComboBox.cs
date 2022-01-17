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
		public async Task When_Index_Is_Removed_Check_Reindex()
		{
			var SUT = new ComboBox();
			var source = new ObservableCollection<string>();
			for (int i = 0; i <= 5; i++)
			{
				InsertAnItem(i);
			}

			SUT.ItemsSource = source;

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			SUT.IsDropDownOpen = true;

			// Items are materialized when the popup is opened
			await WindowHelper.WaitForIdle();

			//Confirm count before RemoveAt
			Assert.AreEqual(SUT.Items.Count, 6);

			//Removed index == 2
			source.RemoveAt(2);

			await Task.Delay(5);

			//Verify if SelectedIndex was reseted
			Assert.AreEqual(-1, SUT.SelectedIndex);

			//Confirm count after RemoveAt
			Assert.AreEqual(SUT.Items.Count, 5);

			//Items that continuous existing on source
			Assert.IsNotNull(SUT.ContainerFromItem("Item #0"));
			Assert.IsNotNull(SUT.ContainerFromItem("Item #1"));
			Assert.IsNotNull(SUT.ContainerFromItem("Item #3"));
			Assert.IsNotNull(SUT.ContainerFromItem("Item #4"));
			Assert.IsNotNull(SUT.ContainerFromItem("Item #5"));

			//Item removed is null
			Assert.IsNull(SUT.ContainerFromItem("Item #2"));

			void InsertAnItem(int index)
			{
				source.Add($"Item #{index}");
			}
		}
		/// <summary>
		/// Skia and WebAssembly issue https://github.com/unoplatform/uno/issues/8204
		/// </summary>
		/// <returns></returns>
#if (!__SKIA__ && !__WASM__)
		[TestMethod]
		public async Task When_ComboBoxItem_Is_Removed_Check_index_NotWrong()
		{
			var SUT = new ComboBox();
			var source = new ObservableCollection<string>();
			for (int i = 0; i <= 5; i++)
			{
				InsertAnItem(i);
			}

			SUT.ItemsSource = source;

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			SUT.IsDropDownOpen = true;

			// Items are materialized when the popup is opened
			await WindowHelper.WaitForIdle();

			//Removed index == 2
			source.RemoveAt(2);

			await Task.Delay(5);

			//Verify if SelectedIndex was reseted
			Assert.AreEqual(-1, SUT.SelectedIndex);

			ComboBoxItem cbi = null;
			await WindowHelper.WaitFor(() => (cbi = SUT.ContainerFromIndex(4) as ComboBoxItem) != null); 
			await WindowHelper.WaitForLoaded(cbi); 
			cbi.IsSelected = true;

			//Verify if the index don't continuous -1 after selected new item
			Assert.AreNotEqual(-1, SUT.SelectedIndex);

			//Verify if the content has a correct value after selected item
			Assert.AreEqual("Item #5", cbi.Content);

			//Verify the quantity of source
			Assert.AreEqual(5, source.Count);

			//Verify the quantity of ItemsPanelRoot
			Assert.AreEqual(5, SUT.ItemsPanelRoot.Children.Count);

			void InsertAnItem(int index)
			{
				source.Add($"Item #{index}");
			}
		}
#endif

		[TestMethod]
		[Ignore] // https://github.com/unoplatform/uno/issues/4686
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
		[Ignore] // https://github.com/unoplatform/uno/issues/4686
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
