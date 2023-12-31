using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.UI.Xaml;

namespace Uno.UI.Tests.Windows_UI_Xaml.FrameworkElementTests
{
	[TestClass]
#if !IS_UNIT_TESTS
	[RuntimeTests.RunsOnUIThread]
#endif
	public partial class Given_FrameworkElement
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

			for (var i = 0; i == 0 || (SUT.IsMeasureDirtyOrMeasureDirtyPath && i < 10); i++)
			{
				grid.Measure(new Size(1000, 1000));
			}

			grid.DesiredSize.Should().Be(new Size(32, 47), because: "Desired Size before Arrange");

			grid.Arrange(new Rect(default(Point), grid.DesiredSize));

			using (new AssertionScope())
			{
				grid.DesiredSize.Should().Be(new Size(32, 47), because: "Desired Size");
				grid.ActualWidth.Should().Be(32d, "ActualWidth");
				grid.ActualHeight.Should().Be(47d, "ActualHeight");
			}
		}

		[TestMethod]
		public void When_SuppressIsEnabled()
		{
			var SUT = new MyEnabledTestControl();

			SUT.IsEnabled = true;

			SUT.PublicSuppressIsEnabled(true);
			Assert.IsFalse(SUT.IsEnabled);

			SUT.IsEnabled = false;
			Assert.IsFalse(SUT.IsEnabled);

			SUT.IsEnabled = true;
			Assert.IsFalse(SUT.IsEnabled);

			SUT.PublicSuppressIsEnabled(false);
			Assert.IsTrue(SUT.IsEnabled);
		}

		[TestMethod]
		public void When_DP_IsEnabled_Null()
		{
			var grid = new UserControl();

			grid.SetValue(Control.IsEnabledProperty, null);
		}
	}

	public partial class MyEnabledTestControl : ContentControl
	{
		public void PublicSuppressIsEnabled(bool suppress) => SuppressIsEnabled(suppress);
	}
}
