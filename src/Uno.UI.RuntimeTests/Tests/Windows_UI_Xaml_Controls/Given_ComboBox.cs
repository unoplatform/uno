using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using static Private.Infrastructure.TestServices;
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

				Assert.AreEqual(Colors.Black, (placeholderTextBlock.Foreground as SolidColorBrush)?.Color);

				using (ThemeHelper.UseDarkTheme())
				{
					Assert.AreEqual(Colors.White, (placeholderTextBlock.Foreground as SolidColorBrush)?.Color);
				}

				Assert.AreEqual(Colors.Black, (placeholderTextBlock.Foreground as SolidColorBrush)?.Color);
			}
		}
	}
}
