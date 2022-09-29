#nullable disable

using Android.Graphics.Drawables;
using Android.Views;
using Uno.UI.DataBinding;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace Uno.UI.DataBindingAdapters
{
	public class ViewBackgroundBindingAdapter : IBindingAdapter
	{
		void IBindingAdapter.SetValue(object instance, object value)
		{
			var view = instance as View;
			var drawable = value as Drawable;

			if (view != null)
			{
				if (drawable != null)
				{
#pragma warning disable 618
					// When this will be completely removed, migrate this line.
					view.SetBackgroundDrawable(drawable);
#pragma warning restore 618
				}
			}
		}

		object IBindingAdapter.GetValue(object instance)
		{
			var view = instance as View;

			if (view != null)
			{
				return view.Background;
			}

			return null;
		}

		Type IBindingAdapter.TargetType
		{
			get { return typeof(Drawable); }
		}
	}
}
