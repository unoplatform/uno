#nullable enable

using Android.App;
using Android.Content;
using Android.Views.InputMethods;

namespace Windows.UI.Input;

internal static class InputPaneInterop
{
	internal static bool TryShow(Activity? activity)
	{
		var view = activity?.CurrentFocus;
		if (view is not null)
		{
			var imm = (InputMethodManager?)activity?.GetSystemService(Context.InputMethodService);
			if (imm is not null)
			{
				imm.ShowSoftInput(view, ShowFlags.Forced);
				return true;
			}
		}

		return false;
	}

	internal static bool TryHide(Activity? activity)
	{
		var view = activity?.CurrentFocus;
		if (view is not null)
		{
			var imm = (InputMethodManager?)activity?.GetSystemService(Context.InputMethodService);
			if (imm is not null)
			{
				imm.HideSoftInputFromWindow(view.WindowToken, HideSoftInputFlags.None);
				return true;
			}
		}

		return false;
	}
}
