using Android.Graphics.Drawables;
using Android.Text;
using Android.Views;
using Android.Widget;
using Uno.UI.DataBinding;
using Uno.UI.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace Uno.UI.DataBindingAdapters
{
	public class ButtonTextHtmlFormattedBindingAdapter : IBindingAdapter
	{
		void IBindingAdapter.SetValue(object instance, object value)
		{
			var button = instance as Button;
			var html = value as string;

			if (button != null)
			{
				if (html != null)
				{
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CA1422 // Validate platform compatibility
					button.SetText(Html.FromHtml(html), TextView.BufferType.Spannable);
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore CS0618 // Type or member is obsolete
				}
			}
		}

		object IBindingAdapter.GetValue(object instance)
		{
			var button = instance as Button;

			if (button != null)
			{
				return button.Text;
			}

			return null;
		}

		Type IBindingAdapter.TargetType
		{
			get { return typeof(string); }
		}
	}
}
