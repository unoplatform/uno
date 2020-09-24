using System;
using System.Collections.Generic;
using System.Text;
using Android.App;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using Android.Widget;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Rect = Android.Graphics.Rect;

namespace Uno.UI
{
	internal class LayoutProvider
	{
		public delegate void LayoutChangedListener(Rect statusBar, Rect keyboard, Rect navigationBar);
		public delegate void InsetsChangedListener(Thickness insets);

		public event LayoutChangedListener LayoutChanged;
		public event InsetsChangedListener InsetsChanged;

		public Thickness Insets { get; internal set; } = new Thickness(0, 0, 0, 0);
		public Rect StatusBarRect { get; private set; } = new Rect(0, 0, 0, 0);
		public Rect KeyboardRect { get; private set; } = new Rect(0, 0, 0, 0);
		public Rect NavigationBarRect { get; private set; } = new Rect(0, 0, 0, 0);

		private readonly Activity _activity;
		private readonly GlobalLayoutProvider _adjustNothingLayoutProvider, _adjustResizeLayoutProvider;

		public LayoutProvider(Activity activity)
		{
			this._activity = activity;

			_adjustNothingLayoutProvider = new GlobalLayoutProvider(activity, null, null)
			{
				SoftInputMode = SoftInput.AdjustNothing | SoftInput.StateUnchanged,
				InputMethodMode = InputMethod.NotNeeded,
			};
			_adjustResizeLayoutProvider = new GlobalLayoutProvider(activity, MeasureLayout, MeasureInsets)
			{
				SoftInputMode = SoftInput.AdjustResize | SoftInput.StateUnchanged,
				InputMethodMode = InputMethod.Needed,
			};
		}

		// lifecycle management
		internal void Start(View view)
		{
			if (view?.WindowToken != null)
			{
				_adjustNothingLayoutProvider.Start(view);
				_adjustResizeLayoutProvider.Start(view);
			}
		}

		internal void Stop()
		{
			_adjustNothingLayoutProvider.Stop();
			_adjustResizeLayoutProvider.Stop();
		}

		private void MeasureLayout(PopupWindow sender)
		{
#pragma warning disable 618
			// We can obtain the size of keyboard by comparing the layout of two popup windows
			// where one (AdjustResize) resizes to keyboard and one(AdjustNothing) that doesn't:
			// [size] realMetrics			: screen 
			// [size] metrics				: screen - dead zones
			// [rect] displayRect			: screen - (bottom: nav_bar)
			// [rect] adjustNothingFrame	: screen - (top: status_bar) - (bottom: nav_bar)
			// [rect] adjustResizeFrame		: screen - (top: status_bar) - (bottom: keyboard + nav_bar)
			var realMetrics = Get<DisplayMetrics>(_activity.WindowManager.DefaultDisplay.GetRealMetrics);
			var metrics = Get<DisplayMetrics>(_activity.WindowManager.DefaultDisplay.GetMetrics);
			var displayRect = Get<Rect>(_activity.WindowManager.DefaultDisplay.GetRectSize);
			var adjustNothingFrame = Get<Rect>(_adjustNothingLayoutProvider.ContentView.GetWindowVisibleDisplayFrame);
			var adjustResizeFrame = Get<Rect>(_adjustResizeLayoutProvider.ContentView.GetWindowVisibleDisplayFrame);
#pragma warning restore 618

			var orientation = DisplayInformation.GetForCurrentView().CurrentOrientation;

			StatusBarRect = new Rect(0, 0, realMetrics.WidthPixels, adjustNothingFrame.Top);
			KeyboardRect = new Rect(0, adjustResizeFrame.Bottom, realMetrics.WidthPixels, adjustNothingFrame.Bottom);

			switch (orientation)
			{
				case DisplayOrientations.Landscape:
					NavigationBarRect = new Rect(0, 0, metrics.WidthPixels - displayRect.Width(), metrics.HeightPixels);
					break;
				case DisplayOrientations.LandscapeFlipped:
					NavigationBarRect = new Rect(adjustNothingFrame.Width(), 0, metrics.WidthPixels - displayRect.Width(), metrics.HeightPixels);
					break;
				// Miss portrait flipped
				case DisplayOrientations.Portrait:
				default:
					NavigationBarRect = new Rect(0, adjustNothingFrame.Bottom, realMetrics.WidthPixels, realMetrics.HeightPixels);
					break;
			}

			LayoutChanged?.Invoke(StatusBarRect, KeyboardRect, NavigationBarRect);

			T Get<T>(Action<T> getter) where T : new()
			{
				var result = new T();
				getter(result);

				return result;
			}
		}

		private void MeasureInsets(PopupWindow sender, WindowInsets insets)
		{
#pragma warning disable 618
			Insets = new Thickness(
				ViewHelper.PhysicalToLogicalPixels(insets.SystemWindowInsetLeft),
				ViewHelper.PhysicalToLogicalPixels(insets.SystemWindowInsetTop),
				ViewHelper.PhysicalToLogicalPixels(insets.SystemWindowInsetRight),
				ViewHelper.PhysicalToLogicalPixels(insets.SystemWindowInsetBottom)
			);
#pragma warning restore 618

			InsetsChanged?.Invoke(Insets);
		}

		private class GlobalLayoutProvider : PopupWindow, ViewTreeObserver.IOnGlobalLayoutListener, View.IOnApplyWindowInsetsListener
		{
			public delegate void GlobalLayoutListener(PopupWindow sender);
			public delegate void WindowInsetsListener(PopupWindow sender, WindowInsets insets);

			private readonly GlobalLayoutListener _globalListener;
			private readonly WindowInsetsListener _insetsListener;
			private readonly Activity _activity;

			public GlobalLayoutProvider(Activity activity, GlobalLayoutListener globalListener, WindowInsetsListener insetsListener) : base(activity)
			{
				_activity = activity;
				_globalListener = globalListener;
				_insetsListener = insetsListener;

				ContentView = new LinearLayout(_activity.BaseContext)
				{
					LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
				};
				Width = 0; // this make sure we don't block touch events
				Height = ViewGroup.LayoutParams.MatchParent;
				SetBackgroundDrawable(new ColorDrawable(Color.Transparent));
			}

			// lifecycle management
			public void Start(View view)
			{
				if (!IsShowing)
				{
					ShowAtLocation(view, GravityFlags.NoGravity, 0, 0);
					ContentView.ViewTreeObserver.AddOnGlobalLayoutListener(this);
				}
			}

			public void StartListenInsets()
			{
				_activity.Window.DecorView.SetOnApplyWindowInsetsListener(this);
			}

			public void Stop()
			{
				if (IsShowing)
				{
					Dismiss();
					ContentView.ViewTreeObserver.RemoveOnGlobalLayoutListener(this);
					_activity.Window.DecorView.SetOnApplyWindowInsetsListener(null);
				}
			}

			// event hook
			public void OnGlobalLayout() => _globalListener?.Invoke(this);

			public WindowInsets OnApplyWindowInsets(View v, WindowInsets insets)
			{
				_insetsListener?.Invoke(this, insets);
				// We need to consume insets here since we will handle them in the Window.Android.cs
#pragma warning disable 618
				return insets.ConsumeSystemWindowInsets();
#pragma warning restore 618
			}
		}
	}
}
