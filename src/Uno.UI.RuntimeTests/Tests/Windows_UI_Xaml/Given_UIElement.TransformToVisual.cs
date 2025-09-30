using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Appointments;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using AwesomeAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.Extensions;
using DependencyObjectExtensions = Uno.UI.Extensions.DependencyObjectExtensions;
using static Private.Infrastructure.TestServices.WindowHelper;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.RuntimeTests.Helpers;
using Combinatorial.MSTest;


#if __APPLE_UIKIT__
using UIKit;
#else
using Uno.UI.Extensions;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	public partial class Given_UIElement
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_TransformToVisual_WithMargin()
		{
			FrameworkElement inner = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.DarkBlue) };

			FrameworkElement container = new Border
			{
				Child = inner,
				Margin = new Thickness(1, 3, 5, 7),
				Padding = new Thickness(11, 13, 17, 19),
				BorderThickness = new Thickness(23),
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Bottom,
				Background = new SolidColorBrush(Colors.DarkSalmon)
			};
			FrameworkElement outer = new Border
			{
				Child = container,
				Padding = new Thickness(8),
				BorderThickness = new Thickness(2),
				Width = 300,
				Height = 300,
				Background = new SolidColorBrush(Colors.MediumSeaGreen)
			};

			TestServices.WindowHelper.WindowContent = outer;

			await TestServices.WindowHelper.WaitForIdle();

			string GetStr(FrameworkElement e)
			{
				var positionMatrix = ((MatrixTransform)e.TransformToVisual(outer)).Matrix;
				return $"{positionMatrix.OffsetX};{positionMatrix.OffsetY};{e.ActualWidth};{e.ActualHeight}";
			}

			var str = $"{GetStr(container)}|{GetStr(inner)}";
			Assert.AreEqual("111;105;174;178|145;141;100;100", str);
		}

#if !WINAPPSDK // Cannot create a DataTemplate on UWP
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_TransformToVisual_ThroughListView()
		{
			var listView = new ListView
			{
				ItemContainerStyle = new Style(typeof(ListViewItem))
				{
					Setters = { new Setter(ListViewItem.PaddingProperty, new Thickness(0)) }
				},
				ItemTemplate = new DataTemplate(() => new Border
				{
					Width = 200,
					Height = 100,
					Background = new SolidColorBrush(Colors.Red),
					Margin = new Thickness(0, 5),
					Child = new TextBlock().Apply(tb => tb.SetBinding(TextBlock.TextProperty, new Binding()))
				}),
				ItemsSource = Enumerable.Range(1, 10),
				Margin = new Thickness(15)
			};
			var sut = new Grid
			{
				Height = 300,
				Width = 200,
				Children = { listView }
			};

			TestServices.WindowHelper.WindowContent = sut;
			await TestServices.WindowHelper.WaitForIdle();

			AssertItem(0); // Top item, fully visible
			AssertItem(2); // Bottom item, partially visible
						   // AssertItem(5); // Overflowing item, not materialized => No container for this
						   // AssertItem(9); // Last item, definitely not materialized => No container for this

			void AssertItem(int index)
			{
				const double defaultTolerance = 1.5;
				var tolerance = defaultTolerance * Math.Min(index + 1, 3);

				var container = listView.ContainerFromIndex(index) as ContentControl
					?? throw new NullReferenceException($"Cannot find the container of item {index}");
				var border = DependencyObjectExtensions.FindFirstChild<Border>(container)
					?? throw new NullReferenceException($"Cannot find the materialized border of item {index}");

				var containerToListView = container.TransformToVisual(listView).TransformBounds(new Rect(0, 0, 42, 42));
				Assert.AreEqual(0, containerToListView.X, tolerance);
				Assert.AreEqual(0, containerToListView.X, tolerance);
				Assert.AreEqual(containerToListView.Y, ((100 + 5 * 2) * index), tolerance);
				Assert.AreEqual(42, containerToListView.Width, tolerance);
				Assert.AreEqual(42, containerToListView.Height, tolerance);

				var borderToListView = border.TransformToVisual(listView).TransformBounds(new Rect(0, 0, 42, 42));
				Assert.AreEqual(0, borderToListView.X, tolerance);
				Assert.AreEqual(borderToListView.Y, ((100 + 5 * 2) * index + 5), tolerance);
				Assert.AreEqual(42, borderToListView.Width, tolerance);
				Assert.AreEqual(42, borderToListView.Height, tolerance);

				var containerToSut = container.TransformToVisual(sut).TransformBounds(new Rect(0, 0, 42, 42));
				Assert.AreEqual(15, containerToSut.X, tolerance);
				Assert.AreEqual(containerToSut.Y, (15 + (100 + 5 * 2) * index), tolerance);
				Assert.AreEqual(42, containerToSut.Width, tolerance);
				Assert.AreEqual(42, containerToSut.Height, tolerance);

				var borderToSut = border.TransformToVisual(sut).TransformBounds(new Rect(0, 0, 42, 42));
				Assert.AreEqual(15, borderToSut.X, tolerance);
				Assert.AreEqual(borderToSut.Y, (15 + (100 + 5 * 2) * index + 5), tolerance);
				Assert.AreEqual(42, borderToSut.Width, tolerance);
				Assert.AreEqual(42, borderToSut.Height, tolerance);
			}
		}
#endif
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_TransformToVisual_Through_ListView_Scrolled()
		{
			var listView = new ListView
			{
				ItemContainerStyle = TestsResourceHelper.GetResource<Style>("NoExtraSpaceListViewContainerStyle"),
				ItemTemplate = TestsResourceHelper.GetResource<DataTemplate>("FixedSizeItemTemplate"),
				ItemsSource = Enumerable.Range(0, 20).ToArray(),
				Height = 120
			};
			var sut = new Grid
			{
				Height = 300,
				Width = 200,
				Children = { listView }
			};

			WindowContent = sut;
			await WaitForLoaded(listView);

			AssertItem(0, 0);
			AssertItem(1, 29);

			var sv = listView.FindFirstChild<ScrollViewer>();
			Assert.IsNotNull(sv);
			sv.ChangeView(null, 10, null);
			await WaitForEqual(10, () => sv.VerticalOffset);

			AssertItem(0, -10);
			AssertItem(1, 19);

			sv.ChangeView(null, 40, null);

			await WaitForEqual(40, () => sv.VerticalOffset);

			AssertItem(1, -11);

			void AssertItem(int index, double expectedY)
			{
				const double defaultTolerance = 1.5;
				var tolerance = defaultTolerance * Math.Min(index + 1, 3);

				var container = listView.ContainerFromIndex(index) as ContentControl
					?? throw new NullReferenceException($"Cannot find the container of item {index}");

				var containerToListView = container.TransformToVisual(listView).TransformBounds(new Rect(0, 0, 42, 42));

				Assert.AreEqual(expectedY, containerToListView.Y, tolerance);
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_TransformToVisual_WithTransformOrigin()
		{
			var sut = new Border
			{
				Width = 100,
				Height = 10,
				RenderTransform = new RotateTransform { Angle = 90 },
				RenderTransformOrigin = new Point(.5, .5),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
			var testRoot = new Grid
			{
				Height = 300,
				Width = 300,
				Children = { sut }
			};

			TestServices.WindowHelper.WindowContent = testRoot;
			await TestServices.WindowHelper.WaitForIdle();

			var result = sut.TransformToVisual(testRoot).TransformPoint(new Point(1, 1));

			Assert.AreEqual(154, result.X);
			Assert.AreEqual(101, result.Y);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_TransformToVisual_From_ScrollViewer()
		{
			var innerBorder = new Border
			{
				Width = 140,
				Height = 60,
				Background = new SolidColorBrush(Colors.Blue)
			};

			var SUT = new ScrollViewer
			{
				Content = new StackPanel
				{
					Children =
					{
						new Ellipse
						{
							Width = 170,
							Height = 140,
							Fill = new SolidColorBrush(Colors.Tomato)
						},
						innerBorder,
						new Ellipse
						{
							Width = 170,
							Height = 640,
							Fill = new SolidColorBrush(Colors.Tomato)
						},
					}
				}
			};

			var hostGrid = new Grid
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Children = {
					new Grid
					{
						Height = 320,
						Margin = new Thickness(76),
						Children =
						{
							SUT
						}
					}
				}
			};
			TestServices.WindowHelper.WindowContent = hostGrid;

			await TestServices.WindowHelper.WaitForLoaded(innerBorder);

			AssertTransformOffset(SUT, 76);
			AssertTransformOffset(innerBorder, 216);

			SUT.ChangeView(null, 96, null);

			await TestServices.WindowHelper.WaitForEqual(96, () => SUT.VerticalOffset);

			AssertTransformOffset(SUT, 76);
			AssertTransformOffset(innerBorder, 120);

			SUT.ChangeView(null, 2000, null);

			await TestServices.WindowHelper.WaitForEqual(520, () => SUT.VerticalOffset);

			AssertTransformOffset(SUT, 76);
			AssertTransformOffset(innerBorder, -304);

			void AssertTransformOffset(FrameworkElement element, double expectedYOffset)
			{
				var transform = element.TransformToVisual(hostGrid) as MatrixTransform;
				Assert.AreEqual(expectedYOffset, transform.Matrix.OffsetY, 1);
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[CombinatorialData]
		public async Task When_PaddedElement_Then_TransformToVisual(
			HorizontalAlignment hAlign,
			VerticalAlignment vAlign,
			[CombinatorialValues(0, 10)] int border,
			[CombinatorialValues(0, 10)] int margin)
		{
			Border sut;
			var root = new Border
			{
				Width = 200,
				Height = 200,
				BorderThickness = new Thickness(0),
				Child = sut = new Border
				{
					Margin = new Thickness(margin),
					BorderBrush = new SolidColorBrush(Colors.Red),
					BorderThickness = new Thickness(border),
					Width = 100,
					Height = 100,
					HorizontalAlignment = hAlign,
					VerticalAlignment = vAlign
				}
			};

			WindowContent = root;
			do
			{
				await WaitForIdle();
			} while (!root.IsLoaded); // kicks-in too early on UWP otherwise

			var expected = new Rect(X(hAlign, root.Width, sut.Width, margin), Y(vAlign, root.Height, sut.Height, margin), sut.Width, sut.Height);
			var actual = sut.TransformToVisual(root).TransformBounds(new Rect(0, 0, sut.Width, sut.Height));

			actual.Should(epsilon: 1).Be(expected);
		}

		[TestMethod]
		[RunsOnUIThread]
		[CombinatorialData]
		public async Task When_PaddedElementInScrollViewer_Then_TransformToVisual(
			HorizontalAlignment hAlign,
			VerticalAlignment vAlign,
			[CombinatorialValues(0, 10)] int border,
			[CombinatorialValues(0, 10)] int margin,
			bool canHorizontallyScroll,
			bool canVerticallyScroll)
		{
			Border sut;
			var root = new ScrollViewer()
			{
				Width = 200,
				Height = 200,
				BorderThickness = new Thickness(0),
				HorizontalScrollBarVisibility = canHorizontallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
				VerticalScrollBarVisibility = canVerticallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
				HorizontalScrollMode = canHorizontallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
				VerticalScrollMode = canVerticallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
				Content = sut = new Border
				{
					Margin = new Thickness(margin),
					BorderBrush = new SolidColorBrush(Colors.Red),
					BorderThickness = new Thickness(border),
					Width = 100,
					Height = 100,
					HorizontalAlignment = hAlign,
					VerticalAlignment = vAlign
				}
			};

			WindowContent = root;
			do
			{
				await WaitForIdle();
			} while (!root.IsLoaded); // kicks-in too early on UWP otherwise

			var expected = new Rect(X(hAlign, root.Width, sut.Width, margin), Y(vAlign, root.Height, sut.Height, margin), sut.Width, sut.Height);
			var actual = sut.TransformToVisual(root).TransformBounds(new Rect(0, 0, sut.Width, sut.Height));

			actual.Should(epsilon: 1).Be(expected);
		}

		[TestMethod]
		[RunsOnUIThread]
		#region DataRows
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Top, 0, 0, false, false)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Top, 10, 0, false, false)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Top, 0, 10, false, false)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Top, 10, 10, false, false)]
#if !__SKIA__ && !__WASM__
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Top, 0, 0, false, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Top, 10, 0, false, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Top, 0, 10, false, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Top, 10, 10, false, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Top, 0, 0, false, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Top, 10, 0, false, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Top, 0, 10, false, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Top, 10, 10, false, false)]
#endif
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Top, 0, 0, false, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Top, 10, 0, false, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Top, 0, 10, false, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Top, 10, 10, false, false)]
#if !__SKIA__ && !__WASM__
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Center, 0, 0, false, false)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Center, 10, 0, false, false)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Center, 0, 10, false, false)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Center, 10, 10, false, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Center, 0, 0, false, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Center, 10, 0, false, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Center, 0, 10, false, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Center, 10, 10, false, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Center, 0, 0, false, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Center, 10, 0, false, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Center, 0, 10, false, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Center, 10, 10, false, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Center, 0, 0, false, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Center, 10, 0, false, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Center, 0, 10, false, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Center, 10, 10, false, false)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Bottom, 0, 0, false, false)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Bottom, 10, 0, false, false)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Bottom, 0, 10, false, false)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Bottom, 10, 10, false, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Bottom, 0, 0, false, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Bottom, 10, 0, false, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Bottom, 0, 10, false, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Bottom, 10, 10, false, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Bottom, 0, 0, false, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Bottom, 10, 0, false, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Bottom, 0, 10, false, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Bottom, 10, 10, false, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Bottom, 0, 0, false, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Bottom, 10, 0, false, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Bottom, 0, 10, false, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Bottom, 10, 10, false, false)]
#endif
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Stretch, 0, 0, false, false)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Stretch, 10, 0, false, false)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Stretch, 0, 10, false, false)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Stretch, 10, 10, false, false)]
#if !__SKIA__ && !__WASM__
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Stretch, 0, 0, false, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Stretch, 10, 0, false, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Stretch, 0, 10, false, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Stretch, 10, 10, false, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Stretch, 0, 0, false, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Stretch, 10, 0, false, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Stretch, 0, 10, false, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Stretch, 10, 10, false, false)]
#endif
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 0, 0, false, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 10, 0, false, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 0, 10, false, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 10, 10, false, false)]

		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Top, 0, 0, true, false)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Top, 10, 0, true, false)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Top, 0, 10, true, false)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Top, 10, 10, true, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Top, 0, 0, true, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Top, 10, 0, true, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Top, 0, 10, true, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Top, 10, 10, true, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Top, 0, 0, true, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Top, 10, 0, true, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Top, 0, 10, true, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Top, 10, 10, true, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Top, 0, 0, true, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Top, 10, 0, true, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Top, 0, 10, true, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Top, 10, 10, true, false)]
#if !__SKIA__ && !__WASM__
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Center, 0, 0, true, false)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Center, 10, 0, true, false)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Center, 0, 10, true, false)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Center, 10, 10, true, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Center, 0, 0, true, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Center, 10, 0, true, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Center, 0, 10, true, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Center, 10, 10, true, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Center, 0, 0, true, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Center, 10, 0, true, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Center, 0, 10, true, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Center, 10, 10, true, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Center, 0, 0, true, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Center, 10, 0, true, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Center, 0, 10, true, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Center, 10, 10, true, false)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Bottom, 0, 0, true, false)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Bottom, 10, 0, true, false)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Bottom, 0, 10, true, false)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Bottom, 10, 10, true, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Bottom, 0, 0, true, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Bottom, 10, 0, true, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Bottom, 0, 10, true, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Bottom, 10, 10, true, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Bottom, 0, 0, true, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Bottom, 10, 0, true, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Bottom, 0, 10, true, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Bottom, 10, 10, true, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Bottom, 0, 0, true, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Bottom, 10, 0, true, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Bottom, 0, 10, true, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Bottom, 10, 10, true, false)]
#endif
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Stretch, 0, 0, true, false)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Stretch, 10, 0, true, false)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Stretch, 0, 10, true, false)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Stretch, 10, 10, true, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Stretch, 0, 0, true, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Stretch, 10, 0, true, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Stretch, 0, 10, true, false)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Stretch, 10, 10, true, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Stretch, 0, 0, true, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Stretch, 10, 0, true, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Stretch, 0, 10, true, false)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Stretch, 10, 10, true, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 0, 0, true, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 10, 0, true, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 0, 10, true, false)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 10, 10, true, false)]


		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Top, 0, 0, false, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Top, 10, 0, false, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Top, 0, 10, false, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Top, 10, 10, false, true)]
#if !__SKIA__ && !__WASM__
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Top, 0, 0, false, true)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Top, 10, 0, false, true)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Top, 0, 10, false, true)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Top, 10, 10, false, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Top, 0, 0, false, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Top, 10, 0, false, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Top, 0, 10, false, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Top, 10, 10, false, true)]
#endif
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Top, 0, 0, false, true)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Top, 10, 0, false, true)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Top, 0, 10, false, true)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Top, 10, 10, false, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Center, 0, 0, false, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Center, 10, 0, false, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Center, 0, 10, false, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Center, 10, 10, false, true)]
#if !__SKIA__ && !__WASM__
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Center, 0, 0, false, true)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Center, 10, 0, false, true)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Center, 0, 10, false, true)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Center, 10, 10, false, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Center, 0, 0, false, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Center, 10, 0, false, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Center, 0, 10, false, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Center, 10, 10, false, true)]
#endif
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Center, 0, 0, false, true)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Center, 10, 0, false, true)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Center, 0, 10, false, true)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Center, 10, 10, false, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Bottom, 0, 0, false, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Bottom, 10, 0, false, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Bottom, 0, 10, false, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Bottom, 10, 10, false, true)]
#if !__SKIA__ && !__WASM__
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Bottom, 0, 0, false, true)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Bottom, 10, 0, false, true)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Bottom, 0, 10, false, true)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Bottom, 10, 10, false, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Bottom, 0, 0, false, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Bottom, 10, 0, false, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Bottom, 0, 10, false, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Bottom, 10, 10, false, true)]
#endif
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Bottom, 0, 0, false, true)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Bottom, 10, 0, false, true)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Bottom, 0, 10, false, true)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Bottom, 10, 10, false, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Stretch, 0, 0, false, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Stretch, 10, 0, false, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Stretch, 0, 10, false, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Stretch, 10, 10, false, true)]
#if !__SKIA__ && !__WASM__
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Stretch, 0, 0, false, true)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Stretch, 10, 0, false, true)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Stretch, 0, 10, false, true)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Stretch, 10, 10, false, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Stretch, 0, 0, false, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Stretch, 10, 0, false, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Stretch, 0, 10, false, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Stretch, 10, 10, false, true)]
#endif
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 0, 0, false, true)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 10, 0, false, true)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 0, 10, false, true)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 10, 10, false, true)]


		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Top, 0, 0, true, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Top, 10, 0, true, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Top, 0, 10, true, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Top, 10, 10, true, true)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Top, 0, 0, true, true)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Top, 10, 0, true, true)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Top, 0, 10, true, true)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Top, 10, 10, true, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Top, 0, 0, true, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Top, 10, 0, true, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Top, 0, 10, true, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Top, 10, 10, true, true)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Top, 0, 0, true, true)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Top, 10, 0, true, true)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Top, 0, 10, true, true)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Top, 10, 10, true, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Center, 0, 0, true, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Center, 10, 0, true, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Center, 0, 10, true, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Center, 10, 10, true, true)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Center, 0, 0, true, true)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Center, 10, 0, true, true)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Center, 0, 10, true, true)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Center, 10, 10, true, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Center, 0, 0, true, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Center, 10, 0, true, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Center, 0, 10, true, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Center, 10, 10, true, true)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Center, 0, 0, true, true)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Center, 10, 0, true, true)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Center, 0, 10, true, true)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Center, 10, 10, true, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Bottom, 0, 0, true, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Bottom, 10, 0, true, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Bottom, 0, 10, true, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Bottom, 10, 10, true, true)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Bottom, 0, 0, true, true)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Bottom, 10, 0, true, true)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Bottom, 0, 10, true, true)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Bottom, 10, 10, true, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Bottom, 0, 0, true, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Bottom, 10, 0, true, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Bottom, 0, 10, true, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Bottom, 10, 10, true, true)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Bottom, 0, 0, true, true)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Bottom, 10, 0, true, true)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Bottom, 0, 10, true, true)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Bottom, 10, 10, true, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Stretch, 0, 0, true, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Stretch, 10, 0, true, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Stretch, 0, 10, true, true)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Stretch, 10, 10, true, true)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Stretch, 0, 0, true, true)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Stretch, 10, 0, true, true)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Stretch, 0, 10, true, true)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Stretch, 10, 10, true, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Stretch, 0, 0, true, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Stretch, 10, 0, true, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Stretch, 0, 10, true, true)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Stretch, 10, 10, true, true)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 0, 0, true, true)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 10, 0, true, true)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 0, 10, true, true)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 10, 10, true, true)]
		#endregion
		public async Task When_PaddedElementBiggerThanParentScrollViewer_Then_TransformToVisual(
			HorizontalAlignment hAlign,
			VerticalAlignment vAlign,
			int border,
			int margin,
			bool canHorizontallyScroll,
			bool canVerticallyScroll)
		{
			Border sut;
			ScrollViewer sv;
			var root = new Border
			{
				Width = 204,
				Height = 204,
				BorderThickness = new Thickness(2),
				BorderBrush = new SolidColorBrush(Colors.Blue),
				Child = sv = new ScrollViewer
				{
					Width = 200,
					Height = 200,
					BorderThickness = new Thickness(0),
					HorizontalScrollBarVisibility = canHorizontallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
					VerticalScrollBarVisibility = canVerticallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
					HorizontalScrollMode = canHorizontallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
					VerticalScrollMode = canVerticallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
					Content = sut = new Border
					{
						Margin = new Thickness(margin),
						BorderBrush = new SolidColorBrush(Colors.Red),
						BorderThickness = new Thickness(border),
						Background = new SolidColorBrush(Colors.Pink),
						Width = 300,
						Height = 300,
						HorizontalAlignment = hAlign,
						VerticalAlignment = vAlign
					}
				}
			};

			WindowContent = root;
			do
			{
				await WaitForIdle();
			} while (!root.IsLoaded); // kicks-in too early on UWP otherwise

			foreach (var offset in GetOffsets())
			{
				sv.ChangeView(offset.x, offset.y, zoomFactor: null, disableAnimation: true);

				await RetryAssert(
					$"after scrolled to ({offset.x},{offset.y}), actual rect is ",
					() =>
					{
						var expected = new Rect(margin - offset.x, margin - offset.y, sut.Width, sut.Height);
						var actual = sut.TransformToVisual(sv).TransformBounds(new Rect(0, 0, sut.Width, sut.Height));

						actual.Should(epsilon: 1).Be(expected);
					});
			}

			IEnumerable<(double x, double y)> GetOffsets()
			{
				yield return (0, 0);

				if (canHorizontallyScroll)
				{
					yield return (sv.ScrollableWidth / 2, 0);
					yield return (sv.ScrollableWidth, 0);
				}

				if (canVerticallyScroll)
				{
					yield return (0, sv.ScrollableHeight / 2);
					yield return (0, sv.ScrollableHeight);
				}

				if (canHorizontallyScroll && canVerticallyScroll)
				{
					yield return (sv.ScrollableWidth / 2, sv.ScrollableHeight / 2);
					yield return (sv.ScrollableWidth, sv.ScrollableHeight);
				}
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Not_InLiveVisualTree()
		{
			var inner = new Rectangle
			{
				Margin = new Thickness(10),
				Width = 150,
				Height = 150,
				Fill = new SolidColorBrush(Microsoft.UI.Colors.Green)
			};
			var outer = new Border
			{
				HorizontalAlignment = HorizontalAlignment.Right,
				Child = inner
			};
			var root = new Grid
			{
				Width = 200,
				Margin = new Thickness(10),
				Children =
				{
					outer
				}
			};
			await UITestHelper.Load(root);

			Assert.IsFalse(((MatrixTransform)inner.TransformToVisual(null)).Matrix.IsIdentity);
			Assert.IsFalse(((MatrixTransform)outer.TransformToVisual(null)).Matrix.IsIdentity);
			Assert.IsFalse(((MatrixTransform)root.TransformToVisual(null)).Matrix.IsIdentity);
			Assert.IsFalse(((MatrixTransform)inner.TransformToVisual(outer)).Matrix.IsIdentity);
			Assert.IsFalse(((MatrixTransform)outer.TransformToVisual(root)).Matrix.IsIdentity);
			Assert.IsFalse(((MatrixTransform)root.TransformToVisual((UIElement)root.Parent)).Matrix.IsIdentity);

			WindowContent = null;

			Assert.IsTrue(((MatrixTransform)inner.TransformToVisual(null)).Matrix.IsIdentity);
			Assert.IsTrue(((MatrixTransform)outer.TransformToVisual(null)).Matrix.IsIdentity);
			Assert.IsTrue(((MatrixTransform)root.TransformToVisual(null)).Matrix.IsIdentity);
			Assert.IsTrue(((MatrixTransform)inner.TransformToVisual(outer)).Matrix.IsIdentity);
			Assert.IsTrue(((MatrixTransform)outer.TransformToVisual(root)).Matrix.IsIdentity);
			Assert.IsTrue(((MatrixTransform)root.TransformToVisual((UIElement)root.Parent)).Matrix.IsIdentity);
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task When_Basic_Triple_Nesting()
		{
			var setup = await SetupTripleScrollViewerScenarioAsync();

			var innerPoint = GetTransformedPoint(setup.item, setup.inner);
			var middlePoint = GetTransformedPoint(setup.item, setup.middle);
			var outerPoint = GetTransformedPoint(setup.item, setup.outer);

			Assert.AreEqual(new Point(60, 60), innerPoint);
			Assert.AreEqual(new Point(80, 80), middlePoint);
			Assert.AreEqual(new Point(100, 100), outerPoint);
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task When_Triple_Nesting_Margin_Item()
		{
			var setup = await SetupTripleScrollViewerScenarioAsync();

			setup.item.Margin = new Thickness(0, 500, 0, 0);

			await WaitForIdle();

			var innerPoint = GetTransformedPoint(setup.item, setup.inner);
			var middlePoint = GetTransformedPoint(setup.item, setup.middle);
			var outerPoint = GetTransformedPoint(setup.item, setup.outer);

			Assert.AreEqual(new Point(60, 520), innerPoint);
			Assert.AreEqual(new Point(80, 540), middlePoint);
			Assert.AreEqual(new Point(100, 560), outerPoint);
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task When_Triple_Nesting_Scroll_Paddings()
		{
			var setup = await SetupTripleScrollViewerScenarioAsync();

			setup.item.Margin = new Thickness(0, 500, 0, 0);
			setup.middle.Padding = new Thickness(30, 300, 0, 0);
			setup.outer.Padding = new Thickness(0, 200, 10, 0);

			await WaitForIdle();

			var innerPoint = GetTransformedPoint(setup.item, setup.inner);
			var middlePoint = GetTransformedPoint(setup.item, setup.middle);
			var outerPoint = GetTransformedPoint(setup.item, setup.outer);

			Assert.AreEqual(new Point(80, 520), innerPoint);
			Assert.AreEqual(new Point(110, 820), middlePoint);
			Assert.AreEqual(new Point(110, 1020), outerPoint);
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task When_Double_Nesting_Scroll_Offsets()
		{
			var outer = new ScrollViewer()
			{
				Padding = new Thickness(20),
				Width = 300,
				Height = 300,
				Background = new SolidColorBrush(Colors.Red)
			};
			var inner = new ScrollViewer()
			{
				Padding = new Thickness(20),
				Width = 400,
				Height = 400,
				Background = new SolidColorBrush(Colors.Yellow)
			};
			var item = new Border()
			{
				Width = 500,
				Height = 500,
				Background = new SolidColorBrush(Colors.Green)
			};
			outer.Content = inner;
			inner.Content = item;

			WindowContent = outer;

			await WaitForLoaded(outer);
			await WaitForIdle();

			inner.ChangeView(0, 100, null, true);

			var innerPoint = GetTransformedPoint(item, inner);
			var outerPoint = GetTransformedPoint(item, outer);

			Assert.AreEqual(new Point(20, -80), innerPoint);
			Assert.AreEqual(new Point(40, -60), outerPoint);
		}

#if __ANDROID__
		[TestMethod]
		[RequiresFullWindow]
		[RunsOnUIThread]
		public async Task When_Offset_Within_Native_View()
		{
			var item = new Border()
			{
				Width = 100,
				Height = 100,
				Background = new SolidColorBrush(Colors.Red)
			};

			var innerNativeContainer = new Android.Widget.LinearLayout(ContextHelper.Current);
			innerNativeContainer.SetPadding(33, 22, 0, 0);
			innerNativeContainer.AddChild(item);

			var outerNativeContainer = new Android.Widget.LinearLayout(ContextHelper.Current);
			outerNativeContainer.SetPadding(7, 8, 0, 0);
			outerNativeContainer.AddChild(innerNativeContainer);

			var root = new ContentControl { Content = outerNativeContainer };

			WindowContent = root;
			await WaitForLoaded(root);
			await WaitForIdle();

			var point = item.TransformToVisual(root).TransformPoint(default);

			Assert.AreEqual(new Point(40, 30), point);
		}
#endif

		private static double X(HorizontalAlignment alignment, double available, double used, double margin)
			=> alignment switch
			{
				HorizontalAlignment.Left => 0 + margin,
				HorizontalAlignment.Center => (available - used) / 2.0,
				HorizontalAlignment.Right => available - used - margin,
				HorizontalAlignment.Stretch => (available - used) / 2.0,
				_ => 0
			};

		private static double Y(VerticalAlignment alignment, double available, double used, double margin)
			=> alignment switch
			{
				VerticalAlignment.Top => 0 + margin,
				VerticalAlignment.Center => (available - used) / 2.0,
				VerticalAlignment.Bottom => available - used - margin,
				VerticalAlignment.Stretch => (available - used) / 2.0,
				_ => 0
			};

		private async Task RetryAssert(string scope, Action assertion)
		{
			var attempt = 0;
			while (true)
			{
				try
				{
					// On UWP we might not be scrolled properly to the right offset (even is we had disabled animation),
					// so we just retry assertion as long as it fails, for up to 300 ms.
					// Using the ViewChanged event is not reliable enough neither.

					using var _ = new AssertionScope(scope);
					assertion();

					break;
				}
				catch (Exception)
				{
					if (attempt++ >= 30)
					{
						throw;
					}

					await Task.Delay(10);
				}
			}
		}

		private async Task<(ScrollViewer outer, ScrollViewer middle, ScrollViewer inner, Border item)> SetupTripleScrollViewerScenarioAsync()
		{
			var outer = new ScrollViewer()
			{
				Padding = new Thickness(20),
				Width = 300,
				Height = 300,
				Background = new SolidColorBrush(Colors.Red)
			};
			var middle = new ScrollViewer()
			{
				Padding = new Thickness(20),
				Background = new SolidColorBrush(Colors.Orange)
			};
			var inner = new ScrollViewer()
			{
				Padding = new Thickness(20),
				Background = new SolidColorBrush(Colors.Yellow)
			};
			var item = new Border()
			{
				Width = 100,
				Height = 100,
				Background = new SolidColorBrush(Colors.Green)
			};
			outer.Content = middle;
			middle.Content = inner;
			inner.Content = item;

			WindowContent = outer;

			await WaitForLoaded(outer);
			await WaitForIdle();

			return (outer, middle, inner, item);
		}

		private Point GetTransformedPoint(UIElement from, UIElement to)
		{
			var transform = from.TransformToVisual(to);
			return transform.TransformPoint(default(Point));
		}
	}
}
