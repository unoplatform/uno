using System;
using Android.App;
using Android.Content;
using Android.Util;
using Android.Views.InputMethods;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Windows.UI.ViewManagement
{
	public partial class InputPane
	{
		private IDisposable _padScrollContentPresenter;

		partial void TryShowPartial()
		{
			var activity = (ContextHelper.Current as Activity);
			var view = activity.CurrentFocus;
			if (view != null)
			{
				var imm = (InputMethodManager)activity.GetSystemService(Context.InputMethodService);
				imm.ShowSoftInput(view, ShowFlags.Forced);
			}
		}

		partial void TryHidePartial()
		{
			var activity = (ContextHelper.Current as Activity);
			var view = activity.CurrentFocus;
			if (view != null)
			{
				var imm = (InputMethodManager)activity.GetSystemService(Context.InputMethodService);
				imm.HideSoftInputFromWindow(view.WindowToken, HideSoftInputFlags.None);
			}
		}

		partial void EnsureFocusedElementInViewPartial()
		{
			_padScrollContentPresenter?.Dispose(); // Restore padding

			if (Visible && FocusManager.GetFocusedElement() is UIElement focusedElement)
			{
				if (focusedElement.FindFirstParent<ScrollContentPresenter>() is { } scp)
				{
					// ScrollViewer can be nested, but the outer-most SV isn't necessarily the one to handle this "padded" scroll.
					// Only the first SV that is constrained would be the one, as unconstrained SV can just expand freely.
					while (double.IsPositiveInfinity(scp.LastAvailableSize.Height)
						&& scp.FindFirstParent<ScrollContentPresenter>(includeCurrent: false) is { } outerScv)
					{
						scp = outerScv;
					}

					_padScrollContentPresenter = scp.Pad(OccludedRect);
				}
				focusedElement.StartBringIntoView();
			}
		}
	}
}
