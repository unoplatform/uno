using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Drawing;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.ScrollBarTests
{
	[TestFixture]
	public partial class UnoSamplesTests_ScrollBar : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // This test cannot run on Android/iOS, because the ScrollBar buttons do not react to touch (which is valid MUX behavior).
		public void ScrollBar_Vertical()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ScrollBar.ScrollBar_Simple");

			var indicatorModeCombo = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("IndicatorModeCombo");
			var verticalScrollBar = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("VerticalScrollBar");
			var verticalValue = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("VerticalValue");
			var scrollValue = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("scrollValue");

			var verticalScrollBarThumb = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("VerticalScrollBar").Descendant().Marked("VerticalThumb");
			var verticalScrollBarSmallDecrease = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("VerticalScrollBar").Descendant().Marked("VerticalSmallDecrease");
			var verticalScrollBarLargeDecrease = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("VerticalScrollBar").Descendant().Marked("VerticalLargeDecrease");
			var verticalScrollBarSmallIncrease = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("VerticalScrollBar").Descendant().Marked("VerticalSmallIncrease");
			var verticalScrollBarLargeIncrease = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("VerticalScrollBar").Descendant().Marked("VerticalLargeIncrease");

			_app.WaitForElement(indicatorModeCombo);

			TakeScreenshot("Startup");

			indicatorModeCombo.SetDependencyPropertyValue("SelectedValue", "MouseIndicator");

			TakeScreenshot("initial indicators");

			verticalScrollBarSmallDecrease.FastTap();

			_app.WaitForText(verticalValue, "99.9");
			_app.WaitForText(scrollValue, "Vertical Scroll: SmallDecrement, 99.9");

			verticalScrollBarLargeDecrease.FastTap();

			_app.WaitForText(verticalValue, "98.9");
			_app.WaitForText(scrollValue, "Vertical Scroll: LargeDecrement, 98.9");

			verticalScrollBarSmallIncrease.FastTap();

			_app.WaitForText(verticalValue, "99");
			_app.WaitForText(scrollValue, "Vertical Scroll: SmallIncrement, 99");

			verticalScrollBarLargeIncrease.FastTap();

			_app.WaitForText(verticalValue, "100");
			_app.WaitForText(scrollValue, "Vertical Scroll: LargeIncrement, 100");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // This test cannot run on Android/iOS, because the ScrollBar buttons do not react to touch (which is valid MUX behavior).
		public void ScrollBar_Horizontal()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ScrollBar.ScrollBar_Simple");

			var indicatorModeCombo = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("IndicatorModeCombo");
			var horizontalScrollBar = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("HorizontalScrollBar");
			var horizontalValue = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("HorizontalValue");
			var scrollValue = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("scrollValue");

			var horizontalScrollBarThumb = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("HorizontalScrollBar").Descendant().Marked("HorizontalThumb");
			var horizontalScrollBarSmallDecrease = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("HorizontalScrollBar").Descendant().Marked("HorizontalSmallDecrease");
			var horizontalScrollBarLargeDecrease = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("HorizontalScrollBar").Descendant().Marked("HorizontalLargeDecrease");
			var horizontalScrollBarSmallIncrease = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("HorizontalScrollBar").Descendant().Marked("HorizontalSmallIncrease");
			var horizontalScrollBarLargeIncrease = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("HorizontalScrollBar").Descendant().Marked("HorizontalLargeIncrease");

			_app.WaitForElement(indicatorModeCombo);

			TakeScreenshot("Startup");

			indicatorModeCombo.SetDependencyPropertyValue("SelectedValue", "MouseIndicator");

			TakeScreenshot("initial indicators");

			horizontalScrollBarSmallDecrease.FastTap();

			_app.WaitForText(horizontalValue, "99.9");
			_app.WaitForText(scrollValue, "Horizontal Scroll: SmallDecrement, 99.9");

			horizontalScrollBarLargeDecrease.FastTap();

			_app.WaitForText(horizontalValue, "98.9");
			_app.WaitForText(scrollValue, "Horizontal Scroll: LargeDecrement, 98.9");

			horizontalScrollBarSmallIncrease.FastTap();

			_app.WaitForText(horizontalValue, "99");
			_app.WaitForText(scrollValue, "Horizontal Scroll: SmallIncrement, 99");

			horizontalScrollBarLargeIncrease.FastTap();

			_app.WaitForText(horizontalValue, "100");
			_app.WaitForText(scrollValue, "Horizontal Scroll: LargeIncrement, 100");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)]//, Platform.Browser)] // Android: https://github.com/unoplatform/uno/issues/3009
		public void ScrollBar_HorizontalThumb()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ScrollBar.ScrollBar_Simple");

			var indicatorModeCombo = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("IndicatorModeCombo");
			var horizontalScrollBar = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("HorizontalScrollBar");
			var horizontalValue = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("HorizontalValue");
			var scrollValue = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("scrollValue");

			var horizontalScrollBarThumb = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("HorizontalScrollBar").Descendant().Marked("HorizontalThumb");
			var horizontalScrollBarSmallDecrease = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("HorizontalScrollBar").Descendant().Marked("HorizontalSmallDecrease");

			_app.WaitForElement(indicatorModeCombo);

			TakeScreenshot("Startup");

			indicatorModeCombo.SetDependencyPropertyValue("SelectedValue", "MouseIndicator");

			TakeScreenshot("initial indicators");

			var thumbResult = _app.Query(horizontalScrollBarThumb).First();

			horizontalScrollBarSmallDecrease.FastTap();
			_app.WaitForText(horizontalValue, "99.9");

			_app.DragCoordinates(thumbResult.Rect.CenterX, thumbResult.Rect.CenterY, thumbResult.Rect.CenterX + 10, thumbResult.Rect.CenterY);

			try
			{
				_app.WaitFor(() => scrollValue.GetDependencyPropertyValue<string>("Text")?.StartsWith("Horizontal Scroll: EndScroll, 126.56666") ?? false);
			}
			catch (TimeoutException error)
			{
				throw new InvalidOperationException($"Failed to get the expected 126.566, got '{scrollValue.GetDependencyPropertyValue<string>("Text")}'.", error);
			}
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)]//, Platform.Browser)] // Android: https://github.com/unoplatform/uno/issues/3009
		public void ScrollBar_VerticalThumb()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ScrollBar.ScrollBar_Simple");

			var indicatorModeCombo = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("IndicatorModeCombo");
			var verticalScrollBar = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("VerticalScrollBar");
			var verticalValue = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("VerticalValue");
			var scrollValue = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("scrollValue");

			var verticalScrollBarThumb = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("VerticalScrollBar").Descendant().Marked("VerticalThumb");
			var verticalScrollBarSmallDecrease = _app.Marked("ScrollBar_Simple_Root").Descendant().Marked("VerticalScrollBar").Descendant().Marked("VerticalSmallDecrease");

			_app.WaitForElement(indicatorModeCombo);

			TakeScreenshot("Startup");

			indicatorModeCombo.SetDependencyPropertyValue("SelectedValue", "MouseIndicator");

			TakeScreenshot("initial indicators");

			var thumbResult = _app.Query(verticalScrollBarThumb).First();

			verticalScrollBarSmallDecrease.FastTap();
			_app.WaitForText(verticalValue, "99.9");

			_app.DragCoordinates(thumbResult.Rect.CenterX, thumbResult.Rect.CenterY, thumbResult.Rect.CenterX, thumbResult.Rect.CenterY + 10);

			try
			{
				_app.WaitFor(() => scrollValue.GetDependencyPropertyValue<string>("Text")?.StartsWith("Vertical Scroll: EndScroll, 126.56666") ?? false);
			}
			catch (TimeoutException error)
			{
				throw new InvalidOperationException($"Failed to get the expected 126.566, got '{scrollValue.GetDependencyPropertyValue<string>("Text")}'.", error);
			}
		}
	}
}
