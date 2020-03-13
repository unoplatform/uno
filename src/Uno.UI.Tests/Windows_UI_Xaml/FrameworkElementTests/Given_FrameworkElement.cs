using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Uno.UI.Tests.Windows_UI_Xaml.FrameworkElementTests
{
	[TestClass]
#if !NET461
	[RuntimeTests.RunsOnUIThread]
#endif
	public class Given_FrameworkElement
	{
		[TestMethod]
		public void When_LayoutUpdated()
		{
			var SUT = new Grid();

			var item1 = new Border();

			var sutLayoutUpdatedCount = 0;

			SUT.LayoutUpdated += delegate
			{
				sutLayoutUpdatedCount++;
			};

			var item1LayoutUpdatedCount = 0;
			item1.LayoutUpdated += delegate
			{
				item1LayoutUpdatedCount++;
			};

			SUT.Children.Add(item1);

			SUT.Measure(new Size(1, 1));
			SUT.Arrange(new Rect(0, 0, 1, 1));

			var sutLayoutUpdate1 = sutLayoutUpdatedCount;
			var item1LayoutUpdate1 = item1LayoutUpdatedCount;

			SUT.Measure(new Size(2, 2));
			SUT.Arrange(new Rect(0, 0, 2, 2));

			var sutLayoutUpdate2 = sutLayoutUpdatedCount;
			var item1LayoutUpdate2 = item1LayoutUpdatedCount;

			SUT.Arrange(new Rect(0, 0, 2, 2));

			using (new AssertionScope())
			{
#if __ANDROID__
				// Android has an issue where LayoutUpdate is called twice, caused by the presence
				// of two calls to arrange (Arrange, ArrangeElement(this)) in FrameworkElement.
				// Failing to call the first Arrange makes some elements fail to have a proper size in
				// some yet unknown conditions.
				// Issue: https://github.com/unoplatform/uno/issues/2769
				sutLayoutUpdate1.Should().Be(2, "sut-before");
				sutLayoutUpdate2.Should().Be(4, "sut-after");
#else
				sutLayoutUpdate1.Should().Be(1, "sut-before");
				sutLayoutUpdate2.Should().Be(2, "sut-after");
#endif

#if __ANDROID__
				item1LayoutUpdate1.Should().Be(1, "item1-before");
				item1LayoutUpdate2.Should().Be(2, "item1-after");
#endif
			}
		}

		[TestMethod]
		public void When_MaxWidth_NaN()
		{
			var SUT = new ContentControl
			{
				MaxWidth = double.NaN,
				MaxHeight = double.NaN,
				Content = new Border { Width = 10, Height = 15 }
			};

			var grid = new Grid
			{
				Width = 32,
				Height = 47
			};

			grid.Children.Add(SUT);

			grid.Measure(new Size(1000, 1000));
			grid.Arrange(new Rect(default(Point), grid.DesiredSize));

			using (new AssertionScope())
			{
				grid.ActualWidth.Should().Be(32d, "width");
				grid.ActualHeight.Should().Be(47d, "height");
			}
		}
	}
}
