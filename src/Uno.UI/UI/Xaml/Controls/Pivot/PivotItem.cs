using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Uno.UI;
#if XAMARIN_ANDROID
using Android.Views;
#endif

namespace Windows.UI.Xaml.Controls
{
    public partial class PivotItem : ContentControl
    {
		public PivotItem()
		{
			this.HorizontalAlignment = HorizontalAlignment.Stretch;
			this.VerticalAlignment = VerticalAlignment.Stretch;

			this.HorizontalContentAlignment = HorizontalAlignment.Stretch;
			this.VerticalContentAlignment = VerticalAlignment.Stretch;

			DefaultStyleKey = typeof(PivotItem);
		}

		public PivotItem(string header) : this()
		{
			Header = header;
		}

		protected override bool CanCreateTemplateWithoutParent => true;

		public object Header
		{
			get { return this.GetValue(HeaderProperty); }
			set { this.SetValue(HeaderProperty, value); }
		}

		public static DependencyProperty HeaderProperty { get ; } =
			DependencyProperty.Register("Header", typeof(object), typeof(PivotItem), new FrameworkPropertyMetadata(null));

#if XAMARIN_ANDROID
		// This allows the PivotItem to fill the whole available space.
		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			if (ChildCount != 0)
			{
				MeasureChild(GetChildAt(0), widthMeasureSpec, heightMeasureSpec);
			}

			SetMeasuredDimension(ViewHelper.MeasureSpecGetSize(widthMeasureSpec), ViewHelper.MeasureSpecGetSize(heightMeasureSpec));
		}
#endif
	}
}
