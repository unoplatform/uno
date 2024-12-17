using Android.Content;
using Android.Graphics;
using Android.Runtime;
using AndroidX.ViewPager.Widget;
using Android.Util;
using Android.Views;
using Uno.UI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public class ExtendedViewPager : ViewPager
	{
		public bool SwipeEnabled { get; set; }

		public ExtendedViewPager(Android.Content.Context context)
			: base(context)
		{
			Initialize();
		}

		[Register(".ctor", "(Landroid/content/Context;Landroid/util/AttributeSet;)V", "")]
		public ExtendedViewPager(Android.Content.Context context, IAttributeSet attrs)
			: base(context, attrs)
		{
			Initialize();
		}

		protected ExtendedViewPager(IntPtr javaReference, JniHandleOwnership transfer)
			: base(javaReference, transfer)
		{
			Initialize();
		}

		private void Initialize()
		{
			// FragmentPagerAdapter uses the fragment container Id (the ViewPager) 
			// and the index of the fragment to define a unique tag. 
			// It then finds the fragment based on this tag and adds it to the ViewPager.
			// Source: android.support.v4.app.FragmentPagerAdapter.MakeFragmentName
			//
			// To make sure that multiple ViewPagers can be used in a 
			// single visual tree, each one must have a unique id.
			Id = ViewHelper.GenerateUniqueViewId();

			PageMargin = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 4, ContextHelper.Current.Resources.DisplayMetrics);
		}

		public override bool OnInterceptTouchEvent(MotionEvent e)
		{
			if (SwipeEnabled)
			{
				return base.OnInterceptTouchEvent(e);
			}

			return false;
		}

		public override bool OnTouchEvent(MotionEvent e)
		{
			if (SwipeEnabled)
			{
				return base.OnTouchEvent(e);
			}

			return false;
		}

		// Ensures the ExtendedViewPager stretches to fill its parent
		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			var heightMode = ViewHelper.MeasureSpecGetMode(heightMeasureSpec);
			var height = ViewHelper.MeasureSpecGetSize(heightMeasureSpec);

			if (heightMode == MeasureSpecMode.Unspecified || height == 0)
			{
				heightMeasureSpec = ViewHelper.MakeMeasureSpec(int.MaxValue, MeasureSpecMode.AtMost);
			}

			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
		}
	}
}
