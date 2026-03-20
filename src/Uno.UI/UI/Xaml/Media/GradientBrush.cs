#nullable enable

using Microsoft.UI.Xaml.Markup;

using Windows.UI;

namespace Microsoft.UI.Xaml.Media
{
	[ContentProperty(Name = "GradientStops")]
	public abstract partial class GradientBrush : Brush
	{
		protected GradientBrush()
		{
			GradientStops = new GradientStopCollection();
		}

		public static DependencyProperty FallbackColorProperty { get; } = DependencyProperty.Register(
			"FallbackColor", typeof(Color), typeof(GradientBrush), new FrameworkPropertyMetadata(default(Color)));

		public Color FallbackColor
		{
			get => (Color)GetValue(FallbackColorProperty);
			set => SetValue(FallbackColorProperty, value);
		}

		public static DependencyProperty GradientStopsProperty { get; } = DependencyProperty.Register(
			"GradientStops",
			typeof(GradientStopCollection),
			typeof(GradientBrush),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.ValueInheritsDataContext | FrameworkPropertyMetadataOptions.LogicalChild | FrameworkPropertyMetadataOptions.AffectsArrange
				)
		);

		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			base.OnPropertyChanged2(args);
			if (args.Property == GradientStopsProperty)
			{
				OnGradientStopsChanged(this, args);
			}
		}

		private static void OnGradientStopsChanged(GradientBrush @this, DependencyPropertyChangedEventArgs args)
		{
			if (args.OldValue is GradientStopCollection oldValue)
			{
				oldValue.Added -= @this.OnAdded;
				oldValue.Removed -= @this.OnRemoved;

				foreach (var stop in oldValue)
				{
					stop.InvalidateRender -= @this.OnInvalidateRender;
				}
			}

			if (args.NewValue is GradientStopCollection newValue)
			{
				newValue.Added += @this.OnAdded;
				newValue.Removed += @this.OnRemoved;

				foreach (var stop in newValue)
				{
					stop.InvalidateRender += @this.OnInvalidateRender;
				}
			}
		}

		private void OnAdded(GradientStop addedStop)
		{
			OnInvalidateRender();
			addedStop.InvalidateRender += OnInvalidateRender;
		}

		private void OnRemoved(GradientStop removedStop)
		{
			OnInvalidateRender();
			removedStop.InvalidateRender -= OnInvalidateRender;
		}

		public GradientStopCollection GradientStops
		{
			get => (GradientStopCollection)GetValue(GradientStopsProperty);
			set => SetValue(GradientStopsProperty, value);
		}

		public static DependencyProperty MappingModeProperty { get; } =
			DependencyProperty.Register(
				"MappingMode",
				typeof(BrushMappingMode),
				typeof(GradientBrush),
				new FrameworkPropertyMetadata(
					defaultValue: BrushMappingMode.RelativeToBoundingBox,
					options: FrameworkPropertyMetadataOptions.AffectsRender));

		public BrushMappingMode MappingMode
		{
			get => (BrushMappingMode)GetValue(MappingModeProperty);
			set => SetValue(MappingModeProperty, value);
		}

		public static DependencyProperty SpreadMethodProperty { get; } =
			DependencyProperty.Register(
				nameof(SpreadMethod),
				typeof(GradientSpreadMethod),
				typeof(GradientBrush),
				new FrameworkPropertyMetadata(
					defaultValue: GradientSpreadMethod.Pad,
					options: FrameworkPropertyMetadataOptions.AffectsRender));

		public GradientSpreadMethod SpreadMethod
		{
			get => (GradientSpreadMethod)GetValue(SpreadMethodProperty);
			set => SetValue(SpreadMethodProperty, value);
		}

		internal Color FallbackColorWithOpacity => GetColorWithOpacity(FallbackColor);

		internal virtual bool CanApplyToBorder(CornerRadius cornerRadius) => true;
	}
}
