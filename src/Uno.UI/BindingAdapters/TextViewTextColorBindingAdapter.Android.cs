#nullable disable

using Android.Graphics;
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
	public class TextViewTextColorBindingAdapter : IBindingAdapter
	{
		void IBindingAdapter.SetValue(object instance, object value)
		{
			var textView = instance as TextView;
			var color = (Color)value;

			if (textView != null)
			{
				textView.SetTextColor(color);
			}
		}

#pragma warning disable 0618
		object IBindingAdapter.GetValue(object instance)
		{
			var textView = instance as TextView;

			if (textView != null)
			{
				return textView.Context.Resources.GetColor(textView.TextColors.DefaultColor);
			}

			return null;
		}
#pragma warning restore 0618

		Type IBindingAdapter.TargetType
		{
			get { return typeof(Color); }
		}
	}
}
