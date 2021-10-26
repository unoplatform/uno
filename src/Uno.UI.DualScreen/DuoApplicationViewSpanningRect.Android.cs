using System;
using System.Collections.Generic;
using Android.App;
using Android.Views;
using Uno.Extensions;
using Uno.Logging;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Uno.Devices.Sensors;

namespace Uno.UI.DualScreen
{
	public class DuoApplicationViewSpanningRects : IApplicationViewSpanningRects, INativeDualScreenProvider
	{
		private bool? _isDualScreenDevice;
		private Microsoft.Device.Display.ScreenHelper _helper;
		private (SurfaceOrientation orientation, List<Rect> result) _previousMode = EmptyMode;

		private static readonly (SurfaceOrientation orientation, List<Rect> result) EmptyMode =
			((SurfaceOrientation)(-1), null);

		private readonly List<Rect> _emptyList = new List<Rect>(0);

		public DuoApplicationViewSpanningRects(object owner)
		{
		}

		public IReadOnlyList<Rect> GetSpanningRects()
		{
			if (ContextHelper.Current is Activity currentActivity)
			{
				InitializeHelper(currentActivity);

				if (_isDualScreenDevice.Value)
				{
					if (_helper.IsDualMode)
					{
						var helperOrientation = _helper.GetRotation();
						if (_previousMode.orientation == helperOrientation && _previousMode.result != null)
						{
							return _previousMode.result;
						}

						_previousMode.orientation = helperOrientation;
						_previousMode.result = null;

						var wuxWindowBounds = ApplicationView.GetForCurrentView().VisibleBounds.LogicalToPhysicalPixels();
						var wuOrientation = DisplayInformation.GetForCurrentView().CurrentOrientation;

						var occludedRects = _helper
							.DisplayMask
							.GetBoundingRectsForRotation(helperOrientation)
							.SelectToArray(r => (Rect)r); // convert to managed Rect

						if (occludedRects.Length > 0)
						{
							if (occludedRects.Length > 1 && this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
							{
								this.Log().Warn($"DualMode: Unknown screen layout, more than one occluded region. Only first will be considered. Please report your device to Uno Platform!");
							}

							var bounds = wuxWindowBounds;
							var occludedRect = occludedRects[0];
							var intersection = bounds.IntersectWith(occludedRect);

							if (wuOrientation == DisplayOrientations.Portrait || wuOrientation == DisplayOrientations.PortraitFlipped)
							{
								// Compensate for the status bar size (the occluded area is rooted on the screen size, whereas
								// wuxWindowBoundsis rooted on the visible size of the window, unless the status bar is translucent.
								if ((int)bounds.X == 0 && (int)bounds.Y == 0)
								{
									var statusBarRect = StatusBar.GetForCurrentView().OccludedRect.LogicalToPhysicalPixels();
									occludedRect.Y -= statusBarRect.Height;
								}
							}

							if (intersection != null) // Occluded region overlaps the app
							{
								if ((int)occludedRect.X == (int)bounds.X)
								{
									// Vertical stacking
									// +---------+
									// |         |
									// |         |
									// +---------+
									// +---------+
									// |         |
									// |         |
									// +---------+

									var spanningRects = new List<Rect> {
										// top region
										new Rect(bounds.X, bounds.Y, bounds.Width, occludedRect.Top),
										// bottom region
										new Rect(bounds.X,
											occludedRect.Bottom,
											bounds.Width,
											bounds.Height - occludedRect.Bottom),
									};

									if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
									{
										this.Log().Debug($"DualMode: Horizontal spanning rects: {string.Join(";", spanningRects)}");
									}

									_previousMode.result = spanningRects;
								}
								else if ((int)occludedRect.Y == (int)bounds.Y)
								{
									// Horizontal side-by-side
									// +-----+ +-----+
									// |     | |     |
									// |     | |     |
									// |     | |     |
									// |     | |     |
									// |     | |     |
									// +-----+ +-----+

									var spanningRects = new List<Rect> {
										// left region
										new Rect(bounds.X, bounds.Y, occludedRect.X, bounds.Height),
										// right region
										new Rect(occludedRect.Right,
											bounds.Y,
											bounds.Width - occludedRect.Right,
											bounds.Height),
									};

									if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
									{
										this.Log().Debug($"DualMode: Vertical spanning rects: {string.Join(";", spanningRects)}");
									}

									_previousMode.result = spanningRects;
								}
								else
								{
									if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
									{
										this.Log().Warn($"DualMode: Unknown screen layout");
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
					else
					{
						_previousMode = EmptyMode;
					}
				}
			}

			return _previousMode.result ?? _emptyList;
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

		public bool? IsSpanned
		{
			get
			{
				if (ContextHelper.Current is Activity currentActivity)
				{
					InitializeHelper(currentActivity);
					return _helper.IsDualMode;
				}

				return null;
			}
		}

		public bool IsDualScreen
		{
			get
			{
				if (!(ContextHelper.Current is Activity currentActivity))
				{
					throw new InvalidOperationException("The API was called too early in the application lifecycle");
				}

				InitializeHelper(currentActivity);
				return _isDualScreenDevice ?? false;
			}
		}
	}
}
