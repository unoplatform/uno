using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public partial class Given_Slider_ThumbAtMaximum_12401
	{
		// Reproduction for https://github.com/unoplatform/uno/issues/12401
		// On the Uno Gallery InfoBadge Opacity sample, the Slider is configured
		// with Minimum=0 / Maximum=1 (fractional range) and the thumb does not
		// travel all the way to the right edge of the track even when Value=Max.
		// With a horizontal slider of known width, Value=Max should place the
		// thumb such that the left edge of the HorizontalDecreaseRect fills
		// the full trackable length (track width minus thumb width).
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Value_At_Maximum_One_Thumb_Reaches_End_12401()
		{
			var slider = new MySlider12401
			{
				Minimum = 0,
				Maximum = 1,
				Value = 1,
				StepFrequency = 0.01,
				Width = 320,
				Orientation = Orientation.Horizontal,
			};

			WindowHelper.WindowContent = slider;
			await WindowHelper.WaitForLoaded(slider);
			await WindowHelper.WaitForIdle();

			Assert.IsNotNull(slider.HorizontalThumb, "HorizontalThumb template part should be present");
			Assert.IsNotNull(slider.HorizontalDecreaseRect, "HorizontalDecreaseRect template part should be present");

			var expectedDecreaseWidth = slider.ActualWidth - slider.HorizontalThumb.ActualWidth;
			Assert.IsTrue(
				expectedDecreaseWidth > 0,
				$"Sanity: slider width ({slider.ActualWidth}) must exceed thumb width ({slider.HorizontalThumb.ActualWidth}).");

			Assert.AreEqual(
				expectedDecreaseWidth,
				slider.HorizontalDecreaseRect.ActualWidth,
				delta: 1.0,
				message: $"At Value=Maximum=1 the decrease rect should span the full trackable length. " +
					$"Actual={slider.HorizontalDecreaseRect.ActualWidth}, expected={expectedDecreaseWidth}. " +
					$"See https://github.com/unoplatform/uno/issues/12401");
		}

		private partial class MySlider12401 : Slider
		{
			public Microsoft.UI.Xaml.Shapes.Rectangle HorizontalDecreaseRect { get; private set; }
			public Thumb HorizontalThumb { get; private set; }

			protected override void OnApplyTemplate()
			{
				base.OnApplyTemplate();
				HorizontalDecreaseRect = GetTemplateChild("HorizontalDecreaseRect") as Microsoft.UI.Xaml.Shapes.Rectangle;
				HorizontalThumb = GetTemplateChild("HorizontalThumb") as Thumb;
			}
		}
	}
}
