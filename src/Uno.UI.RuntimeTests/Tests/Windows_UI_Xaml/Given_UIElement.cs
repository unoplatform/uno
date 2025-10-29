#if __CROSSRUNTIME__
#define MEASURE_DIRTY_PATH_AVAILABLE
#define ARRANGE_DIRTY_PATH_AVAILABLE
#elif __ANDROID__
#define MEASURE_DIRTY_PATH_AVAILABLE
#endif

using System;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using AwesomeAssertions.Execution;
using Private.Infrastructure;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using SamplesApp.UITests;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Point = System.Drawing.Point;
using Uno.UI;
using Windows.UI;
using Windows.ApplicationModel.Appointments;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Hosting;
using Uno.UI.Toolkit.Extensions;
using KeyEventArgs = Windows.UI.Core.KeyEventArgs;
using Combinatorial.MSTest;
using Uno.UI.Toolkit.DevTools.Input;

#if !HAS_UNO_WINUI
using Windows.UI.Input;
#endif

#if __APPLE_UIKIT__
using UIKit;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	public partial class Given_UIElement
	{
		private partial class ButtonChangingClipDuringArrange : Button
		{
			protected override Size ArrangeOverride(Size finalSize)
			{
				Clip = new RectangleGeometry()
				{
					Rect = new Rect(0, 0, 100, 10),
					Transform = new TranslateTransform(),
				};
				return base.ArrangeOverride(finalSize);
			}
		}

		[TestMethod]
		[RunsOnUIThread]
#if __ANDROID__ || __APPLE_UIKIT__
		[Ignore("LayoutStorage not implemented properly for Layouter")]
#endif
		public async Task When_Not_In_Visual_Tree_Should_Reset_LayoutStorage()
		{
			var SUT = new TextBox { Text = "Some text", Margin = new Thickness(10) };
			var border = new Border { Child = SUT };

			await UITestHelper.Load(border);

			Assert.AreNotEqual(default, SUT.DesiredSize);

			TestServices.WindowHelper.WindowContent = null;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(default, SUT.DesiredSize);
		}

		[TestMethod]
		[RunsOnUIThread]
		[DataRow(200)]
		[DataRow(10)]
		public async Task When_Both_Layouting_Clip_And_Clip_DP(double newClipValue)
		{
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var SUT = new Rectangle()
			{
				Fill = new SolidColorBrush(Microsoft.UI.Colors.Red),
				Width = 100,
				Height = 100,
			};

			var grid = new Grid()
			{
				Width = 200,
				Height = 75,
				Children =
				{
					new Rectangle()
					{
						Fill = new SolidColorBrush(Microsoft.UI.Colors.Green),
					},
					SUT,
				},
			};

			await UITestHelper.Load(grid);
			var screenshot = await UITestHelper.ScreenShot(grid);

			var greenBounds = ImageAssert.GetColorBounds(screenshot, Microsoft.UI.Colors.Green, tolerance: 5);
			Assert.AreEqual(new Size(199, 74), new Size(greenBounds.Width, greenBounds.Height));

			var redBounds = ImageAssert.GetColorBounds(screenshot, Microsoft.UI.Colors.Red, tolerance: 5);
			Assert.AreEqual(new Size(99, 74), new Size(redBounds.Width, redBounds.Height));

			SUT.Clip = new RectangleGeometry()
			{
				Rect = new(0, 0, newClipValue, newClipValue),
			};

			await TestServices.WindowHelper.WaitForIdle();

			screenshot = await UITestHelper.ScreenShot(grid);
			greenBounds = ImageAssert.GetColorBounds(screenshot, Microsoft.UI.Colors.Green, tolerance: 5);
			Assert.AreEqual(new Size(199, 74), new Size(greenBounds.Width, greenBounds.Height));

			redBounds = ImageAssert.GetColorBounds(screenshot, Microsoft.UI.Colors.Red, tolerance: 5);
			Assert.AreEqual(new Size(Math.Min(100, newClipValue) - 1, Math.Min(75, newClipValue) - 1), new Size(redBounds.Width, redBounds.Height));
		}

		[TestMethod]
		[RunsOnUIThread]
#if __ANDROID__ || __APPLE_UIKIT__
		[Ignore("It doesn't yet work properly on Android and iOS")]
#endif
		public async Task When_TranslateTransform_And_Clip()
		{
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var grid = new Grid()
			{
				Width = 100,
				Height = 100,
				Background = new SolidColorBrush(Microsoft.UI.Colors.Chartreuse),
				Children =
				{
					new Grid()
					{
						Width = 50,
						Height = 50,
						Background = new SolidColorBrush(Microsoft.UI.Colors.DeepPink),
						Children =
						{
							new Grid()
							{
								Width = 100,
								Height = 100,
								HorizontalAlignment = HorizontalAlignment.Center,
								VerticalAlignment = VerticalAlignment.Center,
								Background = new SolidColorBrush(Microsoft.UI.Colors.DeepSkyBlue),
								Clip = new RectangleGeometry()
								{
									Rect = new Rect(0, 0, 5, 50),
								},
								RenderTransform = new TranslateTransform()
								{
									X = 65,
								},
							},
						},
					},
				},
			};

			await UITestHelper.Load(grid);
			var screenshot = await UITestHelper.ScreenShot(grid);
			var chartreuseBounds = ImageAssert.GetColorBounds(screenshot, Microsoft.UI.Colors.Chartreuse);
			var deepPinkBounds = ImageAssert.GetColorBounds(screenshot, Microsoft.UI.Colors.DeepPink);
			var deepSkyBlueBounds = ImageAssert.GetColorBounds(screenshot, Microsoft.UI.Colors.DeepSkyBlue);

			Assert.AreEqual(new Rect(0, 0, 99, 99), chartreuseBounds);
			Assert.AreEqual(new Rect(25, 25, 49, 49), deepPinkBounds);
			Assert.AreEqual(new Rect(65, 25, 4, 24), deepSkyBlueBounds);
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/17642")]
		public async Task When_Clipping_Changes_During_Arrange()
		{
			await UITestHelper.Load(new ButtonChangingClipDuringArrange() { Content = "Hello" });
		}

#if HAS_UNO // Tests use IsArrangeDirty, which is an internal property
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Visible_InvalidateArrange()
		{
			var sut = new Border() { Width = 100, Height = 10 };

			TestServices.WindowHelper.WindowContent = sut;
			await TestServices.WindowHelper.WaitForIdle();
			sut.InvalidateArrange();
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsFalse(sut.IsArrangeDirty);
		}

#if !__ANDROID__ && !__APPLE_UIKIT__ // Fails on Android & iOS (issue #5002)
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Collapsed_InvalidateArrange()
		{
			var sut = new Border()
			{
				Width = 100,
				Height = 10,
				Visibility = Visibility.Collapsed
			};

			TestServices.WindowHelper.WindowContent = sut;
			await TestServices.WindowHelper.WaitForIdle();
			sut.InvalidateArrange();
			await TestServices.WindowHelper.WaitForIdle();

			await TestServices.WindowHelper.WaitFor(() => !sut.IsArrangeDirty);
		}
#endif
#endif

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_TextBlock_ActualSize()
		{
			Border border = new Border();
			TextBlock text = new TextBlock()
			{
				HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
				VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
				Text = "Short text"
			};
			border.Child = text;

			TestServices.WindowHelper.WindowContent = border;
			await TestServices.WindowHelper.WaitForIdle();

			border.UpdateLayout();

			await TestServices.WindowHelper.WaitFor(() => Math.Abs(text.ActualWidth - text.ActualSize.X) < 1);
			await TestServices.WindowHelper.WaitFor(() => Math.Abs(text.ActualHeight - text.ActualSize.Y) < 1);

			text.Text = "This is a longer text";
			border.UpdateLayout();

			await TestServices.WindowHelper.WaitFor(() => Math.Abs(text.ActualWidth - text.ActualSize.X) < 1);
			await TestServices.WindowHelper.WaitFor(() => Math.Abs(text.ActualHeight - text.ActualSize.Y) < 1);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Rectangle_Set_ActualSize()
		{
			Border border = new Border();

			Rectangle rectangle = new Rectangle()
			{
				HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
				VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
				Width = 42,
				Height = 24,
			};
			border.Child = rectangle;

			TestServices.WindowHelper.WindowContent = border;
			await TestServices.WindowHelper.WaitForIdle();

			border.UpdateLayout();

			await TestServices.WindowHelper.WaitFor(() =>
				Math.Abs(rectangle.ActualWidth - rectangle.ActualSize.X) < 0.01);
			await TestServices.WindowHelper.WaitFor(() =>
				Math.Abs(rectangle.ActualHeight - rectangle.ActualSize.Y) < 0.01);

			rectangle.Width = 16;
			rectangle.Height = 32;
			border.UpdateLayout();

			await TestServices.WindowHelper.WaitFor(() =>
				Math.Abs(rectangle.ActualWidth - rectangle.ActualSize.X) < 0.01);
			await TestServices.WindowHelper.WaitFor(() =>
				Math.Abs(rectangle.ActualHeight - rectangle.ActualSize.Y) < 0.01);
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task When_Root_ActualOffset()
		{
			Border border = new Border();

			TestServices.WindowHelper.WindowContent = border;
			await TestServices.WindowHelper.WaitForIdle();

			border.UpdateLayout();

			Assert.AreEqual(Vector3.Zero, border.ActualOffset);
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task When_Root_Margin_ActualOffset()
		{
			Border border = new Border()
			{
				Margin = new Thickness(10)
			};

			TestServices.WindowHelper.WindowContent = border;
			await TestServices.WindowHelper.WaitForIdle();

			border.UpdateLayout();

			Assert.AreEqual(new Vector3(10, 10, 0), border.ActualOffset);
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task When_Child_ActualOffset()
		{
			var grid = new Grid();
			grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(100, GridUnitType.Pixel) });
			grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
			grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(50, GridUnitType.Pixel) });
			grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });

			var button = new Button() { Content = "Test", HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(10) };
			grid.Children.Add(button);
			Grid.SetColumn(button, 1);
			Grid.SetRow(button, 1);

			TestServices.WindowHelper.WindowContent = grid;
			await TestServices.WindowHelper.WaitForIdle();

			grid.UpdateLayout();

			Assert.AreEqual(new Vector3(110, 60, 0), button.ActualOffset);
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task When_Nested_Child_ActualOffset()
		{
			var border = new Border();
			var grid = new Grid()
			{
				Margin = new Thickness(10)
			};
			grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(100, GridUnitType.Pixel) });
			grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
			grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(50, GridUnitType.Pixel) });
			grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });

			var button = new Button() { Content = "Test", HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(10) };
			grid.Children.Add(button);
			Grid.SetColumn(button, 1);
			Grid.SetRow(button, 1);
			border.Child = grid;

			TestServices.WindowHelper.WindowContent = border;
			await TestServices.WindowHelper.WaitForIdle();

			border.UpdateLayout();

			Assert.AreEqual(new Vector3(110, 60, 0), button.ActualOffset);
		}

#if __SKIA__
		private async Task TapKey(VirtualKey key)
		{
			TestServices.WindowHelper.XamlRoot.VisualTree.ContentRoot.InputManager.Keyboard.OnKeyTestingOnly(
				new KeyEventArgs("test", key, VirtualKeyModifiers.None, new CorePhysicalKeyStatus()), true);

			await TestServices.WindowHelper.WaitForIdle();

			TestServices.WindowHelper.XamlRoot.VisualTree.ContentRoot.InputManager.Keyboard.OnKeyTestingOnly(
				new KeyEventArgs("test", key, VirtualKeyModifiers.None, new CorePhysicalKeyStatus()), false);

			await TestServices.WindowHelper.WaitForIdle();
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_PreviewKeyDown_Basic()
		{
			StackPanel sp2, sp3;
			Button btn1, btn2, btn3;
			var sp1 = new StackPanel
			{
				Name = "sp1",
				Children =
				{
					(sp2 = new StackPanel
					{
						Name = "sp2",
						Children =
						{
							(sp3 = new StackPanel
							{
								Name = "sp3",
								Children =
								{
									(btn1 = new Button { Name = "btn1" }),
									(btn2 = new Button { Name = "btn2" })
								}
							}),
							(btn3 = new Button { Name = "btn3" })
						}
					})
				}
			};

			var result = new StringBuilder();

			btn1.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);
			btn2.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);
			btn3.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);

			btn1.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);
			btn2.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);
			btn3.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);

			sp1.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);
			sp2.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);
			sp3.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);

			sp1.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);
			sp2.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);
			sp3.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);

			await UITestHelper.Load(sp1);

			btn1.Focus(FocusState.Programmatic);
			await TapKey(VirtualKey.A); // any key that doesn't get handled by Button
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(
				"""
				sp1 PreviewKeyDown False
				sp2 PreviewKeyDown False
				sp3 PreviewKeyDown False
				btn1 PreviewKeyDown False
				btn1 KeyDown False
				sp3 KeyDown False
				sp2 KeyDown False
				sp1 KeyDown False
				
				""".ReplaceLineEndings("\n")
				, result.ToString());

			void OnKeyDown(object sender, KeyRoutedEventArgs e)
			{
				result.Append($"{((FrameworkElement)sender).Name} KeyDown {e.Handled}\n");
			}

			void OnPreviewKeyDown(object sender, KeyRoutedEventArgs e)
			{
				result.Append($"{((FrameworkElement)sender).Name} PreviewKeyDown {e.Handled}\n");
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_PreviewKeyDown_KeyDown_DifferentArgs()
		{
			var button = new Button();

			KeyRoutedEventArgs keyDownArgs = default, previewKeyDownArgs = default;
			button.KeyDown += (_, args) => keyDownArgs = args;
			button.PreviewKeyDown += (_, args) => previewKeyDownArgs = args;

			await UITestHelper.Load(button);
			button.Focus(FocusState.Programmatic);
			await TapKey(VirtualKey.A);

			Assert.IsNotNull(keyDownArgs);
			Assert.IsNotNull(previewKeyDownArgs);
			// We use the same args object twice to reduce allocations, which is different from WinUI.
			// Assert.AreNotEqual(keyDownArgs, previewKeyDownArgs);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_PreviewKeyDown_Handled()
		{
			StackPanel sp2, sp3;
			Button btn1, btn2, btn3;
			var sp1 = new StackPanel
			{
				Name = "sp1",
				Children =
				{
					(sp2 = new StackPanel
					{
						Name = "sp2",
						Children =
						{
							(sp3 = new StackPanel
							{
								Name = "sp3",
								Children =
								{
									(btn1 = new Button { Name = "btn1" }),
									(btn2 = new Button { Name = "btn2" })
								}
							}),
							(btn3 = new Button { Name = "btn3" })
						}
					})
				}
			};

			var result = new StringBuilder();

			btn1.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), false); // Change from other tests, true -> false
			btn2.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);
			btn3.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);

			btn1.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);
			btn2.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);
			btn3.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);

			sp1.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);
			sp2.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);
			sp3.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);

			sp1.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);
			sp2.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);
			sp3.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);

			await UITestHelper.Load(sp1);

			btn1.Focus(FocusState.Programmatic);
			await TapKey(VirtualKey.A); // any key that doesn't get handled by Button
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(
				"""
				sp1 PreviewKeyDown False
				sp2 PreviewKeyDown False
				sp3 PreviewKeyDown True
				btn1 PreviewKeyDown True
				sp3 KeyDown True
				sp2 KeyDown True
				sp1 KeyDown True
				
				""".ReplaceLineEndings("\n")
				, result.ToString());

			void OnKeyDown(object sender, KeyRoutedEventArgs e)
			{
				result.Append($"{((FrameworkElement)sender).Name} KeyDown {e.Handled}\n");
			}

			void OnPreviewKeyDown(object sender, KeyRoutedEventArgs e)
			{
				result.Append($"{((FrameworkElement)sender).Name} PreviewKeyDown {e.Handled}\n");

				if (((FrameworkElement)sender).Name == "sp2")
				{
					e.Handled = true;
				}
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_PreviewKeyDown_Handled_Then_Unhandled()
		{
			StackPanel sp2, sp3;
			Button btn1, btn2, btn3;
			var sp1 = new StackPanel
			{
				Name = "sp1",
				Children =
				{
					(sp2 = new StackPanel
					{
						Name = "sp2",
						Children =
						{
							(sp3 = new StackPanel
							{
								Name = "sp3",
								Children =
								{
									(btn1 = new Button { Name = "btn1" }),
									(btn2 = new Button { Name = "btn2" })
								}
							}),
							(btn3 = new Button { Name = "btn3" })
						}
					})
				}
			};

			var result = new StringBuilder();

			btn1.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), false); // change from other tests, true -> false
			btn2.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);
			btn3.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);

			btn1.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);
			btn2.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);
			btn3.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);

			sp1.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);
			sp2.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);
			sp3.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);

			sp1.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);
			sp2.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);
			sp3.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);

			await UITestHelper.Load(sp1);

			btn1.Focus(FocusState.Programmatic);
			await TapKey(VirtualKey.A); // any key that doesn't get handled by Button
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(
				"""
				sp1 PreviewKeyDown False
				sp2 PreviewKeyDown False
				sp3 PreviewKeyDown True
				btn1 PreviewKeyDown False
				btn1 KeyDown False
				sp3 KeyDown False
				sp2 KeyDown False
				sp1 KeyDown False
				
				""".ReplaceLineEndings("\n")
				, result.ToString());

			void OnKeyDown(object sender, KeyRoutedEventArgs e)
			{
				result.Append($"{((FrameworkElement)sender).Name} KeyDown {e.Handled}\n");
			}

			void OnPreviewKeyDown(object sender, KeyRoutedEventArgs e)
			{
				result.Append($"{((FrameworkElement)sender).Name} PreviewKeyDown {e.Handled}\n");

				if (((FrameworkElement)sender).Name == "sp2")
				{
					e.Handled = true;
				}

				if (((FrameworkElement)sender).Name == "sp3")
				{
					e.Handled = false;
				}
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[Ignore("Failing due to #15942")]
		public async Task When_PreviewKeyDown_Reparenting()
		{
			StackPanel sp2, sp3;
			Button btn1, btn2, btn3;
			var sp1 = new StackPanel
			{
				Name = "sp1",
				Children =
				{
					(sp2 = new StackPanel
					{
						Name = "sp2",
						Children =
						{
							(sp3 = new StackPanel
							{
								Name = "sp3",
								Children =
								{
									(btn1 = new Button { Name = "btn1" }),
									(btn2 = new Button { Name = "btn2" })
								}
							}),
							(btn3 = new Button { Name = "btn3" })
						}
					})
				}
			};

			var result = new StringBuilder();

			btn1.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);
			btn2.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);
			btn3.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);

			btn1.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);
			btn2.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);
			btn3.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);

			sp1.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);
			sp2.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);
			sp3.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);

			sp1.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);
			sp2.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);
			sp3.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);

			await UITestHelper.Load(sp1);

			btn1.Focus(FocusState.Programmatic);
			await TapKey(VirtualKey.A); // any key that doesn't get handled by Button
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(
				"""
				sp1 PreviewKeyDown False
				sp2 PreviewKeyDown False
				sp3 PreviewKeyDown False
				btn1 PreviewKeyDown False
				btn3 KeyDown False
				sp2 KeyDown False
				sp1 KeyDown False
				
				""".ReplaceLineEndings("\n")
				, result.ToString());

			void OnKeyDown(object sender, KeyRoutedEventArgs e)
			{
				result.Append($"{((FrameworkElement)sender).Name} KeyDown {e.Handled}\n");
			}

			void OnPreviewKeyDown(object sender, KeyRoutedEventArgs e)
			{
				result.Append($"{((FrameworkElement)sender).Name} PreviewKeyDown {e.Handled}\n");

				if (((FrameworkElement)sender).Name == "sp2")
				{
					sp3.Children.Remove(btn1);
					sp2.Children.Add(btn1);
				}
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_PreviewKeyDown_FocusChanged()
		{
			StackPanel sp2, sp3;
			Button btn1, btn2, btn3;
			var sp1 = new StackPanel
			{
				Name = "sp1",
				Children =
				{
					(sp2 = new StackPanel
					{
						Name = "sp2",
						Children =
						{
							(sp3 = new StackPanel
							{
								Name = "sp3",
								Children =
								{
									(btn1 = new Button { Name = "btn1" }),
									(btn2 = new Button { Name = "btn2" })
								}
							}),
							(btn3 = new Button { Name = "btn3" })
						}
					})
				}
			};

			var result = new StringBuilder();

			btn1.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);
			btn2.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);
			btn3.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);

			btn1.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);
			btn2.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);
			btn3.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);

			sp1.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);
			sp2.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);
			sp3.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);

			sp1.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);
			sp2.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);
			sp3.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), true);

			await UITestHelper.Load(sp1);

			btn1.Focus(FocusState.Programmatic);
			await TapKey(VirtualKey.A); // any key that doesn't get handled by Button
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(
				"""
				sp1 PreviewKeyDown False
				sp2 PreviewKeyDown False
				sp3 PreviewKeyDown False
				btn1 PreviewKeyDown False
				btn2 KeyDown False
				sp3 KeyDown False
				sp2 KeyDown False
				sp1 KeyDown False
				
				""".ReplaceLineEndings("\n")
				, result.ToString());

			void OnKeyDown(object sender, KeyRoutedEventArgs e)
			{
				result.Append($"{((FrameworkElement)sender).Name} KeyDown {e.Handled}\n");
			}

			void OnPreviewKeyDown(object sender, KeyRoutedEventArgs e)
			{
				result.Append($"{((FrameworkElement)sender).Name} PreviewKeyDown {e.Handled}\n");

				if (((FrameworkElement)sender).Name == "sp2")
				{
					btn2.Focus(FocusState.Programmatic);
				}
			}
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_UpdateLayout_Then_ReentrancyNotAllowed()
		{
			var sut = new When_UpdateLayout_Then_ReentrancyNotAllowed_Element();

			TestServices.WindowHelper.WindowContent = sut;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsFalse(sut.Failed);
		}

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
		public void When_GetVisualTreeParent()
		{
			var treeRoot = GetTreeRoot();
			Assert.IsNotNull(treeRoot);
#if __ANDROID__ || __APPLE_UIKIT__
			// On Xamarin platforms, we don't expect the real root of the tree to be a XAML element
			Assert.IsNotInstanceOfType(treeRoot, typeof(UIElement));
#else
			//...and everywhere else, we do
			Assert.IsInstanceOfType(treeRoot, typeof(UIElement));
#endif
			object GetTreeRoot()
			{
				// Ttrick - GetVisualTreeParent's return type is different
				// on each platform, so we use var to get the correct type implicitly
				var current = TestServices.WindowHelper.XamlRoot.Content?.GetVisualTreeParent();
				current = TestServices.WindowHelper.XamlRoot.Content;
				var parent = current?.GetVisualTreeParent();
				while (parent != null)
				{
					current = parent;
					parent = current?.GetVisualTreeParent();
				}
				return current;
			}
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_LayoutInformation_GetAvailableSize_Constraints()
		{
			var noConstraintsBorder = new Border();
			var maxHeightBorder = new Border() { MaxHeight = 122 };
			var hostGrid = new Grid
			{
				Width = 182,
				Height = 313,
				Children =
				{
					noConstraintsBorder,
					maxHeightBorder
				}
			};

			TestServices.WindowHelper.WindowContent = hostGrid;
			await TestServices.WindowHelper.WaitForLoaded(hostGrid);

			await TestServices.WindowHelper.WaitForEqual(313, () => LayoutInformation.GetAvailableSize(noConstraintsBorder).Height);
			var maxHeightAvailableSize = LayoutInformation.GetAvailableSize(maxHeightBorder);
			Assert.AreEqual(313, maxHeightAvailableSize.Height, delta: 1); // Should return unmodified measure size, ignoring constraints like MaxHeight
		}

		[TestMethod]
		[RunsOnUIThread]
#if !MEASURE_DIRTY_PATH_AVAILABLE
		[Ignore("Not supported on this platform")]
#endif
		public async Task When_InvalidatingMeasureExplicitly()
		{
			var (ctl1, ctl2, ctl3) = await SetupMeasureArrangeTest();

			ctl2.InvalidateMeasure();

			await TestServices.WindowHelper.WaitFor(() => ctl2.MeasureCount > 1);

			await TestServices.WindowHelper.WaitForIdle();

			using var _ = new AssertionScope();

			ctl1.MeasureCount.Should().Be(1);
			ctl2.MeasureCount.Should().Be(2);
			ctl3.MeasureCount.Should().Be(1);

#if ARRANGE_DIRTY_PATH_AVAILABLE
			ctl1.ArrangeCount.Should().Be(1);
			ctl2.ArrangeCount.Should().BeInRange(1, 2); // both are acceptable, depends on the capabilities of the platform
			ctl3.ArrangeCount.Should().Be(1);
#endif
		}

#if __WASM__ || __SKIA__
		[TestMethod]
		[RunsOnUIThread]
		[DataRow(0d)]
		[DataRow(-1d)]
		[DataRow(0.001d)]
		[DataRow(0.1d)]
		[DataRow(100d)]
		public void When_InvalidatingMeasureThenMeasure(double size)
		{
			var sut = new MeasureAndArrangeCounter();

			sut.IsMeasureDirty.Should().BeFalse();

			sut.InvalidateMeasure();

			sut.IsMeasureDirty.Should().BeTrue();
			sut.IsMeasureDirtyPath.Should().BeFalse();
			sut.IsMeasureDirtyOrMeasureDirtyPath.Should().BeTrue();

			sut.Measure(new Size(size, size));

			sut.IsMeasureDirtyOrMeasureDirtyPath.Should().BeFalse();
			sut.MeasureCount.Should().Be(1);
			sut.ArrangeCount.Should().Be(0);
		}

		[TestMethod]
		[RunsOnUIThread]
		[DataRow(0d)]
		[DataRow(-1d)]
		[DataRow(0.001d)]
		[DataRow(0.1d)]
		[DataRow(100d)]
		public void When_InvalidatingArrangeThenMeasureAndArrange(double size)
		{
			var sut = new MeasureAndArrangeCounter();

			sut.IsMeasureDirtyOrMeasureDirtyPath.Should().BeFalse();
			sut.IsArrangeDirtyOrArrangeDirtyPath.Should().BeFalse();

			sut.InvalidateMeasure();
			sut.InvalidateArrange();

			sut.IsMeasureDirty.Should().BeTrue();
			sut.IsMeasureDirtyPath.Should().BeFalse();
			sut.IsMeasureDirtyOrMeasureDirtyPath.Should().BeTrue();
			sut.IsArrangeDirtyOrArrangeDirtyPath.Should().BeTrue();

			sut.MeasureCount.Should().Be(0);
			sut.ArrangeCount.Should().Be(0);

			sut.Measure(new Size(size, size));
			sut.Arrange(new Rect(0, 0, size, size));

			sut.IsMeasureDirtyOrMeasureDirtyPath.Should().BeFalse();
			sut.IsArrangeDirtyOrArrangeDirtyPath.Should().BeFalse();
			sut.MeasureCount.Should().Be(1);
			sut.ArrangeCount.Should().Be(1);
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
#if !ARRANGE_DIRTY_PATH_AVAILABLE
		[Ignore("Not supported on this platform")]
#endif
		public async Task When_InvalidatingArrangeExplicitly()
		{
			var (ctl1, ctl2, ctl3) = await SetupMeasureArrangeTest();

			ctl2.InvalidateArrange();

			await TestServices.WindowHelper.WaitFor(() => ctl2.ArrangeCount > 1);

			await TestServices.WindowHelper.WaitForIdle();

			using var _ = new AssertionScope();

			ctl1.MeasureCount.Should().Be(1);
			ctl2.MeasureCount.Should().Be(1);
			ctl3.MeasureCount.Should().Be(1);

			ctl1.ArrangeCount.Should().Be(1);
			ctl2.ArrangeCount.Should().Be(2);
			ctl3.ArrangeCount.Should().Be(1);
		}

		[TestMethod]
		[RunsOnUIThread]
#if !(MEASURE_DIRTY_PATH_AVAILABLE && ARRANGE_DIRTY_PATH_AVAILABLE)
		[Ignore("Not supported on this platform")]
#endif
		public async Task When_InvalidatingMeasureAndArrangeByChangingSize()
		{
			var (ctl1, ctl2, ctl3) = await SetupMeasureArrangeTest();

			ctl2.Width = 200;

			await TestServices.WindowHelper.WaitFor(() => ctl2.ArrangeCount > 1);

			await TestServices.WindowHelper.WaitForIdle();

			using var _ = new AssertionScope();

			// Everything should be remeasured & rearranged
			ctl1.MeasureCount.Should().Be(2);
			ctl2.MeasureCount.Should().Be(2);
			ctl3.MeasureCount.Should().Be(2);

			ctl1.ArrangeCount.Should().Be(2);
			ctl2.ArrangeCount.Should().Be(2);
			ctl3.ArrangeCount.Should().BeInRange(1, 2); // both are acceptable, depends on the capabilities of the platform
		}

		[TestMethod]
		[RunsOnUIThread]
#if !(MEASURE_DIRTY_PATH_AVAILABLE && ARRANGE_DIRTY_PATH_AVAILABLE)
		[Ignore("Not supported on this platform")]
#endif
		public async Task When_InvalidatingMeasureAndArrangeByChangingSizeTwice()
		{
			var (ctl1, ctl2, ctl3) = await SetupMeasureArrangeTest();

			ctl2.Width = 200;
			ctl3.Width = 200;

			await TestServices.WindowHelper.WaitFor(() => ctl2.ArrangeCount > 1);

			await TestServices.WindowHelper.WaitForIdle();

			using (var _ = new AssertionScope("First pass"))
			{
				// Everything should be remeasured & rearranged

				ctl1.MeasureCount.Should().Be(2);
				ctl2.MeasureCount.Should().Be(2);
				ctl3.MeasureCount.Should().Be(2);

				ctl1.ArrangeCount.Should().Be(2);
				ctl2.ArrangeCount.Should().Be(2);
				ctl3.ArrangeCount.Should().Be(2);
			}

			ctl3.Width = 50;

			await TestServices.WindowHelper.WaitFor(() => ctl2.ArrangeCount > 2);

			await TestServices.WindowHelper.WaitForIdle();

			using (var _ = new AssertionScope("Second pass"))
			{
				// "ctl1" should be untouched

				ctl1.MeasureCount.Should().Be(2);
				ctl2.MeasureCount.Should().Be(3);
				ctl3.MeasureCount.Should().Be(3);

				ctl1.ArrangeCount.Should().Be(2);
				ctl2.ArrangeCount.Should().Be(3);
				ctl3.ArrangeCount.Should().Be(3);
			}
		}

		private static async Task<(MeasureAndArrangeCounter, MeasureAndArrangeCounter, MeasureAndArrangeCounter)> SetupMeasureArrangeTest()
		{
			var ctl1 = new MeasureAndArrangeCounter
			{
				Background = new SolidColorBrush(Microsoft.UI.Colors.Yellow),
				Margin = new Thickness(20)
			};
			var ctl2 = new MeasureAndArrangeCounter
			{
				Background = new SolidColorBrush(Microsoft.UI.Colors.DarkRed),
				Margin = new Thickness(20)
			};
			var ctl3 = new MeasureAndArrangeCounter
			{
				Background = new SolidColorBrush(Microsoft.UI.Colors.Cornsilk),
				Margin = new Thickness(20),
				Width = 100,
				Height = 100
			};

			ctl1.Children.Add(ctl2);
			ctl2.Children.Add(ctl3);

			TestServices.WindowHelper.WindowContent = ctl1;

			await TestServices.WindowHelper.WaitForLoaded(ctl3);

			using var _ = new AssertionScope("Setup");

			ctl1.MeasureCount.Should().Be(1);
			ctl2.MeasureCount.Should().Be(1);
			ctl3.MeasureCount.Should().Be(1);

			ctl1.ArrangeCount.Should().Be(1);
			ctl2.ArrangeCount.Should().Be(1);
			ctl3.ArrangeCount.Should().Be(1);

			return (ctl1, ctl2, ctl3);
		}

		private partial class MeasureAndArrangeCounter : Panel
		{
			internal int MeasureCount;
			internal int ArrangeCount;
			protected override Size MeasureOverride(Size availableSize)
			{
				MeasureCount++;

				// copied from FrameworkElement.MeasureOverride and modified to compile on Windows
				var child = Children.Count > 0 ? Children[0] : null;
#if WINAPPSDK
				if (child != null)
				{
					child.Measure(availableSize);
					return child.DesiredSize;
				}

				return new Size(0, 0);
#else
				return child != null ? MeasureElement(child, availableSize) : new Size(0, 0);
#endif
			}

			protected override Size ArrangeOverride(Size finalSize)
			{
				ArrangeCount++;

				// copied from FrameworkElement.ArrangeOverride and modified to compile on Windows
				var child = Children.Count > 0 ? Children[0] : null;

				if (child != null)
				{
#if WINAPPSDK
					child.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
#else
					ArrangeElement(child, new Rect(0, 0, finalSize.Width, finalSize.Height));
#endif
				}

				return finalSize;
			}
		}

#if __CROSSRUNTIME__
		[TestMethod]
		[RunsOnUIThread]
		public void MeasureDirtyTest()
		{
			var sut = new Grid();
			sut.Children.Add(new MeasureAndArrangeCounter());

			using var x = new AssertionScope();

			using (_ = new AssertionScope("Before Measure"))
			{
				sut.IsFirstMeasureDone.Should().BeFalse("IsFirstMeasureDone");
				sut.IsMeasureDirty.Should().BeTrue("IsMeasureDirty");
				sut.IsMeasureDirtyPath.Should().BeTrue("IsMeasureDirtyPath");
			}

			sut.Measure(new Size(100, 100));

			using (_ = new AssertionScope("After Measure"))
			{
				sut.IsFirstMeasureDone.Should().BeTrue("IsFirstMeasureDone");
				sut.IsMeasureDirty.Should().BeFalse("IsMeasureDirty");
				sut.IsMeasureDirtyPath.Should().BeFalse("IsMeasureDirtyPath");
			}
		}


		[TestMethod]
		[RunsOnUIThread]
		public void ArrangeDirtyTest()
		{
			var sut = new Grid();
			sut.Children.Add(new MeasureAndArrangeCounter());

			sut.Measure(new Size(100, 100));

			using var x = new AssertionScope();

			using (_ = new AssertionScope("Before Arrange"))
			{
				sut.IsArrangeDirty.Should().BeTrue("IsArrangeDirty");
			}

			sut.Arrange(new Rect(0, 0, 100, 100));
			using (_ = new AssertionScope("After Arrange"))
			{
				sut.IsArrangeDirty.Should().BeFalse("IsArrangeDirty");
				sut.IsArrangeDirtyPath.Should().BeFalse("IsArrangeDirtyPath");
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Measure_Explicitly_Called()
		{
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var tb = new TextBlock
			{
				Text = "Small"
			};

			var SUT = new StackPanel
			{
				Children =
				{
					tb,
					new ContentControl
					{
						HorizontalContentAlignment = HorizontalAlignment.Center,
						Content = new TextBlock
						{
							Text = "Small",
							Foreground = new SolidColorBrush(Microsoft.UI.Colors.Yellow)
						}
					}
				}
			};

			var sp = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = 200 }
				},
				Children = { SUT }
			};

			await UITestHelper.Load(sp);

			tb.Text = "very very very very very very very very very very very very very very very very very very very very very very very very wide";
			await TestServices.WindowHelper.WaitForIdle();

			SUT.Measure(LayoutInformation.GetAvailableSize(SUT) with { Width = 1000 });
			await TestServices.WindowHelper.WaitForIdle();

			var bitmap = await UITestHelper.ScreenShot(sp);
			ImageAssert.HasColorInRectangle(bitmap, new System.Drawing.Rectangle(new Point(0, 0), bitmap.Size), Microsoft.UI.Colors.Yellow, tolerance: 25);
		}
#endif

#if __SKIA__
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Explicit_Size_Clip_Changes()
		{
			var sut = new UIElement();

			var rect = new Rect(0, 0, 100, 100);
			var clip = new Rect(0, 0, 50, 50);

			sut.ArrangeVisual(rect, clip);
			Assert.IsNotNull(sut.Visual.LayoutClip);

			sut.ArrangeVisual(rect, null);
			Assert.IsNull(sut.Visual.LayoutClip);
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
#if !HAS_COMPOSITION_API
		[Ignore("Composition APIs are not supported on this platform.")]
#endif
		public async Task When_Visual_Offset_Changes_HitTest()
		{
			var sut = new Button
			{
				Content = "Click",
			};

			var visual = ElementCompositionPreview.GetElementVisual(sut);

			var rect = await UITestHelper.Load(sut);

#if HAS_UNO
			var (element, _) = VisualTreeHelper.HitTest(rect.GetCenter(), sut.XamlRoot);
			Assert.IsTrue(sut.IsAncestorOf(element));
#endif

			var matrix1 = ((MatrixTransform)sut.TransformToVisual(null)).Matrix;

			Assert.IsTrue(matrix1.OffsetY > 0);

			visual.Offset = new Vector3(visual.Offset.X, visual.Offset.Y + (float)rect.Height * 2, visual.Offset.Z);

			var matrix2 = ((MatrixTransform)sut.TransformToVisual(null)).Matrix;

			await TestServices.WindowHelper.WaitForIdle();

			var matrix3 = ((MatrixTransform)sut.TransformToVisual(null)).Matrix;

			await Task.Delay(100);

			var matrix4 = ((MatrixTransform)sut.TransformToVisual(null)).Matrix;

#if WINAPPSDK
			// On WinUI, The value of Offset doesn't immediately affect TransformToVisual.
			// Even a call to WaitForIdle isn't enough.
			// So, only the TransformToVisual call after Task.Delay(100) takes the new offset in consideration.
			Assert.AreEqual(matrix1, matrix2);
			Assert.AreEqual(matrix1, matrix3);
			Assert.AreEqual(rect.Height * 2, matrix4.OffsetY - matrix1.OffsetY);
#else
			// On Uno, the value of Offset is taking effect immediately.
			// Part of this could be a lifecycle issue.
			Assert.AreEqual(rect.Height * 2, matrix2.OffsetY - matrix1.OffsetY);
			Assert.AreEqual(rect.Height * 2, matrix3.OffsetY - matrix1.OffsetY);
			Assert.AreEqual(rect.Height * 2, matrix4.OffsetY - matrix1.OffsetY);
#endif

#if HAS_UNO
			var (element2, _) = VisualTreeHelper.HitTest(rect.GetCenter(), sut.XamlRoot);
			Assert.IsFalse(sut.IsAncestorOf(element2));

			var (element3, _) = VisualTreeHelper.HitTest(rect.GetCenter().Offset(0, rect.Height * 2), sut.XamlRoot);
			Assert.IsTrue(sut.IsAncestorOf(element3));
#endif
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
#if !HAS_COMPOSITION_API
		[Ignore("Composition APIs are not supported on this platform.")]
#elif !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_Visual_Offset_Changes_InjectedPointer()
		{
			var sut = new Button
			{
				Content = "Click",
			};

			var visual = ElementCompositionPreview.GetElementVisual(sut);

			var clickCount = 0;

			sut.Click += (_, _) =>
			{
				clickCount++;
			};

			var rect = await UITestHelper.Load(sut);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();
			finger.Press(rect.GetCenter());
			finger.Release();

			Assert.AreEqual(1, clickCount);

			visual.Offset = new Vector3(visual.Offset.X, visual.Offset.Y + (float)rect.Height * 2, visual.Offset.Z);

			await Task.Delay(100);

			finger.Press(rect.GetCenter());
			finger.Release();
			Assert.AreEqual(1, clickCount);

			finger.Press(rect.GetCenter().Offset(0, rect.Height * 2));
			finger.Release();
			Assert.AreEqual(2, clickCount);
		}

#if WINAPPSDK // Translation in Uno not matching WinUI.
		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task When_Element_Has_Translation_HitTest()
		{
			var sut = new Button()
			{
				Content = "Click",
			};

			var visual = ElementCompositionPreview.GetElementVisual(sut);

			var rect = await UITestHelper.Load(sut);

			var originalVisualOffset = visual.Offset;

#if __SKIA__
			var (element, _) = VisualTreeHelper.HitTest(rect.GetCenter(), sut.XamlRoot);
			Assert.IsTrue(sut.IsAncestorOf(element));
#endif

			var matrix1 = ((MatrixTransform)sut.TransformToVisual(null)).Matrix;

			sut.Translation = new Vector3(0, (float)rect.Height * 2, 0);

			var matrix2 = ((MatrixTransform)sut.TransformToVisual(null)).Matrix;

			Assert.AreEqual(rect.Height * 2, matrix2.OffsetY - matrix1.OffsetY);

#if __SKIA__
			var (element2, _) = VisualTreeHelper.HitTest(rect.GetCenter(), sut.XamlRoot);
			Assert.IsFalse(sut.IsAncestorOf(element2));

			var (element3, _) = VisualTreeHelper.HitTest(rect.GetCenter().Offset(0, rect.Height * 2), sut.XamlRoot);
			Assert.IsTrue(sut.IsAncestorOf(element3));
#endif

			await Task.Delay(100);
			var newVisualOffset = visual.Offset;
			Assert.AreEqual(newVisualOffset, originalVisualOffset);
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task When_Element_Has_Translation_InjectedPointer()
		{
			var sut = new Button()
			{
				Content = "Click",
			};

			var visual = ElementCompositionPreview.GetElementVisual(sut);

			var clickCount = 0;

			sut.Click += (_, _) =>
			{
				clickCount++;
			};

			var rect = await UITestHelper.Load(sut);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();
			finger.Press(rect.GetCenter());
			finger.Release();

			Assert.AreEqual(1, clickCount);

			await Task.Delay(100);

			sut.Translation = new Vector3(0, (float)rect.Height * 2, 0);
			sut.InvalidateArrange();
			await TestServices.WindowHelper.WaitForIdle();

			await Task.Delay(100);

			finger.Press(rect.GetCenter());
			finger.Release();
			Assert.AreEqual(1, clickCount);

			finger.Press(rect.GetCenter().Offset(0, rect.Height * 2));
			finger.Release();
			Assert.AreEqual(2, clickCount);
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task When_Element_Has_Translation_And_Visual_Has_Offset()
		{
			var sut = new Button()
			{
				Content = "Click",
			};

			var visual = ElementCompositionPreview.GetElementVisual(sut);

			var rect = await UITestHelper.Load(sut);

			var matrix1 = ((MatrixTransform)sut.TransformToVisual(null)).Matrix;

			visual.Offset = new Vector3(visual.Offset.X, 50, visual.Offset.Z);
			sut.Translation = new Vector3(0, (float)rect.Height * 2, 0);

			var matrix2 = ((MatrixTransform)sut.TransformToVisual(null)).Matrix;

			Assert.AreEqual(rect.Height * 2, matrix2.OffsetY - matrix1.OffsetY);

			await TestServices.WindowHelper.WaitForIdle();

			var matrix3 = ((MatrixTransform)sut.TransformToVisual(null)).Matrix;

			Assert.AreEqual(rect.Height * 2, matrix3.OffsetY - matrix1.OffsetY);

			await Task.Delay(100);

			var matrix4 = ((MatrixTransform)sut.TransformToVisual(null)).Matrix;

			Assert.AreEqual(rect.Height * 2 + 50, matrix4.OffsetY);

#if __SKIA__
			var (element2, _) = VisualTreeHelper.HitTest(rect.GetCenter(), sut.XamlRoot);
			Assert.IsFalse(sut.IsAncestorOf(element2));

			var (element3, _) = VisualTreeHelper.HitTest(rect.GetCenter().Offset(0, rect.Height * 2), sut.XamlRoot);
			Assert.IsTrue(sut.IsAncestorOf(element3));
#endif
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
#if !__SKIA__
		[Ignore("Translation X and Y axis is currently supported on Skia only")]
#endif
		public async Task When_Translation_On_Load()
		{
			var sut = new Rectangle()
			{
				Width = 100,
				Height = 100,
				Fill = new SolidColorBrush(Microsoft.UI.Colors.Blue),
			};
			var canvas = new Canvas();
			canvas.Width = 200;
			canvas.Height = 200;
			canvas.Children.Add(sut);
			TestServices.WindowHelper.WindowContent = canvas;
			await TestServices.WindowHelper.WaitForLoaded(sut);
			sut.Translation += new Vector3(100, 100, 128);
			await TestServices.WindowHelper.WaitForIdle();
			var bitmap = await UITestHelper.ScreenShot(canvas);
			ImageAssert.DoesNotHaveColorAt(bitmap, new Windows.Foundation.Point(50, 50), Microsoft.UI.Colors.Blue);
			ImageAssert.HasColorAt(bitmap, new Windows.Foundation.Point(150, 150), Microsoft.UI.Colors.Blue);
		}

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is only supported on skia")]
#endif
		[CombinatorialData]
		public async Task When_Multiple_Pointer_Buttons_Pressed(bool releaseRightFirst)
		{
			var SUT = new Border
			{
				Background = new SolidColorBrush(Microsoft.UI.Colors.Red),
				Width = 100,
				Height = 100
			};

			var pointerDown = 0;
			var pointerUp = 0;
			var rightTapped = 0;
			SUT.PointerPressed += (_, _) => pointerDown++;
			SUT.PointerReleased += (_, _) => pointerUp++;
			SUT.RightTapped += (_, _) => rightTapped++;

			await UITestHelper.Load(SUT);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			mouse.MoveTo(SUT.GetAbsoluteBoundsRect().GetCenter());
			mouse.Press();
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreEqual(1, pointerDown);
			Assert.AreEqual(0, pointerUp);
			Assert.AreEqual(0, rightTapped);

			mouse.PressRight();
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreEqual(1, pointerDown);
			Assert.AreEqual(0, pointerUp);
			Assert.AreEqual(0, rightTapped);

			if (releaseRightFirst)
			{
				mouse.ReleaseRight();
			}
			else
			{
				mouse.Release();
			}
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreEqual(1, pointerDown);
			Assert.AreEqual(0, pointerUp);
			Assert.AreEqual(0, rightTapped);

			if (releaseRightFirst)
			{
				mouse.Release();
			}
			else
			{
				mouse.ReleaseRight();
			}
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreEqual(1, pointerDown);
			Assert.AreEqual(1, pointerUp);
			Assert.AreEqual(0, rightTapped);
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_Tapped_Recognizer_Owner_Not_Pointer_Event_OriginalSource()
		{
			var inner = new Border
			{
				Width = 100,
				Height = 100,
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Bottom,
				Background = new SolidColorBrush(Microsoft.UI.Colors.Red)
			};
			var outer = new Border
			{
				Background = new SolidColorBrush(Microsoft.UI.Colors.Green),
				Width = 300,
				Height = 300,
				Child = inner
			};

			var tappedCount = 0;
			var doubleTappedCount = 0;
			var tappedPos = new Windows.Foundation.Point(0, 0);
			var doubleTappedPos = new Windows.Foundation.Point(0, 0);
			UIElement tappedOriginalSource = null;
			UIElement doubleTappedOriginalSource = null;
			outer.Tapped += (_, e) =>
			{
				tappedCount++;
				tappedPos = e.GetPosition(null);
				tappedOriginalSource = e.OriginalSource as UIElement;
			};
			outer.DoubleTapped += (_, e) =>
			{
				doubleTappedCount++;
				doubleTappedPos = e.GetPosition(null);
				doubleTappedOriginalSource = e.OriginalSource as UIElement;
			};

			await UITestHelper.Load(outer);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			finger.Tap(inner.GetAbsoluteBoundsRect().GetCenter());
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(1, tappedCount);
			Assert.AreEqual(0, doubleTappedCount);
			Assert.AreEqual(inner, tappedOriginalSource);
			Assert.IsTrue(outer.GetAbsoluteBoundsRect().Contains(tappedPos), $"tappedPos: {tappedPos}, outer absolute bounds: {outer.GetAbsoluteBoundsRect()}");

			await Task.Delay(TimeSpan.FromMicroseconds(GestureRecognizer.MultiTapMaxDelayMicroseconds));

			finger.Tap(inner.GetAbsoluteBoundsRect().GetCenter());
			await TestServices.WindowHelper.WaitForIdle();
			finger.Tap(inner.GetAbsoluteBoundsRect().GetCenter());
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(2, tappedCount);
			Assert.AreEqual(1, doubleTappedCount);
			Assert.AreEqual(inner, tappedOriginalSource);
			Assert.IsTrue(outer.GetAbsoluteBoundsRect().Contains(tappedPos), $"tappedPos: {tappedPos}, outer absolute bounds: {outer.GetAbsoluteBoundsRect()}");
			Assert.AreEqual(inner, doubleTappedOriginalSource);
			Assert.IsTrue(outer.GetAbsoluteBoundsRect().Contains(doubleTappedPos), $"doubleTappedPos: {doubleTappedPos}, outer absolute bounds: {outer.GetAbsoluteBoundsRect()}");

		}

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
#if !__SKIA__
		[Ignore("Hittesting is only accurate in this case on skia.")]
#endif
		[CombinatorialData]
		public async Task When_ScaleTransform_HitTest(bool addClip)
		{
			var sut = new Rectangle
			{
				Width = 100,
				Height = 100,
				Fill = new SolidColorBrush(Microsoft.UI.Colors.Blue),
				RenderTransform = new ScaleTransform
				{
					ScaleX = 2,
					ScaleY = 2
				},
				// adding the clip here should do nothing since the clip matches the size of the drawing
				Clip = addClip ? new RectangleGeometry { Rect = new Rect(0, 0, 100, 100) } : null
			};

			var rect = await UITestHelper.Load(sut);

			var (element, _) = VisualTreeHelper.HitTest(new Windows.Foundation.Point(rect.X + 50, rect.Y + 50), sut.XamlRoot);
			Assert.AreEqual(sut, element);
			(element, _) = VisualTreeHelper.HitTest(new Windows.Foundation.Point(rect.X + 150, rect.Y + 150), sut.XamlRoot);
			Assert.AreEqual(sut, element);
			(element, _) = VisualTreeHelper.HitTest(new Windows.Foundation.Point(rect.X + 50, rect.Y + 150), sut.XamlRoot);
			Assert.AreEqual(sut, element);
			(element, _) = VisualTreeHelper.HitTest(new Windows.Foundation.Point(rect.X + 150, rect.Y + 150), sut.XamlRoot);
			Assert.AreEqual(sut, element);
			(element, _) = VisualTreeHelper.HitTest(new Windows.Foundation.Point(rect.X + 201, rect.Y + 201), sut.XamlRoot);
			Assert.AreNotEqual(sut, element);
			(element, _) = VisualTreeHelper.HitTest(new Windows.Foundation.Point(rect.X, rect.Y - 1), sut.XamlRoot);
			Assert.AreNotEqual(sut, element);
			(element, _) = VisualTreeHelper.HitTest(new Windows.Foundation.Point(rect.X - 1, rect.Y), sut.XamlRoot);
			Assert.AreNotEqual(sut, element);
			(element, _) = VisualTreeHelper.HitTest(new Windows.Foundation.Point(rect.X + 200, rect.Y - 1), sut.XamlRoot);
			Assert.AreNotEqual(sut, element);
			(element, _) = VisualTreeHelper.HitTest(new Windows.Foundation.Point(rect.X + 201, rect.Y), sut.XamlRoot);
			Assert.AreNotEqual(sut, element);
		}
#endif

#if HAS_UNO
		#region Drag and Drop

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/18770")]
		[CombinatorialData]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_DragDrop_AcceptedOperation_None(bool setAcceptedOperation)
		{
			if (TestServices.WindowHelper.IsXamlIsland)
			{
				Assert.Inconclusive("Drag and drop doesn't work in Uno islands.");
			}

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var dropCount = 0;
			Rectangle source, target;
			var sp = new StackPanel
			{
				(source = new Rectangle
				{
					Width = 100,
					Height = 100,
					Fill = new SolidColorBrush(Microsoft.UI.Colors.LightCoral),
					CanDrag = true
				}),
				(target = new Rectangle
				{
					Width = 100,
					Height = 100,
					Fill = new SolidColorBrush(Microsoft.UI.Colors.LightCoral),
					AllowDrop = true
				})
			};

			target.Drop += (_, _) => dropCount++;
			if (setAcceptedOperation)
			{
				target.DragOver += (_, e) => e.AcceptedOperation = DataPackageOperation.Copy;
			}

			await UITestHelper.Load(sp);

			mouse.MoveTo(source.GetAbsoluteBoundsRect().GetCenter());
			await TestServices.WindowHelper.WaitForIdle();
			mouse.Press();
			await TestServices.WindowHelper.WaitForIdle();
			for (int i = 1; i <= 10; i++)
			{
				mouse.MoveBy(0, (target.GetAbsoluteBoundsRect().GetMidY() - source.GetAbsoluteBoundsRect().GetMidY()) * 0.1);
				await TestServices.WindowHelper.WaitForIdle();
			}
			mouse.Release();
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(setAcceptedOperation ? 1 : 0, dropCount);
		}

		[TestMethod]
		[RunsOnUIThread]
		[CombinatorialData]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#elif __WASM__
		[Ignore("Failing on WASM: https://github.com/unoplatform/uno/issues/17742")]
#endif
		public async Task When_DragOver_Fires_Along_DragEnter_Drop(bool waitAfterRelease)
		{
			if (TestServices.WindowHelper.IsXamlIsland)
			{
				Assert.Inconclusive("Drag and drop doesn't work in Uno islands.");
			}

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			mouse.MoveTo(Windows.Foundation.Point.Zero); // anywhere away from SUT
			await TestServices.WindowHelper.WaitForIdle();

			var SUT = new Button { Content = "test", AllowDrop = true };
			var dragEnterCount = 0;
			var dragOverCount = 0;
			var dropCount = 0;
			SUT.DragEnter += (_, args) =>
			{
				args.AcceptedOperation = DataPackageOperation.None; // this shouldn't do anything
				dragEnterCount++;
			};
			SUT.DragOver += (_, args) =>
			{
				args.AcceptedOperation = DataPackageOperation.Move; // this one wins
				dragOverCount++;
			};
			SUT.Drop += (_, _) => dropCount++;

			var lv = new ListView
			{
				CanDragItems = true,
				ItemsSource = "12"
			};

			lv.DragItemsStarting += (_, e) =>
			{
				if (e.Items.Count > 0)
				{
					e.Data.RequestedOperation = DataPackageOperation.Move;
					e.Data.SetText(e.Items.First().ToString()!);
				}
			};

			await UITestHelper.Load(new StackPanel
			{
				Children =
				{
					lv,
					SUT
				}
			});

			mouse.MoveTo(lv.GetAbsoluteBoundsRect().GetCenter());
			mouse.Press();
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(0, dragEnterCount);
			Assert.AreEqual(0, dragOverCount);
			Assert.AreEqual(0, dropCount);

			mouse.MoveTo(new Windows.Foundation.Point(SUT.GetAbsoluteBoundsRect().GetCenter().X, SUT.GetAbsoluteBoundsRect().Top + 15), 1);
			await TestServices.WindowHelper.WaitForIdle();
			mouse.MoveTo(new Windows.Foundation.Point(SUT.GetAbsoluteBoundsRect().GetCenter().X, SUT.GetAbsoluteBoundsRect().Top + 10), 1);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(1, dragEnterCount);
			Assert.AreEqual(2, dragOverCount);
			Assert.AreEqual(0, dropCount);

			mouse.Release();
			if (waitAfterRelease)
			{
				await TestServices.WindowHelper.WaitForIdle();
			}

			Assert.AreEqual(1, dragEnterCount);
			Assert.AreEqual(3, dragOverCount);
			Assert.AreEqual(waitAfterRelease ? 1 : 0, dropCount);
		}

		[TestMethod]
		[RunsOnUIThread]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_DragEnter_Fires_Along_DragStarting()
		{
			if (TestServices.WindowHelper.IsXamlIsland)
			{
				Assert.Inconclusive("Drag and drop doesn't work in Uno islands.");
			}

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var rect = new Rectangle
			{
				Fill = new SolidColorBrush(Microsoft.UI.Colors.Red),
				Width = 100,
				Height = 100,
				CanDrag = true
			};

			var border = new Border
			{
				Background = new SolidColorBrush(Microsoft.UI.Colors.Blue),
				Padding = new Thickness(10),
				Child = rect,
				AllowDrop = true
			};

			var dragStartingCount = 0;
			var dragEnterCount = 0;
			var dragOverCount = 0;

			border.DragEnter += (_, _) => dragEnterCount++;
			border.DragOver += (_, _) => dragOverCount++;
			rect.DragStarting += (_, _) => dragStartingCount++;

			await UITestHelper.Load(border);

			mouse.MoveTo(rect.GetAbsoluteBoundsRect().GetCenter());
			await UITestHelper.WaitForIdle();
			mouse.Press();
			await UITestHelper.WaitForIdle();
			mouse.MoveBy(GestureRecognizer.TapMaxXDelta / 3, 0);
			await UITestHelper.WaitForIdle();
			mouse.MoveBy(GestureRecognizer.TapMaxXDelta / 3, 0);
			await UITestHelper.WaitForIdle();
			mouse.MoveBy(GestureRecognizer.TapMaxXDelta / 3, 0);
			await UITestHelper.WaitForIdle();

			Assert.AreEqual(0, dragStartingCount);
			Assert.AreEqual(0, dragEnterCount);
			Assert.AreEqual(0, dragOverCount);

			mouse.MoveBy(GestureRecognizer.TapMaxXDelta / 3, 0);
			await UITestHelper.WaitForIdle();

			Assert.AreEqual(1, dragStartingCount);
			Assert.AreEqual(1, dragEnterCount);
			Assert.AreEqual(1, dragOverCount);
		}

		[TestMethod]
		[RunsOnUIThread]
		[CombinatorialData]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_CanDrag_Child_And_Parent(bool childCanDrag)
		{
			var child = new Rectangle
			{
				Fill = new SolidColorBrush(Microsoft.UI.Colors.LightBlue),
				Width = 100,
				Height = 80,
				CanDrag = childCanDrag
			};
			var parent = new Frame
			{
				Background = new SolidColorBrush(Microsoft.UI.Colors.LightCoral),
				Width = 400,
				Height = 300,
				CanDrag = true,
				Content = child
			};

			var parentDragStartingCount = 0;
			var childDragStartingCount = 0;
			parent.DragStarting += (_, _) => parentDragStartingCount++;
			child.DragStarting += (_, _) => childDragStartingCount++;

			await UITestHelper.Load(parent);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			mouse.MoveTo(child.GetAbsoluteBoundsRect().GetCenter());
			await UITestHelper.WaitForIdle();
			mouse.Press();
			await UITestHelper.WaitForIdle();
			mouse.MoveTo(child.GetAbsoluteBoundsRect().GetCenter() + new Windows.Foundation.Point(20, 0), steps: 10);
			await UITestHelper.WaitForIdle();
			mouse.Release();
			await UITestHelper.WaitForIdle();

			if (childCanDrag)
			{
				Assert.AreEqual(1, childDragStartingCount);
				Assert.AreEqual(0, parentDragStartingCount);
			}
			else
			{
				Assert.AreEqual(0, childDragStartingCount);
				Assert.AreEqual(1, parentDragStartingCount);
			}
		}

		#endregion
#endif
	}

	internal partial class When_UpdateLayout_Then_ReentrancyNotAllowed_Element : FrameworkElement
	{
		private bool _isMeasuring, _isArranging;

		public bool Failed { get; private set; }

		protected override Size MeasureOverride(Size availableSize)
		{
			Failed |= _isMeasuring;

			if (!Failed)
			{
				_isMeasuring = true;
				UpdateLayout();
				_isMeasuring = false;
			}

			return base.MeasureOverride(availableSize);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			Failed |= _isArranging;

			if (!Failed)
			{
				_isArranging = true;
				UpdateLayout();
				_isArranging = false;
			}

			return base.ArrangeOverride(finalSize);
		}
	}
}
