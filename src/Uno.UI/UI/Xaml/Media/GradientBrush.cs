using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media
{
	public abstract partial class GradientBrush : Brush
	{
		protected GradientBrush()
		{
			GradientStops = new GradientStopCollection();
		}

		public GradientStopCollection GradientStops
		{
			get => (GradientStopCollection)this.GetValue(GradientStopsProperty);
			set => SetValue(GradientStopsProperty, value);
		}
		public static readonly DependencyProperty GradientStopsProperty = DependencyProperty.Register(
			"GradientStops",
			typeof(GradientStopCollection),
			typeof(LinearGradientBrush),
			new PropertyMetadata(null)
		);
	}
}
