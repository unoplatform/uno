#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Android.App;
using Android.Content;
using Android.Views.InputMethods;
using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Android.Widget;
using Android.Views;
using Android.Graphics.Drawables;
using Uno.Disposables;
using Uno.Extensions;

namespace Windows.UI.ViewManagement
{
	public partial class InputPane
	{
		private IDisposable _padScrollContentPresenter;

		// Android specific :
		// Since Android has un-dockable navigation bar, some calculations can depends of the keyboard and/or navigation bar size.
		// => OccludedRect = KeyboardRect + NavigationBarRect
		public Rect KeyboardRect { get; internal set; }

		public Rect NavigationBarRect { get; internal set; }

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
				imm.HideSoftInputFromWindow(view.WindowToken, 0);
			}
		}

		partial void EnsureFocusedElementInViewPartial()
		{
			_padScrollContentPresenter?.Dispose(); // Restore padding

			if (Visible && FocusManager.GetFocusedElement() is UIElement focusedElement)
			{
				var scrollContentViewer = focusedElement.FindFirstParent<IScrollContentPresenter>();
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
