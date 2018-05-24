#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Slider 
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Primitives.SliderSnapsTo SnapsTo
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Primitives.SliderSnapsTo)this.GetValue(SnapsToProperty);
			}
			set
			{
				this.SetValue(SnapsToProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Orientation Orientation
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Orientation)this.GetValue(OrientationProperty);
			}
			set
			{
				this.SetValue(OrientationProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool IsThumbToolTipEnabled
		{
			get
			{
				return (bool)this.GetValue(IsThumbToolTipEnabledProperty);
			}
			set
			{
				this.SetValue(IsThumbToolTipEnabledProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool IsDirectionReversed
		{
			get
			{
				return (bool)this.GetValue(IsDirectionReversedProperty);
			}
			set
			{
				this.SetValue(IsDirectionReversedProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  double IntermediateValue
		{
			get
			{
				return (double)this.GetValue(IntermediateValueProperty);
			}
			set
			{
				this.SetValue(IntermediateValueProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Primitives.TickPlacement TickPlacement
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Primitives.TickPlacement)this.GetValue(TickPlacementProperty);
			}
			set
			{
				this.SetValue(TickPlacementProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  double TickFrequency
		{
			get
			{
				return (double)this.GetValue(TickFrequencyProperty);
			}
			set
			{
				this.SetValue(TickFrequencyProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Data.IValueConverter ThumbToolTipValueConverter
		{
			get
			{
				return (global::Windows.UI.Xaml.Data.IValueConverter)this.GetValue(ThumbToolTipValueConverterProperty);
			}
			set
			{
				this.SetValue(ThumbToolTipValueConverterProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  double StepFrequency
		{
			get
			{
				return (double)this.GetValue(StepFrequencyProperty);
			}
			set
			{
				this.SetValue(StepFrequencyProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.DataTemplate HeaderTemplate
		{
			get
			{
				return (global::Windows.UI.Xaml.DataTemplate)this.GetValue(HeaderTemplateProperty);
			}
			set
			{
				this.SetValue(HeaderTemplateProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  object Header
		{
			get
			{
				return (object)this.GetValue(HeaderProperty);
			}
			set
			{
				this.SetValue(HeaderProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty OrientationProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Orientation", typeof(global::Windows.UI.Xaml.Controls.Orientation), 
			typeof(global::Windows.UI.Xaml.Controls.Slider), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Orientation)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SnapsToProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"SnapsTo", typeof(global::Windows.UI.Xaml.Controls.Primitives.SliderSnapsTo), 
			typeof(global::Windows.UI.Xaml.Controls.Slider), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Primitives.SliderSnapsTo)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty StepFrequencyProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"StepFrequency", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.Slider), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ThumbToolTipValueConverterProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ThumbToolTipValueConverter", typeof(global::Windows.UI.Xaml.Data.IValueConverter), 
			typeof(global::Windows.UI.Xaml.Controls.Slider), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Data.IValueConverter)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TickFrequencyProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"TickFrequency", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.Slider), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TickPlacementProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"TickPlacement", typeof(global::Windows.UI.Xaml.Controls.Primitives.TickPlacement), 
			typeof(global::Windows.UI.Xaml.Controls.Slider), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Primitives.TickPlacement)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IntermediateValueProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IntermediateValue", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.Slider), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsDirectionReversedProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsDirectionReversed", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Slider), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsThumbToolTipEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsThumbToolTipEnabled", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Slider), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty HeaderProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Header", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.Slider), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty HeaderTemplateProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"HeaderTemplate", typeof(global::Windows.UI.Xaml.DataTemplate), 
			typeof(global::Windows.UI.Xaml.Controls.Slider), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.DataTemplate)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public Slider() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Slider", "Slider.Slider()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.Slider()
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.IntermediateValue.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.IntermediateValue.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.StepFrequency.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.StepFrequency.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.SnapsTo.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.SnapsTo.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.TickFrequency.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.TickFrequency.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.TickPlacement.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.TickPlacement.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.Orientation.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.Orientation.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.IsDirectionReversed.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.IsDirectionReversed.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.IsThumbToolTipEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.IsThumbToolTipEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.ThumbToolTipValueConverter.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.ThumbToolTipValueConverter.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.Header.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.Header.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.HeaderTemplate.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.HeaderTemplate.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.HeaderProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.HeaderTemplateProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.IntermediateValueProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.StepFrequencyProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.SnapsToProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.TickFrequencyProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.TickPlacementProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.OrientationProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.IsDirectionReversedProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.IsThumbToolTipEnabledProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Slider.ThumbToolTipValueConverterProperty.get
	}
}
