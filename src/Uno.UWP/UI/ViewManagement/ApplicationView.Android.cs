#if __ANDROID__
using System;
using System.Linq;
using Android.App;
using Android.Views;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.Core;

namespace Windows.UI.ViewManagement
{
	partial class ApplicationView
	{
		Microsoft.Device.Display.ScreenHelper _helper;

		public bool IsScreenCaptureEnabled
		{
			get
			{
				if (!(ContextHelper.Current is Activity activity))
				{
					throw new InvalidOperationException($"{nameof(IsScreenCaptureEnabled)} API must be called when Activity is created");
				}
				return !activity.Window.Attributes.Flags.HasFlag(WindowManagerFlags.Secure);
			}
			set
			{
				if (!(ContextHelper.Current is Activity activity))
				{
					throw new InvalidOperationException($"{nameof(IsScreenCaptureEnabled)} API must be called when Activity is created");
				}
				if (value)
				{
					activity.Window.ClearFlags(WindowManagerFlags.Secure);
				}
				else
				{
					activity.Window.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure);
				}
			}
		}

		internal void SetVisibleBounds(Rect newVisibleBounds)
		{
			if (newVisibleBounds != VisibleBounds)
			{
				VisibleBounds = newVisibleBounds;

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Updated visible bounds {VisibleBounds}");
				}

				VisibleBoundsChanged?.Invoke(this, null);
			}
		}

		public bool TryEnterFullScreenMode()
		{
			CoreDispatcher.CheckThreadAccess();
			UpdateFullScreenMode(true);
			return true;
		}

		public void ExitFullScreenMode()
		{
			CoreDispatcher.CheckThreadAccess();
			UpdateFullScreenMode(false);
		}

		private void UpdateFullScreenMode(bool isFullscreen)
		{
			var activity = ContextHelper.Current as Activity;
			var uiOptions = (int)activity.Window.DecorView.SystemUiVisibility;

			if (isFullscreen)
			{
				uiOptions |= (int)SystemUiFlags.Fullscreen;
				uiOptions |= (int)SystemUiFlags.ImmersiveSticky;
				uiOptions |= (int)SystemUiFlags.HideNavigation;
				uiOptions |= (int)SystemUiFlags.LayoutHideNavigation;
			}
			else
			{
				uiOptions &= ~(int)SystemUiFlags.Fullscreen;
				uiOptions &= ~(int)SystemUiFlags.ImmersiveSticky;
				uiOptions &= ~(int)SystemUiFlags.HideNavigation;
				uiOptions &= ~(int)SystemUiFlags.LayoutHideNavigation;
			}

			activity.Window.DecorView.SystemUiVisibility = (StatusBarVisibility)uiOptions;
		}

		partial void UpdateSpanningRects()
		{
			if (ContextHelper.Current is Activity currentActivity)
			{
				if (_helper == null)
				{
					_helper = new Microsoft.Device.Display.ScreenHelper();
					_helper.Initialize(currentActivity);
				}

				if (_helper?.IsDualMode ?? false)
				{
					var occludedRects = _helper
						.DisplayMask
						.GetBoundingRects()
						.Select(r => (Rect)r)
						.ToArray();

					if(occludedRects.Length > 0)
					{
						Android.Graphics.Rect aBounds = new Android.Graphics.Rect();
						currentActivity.Window.DecorView.RootView.GetDrawingRect(aBounds);

						Rect bounds = aBounds;
						var occludedRect = occludedRects[0];
						var intersection = bounds;
						intersection.Intersect(occludedRect);

						if (intersection != Rect.Empty)
						{
							if(occludedRect.X == bounds.X)
							{
								_spanningRects = new[] {
									new Rect(bounds.X, bounds.Y, bounds.Width, occludedRect.Y),
									new Rect(bounds.X, bounds.Y + occludedRect.Y + occludedRect.Height, bounds.Width, bounds.Height - (occludedRect.Y + occludedRect.Height)),
								};

								if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
								{
									this.Log().Debug($"DualMode: Horizontal spanning rects: {string.Join(";", _spanningRects)}");
								}
							}
							else if(occludedRect.Y == bounds.Y)
							{
								_spanningRects = new[] {
									new Rect(bounds.X, bounds.Y, occludedRect.X, bounds.Height),
									new Rect(bounds.X + occludedRect.X + occludedRect.Width, bounds.Y, bounds.Width - (occludedRect.X + occludedRect.Width), bounds.Height),
								};

								if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
								{
									this.Log().Debug($"DualMode: Vertical spanning rects: {string.Join(";", _spanningRects)}");
								}
							}
							else
							{
								if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
								{
									this.Log().Debug($"DualMode: Unknown screen layout");
								}
							}
						}
						else
						{
							if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
							{
								this.Log().Debug($"DualMode: Without intersection, single screen");
							}
						}
					}
					else
					{
						if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
						{
							this.Log().Debug($"DualMode: Without occlusion");
						}
					}
				}
			}
		}
	}
}
#endif
