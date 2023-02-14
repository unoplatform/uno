#if __ANDROID__
using System;
using Android.App;
using Android.Content;
using Android.Util;
using Android.Views.InputMethods;
using Uno.UI;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

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
				var scrollContentViewer = focusedElement.FindFirstParent<ScrollContentPresenter>();
				if (scrollContentViewer != null)
				{
					_padScrollContentPresenter = scrollContentViewer.Pad(OccludedRect);
				}
				focusedElement.StartBringIntoView();
			}
		}
	}
}
#endif
