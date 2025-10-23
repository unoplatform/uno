using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.FlipViewPages;
using Windows.Foundation.Metadata;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using static Private.Infrastructure.TestServices;
using Windows.UI.Input.Preview.Injection;
using Uno.Extensions;
using Windows.Foundation;
using Uno.UI.Toolkit.DevTools.Input;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_FlipView
	{
		[TestMethod]
		public async Task When_Observable_ItemsSource_And_Added()
		{
			var itemsSource = new ObservableCollection<string>();
			AddItem(itemsSource);
			AddItem(itemsSource);
			AddItem(itemsSource);

			var flipView = new FlipView
			{
				ItemsSource = itemsSource
			};

			WindowHelper.WindowContent = flipView;

			await WindowHelper.WaitForLoaded(flipView);

			Assert.AreEqual(0, flipView.SelectedIndex);

			flipView.SelectedIndex = 1;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, flipView.SelectedIndex);

			AddItem(itemsSource);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, flipView.SelectedIndex);
		}

		[TestMethod]
		[RequiresFullWindow]
		public async Task When_Given_Infinite_Width()
		{
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
			{
				Assert.Inconclusive(); // "System.NotImplementedException: RenderTargetBitmap is not supported on this platform.";
			}

			var stackPanel = new StackPanel()
			{
				Children =
				{
					new FlipView()
					{
						Height = 100,
						Items =
						{
							new Grid
							{
								Background = new SolidColorBrush(Colors.Red),
							},
							new Grid
							{
								Background = new SolidColorBrush(Colors.Green),
							},
							new Grid
							{
								Background = new SolidColorBrush(Colors.Blue),
							},
						}
					}
				},
			};

			await UITestHelper.Load(stackPanel);
			var bitmap = await UITestHelper.ScreenShot(stackPanel);
			var redBounds = ImageAssert.GetColorBounds(bitmap, Microsoft.UI.Colors.Red);
			Assert.AreEqual(redBounds.Width, bitmap.Width - 1);
			ImageAssert.DoesNotHaveColorInRectangle(bitmap, new(default, new(bitmap.Width, bitmap.Height)), Microsoft.UI.Colors.Green);
			ImageAssert.DoesNotHaveColorInRectangle(bitmap, new(default, new(bitmap.Width, bitmap.Height)), Microsoft.UI.Colors.Blue);
		}

		[TestMethod]
		public async Task When_Background_Color()
		{
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.
			}

			var parent = new Border()
			{
				Width = 300,
				Height = 300,
				Background = new SolidColorBrush(Colors.Green)
			};
			var SUT = new FlipView
			{
				Background = new SolidColorBrush(Colors.Red),
				Width = 200,
				Height = 200
			};
			parent.Child = SUT;

			WindowHelper.WindowContent = parent;
			await WindowHelper.WaitForLoaded(parent);

			var snapshot = await TakeScreenshot(parent);

			var coords = parent.GetRelativeCoords(SUT); // logical
			var center = new Point(coords.CenterX, coords.CenterY); // logical
#if __ANDROID__
			// droid-specific: the snapshot size is in physical size, NOT logical
			// so the coords needs to be converted into physical to be against the snapshot.
			center = ViewHelper.LogicalToPhysicalPixels(center); // physical
#endif

			ImageAssert.HasPixels(snapshot, ExpectedPixels
				.At((int)center.X, (int)center.Y)
				.Named("center with color")
				.Pixel(Colors.Red)
			);
		}

		[TestMethod]
		public async Task When_Inline_Items_SelectedIndex()
		{
			var flipView = new FlipView
			{
				Items =
				{
					new FlipViewItem {Content = "Inline item 1"},
					new FlipViewItem {Content = "Inline item 2"},
				}
			};

			WindowHelper.WindowContent = flipView;

			await WindowHelper.WaitForLoaded(flipView);

			Assert.AreEqual(0, flipView.SelectedIndex);

			flipView.SelectedIndex = 1;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, flipView.SelectedIndex);
		}

		private static void AddItem(ObservableCollection<string> items)
		{
			items.Add($"Item {items.Count + 1}");
		}



		[TestMethod]
#if __APPLE_UIKIT__
		[Ignore("Currently fails on iOS https://github.com/unoplatform/uno/issues/9080")]
#endif
		public async Task When_Flipview_Items_Modified()
		{
			var itemsSource = new ObservableCollection<string>();
			AddItem(itemsSource);
			AddItem(itemsSource);
			AddItem(itemsSource);

			var flipView = new FlipView
			{
				ItemsSource = itemsSource
			};

			WindowHelper.WindowContent = flipView;

			await WindowHelper.WaitForLoaded(flipView);

			await WindowHelper.WaitForResultEqual(0, () => flipView.SelectedIndex);

			flipView.SelectedItem = itemsSource[2];

			await WindowHelper.WaitForResultEqual(2, () => flipView.SelectedIndex);

			itemsSource.RemoveAt(2);

			await WindowHelper.WaitForResultEqual(0, () => flipView.SelectedIndex);

			itemsSource.Clear();

			await WindowHelper.WaitForResultEqual(-1, () => flipView.SelectedIndex);

		}

#if __ANDROID__
		[TestMethod]
		public async Task When_NativeChild_Clipped()
		{
			var flipView = new FlipView
			{
				Items =
				{
					new FlipViewItem {Content = "Inline item 1"},
					new FlipViewItem {Content = "Inline item 2"},
				}
			};

			WindowHelper.WindowContent = flipView;

			await WindowHelper.WaitForLoaded(flipView);

			var nativeChild = flipView.FindFirstChild<NativePagedView>();

			Assert.IsNotNull(nativeChild);
			Assert.IsTrue(flipView.ClipChildren);
		}
#endif

		private async Task<RawBitmap> TakeScreenshot(FrameworkElement SUT)
		{
			var renderer = new RenderTargetBitmap();
			await WindowHelper.WaitForIdle();
			await renderer.RenderAsync(SUT);
			var result = await RawBitmap.From(renderer, SUT);
			await WindowHelper.WaitForIdle();
			return result;
		}

		[TestMethod]
#if __APPLE_UIKIT__
		[Ignore("Currently fails on iOS, https://github.com/unoplatform/uno/issues/9080")]
#endif
		public async Task When_Flipview_DataTemplateSelector()
		{
			var dataContext = new When_Flipview_DataTemplateSelector_DataContext();

			var page = new FlipView_TemplateSelectorPage();
			page.DataContext = dataContext;

			WindowHelper.WindowContent = page;
			await WindowHelper.WaitForLoaded(page);
			FlipView SUT = page.MyFlipView;

			static string GetTextBlockName(object item)
			{
				return Convert.ToInt32(item) % 2 == 0 ? "TextEven" : "TextOdd";
			}

			static void AssertTextBlock(TextBlock textblock, object item)
			{
				Assert.IsNotNull(textblock, "The Template applied wasn't the expected");
				Assert.AreEqual(item.ToString(), textblock?.Text, "The TextBlock doesn't have the expected value");
			}


#if __WASM__ || __SKIA__
			var flipViewItems = (SUT as FrameworkElement)?.FindChildren<FlipViewItem>()?.ToArray() ?? new FlipViewItem[0];

			for (var i = 0; i < SUT.Items.Count; i++)
			{
				if (SUT.SelectedIndex != i)
				{
					SUT.SelectedIndex = i;
					await WindowHelper.WaitForIdle();
				}

				var item = SUT.Items[i];
				Assert.AreEqual(SUT.SelectedIndex, i, "SelectedIndex isn't the expected value");

				var textBlockName = GetTextBlockName(item);

				var textblock = flipViewItems[i].FindName(textBlockName) as TextBlock;
				AssertTextBlock(textblock, item);
			}
#else
			for (var i = 0; i < SUT.Items.Count; i++)
			{
				if (SUT.SelectedIndex != i)
				{
					SUT.SelectedIndex = i;
					await WindowHelper.WaitForIdle();
				}

				var item = SUT.Items[i];
				Assert.AreEqual(SUT.SelectedIndex, i, "SelectedIndex isn't the expected value");

				var textBlockName = GetTextBlockName(item);

				var textblock = SUT.FindName(textBlockName) as TextBlock;
				AssertTextBlock(textblock, item);
			}
#endif
		}

#if __WASM__
		[TestMethod]
		public async Task When_Multiple_Items_Should_Not_Scroll()
		{
			var itemsSource = new ObservableCollection<string>();
			AddItem(itemsSource);
			AddItem(itemsSource);
			AddItem(itemsSource);

			var flipView = new FlipView
			{
				Width = 100,
				Height = 100,
				ItemsSource = itemsSource,
			};

			WindowHelper.WindowContent = flipView;
			await WindowHelper.WaitForLoaded(flipView);
			var scrollViewer = (ScrollViewer)flipView.GetTemplateChild("ScrollingHost");
			var border = (Border)VisualTreeHelper.GetChildren(scrollViewer).Single();
			var grid = (Grid)VisualTreeHelper.GetChildren(border).Single();
			var scrollContentPresenter = (ScrollContentPresenter)VisualTreeHelper.GetChildren(grid).First();
			var classes = Uno.Foundation.WebAssemblyRuntime.InvokeJS($"document.getElementById({scrollContentPresenter.HtmlId}).classList");
			var classesArray = classes.Split(' ');
			Assert.IsTrue(classesArray.Contains("scroll-x-hidden"), $"Classes found: {classes}");
			Assert.IsTrue(classesArray.Contains("scroll-y-disabled"), $"Classes found: {classes}");
		}
#endif

		private sealed class FlipViewVM : INotifyPropertyChanged
		{
			private int _index = 0;

			public event PropertyChangedEventHandler PropertyChanged;

			public int Index
			{
				get => _index;
				set
				{
					if (_index != value)
					{
						_index = value;
						OnPropertyChanged(nameof(Index));
					}
				}
			}
			private void OnPropertyChanged(string propertyName)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}

		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/20571")]
		public async Task When_Decimal_Size()
		{
			var flipView = new FlipView
			{
				Width = 301.333,
				Items =
				{
					new FlipViewItem
					{
						Content = new Grid
						{
							Background = new SolidColorBrush(Colors.LightCoral),
							Children =
							{
								new TextBlock
								{
									Text = "First item",
									Foreground = new SolidColorBrush(Colors.White)
								}
							}
						}
					},
					new FlipViewItem
					{
						Content = new Grid
						{
							Background = new SolidColorBrush(Colors.LightSeaGreen),
							Children =
							{
								new TextBlock
								{
									Text = "Second item",
									Foreground = new SolidColorBrush(Colors.White)
								}
							}
						}
					},
					new FlipViewItem
					{
						Content = new Grid
						{
							Background = new SolidColorBrush(Colors.Gold),
							Children =
							{
								new TextBlock
								{
									Text = "Third item",
									Foreground = new SolidColorBrush(Colors.Black)
								}
							}
						}
					}
				}
			};

			WindowHelper.WindowContent = flipView;
			await WindowHelper.WaitForLoaded(flipView);

			flipView.SelectedIndex = 2;
			await WindowHelper.WaitForIdle();
			await Task.Delay(300);

			Assert.AreEqual(2, flipView.SelectedIndex);
		}

		[TestMethod]
		public async Task When_Navigate_Skips_An_Item()
		{
			var flipView = new FlipView
			{
				Width = 500,
				Height = 500,
				Items =
				{
					new Grid
					{
						Background = new SolidColorBrush(Colors.Azure),
					},
					new Grid
					{
						Background = new SolidColorBrush(Colors.Blue),
					},
					new Grid
					{
						Background = new SolidColorBrush(Colors.Yellow),
					},
					new Grid
					{
						Background = new SolidColorBrush(Colors.Fuchsia),
					},
				}
			};

			var horizontalPanel = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				HorizontalAlignment = HorizontalAlignment.Center,
				Children =
				{
					new RadioButton()
					{
						Content = "Page 1",
						Tag = 0,
					},
					new RadioButton()
					{
						Content = "Page 2",
						Tag = 1,
					},
					new RadioButton()
					{
						Content = "Page 3",
						Tag = 2,
					},
					new RadioButton()
					{
						Content = "Page 4",
						Tag = 3,
					},
				}
			};

			flipView.SelectionChanged += FlipView_SelectionChanged;
			flipView.DataContext = new FlipViewVM();
			flipView.SetBinding(FlipView.SelectedIndexProperty, new Binding() { Path = new("Index"), Mode = BindingMode.TwoWay });

			foreach (RadioButton b in horizontalPanel.Children)
			{
				b.Checked += RadioButton_Checked;
			}

			var panel = new StackPanel
			{
				Children =
				{
					flipView,
					horizontalPanel,
				},
			};
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(panel);

			Assert.AreEqual(0, flipView.SelectedIndex);
			Assert.IsTrue(((RadioButton)horizontalPanel.Children[0]).IsChecked);

			((RadioButton)horizontalPanel.Children[2]).IsChecked = true;

			// Using WaitForIdle wouldn't make the test fail when the bug occurs.
			// The bug being tested is related to native scrolling event that breaks things.
			// So, we have to wait long enough to ensure FlipView is working correctly.
			await Task.Delay(1000);

			Assert.AreEqual(2, flipView.SelectedIndex);
			Assert.IsTrue(((RadioButton)horizontalPanel.Children[2]).IsChecked);

			void RadioButton_Checked(object sender, RoutedEventArgs e)
			{
				if (sender is RadioButton rb)
				{
					if (int.TryParse(rb!.Tag.ToString(), out var result))
					{
						flipView.SelectedIndex = result;
					}
				}
			}

			void FlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
			{
				UpdateRadioButtonSelection(flipView.SelectedIndex);
			}

			void UpdateRadioButtonSelection(int selectedIndex)
			{
				if (selectedIndex < 0)
				{
					return;
				}

				var radioButton = (RadioButton)horizontalPanel.Children[selectedIndex];
				radioButton.IsChecked = true;
			}

		}

#if HAS_UNO
		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_ScrollWheel()
		{
			var flipView = new FlipView()
			{
				Width = 100,
				Height = 100,
				Items =
				{
					new Border
					{
						Width = 100,
						Height = 100,
						Background = new SolidColorBrush(Microsoft.UI.Colors.Red),
					},
					new Border
					{
						Width = 100,
						Height = 100,
						Background = new SolidColorBrush(Microsoft.UI.Colors.Green),
					},
					new Border
					{
						Width = 100,
						Height = 100,
						Background = new SolidColorBrush(Microsoft.UI.Colors.Blue),
					},
				}
			};

			var rect = await UITestHelper.Load(flipView);

			Assert.AreEqual(0, flipView.SelectedIndex);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			var mouse = injector.GetMouse();
			mouse.MoveTo(rect.GetCenter().X, rect.GetCenter().Y);
			const int FLIP_VIEW_DISTINCT_SCROLL_WHEEL_DELAY_MS = 200 + 50; // 50ms margin to reduce flakiness
			await Task.Delay(FLIP_VIEW_DISTINCT_SCROLL_WHEEL_DELAY_MS);
			mouse.WheelDown();
			Assert.AreEqual(1, flipView.SelectedIndex);
			await Task.Delay(FLIP_VIEW_DISTINCT_SCROLL_WHEEL_DELAY_MS);
			mouse.WheelDown();
			Assert.AreEqual(2, flipView.SelectedIndex);
			await Task.Delay(FLIP_VIEW_DISTINCT_SCROLL_WHEEL_DELAY_MS);
			mouse.WheelDown();
			Assert.AreEqual(2, flipView.SelectedIndex);
			await Task.Delay(FLIP_VIEW_DISTINCT_SCROLL_WHEEL_DELAY_MS);
			mouse.WheelUp();
			Assert.AreEqual(1, flipView.SelectedIndex);
			await Task.Delay(FLIP_VIEW_DISTINCT_SCROLL_WHEEL_DELAY_MS);
			mouse.WheelUp();
			Assert.AreEqual(0, flipView.SelectedIndex);
			await Task.Delay(FLIP_VIEW_DISTINCT_SCROLL_WHEEL_DELAY_MS);
			mouse.WheelUp();
			Assert.AreEqual(0, flipView.SelectedIndex);
		}

		[TestMethod]
#if __WASM__
		[Ignore("Scrolling is handled by native code and InputInjector is not yet able to inject native pointers.")]
#elif !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_TouchMoveLessThanHalfItem_Then_DoNotFlip()
		{
			var flipView = new FlipView()
			{
				Width = 100,
				Height = 100,
				Items =
				{
					new Border
					{
						Width = 100,
						Height = 100,
						Background = new SolidColorBrush(Microsoft.UI.Colors.Red),
					},
					new Border
					{
						Width = 100,
						Height = 100,
						Background = new SolidColorBrush(Microsoft.UI.Colors.Green),
					},
					new Border
					{
						Width = 100,
						Height = 100,
						Background = new SolidColorBrush(Microsoft.UI.Colors.Blue),
					},
				}
			};

			var rect = await UITestHelper.Load(flipView);

			Assert.AreEqual(0, flipView.SelectedIndex);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			var finger = injector.GetFinger();

			finger.Drag(
				from: new(rect.Right - 10, rect.GetCenter().Y),
				to: new(rect.Right - 30 /* less than center */, rect.GetCenter().Y),
				steps: 5,
				stepOffsetInMilliseconds: 500);

			await UITestHelper.WaitForIdle();

			Assert.AreEqual(0, flipView.SelectedIndex);
		}

		[TestMethod]
#if __WASM__
		[Ignore("Scrolling is handled by native code and InputInjector is not yet able to inject native pointers.")]
#elif !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#elif __SKIA__
		[Ignore("Changes on the ScrollCrontentPresenter made this test to fail. We will look in a separate issue: uno-private#1410")]
#endif
		public async Task When_TouchMoveMoreThanHalfItem_Then_FlipOneItem()
		{
			var flipView = new FlipView()
			{
				Width = 100,
				Height = 100,
				Items =
				{
					new Border
					{
						Width = 100,
						Height = 100,
						Background = new SolidColorBrush(Microsoft.UI.Colors.Red),
					},
					new Border
					{
						Width = 100,
						Height = 100,
						Background = new SolidColorBrush(Microsoft.UI.Colors.Green),
					},
					new Border
					{
						Width = 100,
						Height = 100,
						Background = new SolidColorBrush(Microsoft.UI.Colors.Blue),
					},
				}
			};

			var rect = await UITestHelper.Load(flipView);

			Assert.AreEqual(0, flipView.SelectedIndex);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			var finger = injector.GetFinger();

			finger.Drag(
				from: new(rect.Right - 10, rect.GetCenter().Y),
				to: new(rect.Left - 10, rect.GetCenter().Y),
				steps: 5,
				stepOffsetInMilliseconds: 500);

			await UITestHelper.WaitForIdle();

			Assert.AreEqual(1, flipView.SelectedIndex);
		}

		[TestMethod]
#if __WASM__
		[Ignore("Scrolling is handled by native code and InputInjector is not yet able to inject native pointers.")]
#elif !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_TouchFlick_Then_FlipOneItem()
		{
			var flipView = new FlipView()
			{
				Width = 100,
				Height = 100,
				Items =
				{
					new Border
					{
						Width = 100,
						Height = 100,
						Background = new SolidColorBrush(Microsoft.UI.Colors.Red),
					},
					new Border
					{
						Width = 100,
						Height = 100,
						Background = new SolidColorBrush(Microsoft.UI.Colors.Green),
					},
					new Border
					{
						Width = 100,
						Height = 100,
						Background = new SolidColorBrush(Microsoft.UI.Colors.Blue),
					},
				}
			};

			var rect = await UITestHelper.Load(flipView);

			Assert.AreEqual(0, flipView.SelectedIndex);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			var finger = injector.GetFinger();

			finger.Drag(
				from: new(rect.Right - 10, rect.GetCenter().Y),
				to: new(rect.Right - 30 /* less than center */, rect.GetCenter().Y),
				steps: 5,
				stepOffsetInMilliseconds: 1);

			await UITestHelper.WaitForIdle();

			await Task.Delay(2000); //waiting the drag animation to complete

			Assert.AreEqual(1, flipView.SelectedIndex);
		}
#endif
	}

#if __SKIA__ || __WASM__
	static class Extensions
	{
		internal static IEnumerable<T> FindChildren<T>(this FrameworkElement root) where T : FrameworkElement
		{
			return root.GetDescendants().OfType<T>().ToArray();
		}

		private static IEnumerable<FrameworkElement> GetDescendants(this FrameworkElement root)
		{
			foreach (var child in root._children)
			{
				yield return child as FrameworkElement;

				foreach (var descendant in (child as FrameworkElement).GetDescendants())
				{
					yield return descendant;
				}
			}
		}
	}
#endif

	public class When_Flipview_DataTemplateSelector_DataContext
	{
		public IEnumerable<int> Items { get; set; } = Enumerable.Range(1, 6);
	}
}
