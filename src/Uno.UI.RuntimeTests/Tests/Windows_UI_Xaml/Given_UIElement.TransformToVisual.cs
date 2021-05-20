using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.Extensions;
using Uno.UI.Extensions;
using DependencyObjectExtensions = Uno.UI.Extensions.DependencyObjectExtensions;
#if __IOS__
using UIKit;
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
				Margin = ThicknessHelper.FromLengths(1, 3, 5, 7),
				Padding = ThicknessHelper.FromLengths(11, 13, 17, 19),
				BorderThickness = ThicknessHelper.FromUniformLength(23),
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Bottom,
				Background = new SolidColorBrush(Colors.DarkSalmon)
			};
			FrameworkElement outer = new Border
			{
				Child = container,
				Padding = ThicknessHelper.FromUniformLength(8),
				BorderThickness = ThicknessHelper.FromUniformLength(2),
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

#if !WINDOWS_UWP // Cannot create a DataTemplate on UWP
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
				Assert.AreEqual(containerToListView.X, 0, tolerance);
				Assert.AreEqual(containerToListView.X, 0, tolerance);
				Assert.AreEqual(containerToListView.Y, ((100 + 5 * 2) * index), tolerance);
				Assert.AreEqual(containerToListView.Width, 42, tolerance);
				Assert.AreEqual(containerToListView.Height, 42, tolerance);

				var borderToListView = border.TransformToVisual(listView).TransformBounds(new Rect(0, 0, 42, 42));
				Assert.AreEqual(borderToListView.X, 0, tolerance);
				Assert.AreEqual(borderToListView.Y, ((100 + 5 * 2) * index + 5), tolerance);
				Assert.AreEqual(borderToListView.Width, 42, tolerance);
				Assert.AreEqual(borderToListView.Height, 42, tolerance);

				var containerToSut = container.TransformToVisual(sut).TransformBounds(new Rect(0, 0, 42, 42));
				Assert.AreEqual(containerToSut.X, 15, tolerance);
				Assert.AreEqual(containerToSut.Y, (15 + (100 + 5 * 2) * index), tolerance);
				Assert.AreEqual(containerToSut.Width, 42, tolerance);
				Assert.AreEqual(containerToSut.Height, 42, tolerance);

				var borderToSut = border.TransformToVisual(sut).TransformBounds(new Rect(0, 0, 42, 42));
				Assert.AreEqual(borderToSut.X, 15, tolerance);
				Assert.AreEqual(borderToSut.Y, (15 + (100 + 5 * 2) * index + 5), tolerance);
				Assert.AreEqual(borderToSut.Width, 42, tolerance);
				Assert.AreEqual(borderToSut.Height, 42, tolerance);
			}
		}
#endif

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
	}
}
