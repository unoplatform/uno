using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Uno;
using Uno.Extensions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Text;
using System.Threading.Tasks;
using View = Windows.UI.Xaml.FrameworkElement;
using Windows.Foundation;

namespace Uno.UI.Tests.CustomPanelTests
{
	[TestClass]
	public class Given_CustomPanel
	{
		[TestMethod]
		public void When_Measure_Empty()
		{
			var SUT = new MyPanel() { Name = "test" };

			SUT.Measure(default(Windows.Foundation.Size));
			var size = SUT.DesiredSize;
			SUT.Arrange(default(Windows.Foundation.Rect));

			Assert.AreEqual(default(Windows.Foundation.Size), size);
			Assert.IsTrue(SUT.GetChildren().None());
		}

		[TestMethod]
		public void When_Measure_OneItem()
		{
			var SUT = new MyPanel() { Name = "test" };

			var item1 = new Border() { Width = 10, Height = 10 };
			SUT.Children.Add(item1);

			SUT.Measure(new Size(20, 20));
			var size = SUT.DesiredSize;
			SUT.Arrange(new Rect(0, 0, 10, 10));

			Assert.AreEqual(new Size(10, 10), size);
			Assert.AreEqual(new Rect(0, 0, 10, 10), item1.Arranged);
			Assert.AreEqual(1, SUT.GetChildren().Count());
		}

		public partial class MyPanel : Panel
		{
			protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
			{
				foreach (var child in Children)
				{
					child.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
				}

				return finalSize;
			}

			protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
			{
				double width = 0, height = 0;

				foreach (var child in Children)
				{
					child.Measure(availableSize);

					width = Math.Max(width, child.DesiredSize.Width);
					height = Math.Max(height, child.DesiredSize.Height);
				}

				return new Size(width, height);
			}
		}
	}
}
