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
	public class TextViewHtmlBindingAdapter : IBindingAdapter
	{
		void IBindingAdapter.SetValue(object instance, object value)
		{
			var textView = instance as TextView;
			var html = value as string;

			if (textView != null)
			{
				if (html != null)
				{
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CA1422 // Validate platform compatibility
					textView.SetText(Html.FromHtml(html), TextView.BufferType.Spannable);
#pragma warning disable CA1422 // Validate platform compatibility
#pragma warning restore CS0618 // Type or member is obsolete
				}
			}
		}

		object IBindingAdapter.GetValue(object instance)
		{
			var textView = instance as TextView;

			if (textView != null)
			{
				return textView.Text; // HTML formatting might be lost
			}

			return null;
		}

		Type IBindingAdapter.TargetType
		{
			get { return typeof(string); }
		}
	}
}
