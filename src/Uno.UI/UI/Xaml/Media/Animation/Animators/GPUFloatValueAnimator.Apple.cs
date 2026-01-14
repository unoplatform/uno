#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Windows.UI.Core;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.DataBinding;
using Uno.UI.Extensions;

#if __APPLE_UIKIT__
using UIKit;
using _View = UIKit.UIView;
#else
using AppKit;
using _View = AppKit.NSView;
#endif

namespace Microsoft.UI.Xaml.Media.Animation
{
	/// <summary>
	/// Animates a float property using a native <see cref="CoreAnimation"/>.
	/// </summary>
	internal class GPUFloatValueAnimator : IValueAnimator
	{
		private static readonly string __notSupportedProperty = "This transform is not supported by GPU enabled animations.";
		private static readonly List<ManagedWeakReference> _weakActiveInstanceCache = new();
		private static bool _subscribedToVisibilityChanged;

		private float _to;
		private float _from;
		private long _duration;
		private IEnumerable<IBindingItem> _bindingPath;
		private FloatValueAnimator? _valueAnimator;
		private UnoCoreAnimation? _coreAnimation;
		private IEasingFunction? _easingFunction;
		private bool _isDisposed;
		private bool _isPausedInBackground; // flag the animation to be resumed once foregrounded
		private bool _coreStoppedNotFinished; // the animation came to an abrupt end; used by OnCoreWindowVisibilityChanged to confirm paused by backgrounding

		#region PropertyNameConstants
		private const string TranslateTransformX = "TranslateTransform.X";
		private const string TranslateTransformXWithNamespace = "Microsoft.UI.Xaml.Media:TranslateTransform.X";
		private const string TranslateTransformY = "TranslateTransform.Y";
		private const string TranslateTransformYWithNamespace = "Microsoft.UI.Xaml.Media:TranslateTransform.Y";
		private const string RotateTransformAngle = "RotateTransform.Angle";
		private const string RotateTransformAngleWithNamespace = "Microsoft.UI.Xaml.Media:RotateTransform.Angle";
		private const string ScaleTransformX = "ScaleTransform.ScaleX";
		private const string ScaleTransformXWithNamespace = "Microsoft.UI.Xaml.Media:ScaleTransform.ScaleX";
		private const string ScaleTransformY = "ScaleTransform.ScaleY";
		private const string ScaleTransformYWithNamespace = "Microsoft.UI.Xaml.Media:ScaleTransform.ScaleY";
		private const string SkewTransformAngleX = "SkewTransform.AngleX";
		private const string SkewTransformAngleXWithNamespace = "Microsoft.UI.Xaml.Media:SkewTransform.AngleX";
		private const string SkewTransformAngleY = "SkewTransform.AngleY";
		private const string SkewTransformAngleYWithNamespace = "Microsoft.UI.Xaml.Media:SkewTransform.AngleY";
		private const string CompositeTransformCenterX = "CompositeTransform.CenterX";
		private const string CompositeTransformCenterXWithNamespace = "Microsoft.UI.Xaml.Media:CompositeTransform.CenterX";
		private const string CompositeTransformCenterY = "CompositeTransform.CenterY";
		private const string CompositeTransformCenterYWithNamespace = "Microsoft.UI.Xaml.Media:CompositeTransform.CenterY";
		private const string CompositeTransformTranslateX = "CompositeTransform.TranslateX";
		private const string CompositeTransformTranslateXWithNamespace = "Microsoft.UI.Xaml.Media:CompositeTransform.TranslateX";
		private const string CompositeTransformTranslateY = "CompositeTransform.TranslateY";
		private const string CompositeTransformTranslateYWithNamespace = "Microsoft.UI.Xaml.Media:CompositeTransform.TranslateY";
		private const string CompositeTransformRotation = "CompositeTransform.Rotation";
		private const string CompositeTransformRotationWithNamespace = "Microsoft.UI.Xaml.Media:CompositeTransform.Rotation";
		private const string CompositeTransformScaleX = "CompositeTransform.ScaleX";
		private const string CompositeTransformScaleXWithNamespace = "Microsoft.UI.Xaml.Media:CompositeTransform.ScaleX";
		private const string CompositeTransformScaleY = "CompositeTransform.ScaleY";
		private const string CompositeTransformScaleYWithNamespace = "Microsoft.UI.Xaml.Media:CompositeTransform.ScaleY";
		private const string CompositeTransformSkewX = "CompositeTransform.SkewX";
		private const string CompositeTransformSkewXWithNamespace = "Microsoft.UI.Xaml.Media:CompositeTransform.SkewX";
		private const string CompositeTransformSkewY = "CompositeTransform.SkewY";
		private const string CompositeTransformSkewYWithNamespace = "Microsoft.UI.Xaml.Media:CompositeTransform.SkewY";
		#endregion

		internal static Point GetAnchorForAnimation(Transform transform, Point relativeOrigin, Size viewSize)
		{
			switch (transform)
			{
				case RotateTransform rotate:
					return GetAnimationAnchor(relativeOrigin, viewSize, rotate.CenterX, rotate.CenterY);

				case ScaleTransform scale:
					return GetAnimationAnchor(relativeOrigin, viewSize, scale.CenterX, scale.CenterY);

				case CompositeTransform composite:
					return GetAnimationAnchor(relativeOrigin, viewSize, composite.CenterX, composite.CenterY);

				default:
					return relativeOrigin;
			}
		}

		private static Point GetAnimationAnchor(Point origin, Size size, double centerX, double centerY)
			=> new Point(
				size.Width == 0
					? origin.X
					: centerX / size.Width + origin.X,
				size.Height == 0
					? origin.Y
					: centerY / size.Height + origin.Y);

		public GPUFloatValueAnimator(float from, float to, IEnumerable<IBindingItem> bindingPath)
		{
			if (bindingPath is null)
			{
				throw new ArgumentNullException("The bindingpath cannot be null");
			}

			_to = to;
			_from = from;
			_bindingPath = bindingPath;

			_valueAnimator = new FloatValueAnimator(from, to);
			_valueAnimator.Update += OnInnerAnimatorUpdate;

			_coreAnimation = null;
		}

		private void OnInnerAnimatorUpdate(object? sender, EventArgs e)
		{
			Update?.Invoke(this, e);
		}

		public void Start()
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(GetType().Name);
			}

			InitializeCoreAnimation();
			TrackCurrentInstance();

			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().DebugFormat("Starting GPU Float value animator on property {0}.", _bindingPath.LastOrDefault()?.PropertyName);
			}

			ResetBackgroundPauseTrackingStates();
			_valueAnimator?.Start();
			_coreAnimation?.Start();
		}

		public void Pause()
		{
			var pausedTime = _valueAnimator?.CurrentPlayTime;
			var pausedValue = (float?)_valueAnimator?.AnimatedValue;

			_valueAnimator?.Pause();
			_coreAnimation?.Pause(pausedTime, pausedValue);

			AnimationPause?.Invoke(this, EventArgs.Empty);
		}

		public void Resume()
		{
			ResetBackgroundPauseTrackingStates();
			_valueAnimator?.Resume();
			_coreAnimation?.Resume();
		}

		public void Cancel()
		{
			ResetBackgroundPauseTrackingStates();
			_valueAnimator?.Cancel();
			_coreAnimation?.Cancel();
			AnimationCancel?.Invoke(this, EventArgs.Empty);

			ReleaseCoreAnimation();
			UntrackCurrentInstance();
		}

		private void InitializeCoreAnimation()
		{
			var animatedItem = (_bindingPath.LastOrDefault()) ?? throw new InvalidOperationException("The binding path is empty.");
			switch (animatedItem.DataContext)
			{
				case _View view when animatedItem.PropertyName.EndsWith("Opacity", StringComparison.Ordinal):
					_coreAnimation = InitializeOpacityCoreAnimation(view);
					return;

				case TranslateTransform translate:
					_coreAnimation = InitializeTranslateCoreAnimation(translate, animatedItem);
					return;

				case RotateTransform rotate:
					_coreAnimation = InitializeRotateCoreAnimation(rotate, animatedItem);
					return;

				case ScaleTransform scale:
					_coreAnimation = InitializeScaleCoreAnimation(scale, animatedItem);
					return;

				case SkewTransform skew:
					_coreAnimation = InitializeSkewCoreAnimation(skew, animatedItem);
					return;

				case CompositeTransform composite:
					_coreAnimation = InitializeCompositeCoreAnimation(composite, animatedItem);
					return;

				// case TransformGroup group:
				//  ==> No needs to validate the TransformGroup: there is no animatable property on it.
				//		If a animation is declared on it (e.g. "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"),
				//		the _bindingPath should resolve the target child Transform, and animatedItem.DataContext should be the ScaleTransform.


				default:
					throw new NotSupportedException(__notSupportedProperty);
			}
		}

		private void TrackCurrentInstance()
		{
			lock (_weakActiveInstanceCache)
			{
				_weakActiveInstanceCache.Add(WeakReferencePool.RentWeakReference(this, this));
			}
			if (!_subscribedToVisibilityChanged &&
				CoreWindow.GetForCurrentThread() is { } coreWindow)
			{
				coreWindow.VisibilityChanged += OnCoreWindowVisibilityChanged;
				_subscribedToVisibilityChanged = true;
			}
		}

		private void UntrackCurrentInstance()
		{
			lock (_weakActiveInstanceCache)
			{
				for (int i = _weakActiveInstanceCache.Count - 1; i >= 0; i--)
				{
					if (_weakActiveInstanceCache[i].TryGetTarget<GPUFloatValueAnimator>(out var pInstance) &&
						pInstance == this)
					{
						_weakActiveInstanceCache.RemoveAt(i);
						WeakReferencePool.ReturnWeakReference(this, _weakActiveInstanceCache[i]);
					}
				}
			}
		}

		public bool IsRunning => _valueAnimator?.IsRunning ?? false;

		public long StartDelay
		{
			get => _valueAnimator?.StartDelay ?? 0L;
			set
			{
				if (_valueAnimator is not null)
				{
					_valueAnimator.StartDelay = value;
				}
			}
		}

		public object? AnimatedValue => _valueAnimator?.AnimatedValue;

		public long CurrentPlayTime
		{
			get => _valueAnimator?.CurrentPlayTime ?? 0L;
			set
			{
				if (_valueAnimator is not null)
				{
					_valueAnimator.CurrentPlayTime = value;
				}
			}
		}

		public long Duration => _duration;

		public event EventHandler? Update;

		public event EventHandler? AnimationPause;

		public event EventHandler? AnimationEnd;

		public event EventHandler? AnimationCancel;

		public event EventHandler? AnimationFailed;

		public void SetDuration(long duration)
		{
			_duration = duration;
			_valueAnimator?.SetDuration(duration);
		}

		public void SetEasingFunction(IEasingFunction easingFunction)
		{
			_easingFunction = easingFunction;
			_valueAnimator?.SetEasingFunction(easingFunction);
		}

		#region coreAnimationInitializers
		private UnoCoreAnimation InitializeOpacityCoreAnimation(_View view)
		{
			return CreateCoreAnimation(view, "opacity", value => new NSNumber(value));
		}
		private UnoCoreAnimation? InitializeTranslateCoreAnimation(TranslateTransform transform, IBindingItem animatedItem)
		{
			if (animatedItem.PropertyName.Equals("X")
				|| animatedItem.PropertyName.Equals(TranslateTransformX)
				|| animatedItem.PropertyName.Equals(TranslateTransformXWithNamespace))
			{
				return CreateCoreAnimation(transform, "transform.translation.x", value => new NSNumber(value));
			}
			else if (animatedItem.PropertyName.Equals("Y")
				|| animatedItem.PropertyName.Equals(TranslateTransformY)
				|| animatedItem.PropertyName.Equals(TranslateTransformYWithNamespace))
			{
				return CreateCoreAnimation(transform, "transform.translation.y", value => new NSNumber(value));
			}
			else
			{
				throw new NotSupportedException(__notSupportedProperty);
			}
		}

		private UnoCoreAnimation? InitializeRotateCoreAnimation(RotateTransform transform, IBindingItem animatedItem)
		{
			if (animatedItem.PropertyName.Equals("Angle")
				|| animatedItem.PropertyName.Equals(RotateTransformAngle)
				|| animatedItem.PropertyName.Equals(RotateTransformAngleWithNamespace))
			{
				return CreateCoreAnimation(transform, "transform.rotation", value => new NSNumber(MathEx.ToRadians(value)));
			}
			else
			{
				throw new NotSupportedException(__notSupportedProperty);
			}
		}

		private UnoCoreAnimation? InitializeScaleCoreAnimation(ScaleTransform transform, IBindingItem animatedItem)
		{
			if (animatedItem.PropertyName.Equals("ScaleX")
				|| animatedItem.PropertyName.Equals(ScaleTransformX)
				|| animatedItem.PropertyName.Equals(ScaleTransformXWithNamespace))
			{
				return CreateCoreAnimation(transform, "transform.scale.x", value => new NSNumber(value));
			}
			else if (animatedItem.PropertyName.Equals("ScaleY")
				|| animatedItem.PropertyName.Equals(ScaleTransformY)
				|| animatedItem.PropertyName.Equals(ScaleTransformYWithNamespace))
			{
				return CreateCoreAnimation(transform, "transform.scale.y", value => new NSNumber(value));
			}
			else
			{
				throw new NotSupportedException(__notSupportedProperty);
			}
		}

		private UnoCoreAnimation? InitializeSkewCoreAnimation(SkewTransform transform, IBindingItem animatedItem)
		{
			// We need to review this.  This won't play along if other transforms are happening at the same time since we are animating the whole transform
			if (transform.View is not _View view)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Warning))
				{
					this.Log().Warn("The View property of the SkewTransform is null.");
				}

				return null;
			}

			if (animatedItem.PropertyName.Equals("AngleX")
				|| animatedItem.PropertyName.Equals(SkewTransformAngleX)
				|| animatedItem.PropertyName.Equals(SkewTransformAngleXWithNamespace))
			{
				return CreateCoreAnimation(view, "transform", value => ToCASkewTransform(value, 0));
			}
			else if (animatedItem.PropertyName.Equals("AngleY")
				|| animatedItem.PropertyName.Equals(SkewTransformAngleY)
				|| animatedItem.PropertyName.Equals(SkewTransformAngleYWithNamespace))
			{
				return CreateCoreAnimation(view, "transform", value => ToCASkewTransform(0, value));
			}
			else
			{
				throw new NotSupportedException(__notSupportedProperty);
			}
		}

		private UnoCoreAnimation? InitializeCompositeCoreAnimation(CompositeTransform transform, IBindingItem animatedItem)
		{
			switch (animatedItem.PropertyName)
			{
				case CompositeTransformCenterX:
				case CompositeTransformCenterXWithNamespace:
				case "CenterX"://This animation is a Lie. transform.position.x doesn't exist, the real animator is the CPU bound one
					return CreateCoreAnimation(transform, "transform.position.x", value => new NSNumber(value));
				case CompositeTransformCenterY:
				case CompositeTransformCenterYWithNamespace:
				case "CenterY"://This animation is a Lie. transform.position.x doesn't exist, the real animator is the CPU bound one
					return CreateCoreAnimation(transform, "transform.position.y", value => new NSNumber(value));
				case CompositeTransformTranslateX:
				case CompositeTransformTranslateXWithNamespace:
				case "TranslateX":
					return CreateCoreAnimation(transform, "transform.translation.x", value => new NSNumber(value));
				case CompositeTransformTranslateY:
				case CompositeTransformTranslateYWithNamespace:
				case "TranslateY":
					return CreateCoreAnimation(transform, "transform.translation.y", value => new NSNumber(value));
				case CompositeTransformRotation:
				case CompositeTransformRotationWithNamespace:
				case "Rotation":
					return CreateCoreAnimation(transform, "transform.rotation", value => new NSNumber(MathEx.ToRadians(value)));
				case CompositeTransformScaleX:
				case CompositeTransformScaleXWithNamespace:
				case "ScaleX":
					return CreateCoreAnimation(transform, "transform.scale.x", value => new NSNumber(value));
				case CompositeTransformScaleY:
				case CompositeTransformScaleYWithNamespace:
				case "ScaleY":
					return CreateCoreAnimation(transform, "transform.scale.y", value => new NSNumber(value));

				//Again, we need to review how we handle SkewTransforms
				case CompositeTransformSkewX:
				case CompositeTransformSkewXWithNamespace:
				case "SkewX":
					return CreateTransformCoreAnimation(transform, value => ToCASkewTransform(value, 0));
				case CompositeTransformSkewY:
				case CompositeTransformSkewYWithNamespace:
				case "SkewY":
					return CreateTransformCoreAnimation(transform, value => ToCASkewTransform(0, value));
				default:
					throw new NotSupportedException(__notSupportedProperty);
			}
		}
		#endregion

		private UnoCoreAnimation? CreateTransformCoreAnimation(
			Transform transform,
			Func<float, NSValue> nsValueConversion)
		{
			if (transform.View is null)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Warning))
				{
					this.Log().Warn("The View property of the Transform is null.");
				}

				return null;
			}

			return CreateCoreAnimation(transform.View, "transform", nsValueConversion);
		}

		private UnoCoreAnimation? CreateCoreAnimation(
			Transform transform,
			string property,
			Func<float, NSValue> nsValueConversion)
		{
			if (transform.View is null)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Warning))
				{
					this.Log().Warn("The View property of the Transform is null.");
				}

				return null;
			}

			return CreateCoreAnimation(transform.View, property, nsValueConversion, transform.StartAnimation, transform.EndAnimation);
		}

		private UnoCoreAnimation CreateCoreAnimation(
			_View view,
			string property,
			Func<float, NSValue> nsValueConversion,
			Action? prepareAnimation = null,
			Action? endAnimation = null)
		{
			if (view.Layer is null)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Warning))
				{
					this.Log().Warn("The Layer property of the View is null.");
				}

				throw new InvalidOperationException("The Layer property of the View is null.");
			}

			var timingFunction = _easingFunction == null ?
				CAMediaTimingFunction.FromName(CAMediaTimingFunction.Linear) :
				_easingFunction.GetTimingFunction();

			var isDiscrete = _easingFunction is DiscreteDoubleKeyFrame.DiscreteDoubleKeyFrameEasingFunction;

			return prepareAnimation == null || endAnimation == null
				? new UnoCoreAnimation(view.Layer, property, _from, _to, StartDelay, _duration, timingFunction, nsValueConversion, FinalizeAnimation, isDiscrete)
				: new UnoCoreAnimation(view.Layer, property, _from, _to, StartDelay, _duration, timingFunction, nsValueConversion, FinalizeAnimation, isDiscrete, prepareAnimation, endAnimation);
		}

		private NSValue ToCASkewTransform(float angleX, float angleY)
		{
			var matrix = CGAffineTransform.MakeIdentity();
			var b = (float)Math.Tan(MathEx.ToRadians(angleY));
			var c = (float)Math.Tan(MathEx.ToRadians(angleX));

			matrix.B = b;
			matrix.C = c;

			return NSValue.FromCATransform3D(CATransform3D.MakeFromAffine(matrix));
		}

		private static void OnCoreWindowVisibilityChanged(CoreWindow sender, VisibilityChangedEventArgs args)
		{
			lock (_weakActiveInstanceCache)
			{
				for (int i = _weakActiveInstanceCache.Count - 1; i >= 0; i--)
				{
					if (_weakActiveInstanceCache[i].TryGetTarget<GPUFloatValueAnimator>(out var instance))
					{
						instance.OnCoreWindowVisibilityChangedImpl(args);
					}
					else // purge collected instance
					{
						var weak = _weakActiveInstanceCache[i];
						var owner = weak.Owner;
						_weakActiveInstanceCache.RemoveAt(i);
						WeakReferencePool.ReturnWeakReference(owner, weak);
					}
				}
			}
		}

		private void OnCoreWindowVisibilityChangedImpl(VisibilityChangedEventArgs args)
		{
			// note: There is no guarantee on which is called first between this (didEnterBackgroundNotification)
			// and FinalizeAnimation (animationDidStop).
			if (!args.Visible)
			{
				if (IsRunning || _coreStoppedNotFinished)
				{
					Pause();
					_isPausedInBackground = true;
				}
			}
			else
			{
				if (_isPausedInBackground)
				{
					if (_coreAnimation is { })
					{
						Resume();
					}
					else
					{
						Start();
					}
				}
			}
		}

		private void FinalizeAnimation(UnoCoreAnimation.CompletedInfo completedInfo)
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().DebugFormat("Finalizing animation for GPU Float value animator on property {0}.", _bindingPath.LastOrDefault()?.PropertyName);
			}

			var wasRunning = _valueAnimator?.IsRunning ?? false;
			if (wasRunning)
			{
				_valueAnimator?.Cancel();
			}

			switch (completedInfo)
			{
				case UnoCoreAnimation.CompletedInfo.Success:
					AnimationEnd?.Invoke(this, EventArgs.Empty);
					break;

				case UnoCoreAnimation.CompletedInfo.Error:
					// ref: https://developer.apple.com/documentation/quartzcore/caanimationdelegate/2097259-animationdidstop
					// This callback from animationDidStop:finished with the finished=false (which is translated to Error here)
					// means that the animation have ended because "it has been removed from the layer it is attached to."
					// It could mean it was explicitly CoreAnimation::StopAnimation'd by us, or killed by the os as the app is background'd.
					// Since we don't know which case it is, we are just setting another flag to be later confirmed in OnCoreWindowVisibilityChanged.
					AnimationFailed?.Invoke(this, EventArgs.Empty);
					if (wasRunning)
					{
						_coreStoppedNotFinished = true;
					}
					break;

				default: throw new NotSupportedException($"{completedInfo} is not supported");
			}

			ReleaseCoreAnimation();
			UntrackCurrentInstance();
		}

		private void ReleaseCoreAnimation()
		{
			_coreAnimation?.Dispose();
			_coreAnimation = null;
		}

		private void ResetBackgroundPauseTrackingStates()
		{
			_coreStoppedNotFinished = false;
			_isPausedInBackground = false;
		}

		public void Dispose()
		{
			if (!_isDisposed)
			{
				_bindingPath = null!;

				_valueAnimator?.Dispose();
				_valueAnimator = null;

				_coreAnimation?.Dispose();
				_coreAnimation = null;

				this.Update = null;
				this.AnimationEnd = null;
				this.AnimationCancel = null;
			}

			_isDisposed = true;
		}
	}
}
