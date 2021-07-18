using System;
using System.Collections.Generic;
using Android.Views;
using static Android.Views.View;

namespace Uno.Gaming.Input.Internal
{
	internal class GamepadKeyListener : Java.Lang.Object, IOnUnhandledKeyEventListener
	{
		public bool OnUnhandledKeyEvent(View v, KeyEvent e)
		{
			global::System.Diagnostics.Debug.WriteLine(e.KeyCode);
			return false;
		}
	}
}
