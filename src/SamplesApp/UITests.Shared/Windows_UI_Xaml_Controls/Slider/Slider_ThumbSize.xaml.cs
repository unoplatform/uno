using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using System;

namespace UITests.Windows_UI_Xaml_Controls.Slider
{
	[Sample("Slider", Description="Tests that the thumbs are not resized when moved to max, slider are not resized when thumb moves and that slider always return a number.")]
	public sealed partial class Slider_ThumbSize : UserControl
	{
		private double _maxHeightAttained;
		public Slider_ThumbSize()
		{
			this.InitializeComponent();
		}

		public void VerticalSlider2_SizeChanged(object s, SizeChangedEventArgs e)
		{
			if (e.NewSize.Height > _maxHeightAttained)
			{
				_maxHeightAttained = e.NewSize.Height;
			}
			VerticalSlider2_MaxHeight.Text = $"{_maxHeightAttained}";
		}
	}
}
