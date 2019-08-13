using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.LayoutTestControl
{
	public partial class MeasureArrangeCounter : ContentControl
	{
		public MeasureArrangeCounter()
		{
			HorizontalAlignment = HorizontalAlignment.Stretch;
			HorizontalContentAlignment = HorizontalAlignment.Stretch;
			VerticalAlignment = VerticalAlignment.Stretch;
			VerticalContentAlignment = VerticalAlignment.Stretch;
		}
		public string MeasureCount
		{
			get { return (string)GetValue(MeasureCountProperty); }
			set { SetValue(MeasureCountProperty, value); }
		}

		public static readonly DependencyProperty MeasureCountProperty =
			DependencyProperty.Register("MeasureCount", typeof(string), typeof(MeasureArrangeCounter), new PropertyMetadata("."));

		public string ArrangeCount
		{
			get { return (string)GetValue(ArrangeCountProperty); }
			set { SetValue(ArrangeCountProperty, value); }
		}

		public static readonly DependencyProperty ArrangeCountProperty =
			DependencyProperty.Register("ArrangeCount", typeof(string), typeof(MeasureArrangeCounter), new PropertyMetadata("."));


		public void UpdateCounts()
		{
			MeasureCount = $"Measure : {_measureCount}";
			ArrangeCount = $"Arrange : {_arrangeCount}";
		}

		int _measureCount = 0;
		int _arrangeCount = 0;

		protected override Size MeasureOverride(Size availableSize)
		{
			_measureCount++;
			return base.MeasureOverride(availableSize);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			_arrangeCount++;
			return base.ArrangeOverride(finalSize);
		}
	}
}
