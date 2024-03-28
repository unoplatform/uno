using Uno.Collections;
using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Uno.UI.Extensions;
using Uno.UI.Xaml;
using Windows.UI.Xaml.Media;

#if __ANDROID__
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
#elif __IOS__
using UIKit;
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#elif __MACOS__
using AppKit;
using View = AppKit.NSView;
using Color = AppKit.NSColor;
using Font = AppKit.NSFont;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	partial class Grid
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

		#region Row Property
		[GeneratedDependencyProperty(DefaultValue = 0, Options = FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange, AttachedBackingFieldOwner = typeof(UIElement), Attached = true, ChangedCallback = true)]
		public static DependencyProperty RowProperty { get; } = CreateRowProperty();

		public static int GetRow(View view) => GetRowValue(view);

		public static void SetRow(View view, int row) => SetRowValue(view, row);

		private static void OnRowChanged(DependencyObject instance, DependencyPropertyChangedEventArgs args)
		{
			if (instance is IFrameworkElement { Parent: IFrameworkElement parent })
			{
				parent.InvalidateMeasure();
			}
		}
		#endregion

		#region Column Property
		[GeneratedDependencyProperty(DefaultValue = 0, Options = FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange, AttachedBackingFieldOwner = typeof(UIElement), Attached = true, ChangedCallback = true)]
		public static DependencyProperty ColumnProperty { get; } = CreateColumnProperty();

		public static int GetColumn(View view) => GetColumnValue(view);

		public static void SetColumn(View view, int column) => SetColumnValue(view, column);

		private static void OnColumnChanged(DependencyObject instance, DependencyPropertyChangedEventArgs args)
		{
			if (instance is IFrameworkElement { Parent: IFrameworkElement parent })
			{
				parent.InvalidateMeasure();
			}
		}

		#endregion

		#region RowSpan Property
		[GeneratedDependencyProperty(DefaultValue = 1, Options = FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange, AttachedBackingFieldOwner = typeof(UIElement), Attached = true, ChangedCallback = true)]
		public static DependencyProperty RowSpanProperty { get; } = CreateRowSpanProperty();

		public static int GetRowSpan(View view) => GetRowSpanValue(view as UIElement);

		public static void SetRowSpan(View view, int rowSpan)
		{
			if (rowSpan <= 0)
			{
				throw new ArgumentException("The value must be above zero", nameof(rowSpan));
			}

			SetRowSpanValue(view as UIElement, rowSpan);
		}

		private static void OnRowSpanChanged(DependencyObject instance, DependencyPropertyChangedEventArgs args)
		{
			if (instance is IFrameworkElement { Parent: IFrameworkElement parent })
			{
				parent.InvalidateMeasure();
			}
		}
		#endregion

		#region ColumnSpan Property
		[GeneratedDependencyProperty(DefaultValue = 1, Options = FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange, AttachedBackingFieldOwner = typeof(UIElement), Attached = true, ChangedCallback = true)]
		public static DependencyProperty ColumnSpanProperty { get; } = CreateColumnSpanProperty();

		public static int GetColumnSpan(View view) => GetColumnSpanValue(view as UIElement);

		public static void SetColumnSpan(View view, int columnSpan)
		{
			if (columnSpan <= 0)
			{
				throw new ArgumentException("The value must be above zero", nameof(columnSpan));
			}

			SetColumnSpanValue(view as UIElement, columnSpan);
		}

		private static void OnColumnSpanChanged(DependencyObject instance, DependencyPropertyChangedEventArgs args)
		{
			if (instance is IFrameworkElement { Parent: IFrameworkElement parent })
			{
				parent.InvalidateMeasure();
			}
		}
		#endregion

		public double RowSpacing
		{
			get => (double)GetValue(RowSpacingProperty);
			set => SetValue(RowSpacingProperty, value);
		}

		public static DependencyProperty RowSpacingProperty { get; } =
		DependencyProperty.Register(
			"RowSpacing", typeof(double),
			typeof(Grid),
			new FrameworkPropertyMetadata(
				default(double),
				FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

		public double ColumnSpacing
		{
			get => (double)GetValue(ColumnSpacingProperty);
			set => SetValue(ColumnSpacingProperty, value);
		}

		public static DependencyProperty ColumnSpacingProperty { get; } =
		DependencyProperty.Register(
			"ColumnSpacing", typeof(double),
			typeof(Grid),
			new FrameworkPropertyMetadata(
				default(double),
				FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));
	}
}
