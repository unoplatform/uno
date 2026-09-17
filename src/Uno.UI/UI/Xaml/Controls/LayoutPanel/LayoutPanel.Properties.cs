using Uno.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	partial class LayoutPanel
	{
		#region BorderBrush DependencyProperty

		[GeneratedDependencyProperty(ChangedCallback = true, ChangedCallbackName = nameof(OnPropertyChanged), Options = FrameworkPropertyMetadataOptions.ValueInheritsDataContext)]
		public partial Brush BorderBrush { get; set; }

		private static Brush GetBorderBrushDefaultValue() => SolidColorBrushHelper.Transparent;

		#endregion

		#region BorderThickness DependencyProperty

		[GeneratedDependencyProperty(ChangedCallback = true, ChangedCallbackName = nameof(OnPropertyChanged))]
		public partial Thickness BorderThickness { get; set; }

		private static Thickness GetBorderThicknessDefaultValue() => Thickness.Empty;

		#endregion

		#region Padding DependencyProperty

		[GeneratedDependencyProperty(ChangedCallback = true, ChangedCallbackName = nameof(OnPropertyChanged), Options = FrameworkPropertyMetadataOptions.AffectsMeasure)]
		public partial Thickness Padding { get; set; }

		private static Thickness GetPaddingDefaultValue() => Thickness.Empty;

		#endregion

		#region CornerRadius DependencyProperty

		[GeneratedDependencyProperty(ChangedCallback = true, ChangedCallbackName = nameof(OnPropertyChanged))]
		public partial CornerRadius CornerRadius { get; set; }

		private static CornerRadius GetCornerRadiusDefaultValue() => CornerRadius.None;

		#endregion

		public static DependencyProperty LayoutProperty { get; } = DependencyProperty.Register(
			"Layout", typeof(Layout), typeof(LayoutPanel), new FrameworkPropertyMetadata(default(Layout), propertyChangedCallback: (sender, args) => ((LayoutPanel)sender).OnPropertyChanged(args)));

		public Layout Layout
		{
			get => (Layout)GetValue(LayoutProperty);
			set => SetValue(LayoutProperty, value);
		}
	}
}
