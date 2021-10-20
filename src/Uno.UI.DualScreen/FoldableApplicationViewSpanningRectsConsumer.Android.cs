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

namespace Uno.UI.DualScreen
{
	/// <summary>
	/// Provides two Rect that represent the two screen dimensions when
	/// an Android application is spanned across a hinge or fold (eg. Surface Duo)
	/// </summary>
	/// <remarks>
	/// Relies on the MainActivity implementing Jetpack Window Manager layout change listener,
	/// and exposing the properties needed to make UI change when required.
	/// HACK: need to implement an event for layout changes, so we can detect folding state
	/// </remarks>
	public partial class FoldableApplicationViewSpanningRects : Java.Lang.Object, AndroidX.Core.Util.IConsumer
	{
		const string TAG = "JWM"; // Jetpack Window Manager
		WindowInfoRepositoryCallbackAdapter windowInfoRepository;
		IWindowMetricsCalculator windowMetricsCalculator;

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
			Android.Util.Log.Info(TAG, "===LayoutStateChangeCallback.Accept");
			Android.Util.Log.Info(TAG, newLayoutInfo.ToString());
			layoutStateChange(newLayoutInfo as WindowLayoutInfo);
		}

		void layoutStateChange(WindowLayoutInfo newLayoutInfo)
		{
			Android.Util.Log.Info(TAG, "Current: " + windowMetricsCalculator.ComputeCurrentWindowMetrics(ContextHelper.Current as Android.App.Activity).Bounds.ToString());
			Android.Util.Log.Info(TAG, "Maximum: " + windowMetricsCalculator.ComputeMaximumWindowMetrics(ContextHelper.Current as Android.App.Activity).Bounds.ToString());

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
					FoldBounds = foldingFeature.Bounds;
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
					var summary = "\nIsSeparating: " + foldingFeature.IsSeparating
							+ "\nOrientation: " + foldingFeature.Orientation  // FoldingFeatureOrientation.Vertical or Horizontal
							+ "\nState: " + foldingFeature.State; // FoldingFeatureState.Flat or HalfOpened
					Android.Util.Log.Info(TAG, summary);
				}
				else
				{
					Android.Util.Log.Info(TAG, "DisplayFeature is not a fold or hinge");
				}
			}

			_layoutChanged?.Invoke(this, lastFoldingFeature);
		}
		#endregion
	}
}
