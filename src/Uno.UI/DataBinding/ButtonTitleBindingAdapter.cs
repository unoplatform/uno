#nullable disable

#if XAMARIN_IOS
using Uno.UI.DataBinding;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

#if XAMARIN_IOS_UNIFIED
using UIKit;
#elif XAMARIN_IOS
using MonoTouch.UIKit;
#endif

namespace Uno.UI.DataBindingAdapters
{
	public class ButtonTitleBindingAdapter : IBindingAdapter
	{
		void IBindingAdapter.SetValue(object instance, object value)
		{
			var button = instance as UIButton;

			if (button != null)
			{
				button.SetTitle((string)value, button.State);
			}
		}

		object IBindingAdapter.GetValue(object instance)
		{
			var button = instance as UIButton;

			if (button != null)
			{
				return button.Title(button.State);
			}

			return null;
		}

		Type IBindingAdapter.TargetType
		{
			get { return typeof(string); }
		}
	}
}
#endif