using System;
using Android.App;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Uno.UI
{
	/// <summary>
	/// A <see cref="PopupWindow"/> that provides the size and location of the keyboard by being resized using <see cref="SoftInput.AdjustResize"/>.
	/// </summary>
	internal class KeyboardRectProvider : PopupWindow, ViewTreeObserver.IOnGlobalLayoutListener
	{
		public delegate void LayoutChangedListener(Rect keyboard, Rect navigation, Rect union);
		
		private readonly LayoutChangedListener _onLayoutChanged;
		private readonly Activity _activity;
		private readonly View _popupView;

		public KeyboardRectProvider(Activity activity, LayoutChangedListener onLayoutChanged) : base(activity)
		{
			_activity = activity;
			_popupView = new LinearLayout(_activity.BaseContext)
			{
				LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
			};
			_onLayoutChanged = onLayoutChanged;

			ContentView = _popupView;
			SoftInputMode = SoftInput.AdjustResize | SoftInput.StateAlwaysVisible;
			InputMethodMode = InputMethod.Needed;
			Width = 0;
			Height = ViewGroup.LayoutParams.MatchParent;
			SetBackgroundDrawable(new ColorDrawable(Android.Graphics.Color.Transparent));
		}

		/// <summary>
		/// Shows the <see cref="PopupWindow"/> and starts listening to keyboard rect changes.
		/// </summary>
		/// <param name="view">A view to get the Android.Views.View.WindowToken token from. It must be attached to a Window for it to work.</param>
		public void Start(View view)
		{
			if (!IsShowing && view != null && view.WindowToken != null)
			{
				ShowAtLocation(view, GravityFlags.NoGravity, 0, 0);
				_popupView.ViewTreeObserver.AddOnGlobalLayoutListener(this);
			}
		}

		/// <summary>
		/// Dismisses the <see cref="PopupWindow"/> and stops listening to keyboard rect changes.
		/// </summary>
		public void Stop()
		{
			if (IsShowing)
			{
				Dismiss();
				_popupView.ViewTreeObserver.RemoveOnGlobalLayoutListener(this);
			}
		}

		/// <summary>
		/// Called whenever the size of the <see cref="_popupView"/> changes.
		/// The size and location of the keyboard can be inferred from the
		/// remaining area of the screen (below the <see cref="_popupView"/>).
		/// </summary>
		void ViewTreeObserver.IOnGlobalLayoutListener.OnGlobalLayout()
		{
			var realMetrics = Get<DisplayMetrics>(_activity.WindowManager.DefaultDisplay.GetRealMetrics);
			var displayRect = Get<Rect>(_activity.WindowManager.DefaultDisplay.GetRectSize);
			var usableRect = Get<Rect>(_popupView.GetWindowVisibleDisplayFrame);

			// we assume that the keyboard and the navigation bar always occupy the bottom area, with the keyboard being above the navigation bar
			// their placements can be calculated based on the follow observation:
			// [size] realMetrics	: screen
			// [rect] displayRect	: display area = screen - (bottom: nav_bar)
			// [rect] usableRect	: usable area = screen - (top: status_bar) - (bottom: keyboard + nav_bar)
			var navigationRect = new Rect(0, displayRect.Bottom, realMetrics.WidthPixels, realMetrics.HeightPixels);
			var keyboardRect = new Rect(0, usableRect.Bottom, realMetrics.WidthPixels, displayRect.Bottom);
			var occupiedRect = new Rect(0, usableRect.Bottom, realMetrics.WidthPixels, realMetrics.HeightPixels);

			// On dockable / undockable navigation bar devices, the keyboardRect can have a negative height. We need to avoid that behavior.
			if(keyboardRect.Bottom < keyboardRect.Top)
			{
				keyboardRect.Bottom = keyboardRect.Top;
			}

			_onLayoutChanged?.Invoke(keyboardRect, navigationRect, occupiedRect);

			T Get<T>(Action<T> getter) where T : new()
			{
				var result = new T();
				getter(result);

				return result;
			}
		}
	}
}
