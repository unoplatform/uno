using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ListView
{
	public partial class ProportionalPanel : Panel
	{
		// source from: src\Umbrella.View\Panels\ProportionalPanel\ProportionalPanel.cs

		public enum ProportionMode
		{
			HeightFollowsWidth,
			WidthFollowsHeight
		}

		[DefaultValue(ProportionMode.HeightFollowsWidth)]
		public ProportionMode Mode { get; set; }

		[DefaultValue(1.0)]
		public double Ratio { get; set; } = 1.0;

		protected override Size MeasureOverride(Size availableSize)
		{
			var initialAvailableSize = availableSize;

			var calculatedWidth = availableSize.Height * Ratio;
			var calculatedHeight = availableSize.Width / Ratio;

			// Assign Height/Width according to ProportionMode
			if (Mode == ProportionMode.HeightFollowsWidth)
			{
				if (calculatedHeight <= availableSize.Height)
				{
					availableSize.Height = calculatedHeight;
				}
				else
				{
					availableSize.Width = calculatedWidth;
				}
			}
			else if (Mode == ProportionMode.WidthFollowsHeight)
			{
				if (calculatedWidth <= availableSize.Width)
				{
					availableSize.Width = calculatedWidth;
				}
				else
				{
					availableSize.Height = calculatedHeight;
				}
			}

#if __IOS__ || __ANDROID__
			base.MeasureOverride(availableSize);
#else
			foreach (UIElement child in Children)
			{
				child.Measure(availableSize);
			}
#endif

			return availableSize;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
#if __IOS__ || __ANDROID__
			var output = base.ArrangeOverride(finalSize);
			return output;
#else
			foreach (UIElement child in Children)
			{
				child.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
			}

			return finalSize;
#endif
		}
	}
}
