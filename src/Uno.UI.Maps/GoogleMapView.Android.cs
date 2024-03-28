using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Gms.Maps;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Size = System.Drawing.Size;

namespace Windows.UI.Xaml.Controls.Maps.Presenters
{
	public class GoogleMapView : MapView
	{
		private Size _lastAvailableSize;
		private Size _lastLayoutedSize;

		public event EventHandler<MotionEvent> TouchOccurred;

		public GoogleMapView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
		{
		}

		public GoogleMapView(Android.Content.Context context, GoogleMapOptions options) : base(context, options)
		{
		}

		public GoogleMapView(Android.Content.Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
		{
		}

		public GoogleMapView(Android.Content.Context context, IAttributeSet attrs) : base(context, attrs)
		{
		}

		public GoogleMapView(Android.Content.Context context) : base(context)
		{
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			_lastAvailableSize = new Size(MeasureSpec.GetSize(widthMeasureSpec), MeasureSpec.GetSize(heightMeasureSpec));

			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
		}

		protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
		{
			_lastLayoutedSize = new Size(right - left, bottom - top);

			// Measures the map to its layouted size so that it fills its bounds
			OnMeasure(
				MeasureSpec.MakeMeasureSpec(_lastLayoutedSize.Width, MeasureSpecMode.AtMost),
				MeasureSpec.MakeMeasureSpec(_lastLayoutedSize.Height, MeasureSpecMode.AtMost)
			);

			base.OnLayout(changed, left, top, right, bottom);
		}

		public override void RequestLayout()
		{
			// MapView contains a TextView (Google Trademark text) 
			// that calls requestLayout continuously when GoogleMap.MyLocationEnabled is true.
			// We don't want to propagate the request to the Map's parents when the
			// size of the map is exactly the same as before.
			if (_lastAvailableSize != _lastLayoutedSize)
			{
				base.RequestLayout();
			}
		}

		public override bool DispatchTouchEvent(MotionEvent e)
		{
			TouchOccurred?.Invoke(this, e);
			return base.DispatchTouchEvent(e);
		}
	}
}
