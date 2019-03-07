using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Uno;
using Uno.Extensions;
using Uno.Logging;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Uno.Disposables;
using System.Text;
using System.Threading.Tasks;
using View = Windows.UI.Xaml.FrameworkElement;

namespace Uno.UI.Tests.BorderTests
{
	[TestClass]
	public class Given_Border : Context
	{
		[TestMethod]
		public void When_Border_Has_Fixed_Size()
		{
			var fixedSize = new Windows.Foundation.Size(100, 100);

			var SUT = new Border()
			{
				Width = fixedSize.Width,
				Height = fixedSize.Height
			};

			SUT.Measure(new Windows.Foundation.Size(500, 500));
			var measuredSize = SUT.DesiredSize;
			Assert.AreEqual(fixedSize, measuredSize);
		}

        [TestMethod]
        public void When_Border_Has_Margin()
        {
            var fixedSize = new Windows.Foundation.Size(100, 120);
            var margin = new Thickness(10,20,30,40);
            var totalSize = new Windows.Foundation.Size(fixedSize.Width + margin.Left + margin.Right, fixedSize.Height + margin.Top + margin.Bottom);

            var SUT = new Border()
            {
                Width = fixedSize.Width,
                Height = fixedSize.Height,
                Margin = margin,
            };

            SUT.Measure(new Windows.Foundation.Size(500, 500));
            var measuredSize = SUT.DesiredSize;
            Assert.AreEqual(totalSize, measuredSize);
        }

		[TestMethod]
		public void When_Child_Has_Fixed_Size_Smaller_than_Parent()
		{
			var parentSize = new Windows.Foundation.Size(100, 100);
			var childSize = new Windows.Foundation.Size(500, 500);

			var SUT = new Border()
			{
				Width = parentSize.Width,
				Height = parentSize.Height
			};

			var child = new View()
			{
				Width = childSize.Width,
				Height = childSize.Height
			};

			SUT.AddChild(child);

			SUT.Measure(new Windows.Foundation.Size(100, 100));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0,100, 100));

			Assert.AreEqual(parentSize, measuredSize);
			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, parentSize.Width, parentSize.Height), child.Arranged);
		}

		[TestMethod]
		public void When_Child_Is_Stretch()
		{
			var parentSize = new Windows.Foundation.Size(500, 500);

			var SUT = new Border()
			{
				Width = parentSize.Width,
				Height = parentSize.Height
			};

			var child = new View();

			SUT.AddChild(child);

			SUT.Measure(new Windows.Foundation.Size(500, 500));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0,500, 500));

			Assert.AreEqual(parentSize, measuredSize);
			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, parentSize.Width, parentSize.Height), child.Arranged);
		}

		[TestMethod]
		public void When_Child_Is_Not_Stretch()
		{
			var parentSize = new Windows.Foundation.Size(500, 500);

			var SUT = new Border()
			{
				Width = parentSize.Width,
				Height = parentSize.Height
			};

			var child = new View()
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top
			};

			SUT.AddChild(child);

			SUT.Measure(new Windows.Foundation.Size(500, 500));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0,500, 500));

			Assert.AreEqual(parentSize, measuredSize);
			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 0, 0), child.Arranged);
		}

		[TestMethod]
		public void When_Child_Is_Stretch_With_MaxSize()
		{
			var parentSize = new Windows.Foundation.Size(500, 500);
			var maxSize = new Windows.Foundation.Size(100, 100);

			var SUT = new Border()
			{
				Width = parentSize.Width,
				Height = parentSize.Height
			};

			var child = new View()
			{
				MaxWidth = maxSize.Width,
				MaxHeight = maxSize.Height
			};

			SUT.AddChild(child);

			SUT.Measure(new Windows.Foundation.Size(500, 500));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0,500, 500));

			Assert.AreEqual(parentSize, measuredSize);
			Assert.AreEqual(new Windows.Foundation.Rect((SUT.Width - maxSize.Width)/2, (SUT.Height - maxSize.Height) / 2, maxSize.Width, maxSize.Height), child.Arranged);
		}

		[TestMethod]
		public void When_Child_Is_Not_Stretch_With_MinSize()
		{
			var parentSize = new Windows.Foundation.Size(500, 500);
			var minSize = new Windows.Foundation.Size(100, 100);

			var SUT = new Border()
			{
				Width = parentSize.Width,
				Height = parentSize.Height
			};

			var child = new View()
			{
				MinWidth = minSize.Width,
				MinHeight = minSize.Height,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top
			};

			SUT.AddChild(child);

			SUT.Measure(new Windows.Foundation.Size(500, 500));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0,500, 500));

			Assert.AreEqual(parentSize, measuredSize);
			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, minSize.Width, minSize.Height), child.Arranged);
		}

		[TestMethod]
		public void When_Child_Is_Stretch_With_MaxSize_And_MinSize()
		{
			var parentSize = new Windows.Foundation.Size(500, 500);
			var maxSize = new Windows.Foundation.Size(100, 100);
			var minSize = new Windows.Foundation.Size(50, 50);

			var SUT = new Border()
			{
				Width = parentSize.Width,
				Height = parentSize.Height
			};

			var child = new View()
			{
				MaxWidth = maxSize.Width,
				MaxHeight = maxSize.Height,
				MinWidth = minSize.Width,
				MinHeight = minSize.Height
			};

			SUT.AddChild(child);

			SUT.Measure(new Windows.Foundation.Size(500, 500));
			var measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0,500, 500));

			Assert.AreEqual(parentSize, measuredSize);
			Assert.AreEqual(new Windows.Foundation.Rect((SUT.Width - maxSize.Width) / 2, (SUT.Height - maxSize.Height) / 2, maxSize.Width, maxSize.Height), child.Arranged);

			child.HorizontalAlignment = HorizontalAlignment.Left;
			child.VerticalAlignment = VerticalAlignment.Top;

			SUT.Measure(new Windows.Foundation.Size(500, 500));
			measuredSize = SUT.DesiredSize;
			SUT.Arrange(new Windows.Foundation.Rect(0, 0,500, 500));

			Assert.AreEqual(parentSize, measuredSize);
			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, minSize.Width, minSize.Height), child.Arranged);
		}
	}
}
