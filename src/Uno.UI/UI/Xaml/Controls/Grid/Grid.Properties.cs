using Uno.Collections;
using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Uno.UI.Xaml;

#if XAMARIN_ANDROID
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
#elif XAMARIN_IOS_UNIFIED
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
	public partial class Grid
	{
		#region Row Property
		[GeneratedDependencyProperty(DefaultValue = 0, AttachedBackingFieldOwner = typeof(UIElement), Attached = true, ChangedCallbackName = nameof(OnGenericPropertyChanged))]
		public static DependencyProperty RowProperty { get ; } = CreateRowProperty();

		public static int GetRow(View view) => GetRowValue(view);

		public static void SetRow(View view, int row) => SetRowValue(view, row);
		#endregion

		#region Column Property
		[GeneratedDependencyProperty(DefaultValue = 0, AttachedBackingFieldOwner = typeof(UIElement), Attached = true, ChangedCallbackName = nameof(OnGenericPropertyChanged))]
		public static DependencyProperty ColumnProperty { get ; } = CreateColumnProperty();

		public static int GetColumn(View view) => GetColumnValue(view);

		public static void SetColumn(View view, int column) => SetColumnValue(view, column);
		#endregion

		#region RowSpan Property
		[GeneratedDependencyProperty(DefaultValue = 1, AttachedBackingFieldOwner = typeof(UIElement), Attached = true, ChangedCallbackName = nameof(OnGenericPropertyChanged))]
		public static DependencyProperty RowSpanProperty { get ; } = CreateRowSpanProperty();

		public static int GetRowSpan(View view) => GetRowSpanValue(view);

		public static void SetRowSpan(View view, int rowSpan)
		{
			if (rowSpan <= 0)
			{
				throw new ArgumentException("The value must be above zero", nameof(rowSpan));
			}

			SetRowSpanValue(view, rowSpan);
		}
		#endregion

		#region ColumnSpan Property
		[GeneratedDependencyProperty(DefaultValue = 1, AttachedBackingFieldOwner = typeof(UIElement), Attached = true, ChangedCallbackName = nameof(OnGenericPropertyChanged))]
		public static DependencyProperty ColumnSpanProperty { get ; } = CreateColumnSpanProperty();

		public static int GetColumnSpan(View view) => GetColumnSpanValue(view);

		public static void SetColumnSpan(View view, int columnSpan)
		{
			if(columnSpan <= 0)
			{
				throw new ArgumentException("The value must be above zero", nameof(columnSpan));
			}

			SetColumnSpanValue(view, columnSpan);
		}
		#endregion

		public double RowSpacing
		{
			get
			{
				return (double)this.GetValue(RowSpacingProperty);
			}
			set
			{
				this.SetValue(RowSpacingProperty, value);
			}
		}

		public static DependencyProperty RowSpacingProperty { get; } =
		DependencyProperty.Register(
			"RowSpacing", typeof(double),
			typeof(Grid),
			new FrameworkPropertyMetadata(default(double), OnGenericPropertyChanged));

		public double ColumnSpacing
		{
			get
			{
				return (double)this.GetValue(ColumnSpacingProperty);
			}
			set
			{
				this.SetValue(ColumnSpacingProperty, value);
			}
		}

		public static DependencyProperty ColumnSpacingProperty { get; } =
		DependencyProperty.Register(
			"ColumnSpacing", typeof(double),
			typeof(Grid),
			new FrameworkPropertyMetadata(default(double), OnGenericPropertyChanged));

		private static void OnGenericPropertyChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var view = dependencyObject as View;

			if (view != null)
			{
				view.InvalidateArrange();
			}
		}
	}
}
