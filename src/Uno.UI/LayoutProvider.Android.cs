using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Android.App;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using Android.Widget;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Logging;
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

		internal void StartListenInsets()
		{
			_adjustNothingLayoutProvider.StartListenInsets();
			_adjustResizeLayoutProvider.StartListenInsets();
		}

		internal void Stop()
		{
			_adjustNothingLayoutProvider.Stop();
			_adjustResizeLayoutProvider.Stop();
		}

		// handlers
		private void MeasureLayout(PopupWindow sender)
		{
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

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				var flags = _activity.Window.Attributes.Flags;
				var systemUiVisibility = _activity.Window.DecorView.SystemUiVisibility;

				var props = string.Join(", ", new Dictionary<string, string>
				{
					// measured values
					[nameof(realMetrics)] = JsonHelper.Jsonify(realMetrics),
					[nameof(adjustNothingFrame)] = JsonHelper.Jsonify(adjustNothingFrame),
					[nameof(adjustResizeFrame)] = JsonHelper.Jsonify(adjustResizeFrame),
					// computed values
					[nameof(StatusBarRect)] = JsonHelper.Jsonify(StatusBarRect),
					[nameof(KeyboardRect)] = JsonHelper.Jsonify(KeyboardRect),
					[nameof(NavigationBarRect)] = JsonHelper.Jsonify(NavigationBarRect),
					// for debugging
					[nameof(orientation)] = JsonHelper.Jsonify(orientation),
					[nameof(flags)] = JsonHelper.Jsonify(flags),
					[nameof(systemUiVisibility)] = JsonHelper.Jsonify(systemUiVisibility),
					[nameof(flags) + "Raw"] = JsonHelper.Jsonify((int)flags),
					[nameof(systemUiVisibility) + "Raw"] = JsonHelper.Jsonify((int)systemUiVisibility),
				}.Select(kvp => string.Concat(JsonHelper.Camelize(kvp.Key), ": ", kvp.Value)));

				this.Log().Debug($"=== MeasureLayout: {{ {props} }}");
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
			Insets = new Thickness(
				ViewHelper.PhysicalToLogicalPixels(insets.SystemWindowInsetLeft),
				ViewHelper.PhysicalToLogicalPixels(insets.SystemWindowInsetTop),
				ViewHelper.PhysicalToLogicalPixels(insets.SystemWindowInsetRight),
				ViewHelper.PhysicalToLogicalPixels(insets.SystemWindowInsetBottom)
			);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"=== MeasureInsets: {{ physicalInsets: {JsonHelper.Jsonify(ViewHelper.LogicalToPhysicalPixels(Insets))} }}");
			}

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
				return insets.ConsumeSystemWindowInsets();
			}
		}

		private static class JsonHelper
		{
			public static string Camelize(string x) => Regex.Replace(x, @"^\w", c => c.Value.ToLower());

			public static string Jsonify(bool x) => x.ToString().ToLower();

			public static string Jsonify(int x) => x.ToString().ToLower();

			public static string Jsonify(Enum x) => $"\"{x}\"";

			public static string Jsonify(DisplayMetrics x) => $"{{ width: {x.WidthPixels}, height: {x.HeightPixels} }}";

			public static string Jsonify(Rect x) => $"{{ left: {x.Left}, top: {x.Top}, right: {x.Right}, bottom: {x.Bottom} }}";

			public static string Jsonify(Thickness x) => $"{{ left: {x.Left}, top: {x.Top}, right: {x.Right}, bottom: {x.Bottom} }}";
		}
	}
}
