using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;

namespace Uno.UI.Tests.Extensions
{
	[TestClass]
	public class Given_LayoutHelper
	{
		[TestMethod]
		public void LayoutHelper_GetMinSize()
		{
			new Border().GetMinSize()
				.Should().Be(new Size(0, 0));

			new Border { MinWidth = 100 }.GetMinSize()
				.Should().Be(new Size(100, 0));

			new Border { MinHeight = 200 }.GetMinSize()
				.Should().Be(new Size(0, 200));

			new Border { MinWidth = 100, MinHeight = 200, }.GetMinSize()
				.Should().Be(new Size(100, 200));
		}

		[TestMethod]
		public void LayoutHelper_GetMaxSize()
		{
			new Border().GetMaxSize()
				.Should().Be(new Size(double.PositiveInfinity, double.PositiveInfinity));

			new Border { MaxWidth = 100 }.GetMaxSize()
				.Should().Be(new Size(100, double.PositiveInfinity));

			new Border { MaxHeight = 200 }.GetMaxSize()
				.Should().Be(new Size(double.PositiveInfinity, 200));

			new Border { MaxWidth = 100, MaxHeight = 200, }.GetMaxSize()
				.Should().Be(new Size(100, 200));
		}

		[DataRow(",", ",", ",", "0,0", "*,*")]
		[DataRow("10,10", ",", "100,100", "10,10", "100,100")]
		[DataRow("100,100", ",", "10,10", "100,100", "100,100")]
		[DataRow("100,100", ",", ",", "100,100", "*,*")]
		[DataRow("100,100", "5,5", ",", "100,100", "100,100")]
		[DataRow("*,*", ",", ",", "*,*", "*,*")]
		[DataRow(",", "100,100", ",", "100,100", "100,100")]
		[DataRow("50,50", "100,100", "150,150", "100,100", "100,100")]
		[DataRow(",", ",", "100,100", "0,0", "100,100")]
		[DataRow("100,100", "50,50", "10,10", "100,100", "100,100")]
		[DataRow("100,100", "500,500", "10,10", "100,100", "100,100")]
		[DataRow(",", "500,500", "10,10", "10,10", "10,10")]
		[TestMethod]
		public void LayoutHelper_GetMinMax(
			string min,
			string size,
			string max,
			string expectedMin,
			string expectedMax)
		{
			var minSize = ParseSize(min);
			var sizeSize = ParseSize(size);
			var maxSize = ParseSize(max);

			var element = new Border();
			void Set(DependencyProperty dp, double value)
			{
				if (!value.IsNaN())
				{
					element.SetValue(dp, value);
				}
			}

			Set(FrameworkElement.MinWidthProperty, minSize.Width);
			Set(FrameworkElement.MinHeightProperty, minSize.Height);

			Set(FrameworkElement.WidthProperty, sizeSize.Width);
			Set(FrameworkElement.HeightProperty, sizeSize.Height);

			Set(FrameworkElement.MaxWidthProperty, maxSize.Width);
			Set(FrameworkElement.MaxHeightProperty, maxSize.Height);

			var (calculatedMin, calculatedMax) = element.GetMinMax();

			calculatedMin.Should().Be(ParseSize(expectedMin), 2, "Invalid calculated min size");
			calculatedMax.Should().Be(ParseSize(expectedMax), 2, "Invalid calculated max size");
		}

		[DataRow(",", ",", ",")]
		[DataRow("10,10", ",", ",")]
		[DataRow(",", "10,10", ",")]
		[DataRow("0,0", "10,10", "0,0")]
		[DataRow("100,10", "10,100", "10,10")]
		[DataRow("-20,-40", "*,-*", "-20,-*")]
		[DataRow("-*,*", "10,10", "-*,10")]
		[TestMethod]
		public void LayoutHelper_MinSize(string s1, string s2, string expected)
		{
			LayoutHelper.Min(ParseSize(s1), ParseSize(s2)).Should().Be(ParseSize(expected));
		}

		[DataRow(",", ",", ",")]
		[DataRow("10,10", ",", ",")]
		[DataRow(",", "10,10", ",")]
		[DataRow("0,0", "10,10", "10,10")]
		[DataRow("100,10", "10,100", "100,100")]
		[DataRow("-20,-40", "*,-*", "*,-40")]
		[DataRow("-*,*", "10,10", "10,*")]
		[TestMethod]
		public void LayoutHelper_MaxSize(string s1, string s2, string expected)
		{
			LayoutHelper.Max(ParseSize(s1), ParseSize(s2)).Should().Be(ParseSize(expected));
		}

		[DataRow(",", ",", ",")]
		[DataRow("10,10", ",", ",")]
		[DataRow(",", "10,10", ",")]
		[DataRow("20,20", "10,10", "10,10")]
		[DataRow("0,0", "100,100", "-100,-100")]
		[DataRow("-20,-40", "*,-*", "-*,*")]
		[DataRow("-*,*", "10,10", "-*,*")]
		[TestMethod]
		public void LayoutHelper_Subtract(string s1, string s2, string expected)
		{
			LayoutHelper.Subtract(ParseSize(s1), ParseSize(s2)).Should().Be(ParseSize(expected));
		}

		[DataRow("0,0,0,0", "0", "0,0,0,0")]
		[DataRow("0,0,100,100", "0", "0,0,100,100")]
		[DataRow("0,0,100,100", "10", "10,10,80,80")]
		[DataRow("0,0,100,100", "10,20", "10,20,80,60")]
		[DataRow("0,0,100,100", "10,20,30,40", "10,20,60,40")]
		[DataRow("100,100,10,10", "5,5", "105,105,0,0")]
		[DataRow("100,100,10,10", "12,12", "112,112,0,0")]
		[DataRow("100,100,10,10", "400,400", "500,500,0,0")]
		[DataRow("100,100,10,10", "-400,1400", "-300,1500,810,0")]
		[DataRow("100,100,10,10", "1400,-400", "1500,-300,0,810")]
		[TestMethod]
		public void LayoutHelper_DeflateBy(string rect, string thickness, string expected)
		{
			var sut = (Rect)rect;
			var toGrow = ParseThickness(thickness);
			var expectedRect = (Rect)expected;

			sut.DeflateBy(toGrow).Should().Be(expectedRect);
		}

		[DataRow("0,0,0,0", "0", "0,0,0,0")]
		[DataRow("0,0,100,100", "0", "0,0,100,100")]
		[DataRow("0,0,100,100", "10", "-10,-10,120,120")]
		[DataRow("0,0,100,100", "10,20", "-10,-20,120,140")]
		[DataRow("0,0,100,100", "10,20,30,40", "-10,-20,140,160")]
		[TestMethod]
		public void LayoutHelper_InflateBy(string rect, string thickness, string expected)
		{
			var sut = (Rect)rect;
			var toGrow = ParseThickness(thickness);
			var expectedRect = (Rect)expected;

			sut.InflateBy(toGrow).Should().Be(expectedRect);
		}

		[DataRow("0,0,0,0", "0,0,0,0", "0,0,0,0")]
		[DataRow("10,10,0,0", "0,0,0,0", "null")]
		[DataRow("0,0,100,100", "0,0,100,100", "0,0,100,100")]
		[DataRow("0,0,10,100", "0,0,100,10", "0,0,10,10")]
		[DataRow("0,0,100,10", "0,0,10,100", "0,0,10,10")]
		[DataRow("0,0,-100,-10", "0,0,-10,-100", "null")]
		[DataRow("-100,-100,100,100", "-10,-10,10,10", "-10,-10,10,10")]
		[DataRow("-100,-100,200,200", "-100,-100,10,10", "-100,-100,10,10")]
		[DataRow("100,200,300,400", "400,300,200,100", "400,300,0,100")]
		[TestMethod]
		public void LayoutHelper_IntersectWith(string rect1, string rect2, string expected)
		{
			((Rect)rect1)
				.IntersectWith((Rect)rect2)
				.Should()
				.Be(ParseNullableRect(expected));
		}

		private static Size ParseSize(string s)
		{
			var parts = s.Split(',');
			return new Size(ParseDouble(parts[0]), ParseDouble(parts[1]));
		}

		private static Thickness ParseThickness(string s)
		{
			var parts = s.Split(',');
			return new Thickness(
				ParseDouble(parts[0]),
				ParseDouble(parts.Length > 1 ? parts[1] : parts[0]),
				ParseDouble(parts.Length > 2 ? parts[2] : parts[0]),
				ParseDouble(parts.Length > 2 ? parts[3] : (parts.Length > 1 ? parts[1] : parts[0])));
		}

		private static Rect? ParseNullableRect(string s)
		{
			return s.IsNullOrWhiteSpace() || s == "null" ? (Rect?)null : (Rect)s;
		}

		private static double ParseDouble(string p)
		{
			if (double.TryParse(p, out var d))
			{
				return d;
			}

			switch (p)
			{
				case "*":
					return double.PositiveInfinity;
				case "-*":
					return double.NegativeInfinity;
				default:
					return double.NaN;
			}
		}
	}
}

