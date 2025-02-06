using System.Threading.Tasks;
using Android.App;
using Android.Views;
using Uno.Foundation.Logging;
using Uno.UI;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Uno.Foundation.Logging;
using System.Threading.Tasks;
using Uno.UI.ViewManagement.Helpers;

namespace Windows.UI.ViewManagement
{
	public sealed partial class StatusBar
	{
		private bool? _isShown;

		public double BackgroundOpacity
		{
			get => StatusBarHelper.BackgroundColor is { } color ? color.A / 255.0 : 0;
			set
			{
				var existingColor = StatusBarHelper.BackgroundColor ?? Colors.Transparent;
				var updatedColor = Color.FromArgb((byte)(value * 255.0), existingColor.R, existingColor.G, existingColor.B);
				StatusBarHelper.BackgroundColor = updatedColor;
			}
		}

		public global::Windows.UI.Color? BackgroundColor
		{
			get => StatusBarHelper.BackgroundColor;
			set => StatusBarHelper.BackgroundColor = value;
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

		private double PhysicalToLogicalPixels(int physicalPixels) => physicalPixels / _displayInformation.RawPixelsPerViewPixel;

		public IAsyncAction ShowAsync()
		{
			return AsyncAction.FromTask(ct =>
			{
				CoreDispatcher.CheckThreadAccess();
				_isShown = true;
				UpdateSystemUiVisibility();
				Showing?.Invoke(this, null);
				return Task.CompletedTask;
			});
		}

		public IAsyncAction HideAsync()
		{
			return AsyncAction.FromTask(ct =>
			{
				CoreDispatcher.CheckThreadAccess();
				_isShown = false;
				UpdateSystemUiVisibility();
				Hiding?.Invoke(this, null);
				return Task.CompletedTask;
			});
		}

		internal void UpdateSystemUiVisibility()
		{
#pragma warning disable 618
			var activity = ContextHelper.Current as Activity;
			var decorView = activity.Window.DecorView;
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
				if (StatusBarHelper.ForegroundType == StatusBarForegroundType.Dark)
				{
					// Dark text to show up on your light status bar
					newUiOptions |= (int)SystemUiFlags.LightStatusBar;
				}
				else if (StatusBarHelper.ForegroundType == StatusBarForegroundType.Light)
				{
					// Light text to show up on your dark status bar
					newUiOptions &= ~(int)SystemUiFlags.LightStatusBar;
				}
			}

#pragma warning disable CA1422 // Validate platform compatibility
			decorView.SystemUiVisibility = (StatusBarVisibility)newUiOptions;
#pragma warning restore CA1422 // Validate platform compatibility
			activity.OnConfigurationChanged(activity.Resources.Configuration);
#pragma warning restore 618
		}

		private int StatusBarHeightResourceId =>
			_statusBarHeightResourceId ??=
				((Activity)ContextHelper.Current).Resources.GetIdentifier("status_bar_height", "dimen", "android");
	}
}
