using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Views;
using Uno.Extensions;
using Uno.Logging;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;

namespace Uno.UI.DualScreen
{
	public class DuoApplicationViewSpanningRects : IApplicationViewSpanningRects
	{
		private bool? _isDualScreenDevice;
		private Microsoft.Device.Display.ScreenHelper _helper;
		private List<Rect> _spanningRects = null;

		private readonly List<Rect> EmptyList = new List<Rect>(0);

		public DuoApplicationViewSpanningRects(object owner)
		{
		}

		public IReadOnlyList<Rect> GetSpanningRects()
		{
			if (ContextHelper.Current is Activity currentActivity)
			{
				InitializeHelper(currentActivity);

				if (_isDualScreenDevice.Value && _helper.IsDualMode)
				{
					var wuxWindowBounds = ApplicationView.GetForCurrentView().VisibleBounds.LogicalToPhysicalPixels();
					var helperOrientation = _helper.GetRotation();
					var wuOrientation = DisplayInformation.GetForCurrentView().CurrentOrientation;

					var occludedRects = _helper
						.DisplayMask
						.GetBoundingRectsForRotation(helperOrientation)
						.Select(r => (Rect)r)
						.ToArray();

					if (occludedRects.Length > 0)
					{
						Rect bounds = wuxWindowBounds;
						var occludedRect = occludedRects[0];
						var intersection = bounds;
						intersection.Intersect(occludedRect);

						if (wuOrientation == DisplayOrientations.Portrait || wuOrientation == DisplayOrientations.PortraitFlipped)
						{
							// Compensate for the status bar size (the occluded area is rooted on the screen size, whereas
							// wuxWindowBoundsis rooted on the visible size of the window, unless the status bar is translucent.
							if (bounds.X == 0 && bounds.Y == 0)
							{
								var statusBarRect = StatusBar.GetForCurrentView().OccludedRect.LogicalToPhysicalPixels();
								occludedRect.Y -= statusBarRect.Height;
							}
						}

						if (intersection != Rect.Empty)
						{
							if (occludedRect.X == bounds.X)
							{
								_spanningRects = new List<Rect> {
										new Rect(bounds.X, bounds.Y, bounds.Width, occludedRect.Y),
										new Rect(bounds.X, bounds.Y + occludedRect.Y + occludedRect.Height, bounds.Width, bounds.Height - (occludedRect.Y + occludedRect.Height)),
									};

								if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
								{
									this.Log().Debug($"DualMode: Horizontal spanning rects: {string.Join(";", _spanningRects)}");
								}
							}
							else if (occludedRect.Y == bounds.Y)
							{
								// Horizontal

								_spanningRects = new List<Rect> {
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

			return _spanningRects ?? EmptyList;
		}

		private void InitializeHelper(Activity currentActivity)
		{
			if (!_isDualScreenDevice.HasValue && _helper == null)
			{
				_isDualScreenDevice = Microsoft.Device.Display.ScreenHelper.IsDualScreenDevice(currentActivity);

				if (_isDualScreenDevice.Value)
				{
					_helper = new Microsoft.Device.Display.ScreenHelper();
					_helper.Initialize(currentActivity);
				}
			}
		}

		private SurfaceOrientation GetOrientation()
		{
			switch (DisplayInformation.GetForCurrentView().CurrentOrientation)
			{
				default:
				case DisplayOrientations.Portrait:
					return SurfaceOrientation.Rotation0;
				case DisplayOrientations.Landscape:
					return SurfaceOrientation.Rotation90;
				case DisplayOrientations.PortraitFlipped:
					return SurfaceOrientation.Rotation180;
				case DisplayOrientations.LandscapeFlipped:
					return SurfaceOrientation.Rotation270;
			}
		}
	}
}
