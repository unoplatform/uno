#nullable disable

using Android.Graphics.Drawables;
using Android.Text;
using Android.Views;
using Android.Widget;
using Uno.UI.DataBinding;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace Uno.UI.DataBindingAdapters
{
	public class ImageViewDrawableBindingAdapter : IBindingAdapter
	{
		void IBindingAdapter.SetValue(object instance, object value)
		{
			var imageView = instance as ImageView;
			var drawable = value as Drawable;

			if (imageView != null)
			{
				if (drawable != null)
				{
					imageView.SetImageDrawable(drawable);
				}
			}
		}

		object IBindingAdapter.GetValue(object instance)
		{
			var imageView = instance as ImageView;

			if (imageView != null)
			{
				return imageView.Drawable;
			}

			return null;
		}

		Type IBindingAdapter.TargetType
		{
			get { return typeof(Drawable); }
		}
	}
}
