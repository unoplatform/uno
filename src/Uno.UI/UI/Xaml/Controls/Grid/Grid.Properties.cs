using Uno.Collections;
using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Uno.UI.Extensions;
using Uno.UI.Helpers.Boxes;
using Uno.UI.Xaml;
using Microsoft.UI.Xaml.Media;


namespace Microsoft.UI.Xaml.Controls
{
	partial class Grid
	{

		#region BackgroundSizing DepedencyProperty
		[GeneratedDependencyProperty(DefaultValue = default(BackgroundSizing), ChangedCallback = true)]
		public partial BackgroundSizing BackgroundSizing { get; set; }

		private void OnBackgroundSizingChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnBackgroundSizingChangedInnerPanel(e);
		}
		#endregion

		#region BorderBrush DependencyProperty

		[GeneratedDependencyProperty(ChangedCallbackName = nameof(OnBorderBrushPropertyChanged), Options = FrameworkPropertyMetadataOptions.ValueInheritsDataContext)]
		public partial Brush BorderBrush { get; set; }

		private static Brush GetBorderBrushDefaultValue() => SolidColorBrushHelper.Transparent;

		private void OnBorderBrushPropertyChanged(Brush oldValue, Brush newValue)
		{
			BorderBrushInternal = newValue;
			OnBorderBrushChanged(oldValue, newValue);
		}

		#endregion

		#region BorderThickness DependencyProperty

		[GeneratedDependencyProperty(ChangedCallbackName = nameof(OnBorderThicknessPropertyChanged), Options = FrameworkPropertyMetadataOptions.AffectsMeasure)]
		public partial Thickness BorderThickness { get; set; }

		private static Thickness GetBorderThicknessDefaultValue() => default(Thickness);

		private void OnBorderThicknessPropertyChanged(Thickness oldValue, Thickness newValue)
		{
			BorderThicknessInternal = newValue;
			OnBorderThicknessChanged(oldValue, newValue);
		}

		#endregion

		#region Padding DependencyProperty

		[GeneratedDependencyProperty(ChangedCallbackName = nameof(OnPaddingPropertyChanged), Options = FrameworkPropertyMetadataOptions.AffectsMeasure)]
		public partial Thickness Padding { get; set; }

		private static Thickness GetPaddingDefaultValue() => default(Thickness);

		private void OnPaddingPropertyChanged(Thickness oldValue, Thickness newValue)
		{
			PaddingInternal = newValue;
			OnPaddingChanged(oldValue, newValue);
		}

		#endregion

		#region CornerRadius DependencyProperty

		[GeneratedDependencyProperty(ChangedCallbackName = nameof(OnCornerRadiusPropertyChanged))]
		public partial CornerRadius CornerRadius { get; set; }

		private static CornerRadius GetCornerRadiusDefaultValue() => default(CornerRadius);

		private void OnCornerRadiusPropertyChanged(CornerRadius oldValue, CornerRadius newValue)
		{
			CornerRadiusInternal = newValue;
			OnCornerRadiusChanged(oldValue, newValue);
		}

		#endregion

		#region Row Property
		[GeneratedDependencyProperty(DefaultValue = 0, Options = FrameworkPropertyMetadataOptions.AffectsMeasure, AttachedBackingFieldOwner = typeof(UIElement), ChangedCallback = true)]
		public static partial int GetRow(FrameworkElement element);

		public static partial void SetRow(FrameworkElement element, int value);

		private static void OnRowChanged(DependencyObject instance, DependencyPropertyChangedEventArgs args)
		{
			if (instance is IFrameworkElement { Parent: FrameworkElement parent })
			{
				parent.InvalidateMeasure();
			}
		}
		#endregion

		#region Column Property
		[GeneratedDependencyProperty(DefaultValue = 0, Options = FrameworkPropertyMetadataOptions.AffectsMeasure, AttachedBackingFieldOwner = typeof(UIElement), ChangedCallback = true)]
		public static partial int GetColumn(FrameworkElement element);

		public static partial void SetColumn(FrameworkElement element, int value);

		private static void OnColumnChanged(DependencyObject instance, DependencyPropertyChangedEventArgs args)
		{
			if (instance is IFrameworkElement { Parent: FrameworkElement parent })
			{
				parent.InvalidateMeasure();
			}
		}

		#endregion

		#region RowSpan Property
		[GeneratedDependencyProperty(DefaultValue = 1, Options = FrameworkPropertyMetadataOptions.AffectsMeasure, AttachedBackingFieldOwner = typeof(UIElement), ChangedCallback = true)]
		public static partial int GetRowSpan(FrameworkElement element);

		public static void SetRowSpan(FrameworkElement element, int value)
		{
			if (value <= 0)
			{
				throw new ArgumentException("The value must be above zero", nameof(value));
			}

			SetRowSpanValue(element, value);
		}

		private static void OnRowSpanChanged(DependencyObject instance, DependencyPropertyChangedEventArgs args)
		{
			if (instance is IFrameworkElement { Parent: FrameworkElement parent })
			{
				parent.InvalidateMeasure();
			}
		}
		#endregion

		#region ColumnSpan Property
		[GeneratedDependencyProperty(DefaultValue = 1, Options = FrameworkPropertyMetadataOptions.AffectsMeasure, AttachedBackingFieldOwner = typeof(UIElement), ChangedCallback = true)]
		public static partial int GetColumnSpan(FrameworkElement element);

		public static void SetColumnSpan(FrameworkElement element, int value)
		{
			if (value <= 0)
			{
				throw new ArgumentException("The value must be above zero", nameof(value));
			}

			SetColumnSpanValue(element, value);
		}

		private static void OnColumnSpanChanged(DependencyObject instance, DependencyPropertyChangedEventArgs args)
		{
			if (instance is IFrameworkElement { Parent: FrameworkElement parent })
			{
				parent.InvalidateMeasure();
			}
		}
		#endregion

		public double RowSpacing
		{
			get => (double)GetValue(RowSpacingProperty);
			set => SetValue(RowSpacingProperty, Boxer.Box(value));
		}

		public static DependencyProperty RowSpacingProperty { get; } =
		DependencyProperty.Register(
			"RowSpacing", typeof(double),
			typeof(Grid),
			new FrameworkPropertyMetadata(
				DoubleBoxes.Zero,
				FrameworkPropertyMetadataOptions.AffectsMeasure));

		public double ColumnSpacing
		{
			get => (double)GetValue(ColumnSpacingProperty);
			set => SetValue(ColumnSpacingProperty, Boxer.Box(value));
		}

		public static DependencyProperty ColumnSpacingProperty { get; } =
		DependencyProperty.Register(
			"ColumnSpacing", typeof(double),
			typeof(Grid),
			new FrameworkPropertyMetadata(
				DoubleBoxes.Zero,
				FrameworkPropertyMetadataOptions.AffectsMeasure));
	}
}
