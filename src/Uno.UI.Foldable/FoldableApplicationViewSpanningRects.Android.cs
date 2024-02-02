using Android.App;
using Android.Views;
using AndroidX.Window.Java.Layout;
using AndroidX.Window.Layout;
using System;
using System.Collections.Generic;
using Uno.Devices.Sensors;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;

namespace Uno.UI.Foldable
{
	/// <summary>
	/// Provides two Rect that represent the two screen dimensions when
	/// an Android application is spanned across a hinge or fold (eg. Surface Duo)
	/// </summary>
	/// <remarks>
	/// Relies on the MainActivity implementing Jetpack Window Manager layout change listener,
	/// and exposing the properties needed to make UI change when required.
	/// FUTURE: implement an event for layout changes, so we can respond to folding state changes in app code too
	/// </remarks>
	[Preserve]
	public partial class FoldableApplicationViewSpanningRects : IApplicationViewSpanningRects, INativeDualScreenProvider
	{
		private (SurfaceOrientation orientation, List<Rect> result) _previousMode = EmptyMode;

		private static readonly (SurfaceOrientation orientation, List<Rect> result) EmptyMode =
			((SurfaceOrientation)(-1), null);

		private readonly List<Rect> _emptyList = new List<Rect>(0);

		public FoldableApplicationViewSpanningRects(object owner)
		{
			ViewManagement.ApplicationViewHelper.GetBaseActivityEvents().Create += OnCreateEvent;
			ViewManagement.ApplicationViewHelper.GetBaseActivityEvents().Start += OnStartEvent;
			ViewManagement.ApplicationViewHelper.GetBaseActivityEvents().Stop += OnStopEvent;
		}
		private void OnCreateEvent(Android.OS.Bundle savedInstanceState)
		{
			windowInfoTrackerCallbackAdapter = new WindowInfoTrackerCallbackAdapter(WindowInfoTracker.Companion.GetOrCreate(ContextHelper.Current as Android.App.Activity));
			windowMetricsCalculator = WindowMetricsCalculator.Companion.OrCreate; // HACK: source method is `getOrCreate`, binding generator munges this badly :(
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"DualMode: FoldableApplicationViewSpanningRects.OnCreateEvent");
			}
		}
		private void OnStartEvent()
		{
			windowInfoTrackerCallbackAdapter.AddWindowLayoutInfoListener(ContextHelper.Current as Activity, runOnUiThreadExecutor(), this); // `this` is the IConsumer implementation
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"DualMode: FoldableApplicationViewSpanningRects.OnStartEvent");
			}
		}
		private void OnStopEvent()
		{
			windowInfoTrackerCallbackAdapter.RemoveWindowLayoutInfoListener(this);
		}
		public IReadOnlyList<Rect> GetSpanningRects()
		{
			this.Log().Info($"DualMode: FoldableApplicationViewSpanningRects.GetSpanningRects HasFoldFeature={HasFoldFeature}");
			if (HasFoldFeature) // IsSeparating or just "is fold present?" - changing this will affect the behavior of TwoPaneView on foldable devices
			{
				_previousMode.result = null;

				var wuxWindowBounds = ApplicationView.GetForCurrentView().VisibleBounds.LogicalToPhysicalPixels();
				var wuOrientation = DisplayInformation.GetForCurrentView().CurrentOrientation;

				this.Log().Info($"DualMode:                  FoldBounds={FoldBounds}");
				// TODO: bring the list of all folding features here, for future compatibility
				List<Rect> occludedRects = new List<Rect>
					{   // Hinge/fold bounds
						new Rect(FoldBounds.Left,
						FoldBounds.Top,
						FoldBounds.Width(),
						FoldBounds.Height())
					};

				if (occludedRects.Count > 0)
				{
					if (occludedRects.Count > 1 && this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().Warn($"DualMode: Unknown screen layout, more than one occluded region. Only first will be considered. Please report your device to Uno Platform!");
					}

					var bounds = wuxWindowBounds;
					var occludedRect = occludedRects[0];
					var intersecting = ((Android.Graphics.RectF)bounds).Intersect(occludedRect);

					this.Log().Info($"DualMode: Intersect calculation: window " + bounds + " with occluded " + occludedRect);

					if (IsFoldVertical == false) // FoldOrientation == AndroidX.Window.Layout.FoldingFeatureOrientation.Horizontal)
					{
						// Compensate for the status bar size (the occluded area is rooted on the screen size, whereas
						// wuxWindowBoundsis rooted on the visible size of the window, unless the status bar is translucent.
						if ((int)bounds.X == 0 && (int)bounds.Y == 0)
						{
							var statusBarRect = StatusBar.GetForCurrentView().OccludedRect.LogicalToPhysicalPixels();
							occludedRect.Y -= statusBarRect.Height;
						}
					}

					if (intersecting) // Occluded region overlaps the app
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

							if (this.Log().IsEnabled(LogLevel.Debug))
							{
								this.Log().Debug($"DualMode: Horizontal spanning rects: {string.Join(';', spanningRects)}");
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

							if (this.Log().IsEnabled(LogLevel.Debug))
							{
								this.Log().Debug($"DualMode: Vertical spanning rects: {string.Join(';', spanningRects)}");
							}

							_previousMode.result = spanningRects;
						}
						else
						{
							if (this.Log().IsEnabled(LogLevel.Warning))
							{
								this.Log().Warn($"DualMode: Unknown screen layout");
							}
						}
						if (this.Log().IsEnabled(LogLevel.Debug))
						{
							this.Log().Debug($"DualMode:       _previousMode.result={_previousMode.result.Count}");
							foreach (var pmr in _previousMode.result)
							{
								this.Log().Debug($"               > {pmr}");
							}
						}
					}
					else
					{
						if (this.Log().IsEnabled(LogLevel.Debug))
						{
							this.Log().Debug($"DualMode: Without intersection, single screen");
						}
					}
				}
				else
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug($"DualMode: Without occlusion");
					}
				}
			}
			else
			{
				_previousMode = EmptyMode;
			}
			return _previousMode.result ?? _emptyList;
		}

		/// <summary>
		/// Whether the app is spanned across a hinge or fold (if the fold is not occluding, will not be detected as spanned)
		/// </summary>
		public bool? IsSpanned
		{
			get
			{
				return IsSeparating;
			}
		}

		public bool SupportsSpanning => HasFoldFeature || FoldableHingeAngleSensor.HasHinge;

		public Rect Bounds
		{
			get
			{
				if (!FoldBounds.IsEmpty)
				{
					return new Rect(FoldBounds.Left,
						FoldBounds.Top,
						FoldBounds.Width(),
						FoldBounds.Height());
				}

				return new Rect(0, 0, 0, 0);
			}
		}
	}
}
