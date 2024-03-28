using Android.Util;
using Android.Widget;
using Uno.UI.DataBinding;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using Uno.Extensions;

namespace Uno.UI.Controls
{
	public partial class BindableToggleButton : ToggleButton, DependencyObject
	{
		public BindableToggleButton()
			: base(ContextHelper.Current)
		{
			InitializeBinder();
			this.Click += OnClick;
		}

		private void OnClick(object sender, EventArgs e)
		{
			//override Checked doesn't work so
			//update binding with the last Checked value
			SetBindingValue(Checked, "Checked");
		}

		public BindableToggleButton(Android.Content.Context context, IAttributeSet attrs)
			: base(context, attrs)
		{
			InitializeBinder();
		}

		public BindableToggleButton(Android.Content.Context context, IAttributeSet attrs, int defStyle)
			: base(context, attrs, defStyle)
		{
			InitializeBinder();
		}
	}
}
