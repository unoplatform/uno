using System;
using System.Collections.Generic;
using CoreAnimation;
using CoreGraphics;
using Uno.Disposables;
using Windows.UI.Xaml.Controls;
#if __IOS__
using UIKit;
using _Color = UIKit.UIColor;
using _View = UIKit.UIView;
using _VisualEffectView = UIKit.UIVisualEffectView;
#else
using AppKit;
using _Color = AppKit.NSColor;
using _View = AppKit.NSView;
using _VisualEffectView = AppKit.NSVisualEffectView;
#endif

namespace Windows.UI.Xaml.Media
{
	public partial class AcrylicBrush
	{
		/// <summary>
		/// Subscribes to AcrylicBrush for a given UI element and applies it.
		/// </summary>
		/// <param name="uiElement">UI element.</param>
		/// <returns>Disposable.</returns>
		internal IDisposable Subscribe(
			UIElement owner,
			CGRect fullArea,
			CGRect insideArea,
			CALayer layer,
			List<CALayer> sublayers,
			ref int insertionIndex,
			CAShapeLayer fillMask)
		{
			var state = new AcrylicState(
				owner,
				fullArea,
				insideArea,
				layer,
				sublayers,
				insertionIndex++, // we always use a single layer for acrylic
				fillMask);

			var compositeDisposable = new CompositeDisposable(7);

			this.RegisterDisposablePropertyChangedCallback(
				AlwaysUseFallbackProperty,
				(_, __) => Apply(state))
					.DisposeWith(compositeDisposable);

			this.RegisterDisposablePropertyChangedCallback(
				FallbackColorProperty,
				(_, __) => Apply(state))
					.DisposeWith(compositeDisposable);

			this.RegisterDisposablePropertyChangedCallback(
				TintColorProperty,
				(_, __) => Apply(state))
					.DisposeWith(compositeDisposable);

			this.RegisterDisposablePropertyChangedCallback(
				TintOpacityProperty,
				(_, __) => Apply(state))
					.DisposeWith(compositeDisposable);

			this.RegisterDisposablePropertyChangedCallback(
				OpacityProperty,
				(_, __) => Apply(state))
					.DisposeWith(compositeDisposable);

#if __MACOS__
			this.RegisterDisposablePropertyChangedCallback(
				BackgroundSourceProperty,
				(_, __) => Apply(state))
					.DisposeWith(compositeDisposable);
#endif

			Apply(state);

			Disposable.Create(() => state.ApplyDisposable.Disposable = null)
				.DisposeWith(compositeDisposable);

			return compositeDisposable;
		}

		/// <summary>
		/// Applies the current state of Acrylic brush to a given UIElement
		/// </summary>
		/// <param name="uiElement">UIElement to set background brush to.</param>
		private void Apply(AcrylicState acrylicState)
		{
			var compositeDisposable = new CompositeDisposable();

			// Reset existing layers
			acrylicState.ApplyDisposable.Disposable = compositeDisposable;

			if (acrylicState.AcrylicContainerLayer == null)
			{
				// Initialize the container layer.
				// This is done only once and the layer is reused if brush
				// properties change.
				acrylicState.AcrylicContainerLayer = new CALayer
				{
					Frame = acrylicState.FullArea,
					Mask = acrylicState.FillMask,
					BackgroundColor = _Color.Clear.CGColor,
					MasksToBounds = true,
				};
				acrylicState.Parent.InsertSublayer(acrylicState.AcrylicContainerLayer, acrylicState.InsertionIndex);
				acrylicState.Sublayers.Add(acrylicState.AcrylicContainerLayer);

				// The layer itself is removed automatically by the BorderLayoutRenderer
			}

			if (AlwaysUseFallback)
			{
				// Apply solid color only
				var previousColor = acrylicState.AcrylicContainerLayer.BackgroundColor;
				acrylicState.AcrylicContainerLayer.BackgroundColor = FallbackColorWithOpacity;

				Disposable.Create(() => acrylicState.AcrylicContainerLayer.BackgroundColor = previousColor)
					.DisposeWith(compositeDisposable);
			}
			else
			{
				acrylicState.AcrylicContainerLayer.BackgroundColor = _Color.Clear.CGColor;

				var acrylicFrame = new CGRect(new CGPoint(acrylicState.InsideArea.X, acrylicState.InsideArea.Y), acrylicState.InsideArea.Size);

				var acrylicLayer = new CALayer
				{
					Frame = acrylicFrame,
					MasksToBounds = true,
					Opacity = (float)TintOpacity,
					BackgroundColor = TintColor
				};

				acrylicState.BlurViews = CreateBlurViews(acrylicFrame);
				InsertViewsAtStart(acrylicState.Owner, acrylicState.BlurViews);

				acrylicState.AcrylicContainerLayer.AddSublayer(acrylicLayer);

				Disposable.Create(() =>
				{
					acrylicState.AcrylicContainerLayer.Sublayers[0].RemoveFromSuperLayer();
					RemoveViews(acrylicState.Owner, acrylicState.BlurViews);
					acrylicState.BlurViews = null;
				}).DisposeWith(compositeDisposable);
			}
		}

		private _View[] CreateBlurViews(CGRect acrylicFrame)
		{
#if __IOS__
			return new _View[]
			{
				new _VisualEffectView()
				{
					ClipsToBounds = true,
					BackgroundColor = _Color.Clear,
					Frame = acrylicFrame,
					Effect = UIBlurEffect.FromStyle(UIBlurEffectStyle.Light)
				}
			};
#else
			var blurView = new _VisualEffectView()
			{
				BlendingMode = BackgroundSource == AcrylicBackgroundSource.HostBackdrop ?
					NSVisualEffectBlendingMode.BehindWindow : NSVisualEffectBlendingMode.WithinWindow,
				Material = NSVisualEffectMaterial.Light,
				State = NSVisualEffectState.Active,
				Frame = acrylicFrame
			};
			var tintView = new NSView()
			{
				WantsLayer = true,
				Frame = acrylicFrame
			};
			tintView.Layer.BackgroundColor = TintColor;
			tintView.Layer.Opacity = (float)TintOpacity;
			return new _View[]
			{
				blurView,
				tintView
			};
#endif
		}

		private void InsertViewsAtStart(_View owner, _View[] subviews)
		{
#if __IOS__
			for (int i = 0; i < subviews.Length; i++)
			{
				var subview = subviews[i];
				owner.InsertSubview(subview, i);
			}
#else
			for (int i = 0; i < subviews.Length; i++)
			{
				var subview = subviews[i];
				if (i == 0)
				{
					// First view goes below everything
					owner.AddSubview(subview, NSWindowOrderingMode.Below, null);
				}
				else
				{
					// Other views go above previous
					owner.AddSubview(subview, NSWindowOrderingMode.Above, subviews[i - 1]);
				}
			}
#endif
		}

		private void RemoveViews(_View owner, _View[] subviews)
		{
			for (int i = 0; i < subviews.Length; i++)
			{
				var subview = subviews[i];
				subview.RemoveFromSuperview();
			}
		}

		/// <summary>
		/// Wraps the acrylic brush metadata for a single UI element.
		/// </summary>
		private class AcrylicState
		{
			public AcrylicState(UIElement owner, CGRect fullArea, CGRect insideArea, CALayer layer, List<CALayer> sublayers, int insertionIndex, CAShapeLayer fillMask)
			{
				Owner = owner;
				FullArea = fullArea;
				InsideArea = insideArea;
				Parent = layer;
				Sublayers = sublayers;
				InsertionIndex = insertionIndex;
				FillMask = fillMask;
			}

			public SerialDisposable ApplyDisposable { get; } = new SerialDisposable();

			public CALayer AcrylicContainerLayer { get; set; }

			public _View[] BlurViews { get; set; }

			public UIElement Owner { get; }

			public CGRect FullArea { get; }

			public CGRect InsideArea { get; }

			public CALayer Parent { get; }

			public List<CALayer> Sublayers { get; }

			public int InsertionIndex { get; }

			public CAShapeLayer FillMask { get; }
		}
	}
}
