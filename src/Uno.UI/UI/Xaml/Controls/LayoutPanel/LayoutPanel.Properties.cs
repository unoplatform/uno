using Uno.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	partial class LayoutPanel
	{
		#region BorderBrush DependencyProperty

		public Brush BorderBrush
		{
			get => GetBorderBrushValue();
			set => SetBorderBrushValue(value);
		}

		private static Brush GetBorderBrushDefaultValue() => SolidColorBrushHelper.Transparent;

		[GeneratedDependencyProperty(ChangedCallback = true, ChangedCallbackName = nameof(OnPropertyChanged), Options = FrameworkPropertyMetadataOptions.ValueInheritsDataContext)]
		public static DependencyProperty BorderBrushProperty { get; } = CreateBorderBrushProperty();

		#endregion

		#region BorderThickness DependencyProperty

		public Thickness BorderThickness
		{
			get => GetBorderThicknessValue();
			set => SetBorderThicknessValue(value);
		}

		private static Thickness GetBorderThicknessDefaultValue() => Thickness.Empty;

		[GeneratedDependencyProperty(ChangedCallback = true, ChangedCallbackName = nameof(OnPropertyChanged))]
		public static DependencyProperty BorderThicknessProperty { get; } = CreateBorderThicknessProperty();

		#endregion

		#region Padding DependencyProperty

		public Thickness Padding
		{
			get => GetPaddingValue();
			set => SetPaddingValue(value);
		}

		private static Thickness GetPaddingDefaultValue() => Thickness.Empty;

		[GeneratedDependencyProperty(ChangedCallback = true, ChangedCallbackName = nameof(OnPropertyChanged), Options = FrameworkPropertyMetadataOptions.AffectsMeasure)]
		public static DependencyProperty PaddingProperty { get; } = CreatePaddingProperty();

		#endregion

		#region CornerRadius DependencyProperty

		public CornerRadius CornerRadius
		{
			get => GetCornerRadiusValue();
			set => SetCornerRadiusValue(value);
		}

		private static CornerRadius GetCornerRadiusDefaultValue() => CornerRadius.None;

		[GeneratedDependencyProperty(ChangedCallback = true, ChangedCallbackName = nameof(OnPropertyChanged))]
		public static DependencyProperty CornerRadiusProperty { get; } = CreateCornerRadiusProperty();

		#endregion

		public static DependencyProperty LayoutProperty { get; } = DependencyProperty.Register(
			"Layout", typeof(Layout), typeof(LayoutPanel), new FrameworkPropertyMetadata(default(Layout), propertyChangedCallback: (sender, args) => ((LayoutPanel)sender).OnPropertyChanged(args)));

#if __ANDROID__
		new
#endif
		public Layout Layout
		{
			get => (Layout)GetValue(LayoutProperty);
			set => SetValue(LayoutProperty, value);
		}
	}
}
