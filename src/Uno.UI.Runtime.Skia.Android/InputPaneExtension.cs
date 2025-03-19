using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Views.InputMethods;
using Uno.UI;
using Windows.UI.ViewManagement;

namespace Uno.WinUI.Runtime.Skia.Android;

internal class InputPaneExtension : IInputPaneExtension
{
	public void TryShow()
	{
		var activity = (ContextHelper.Current as Activity);
		if (activity is null)
		{
			return;
		}
		var view = activity.CurrentFocus;
		if (view != null)
		{
			var imm = (InputMethodManager)activity.GetSystemService(Context.InputMethodService);
			imm.ShowSoftInput(view, ShowFlags.Forced);
		}
	}

	public void TryHide()
	{
		var activity = (ContextHelper.Current as Activity);
		var view = activity.CurrentFocus;
		if (view != null)
		{
			var imm = (InputMethodManager)activity.GetSystemService(Context.InputMethodService);
			imm.HideSoftInputFromWindow(view.WindowToken, HideSoftInputFlags.None);
		}
	}
}
