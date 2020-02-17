using Uno.Collections;
using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;

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
		public static readonly DependencyProperty RowProperty =
			DependencyProperty.RegisterAttached(
				"Row",
				typeof(int),
				typeof(Grid),
				new PropertyMetadata(0, OnGenericPropertyChanged)
			);

		public static int GetRow(View view)
		{
			return (int)DependencyObjectExtensions.GetValue(view, RowProperty);
		}

		public static void SetRow(View view, int row)
		{
			DependencyObjectExtensions.SetValue(view, RowProperty, row);
		}
		#endregion

		#region Column Property
		public static readonly DependencyProperty ColumnProperty =
			DependencyProperty.RegisterAttached(
				"Column",
				typeof(int),
				typeof(Grid),
				new PropertyMetadata(0, OnGenericPropertyChanged)
			);

		public static int GetColumn(View view)
		{
			return (int)DependencyObjectExtensions.GetValue(view, ColumnProperty);
		}

		public static void SetColumn(View view, int column)
		{
			DependencyObjectExtensions.SetValue(view, ColumnProperty, column);
		}
		#endregion

		#region RowSpan Property

		public static readonly DependencyProperty RowSpanProperty =
			DependencyProperty.RegisterAttached(
				"RowSpan",
				typeof(int),
				typeof(Grid),
				new PropertyMetadata(1, OnGenericPropertyChanged)
			);

		public static int GetRowSpan(View view)
		{
			return (int)DependencyObjectExtensions.GetValue(view, RowSpanProperty);
		}

		public static void SetRowSpan(View view, int rowSpan)
		{
			if (rowSpan <= 0)
			{
				throw new ArgumentException("The value must be above zero", nameof(rowSpan));
			}

			DependencyObjectExtensions.SetValue(view, RowSpanProperty, rowSpan);
		}
		#endregion

		#region ColumnSpan Property

		public static readonly DependencyProperty ColumnSpanProperty =
			DependencyProperty.RegisterAttached(
				"ColumnSpan",
				typeof(int),
				typeof(Grid),
				new PropertyMetadata(1, OnGenericPropertyChanged)
			);

		public static int GetColumnSpan(View view)
		{
			return (int)DependencyObjectExtensions.GetValue(view, ColumnSpanProperty);
		}

		public static void SetColumnSpan(View view, int columnSpan)
		{
			if(columnSpan <= 0)
			{
				throw new ArgumentException("The value must be above zero", nameof(columnSpan));
			}

			DependencyObjectExtensions.SetValue(view, ColumnSpanProperty, columnSpan);
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
