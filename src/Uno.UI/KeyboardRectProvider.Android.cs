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
		private readonly Activity _activity;
		private readonly Action<Rect> _onKeyboardRectChanged;
		private readonly View _popupView;
		
		public KeyboardRectProvider(Activity activity, Action<Rect> onKeyboardRectChanged) : base(activity)
		{
			_activity = activity;
			_onKeyboardRectChanged = onKeyboardRectChanged;
			_popupView = new LinearLayout(_activity.BaseContext)
			{
				LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
			};

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
			var metrics = new DisplayMetrics();
			_activity.WindowManager.DefaultDisplay.GetMetrics(metrics);

			var realMetrics = new DisplayMetrics();
			_activity.WindowManager.DefaultDisplay.GetRealMetrics(realMetrics);

			var popupRect = new Rect();
			_popupView.GetWindowVisibleDisplayFrame(popupRect);

			// We are only interested in the height above the navigation bar. If the navigation bar visibility changes, it will also raise OnGlobalLayout.
			// While the keyboard is visible the navigation bar will always also be visible (enabling the close keyboard button).
			var keyboardRect = new Rect(
				0,
				Math.Min(popupRect.Bottom, metrics.HeightPixels),
				realMetrics.WidthPixels,
				realMetrics.HeightPixels
			);

			_onKeyboardRectChanged?.Invoke(keyboardRect);
		}
	}
}
