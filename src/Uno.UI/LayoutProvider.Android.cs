using System;
using System.Collections.Generic;
using System.Text;
using Android.App;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using Android.Widget;
using Windows.Foundation;
using Rect = Android.Graphics.Rect;

namespace Uno.UI
{
	public class LayoutProvider
	{
		public delegate void LayoutChangedListener(Rect statusBar, Rect keyboard, Rect navigationBar);

		public static LayoutProvider Instance { get; private set; }

		public event LayoutChangedListener LayoutChanged;

		public Rect StatusBarLayout { get; private set; } = new Rect(0, 0, 0, 0);
		public Rect KeyboardLayout { get; private set; } = new Rect(0, 0, 0, 0);
		public Rect NavigationBarLayout { get; private set; } = new Rect(0, 0, 0, 0);

		private readonly Activity _activity;
		private readonly GlobalLayoutProvider _adjustNothingLayoutProvider, _adjustResizeLayoutProvider;

		internal LayoutProvider(Activity activity)
		{
			this._activity = activity;

			_adjustNothingLayoutProvider = new GlobalLayoutProvider(activity, null)
			{
				SoftInputMode = SoftInput.AdjustNothing | SoftInput.StateUnchanged,
				InputMethodMode = InputMethod.NotNeeded,
			};
			_adjustResizeLayoutProvider = new GlobalLayoutProvider(activity, MeasureLayout)
			{
				SoftInputMode = SoftInput.AdjustResize | SoftInput.StateUnchanged,
				InputMethodMode = InputMethod.Needed,
			};

			Instance = this;
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
			// We can obtain the size of keyboard by comparing the layout of two popup windows
			// where one (AdjustResize) resizes to keyboard and one(AdjustNothing) that doesn't:
			// [size] realMetrics			: screen
			// [rect] adjustNothingFrame	: screen - (top: status_bar) - (bottom: nav_bar)
			// [rect] adjustResizeFrame		: screen - (top: status_bar) - (bottom: keyboard + nav_bar)
			var realMetrics = Get<DisplayMetrics>(_activity.WindowManager.DefaultDisplay.GetRealMetrics);
			var adjustNothingFrame = Get<Rect>(_adjustNothingLayoutProvider.ContentView.GetWindowVisibleDisplayFrame);
			var adjustResizeFrame = Get<Rect>(_adjustResizeLayoutProvider.ContentView.GetWindowVisibleDisplayFrame);

			StatusBarLayout = new Rect(0, 0, realMetrics.WidthPixels, adjustNothingFrame.Top);
			KeyboardLayout = new Rect(0, adjustResizeFrame.Bottom, realMetrics.WidthPixels, adjustNothingFrame.Bottom);
			NavigationBarLayout = new Rect(0, adjustNothingFrame.Bottom, realMetrics.WidthPixels, realMetrics.HeightPixels);

			LayoutChanged?.Invoke(StatusBarLayout, KeyboardLayout, NavigationBarLayout);

			T Get<T>(Action<T> getter) where T : new()
			{
				var result = new T();
				getter(result);

				return result;
			}
		}


		private class GlobalLayoutProvider : PopupWindow, ViewTreeObserver.IOnGlobalLayoutListener
		{
			public delegate void GlobalLayoutListener(PopupWindow sender);

			private readonly GlobalLayoutListener _listener;
			private readonly Activity _activity;

			public GlobalLayoutProvider(Activity activity, GlobalLayoutListener listener) : base(activity)
			{
				this._activity = activity;
				this._listener = listener;

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
			public void Stop()
			{
				if (IsShowing)
				{
					Dismiss();
					ContentView.ViewTreeObserver.RemoveOnGlobalLayoutListener(this);
				}
			}

			// event hook
			public void OnGlobalLayout() => _listener?.Invoke(this);
		}
	}
}
