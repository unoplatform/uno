using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Android.Content.Res;
using Android.Graphics;
using Android.Util;
using Android.Widget;
using Windows.UI.Xaml;

namespace Uno.UI.Controls
{
	public partial class BindableSwitch : Android.Support.V7.Widget.SwitchCompat, DependencyObject
	{
		public BindableSwitch()
			: base(ContextHelper.Current)
		{
            InitializeBinder();

            CheckedChange += OnCheckedChange;

			// Must be set or the following will happen because the text is null.
			// E / AndroidRuntime(6313): java.lang.NullPointerException: Attempt to invoke interface method 'int java.lang.CharSequence.length()' on a null object reference
			// E / AndroidRuntime(6313): 	at android.text.StaticLayout.< init > (StaticLayout.java:49)
			// E / AndroidRuntime(6313): 	at android.support.v7.widget.SwitchCompat.makeLayout(SwitchCompat.java:606)
			// E / AndroidRuntime(6313): 	at android.support.v7.widget.SwitchCompat.onMeasure(SwitchCompat.java:526)
			// E / AndroidRuntime(6313): 	at android.view.View.measure(View.java:17547)

			TextOff = "";
			TextOn = "";
		}

        private void OnCheckedChange(object sender, CheckedChangeEventArgs e)
        {
            SetBindingValue(Checked, "Checked");
        }
        
        public BindableSwitch(Android.Content.Context context, IAttributeSet attrs)
			: base(context, attrs)
		{
			InitializeBinder();
		}

		public BindableSwitch(Android.Content.Context context, IAttributeSet attrs, int defStyle)
			: base(context, attrs, defStyle)
		{
			InitializeBinder();
		}
	}
}
