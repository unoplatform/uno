using System;
using Android.App;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Uno.UI.Controls;

namespace Uno.UI
{
	/// <summary>
	/// A <see cref="PopupWindow"/> that provides the size and location of the keyboard by being resized using <see cref="SoftInput.AdjustResize"/>.
	/// </summary>
	internal class KeyboardRectProvider : PopupWindow, ViewTreeObserver.IOnGlobalLayoutListener, View.IOnSystemUiVisibilityChangeListener
	{
		public delegate void LayoutChangedListener(Rect union);
		
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
			SetBackgroundDrawable(new ColorDrawable(Color.Transparent));
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
				_popupView.SetOnSystemUiVisibilityChangeListener(this);
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
			MeasureRect();
		}

		public void OnSystemUiVisibilityChange([GeneratedEnum] StatusBarVisibility visibility)
		{
			var isVisibleNavigationBar = ((int)visibility & (int)SystemUiFlags.HideNavigation) == 0;
			MeasureRect(isVisibleNavigationBar);
		}

		private void MeasureRect(bool? isVisibleNavigationBar = null)
		{
			// It is possible to get the usable rect in order to find the free usable space. Since it is also possible to hide / show navigation
			// on some devices, we need to manually remove it if needed
			// Their placements can be calculated based on the follow observation:
			// [size] realMetrics	: screen
			// [rect] usableRect	: usable area = screen - (top: status_bar) - (bottom: keyboard + nav_bar)
			var realMetrics = Get<DisplayMetrics>(_activity.WindowManager.DefaultDisplay.GetRealMetrics);
			var usableRect = Get<Rect>(_popupView.GetWindowVisibleDisplayFrame);

			var isNavigationBarUnconsidered = NavigationBarHelper.PhysicalNavigationBarHeight == (realMetrics.HeightPixels - usableRect.Bottom);
			var isNavigationBarVisible = isVisibleNavigationBar ?? NavigationBarHelper.IsNavigationBarVisible;

			var topOccludedRect = isNavigationBarUnconsidered && !isNavigationBarVisible
				? realMetrics.HeightPixels
				: usableRect.Bottom;

			var occupiedRect = new Rect(0, topOccludedRect, realMetrics.WidthPixels, realMetrics.HeightPixels);

			_onLayoutChanged?.Invoke(occupiedRect);

			T Get<T>(Action<T> getter) where T : new()
			{
				var result = new T();
				getter(result);

				return result;
			}
		}
	}
}
