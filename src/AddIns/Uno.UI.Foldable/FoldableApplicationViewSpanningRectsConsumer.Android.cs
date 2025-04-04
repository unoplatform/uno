using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.Window.Java.Layout;
using AndroidX.Window.Layout;
using Java.Lang;
using Java.Util.Concurrent;
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
	/// See the _other partial class_ file for wiring up Jetpack Window Manager (Xamarin.AndroidX.Window.WindowJava)
	/// via the ApplicationViewHelper.GetBaseActivityEvents() helper and ContextHelper.Current
	/// </remarks>
	public partial class FoldableApplicationViewSpanningRects : Java.Lang.Object, AndroidX.Core.Util.IConsumer
	{
		WindowInfoTrackerCallbackAdapter windowInfoTrackerCallbackAdapter;
		IWindowMetricsCalculator windowMetricsCalculator;

		/// <summary>Rectangle that describes the coordinates of the hinge or fold on a dual-screen device</summary>
		public Android.Graphics.Rect FoldBounds { get; set; }
		/// <summary>Deprecated - hinges and folds can be across the vertical or horizontal axis, so orientation is not relevant for determining spanning rectangles</summary>
		[Obsolete("Can't surface this in a device agnostic way, not sure we need it when we have FoldOrientation via Window Manager. Has not been deleted since it was part of the original public API")]
		public SurfaceOrientation Orientation = SurfaceOrientation.Rotation0;

		#region Future public properties to expand dual-screen functionality - keep private until needed
		private bool HasFoldFeature { get; set; }
		private bool IsSeparating { get; set; }
		private bool IsFoldVertical { get; set; }
		private FoldingFeatureState FoldState;
		private FoldingFeatureOcclusionType FoldOcclusionType;
		private FoldingFeatureOrientation FoldOrientation;

		//private EventHandler<NativeFold> _layoutChanged;
		//// ENDHACK
		//public event EventHandler<NativeFold> LayoutChanged
		//{
		//	add
		//	{
		//		_layoutChanged += value;
		//	}
		//	remove
		//	{
		//		_layoutChanged -= value;
		//	}
		//}
		#endregion

		#region Used by WindowInfoRepository callback
		IExecutor runOnUiThreadExecutor()
		{
			return new MyExecutor();
		}
		class MyExecutor : Java.Lang.Object, IExecutor
		{
			Handler handler = new Handler(Looper.MainLooper);
			public void Execute(IRunnable r)
			{
				handler.Post(r);
			}
		}

		/// <summary>
		/// Not for public use - this method is for the IConsumer interface implementation and should not be called by application code
		/// </summary>
		public void Accept(Java.Lang.Object newLayoutInfo)  // Object will be WindowLayoutInfo
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"DualMode: FoldableApplicationViewSpanningRects.Accept\n     newLayoutInfo: {newLayoutInfo}");
			}
			layoutStateChange(newLayoutInfo as WindowLayoutInfo);
		}

		void layoutStateChange(WindowLayoutInfo newLayoutInfo)
		{
			var wm = windowMetricsCalculator.ComputeCurrentWindowMetrics(ContextHelper.Current as Android.App.Activity);
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"DualMode:    CurrentWindowBounds: " + wm.Bounds.ToString());
				this.Log().Debug($"DualMode:    MaximumWindowBounds: " + windowMetricsCalculator.ComputeMaximumWindowMetrics(ContextHelper.Current as Android.App.Activity).Bounds.ToString());

			}

			FoldBounds = null;
			IsSeparating = false;
			HasFoldFeature = false;

			NativeFold lastFoldingFeature = null;

			foreach (var displayFeature in newLayoutInfo.DisplayFeatures)
			{
				var foldingFeature = displayFeature.JavaCast<IFoldingFeature>();

				if (foldingFeature != null) // HACK: requires JavaCast as shown above
				{
					// Set properties for FoldableApplicationViewSpanningRects to reference
					HasFoldFeature = true;
					IsSeparating = foldingFeature.IsSeparating;
					FoldBounds = foldingFeature.Bounds; // physical pixel values
					FoldState = foldingFeature.State;
					FoldOcclusionType = foldingFeature.OcclusionType;

					if (foldingFeature.Orientation == FoldingFeatureOrientation.Horizontal)
					{
						//Orientation = SurfaceOrientation.Rotation90;
						IsFoldVertical = false;
						FoldOrientation = FoldingFeatureOrientation.Horizontal;
					}
					else
					{
						//Orientation = SurfaceOrientation.Rotation0; // HACK: what about 180 and 270?
						IsFoldVertical = true;
						FoldOrientation = FoldingFeatureOrientation.Vertical;
					}

					lastFoldingFeature = new NativeFold
					{
						Bounds = FoldBounds,
						IsOccluding = foldingFeature.OcclusionType == FoldingFeatureOcclusionType.Full,
						IsFlat = foldingFeature.State == FoldingFeatureState.Flat,
						IsVertical = IsFoldVertical
					};

					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						var summary = "\n    IsSeparating: " + foldingFeature.IsSeparating
							+ "\n    OcclusionType: " + foldingFeature.OcclusionType  // FoldingFeatureOcclusionType.None or Full
							+ "\n    Orientation: " + foldingFeature.Orientation  // FoldingFeatureOrientation.Vertical or Horizontal
							+ "\n    State: " + foldingFeature.State; // FoldingFeatureState.Flat or HalfOpened
						this.Log().Debug($"DualMode: {summary}");
					}
				}
				else
				{
					this.Log().Debug($"DualMode: DisplayFeature is not a fold or hinge");
				}
			}
			if (lastFoldingFeature is null)
			{
				this.Log().Debug($"DualMode: App is not spanned, on a single screen");
			}

			// FUTURE USE
			//_layoutChanged?.Invoke(this, lastFoldingFeature);
		}
		#endregion
	}
}
