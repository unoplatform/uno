using System.Threading.Tasks;
using Android.App;
using Android.Views;
using AndroidX.Core.View;
using Uno.Foundation.Logging;
using Uno.UI;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.UI.Core;

namespace Windows.UI.ViewManagement
{
	public sealed partial class StatusBar
	{
		private StatusBarForegroundType? _foregroundType;
		private bool? _isShown;

		private DisplayInformation _displayInformation;

		private int? _statusBarHeightResourceId;

		private void SetStatusBarForegroundType(StatusBarForegroundType foregroundType)
		{
			if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
			{
				_foregroundType = foregroundType;
				UpdateSystemUiVisibility();
			}
			else
			{
				this.Log().Warn("The status bar foreground color couldn't be changed. This API is only available starting from Android M (API 23).");
			}
		}

		private StatusBarForegroundType GetStatusBarForegroundType()
		{
			if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
			{
				var activity = ContextHelper.Current as Activity;
#pragma warning disable 618
#pragma warning disable CA1422 // Validate platform compatibility
				int uiVisibility = (int)activity.Window.DecorView.SystemUiVisibility;
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore 618

				var isForegroundDark = (int)SystemUiFlags.LightStatusBar == (uiVisibility & (int)SystemUiFlags.LightStatusBar);

				return isForegroundDark
					? StatusBarForegroundType.Dark
					: StatusBarForegroundType.Light;
			}
			else
			{
				// The status bar foreground is always light below Android M (API 23)
				return StatusBarForegroundType.Light;
			}
		}

		public Rect GetOccludedRect()
		{
			var activity = ContextHelper.Current as Activity;

			var occludedRect = new Rect();

			// Height
			if (StatusBarHeightResourceId > 0)
			{
				var physicalStatusBarHeight = activity.Resources.GetDimensionPixelSize(StatusBarHeightResourceId);
				var logicalStatusBarHeight = PhysicalToLogicalPixels(physicalStatusBarHeight);
				occludedRect.Height = logicalStatusBarHeight;
			}

			// Width
			var physicalStatusBarWidth = activity.Window.DecorView.Width;
			var logicalStatusBarWidth = PhysicalToLogicalPixels(physicalStatusBarWidth);
			occludedRect.Width = logicalStatusBarWidth;

			return occludedRect;
		}

		private double PhysicalToLogicalPixels(int physicalPixels)
		{
			_displayInformation ??= DisplayInformation.GetForCurrentViewSafe(); // TODO Uno: Avoid using this #16404
			return physicalPixels / _displayInformation.RawPixelsPerViewPixel;
		}

		public IAsyncAction ShowAsync() => SetVisibilityAsyncCore(true);

		public IAsyncAction HideAsync() => SetVisibilityAsyncCore(false);

		private IAsyncAction SetVisibilityAsyncCore(bool visible)
		{
			return AsyncAction.FromTask(ct =>
			{
				CoreDispatcher.CheckThreadAccess();
				_isShown = visible;
				UpdateSystemUiVisibility();

				if (visible)
				{
					Showing?.Invoke(this, null);
				}
				else
				{
					Hiding?.Invoke(this, null);
				}

				return Task.CompletedTask;
			});
		}

		internal void UpdateSystemUiVisibility()
		{
			if (!ContextHelper.TryGetCurrent(out var context) || context is not Activity activity)
			{
				// The API was used too early in application lifecycle
				return;
			}

			var decorView = activity.Window?.DecorView;

			if (activity is null || decorView is null)
			{
				// The API was used too early in application lifecycle
				return;
			}

			if ((int)Android.OS.Build.VERSION.SdkInt >= 30)
			{
				var insetsController = WindowCompat.GetInsetsController(activity.Window, decorView);
				if (insetsController != null)
				{
					if (_isShown.HasValue)
					{
						if (_isShown.Value)
						{
							insetsController.Show(WindowInsetsCompat.Type.StatusBars());
						}
						else
						{
							insetsController.Hide(WindowInsetsCompat.Type.StatusBars());
						}
					}

					// A bit confusingly, "appearance light" refers to light theme, so dark foreground is used!
					insetsController.AppearanceLightStatusBars = _foregroundType == StatusBarForegroundType.Dark;
				}
			}
			else
			{
#pragma warning disable 618
#pragma warning disable CA1422 // Validate platform compatibility
				var uiOptions = (int)decorView.SystemUiVisibility;
#pragma warning restore CA1422 // Validate platform compatibility
				var newUiOptions = (int)uiOptions;

				if (_isShown.HasValue)
				{
					if (_isShown.Value)
					{
						newUiOptions &= ~(int)SystemUiFlags.Fullscreen;
					}
					else
					{
						newUiOptions |= (int)SystemUiFlags.Fullscreen;
					}
				}

				if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
				{
					if (_foregroundType == StatusBarForegroundType.Dark)
					{
						// Dark text to show up on your light status bar
						newUiOptions |= (int)SystemUiFlags.LightStatusBar;
					}
					else if (_foregroundType == StatusBarForegroundType.Light)
					{
						// Light text to show up on your dark status bar
						newUiOptions &= ~(int)SystemUiFlags.LightStatusBar;
					}
				}

#pragma warning disable CA1422 // Validate platform compatibility
				decorView.SystemUiVisibility = (StatusBarVisibility)newUiOptions;
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore 618
			}

			activity.OnConfigurationChanged(activity.Resources.Configuration);
		}

		private int StatusBarHeightResourceId =>
			_statusBarHeightResourceId ??=
				((Activity)ContextHelper.Current).Resources.GetIdentifier("status_bar_height", "dimen", "android");
	}
}
