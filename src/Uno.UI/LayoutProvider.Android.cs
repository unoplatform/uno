using System;
using System.Collections.Generic;
using System.Text;
using Android.App;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Core.View;
using Uno.Disposables;
using Uno.UI.Extensions;
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
		public delegate void KeyboardChangedListener(Rect keyboard);
		public delegate void InsetsChangedListener(Thickness insets);

		public event KeyboardChangedListener KeyboardChanged;
		public event InsetsChangedListener InsetsChanged;

		public Thickness Insets { get; internal set; } = new Thickness(0, 0, 0, 0);

		/// <summary>
		/// // Used by legacy visual bounds calculation on devices below API 30. Will always be default value on devices running API 30 and above.
		/// </summary>
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
				// making sure to stop before starting new ones.
				Stop();

				_adjustResizeLayoutProvider.Start(view);
				_adjustNothingLayoutProvider.Start(view);
			}
		}

		internal void Stop()
		{
			_adjustNothingLayoutProvider.Stop();
			_adjustResizeLayoutProvider.Stop();
		}

		private void MeasureLayout(PopupWindow sender)
		{
			var osVersion = Android.OS.Build.VERSION.SdkInt;
			if (osVersion >= Android.OS.BuildVersionCodes.R)
			{
				// Use newer API on 30 and above
				var windowMetrics = _activity.WindowManager.CurrentWindowMetrics;
				var imeInsets = windowMetrics.WindowInsets.GetInsets(WindowInsets.Type.Ime());
				var windowBottom = windowMetrics.Bounds.Bottom;
				var keyboardHeight = windowBottom - imeInsets.Bottom;
				var keyboardRect = keyboardHeight > 0 ? new Rect(0, keyboardHeight, windowMetrics.Bounds.Right, windowBottom) : new Rect();

				KeyboardChanged?.Invoke(keyboardRect);
			}
			else
			{
#pragma warning disable 618
#pragma warning disable CA1422 // Validate platform compatibility
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
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore 618

				var orientation = DisplayInformation.GetForCurrentView().CurrentOrientation;

				var keyboardRect = new Rect(0, adjustResizeFrame.Bottom, realMetrics.WidthPixels, adjustNothingFrame.Bottom);

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

				KeyboardChanged?.Invoke(keyboardRect);

				T Get<T>(Action<T> getter) where T : new()
				{
					var result = new T();
					getter(result);

					return result;
				}
			}
		}

		private void MeasureInsets(PopupWindow sender, WindowInsetsCompat insets)
		{
			var systemInsets = insets.GetInsets(WindowInsetsCompat.Type.SystemBars())
				.ToThickness()
				.PhysicalToLogicalPixels();

			InsetsChanged?.Invoke(systemInsets);
		}

		private class GlobalLayoutProvider : PopupWindow, ViewTreeObserver.IOnGlobalLayoutListener, AndroidX.Core.View.IOnApplyWindowInsetsListener
		{
			public delegate void GlobalLayoutListener(PopupWindow sender);
			public delegate void WindowInsetsListener(PopupWindow sender, WindowInsetsCompat insets);

			private readonly GlobalLayoutListener _globalListener;
			private readonly WindowInsetsListener _insetsListener;
			private readonly Activity _activity;
			private readonly SerialDisposable _listenerSubscription = new SerialDisposable();

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
					var disposables = new CompositeDisposable();
					_listenerSubscription.Disposable = disposables;

					ShowAtLocation(view, GravityFlags.NoGravity, 0, 0);
					disposables.Add(Disposable.Create(() => Dismiss()));


					ContentView.ViewTreeObserver.AddOnGlobalLayoutListener(this);
					disposables.Add(Disposable.Create(() => ContentView.ViewTreeObserver.RemoveOnGlobalLayoutListener(this)));

					if (_insetsListener is { })
					{
						ViewCompat.SetOnApplyWindowInsetsListener(view, this);
						disposables.Add(Disposable.Create(() => ViewCompat.SetOnApplyWindowInsetsListener(view, null)));
					}
				}
			}

			public void Stop()
			{
				if (IsShowing)
				{
					_listenerSubscription.Disposable = null;
				}
			}

			// event hook
			public void OnGlobalLayout() => _globalListener?.Invoke(this);

			public WindowInsetsCompat OnApplyWindowInsets(View v, WindowInsetsCompat insets)
			{
				_insetsListener?.Invoke(this, insets);

				return insets;
			}
		}
	}
}
