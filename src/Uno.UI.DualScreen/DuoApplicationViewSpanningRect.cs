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
		Microsoft.Device.Display.ScreenHelper _helper;
		List<Rect> _spanningRects = null;

		private readonly List<Rect> EmptyList = new List<Rect>(0);

		public DuoApplicationViewSpanningRects(object owner)
		{
		}

		public IReadOnlyList<Rect> GetSpanningRects()
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
					var wuxWindowBounds = ApplicationView.GetForCurrentView().VisibleBounds.LogicalToPhysicalPixels();
					var helperOrientation = _helper.GetRotation();
					var wuOrientation = DisplayInformation.GetForCurrentView().CurrentOrientation;

					var occludedRects = _helper
						.DisplayMask
						.GetBoundingRectsForRotation(_helper.GetRotation())
						.Select(r => (Rect)r)
						.ToArray();

					if (occludedRects.Length > 0)
					{
						Android.Graphics.Rect aBounds = new Android.Graphics.Rect();
						currentActivity.Window.DecorView.RootView.GetDrawingRect(aBounds);


						Rect bounds = wuxWindowBounds;
						var occludedRect = occludedRects[0];
						var intersection = bounds;
						intersection.Intersect(occludedRect);

						if(wuOrientation == DisplayOrientations.Portrait || wuOrientation == DisplayOrientations.PortraitFlipped)
						{
							var statusBarRect = StatusBar.GetForCurrentView().OccludedRect.LogicalToPhysicalPixels();
							occludedRect.Y -= statusBarRect.Height;
						}

						if (bounds == wuxWindowBounds)
						{
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
								this.Log().Debug($"DualMode: Skipping inconsistent window size");
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
