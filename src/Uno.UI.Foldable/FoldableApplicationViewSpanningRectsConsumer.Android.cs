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
using Uno.Logging;
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
		const string TAG = "DualScreen-JWM"; // Jetpack Window Manager
		WindowInfoRepositoryCallbackAdapter windowInfoRepository;
		IWindowMetricsCalculator windowMetricsCalculator;
		float density = 1f;

		// HACK: expose properties for FoldableApplicationViewSpanningRects
		public bool HasFoldFeature { get; set; }
		public bool IsSeparating { get; set; }
		public bool IsFoldVertical { get; set; }
		public Android.Graphics.Rect FoldBounds { get; set; }
		[Obsolete("Can't surface this in a platform agnostic way, not sure we need it when we have FoldOrientation via Window Manager")]
		public SurfaceOrientation Orientation = SurfaceOrientation.Rotation0;
		public FoldingFeatureState FoldState;
		public FoldingFeatureOcclusionType FoldOcclusionType;
		public FoldingFeatureOrientation FoldOrientation;

		private EventHandler<NativeFold> _layoutChanged;
		// ENDHACK
		public event EventHandler<NativeFold> LayoutChanged
		{
			add
			{
				_layoutChanged += value;
			}
			remove
			{
				_layoutChanged -= value;
			}
		}

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

		public void Accept(Java.Lang.Object newLayoutInfo)  // Object will be WindowLayoutInfo
		{
			Android.Util.Log.Info(TAG, "FoldableApplicationViewSpanningRects.Accept");
			Android.Util.Log.Info(TAG, "    newLayoutInfo: " + newLayoutInfo.ToString());
			layoutStateChange(newLayoutInfo as WindowLayoutInfo);
		}

		void layoutStateChange(WindowLayoutInfo newLayoutInfo)
		{
			var wm = windowMetricsCalculator.ComputeCurrentWindowMetrics(ContextHelper.Current as Android.App.Activity);
			Android.Util.Log.Info(TAG, "    Current: " + wm.Bounds.ToString());
			Android.Util.Log.Info(TAG, "    Maximum: " + windowMetricsCalculator.ComputeMaximumWindowMetrics(ContextHelper.Current as Android.App.Activity).Bounds.ToString());

			density = (ContextHelper.Current as Android.App.Activity).Resources.DisplayMetrics.Density;
			Android.Util.Log.Info(TAG, "    Density: " + density);

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
					// DEBUG INFO
					if (foldingFeature.OcclusionType == FoldingFeatureOcclusionType.None)
					{
						Android.Util.Log.Info(TAG, "App is spanned across a fold");
					}
					if (foldingFeature.OcclusionType == FoldingFeatureOcclusionType.Full)
					{
						Android.Util.Log.Info(TAG, "App is spanned across a hinge");
					}
					var summary = "\n    IsSeparating: " + foldingFeature.IsSeparating
							+ "\n    Orientation: " + foldingFeature.Orientation  // FoldingFeatureOrientation.Vertical or Horizontal
							+ "\n    State: " + foldingFeature.State; // FoldingFeatureState.Flat or HalfOpened
					Android.Util.Log.Info(TAG, summary);
					// END DEBUG INFO
				}
				else
				{
					Android.Util.Log.Info(TAG, "DisplayFeature is not a fold or hinge");
				}
			}
			if (lastFoldingFeature is null)
			{
				Android.Util.Log.Info(TAG, "App is not spanned, on a single screen");
			}

			_layoutChanged?.Invoke(this, lastFoldingFeature);
		}
		#endregion
	}
}
