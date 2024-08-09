using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.Xaml;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
#if __ANDROID__
using Android.Views;
#elif __IOS__
using View = UIKit.UIView;
#elif __MACOS__
using View = AppKit.NSView;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class StackPanel : Panel
	{

		#region BackgroundSizing DepedencyProperty
		[GeneratedDependencyProperty(DefaultValue = default(BackgroundSizing), ChangedCallback = true)]
		public static DependencyProperty BackgroundSizingProperty { get; } = CreateBackgroundSizingProperty();

		public BackgroundSizing BackgroundSizing
		{
			get => GetBackgroundSizingValue();
			set => SetBackgroundSizingValue(value);
		}

		private void OnBackgroundSizingChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnBackgroundSizingChangedInnerPanel(e);
		}
		#endregion

		#region BorderBrush DependencyProperty

		public Brush BorderBrush
		{
			get => GetBorderBrushValue();
			set => SetBorderBrushValue(value);
		}

		private static Brush GetBorderBrushDefaultValue() => SolidColorBrushHelper.Transparent;

		[GeneratedDependencyProperty(ChangedCallbackName = nameof(OnBorderBrushPropertyChanged), Options = FrameworkPropertyMetadataOptions.ValueInheritsDataContext)]
		public static DependencyProperty BorderBrushProperty { get; } = CreateBorderBrushProperty();

		private void OnBorderBrushPropertyChanged(Brush oldValue, Brush newValue)
		{
			BorderBrushInternal = newValue;
			OnBorderBrushChanged(oldValue, newValue);
		}

		#endregion

		#region BorderThickness DependencyProperty

		public Thickness BorderThickness
		{
			get => GetBorderThicknessValue();
			set => SetBorderThicknessValue(value);
		}

		private static Thickness GetBorderThicknessDefaultValue() => Thickness.Empty;

		[GeneratedDependencyProperty(ChangedCallbackName = nameof(OnBorderThicknessPropertyChanged), Options = FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange)]
		public static DependencyProperty BorderThicknessProperty { get; } = CreateBorderThicknessProperty();

		private void OnBorderThicknessPropertyChanged(Thickness oldValue, Thickness newValue)
		{
			BorderThicknessInternal = newValue;
			OnBorderThicknessChanged(oldValue, newValue);
		}

		private Size BorderAndPaddingSize
		{
			get
			{
				var border = BorderThickness;
				var padding = Padding;
				var width = border.Left + border.Right + padding.Left + padding.Right;
				var height = border.Top + border.Bottom + padding.Top + padding.Bottom;
				return new Size(width, height);
			}
		}

		#endregion

		#region Padding DependencyProperty

		public Thickness Padding
		{
			get => GetPaddingValue();
			set => SetPaddingValue(value);
		}

		private static Thickness GetPaddingDefaultValue() => Thickness.Empty;

		[GeneratedDependencyProperty(ChangedCallbackName = nameof(OnPaddingPropertyChanged), Options = FrameworkPropertyMetadataOptions.AffectsMeasure)]
		public static DependencyProperty PaddingProperty { get; } = CreatePaddingProperty();

		private void OnPaddingPropertyChanged(Thickness oldValue, Thickness newValue)
		{
			PaddingInternal = newValue;
			OnPaddingChanged(oldValue, newValue);
		}

		#endregion

		#region CornerRadius DependencyProperty

		public CornerRadius CornerRadius
		{
			get => GetCornerRadiusValue();
			set => SetCornerRadiusValue(value);
		}

		private static CornerRadius GetCornerRadiusDefaultValue() => CornerRadius.None;

		[GeneratedDependencyProperty(ChangedCallbackName = nameof(OnCornerRadiusPropertyChanged))]
		public static DependencyProperty CornerRadiusProperty { get; } = CreateCornerRadiusProperty();

		private void OnCornerRadiusPropertyChanged(CornerRadius oldValue, CornerRadius newValue)
		{
			CornerRadiusInternal = newValue;
			OnCornerRadiusChanged(oldValue, newValue);
		}

		#endregion

		#region Orientation DependencyProperty

		internal override Orientation? PhysicalOrientation => Orientation;

		public Orientation Orientation
		{
			get { return (Orientation)GetValue(OrientationProperty); }
			set { SetValue(OrientationProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Orientation.  This enables animation, styling, binding, etc...
		public static DependencyProperty OrientationProperty { get; } =
			DependencyProperty.Register(
				"Orientation",
				typeof(Orientation),
				typeof(StackPanel),
				new FrameworkPropertyMetadata(Orientation.Vertical, (s, e) => ((StackPanel)s)?.OnOrientationChanged(e))
			);


		private void OnOrientationChanged(DependencyPropertyChangedEventArgs e)
		{
			this.InvalidateMeasure();
		}

		#endregion


		/// <summary>
		/// Gets or sets a uniform distance (in pixels) between stacked items. It is applied in the direction of the StackPanel's Orientation.
		/// </summary>
		public double Spacing
		{
			get => (double)GetValue(SpacingProperty);
			set => SetValue(SpacingProperty, value);
		}

		public static DependencyProperty SpacingProperty { get; } =
			DependencyProperty.Register(
				name: "Spacing",
				propertyType: typeof(double),
				ownerType: typeof(StackPanel),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: 0.0,
					options: FrameworkPropertyMetadataOptions.AffectsMeasure
				));

		protected override bool? IsWidthConstrainedInner(View requester)
		{
			if (requester != null && Orientation == Orientation.Horizontal)
			{
				return false;
			}

			return this.IsWidthConstrainedSimple();
		}

		protected override bool? IsHeightConstrainedInner(View requester)
		{
			if (requester != null && Orientation == Orientation.Vertical)
			{
				return false;
			}

			return this.IsHeightConstrainedSimple();
		}
	}
}
