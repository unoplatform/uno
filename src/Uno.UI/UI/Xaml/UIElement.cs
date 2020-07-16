#if NET461 || __WASM__
#pragma warning disable CS0067
#endif

using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using System.Collections.Generic;
using Uno.Extensions;
using Uno.Logging;
using Uno.Disposables;
using System.Linq;
using Windows.Devices.Input;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Uno.UI;
using Uno;
using Uno.UI.Controls;
using Uno.UI.Media;
using System;
using System.Numerics;
using System.Reflection;
using Windows.UI.Xaml.Markup;
using Microsoft.Extensions.Logging;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Core;

#if __IOS__
using UIKit;
#endif

namespace Windows.UI.Xaml
{
	public partial class UIElement : DependencyObject, IXUidProvider
	{
		private readonly SerialDisposable _clipSubscription = new SerialDisposable();
		private string _uid;

		private void Initialize()
		{
			this.SetValue(KeyboardAcceleratorsProperty, new List<KeyboardAccelerator>(0), DependencyPropertyValuePrecedences.DefaultValue);
		}

		string IXUidProvider.Uid
		{
			get => _uid;
			set
			{
				_uid = value;
				OnUidChangedPartial();
			}
		}

		partial void OnUidChangedPartial();

		private protected bool RequiresClipping { get; set; } = true;

		#region Clip DependencyProperty

		public RectangleGeometry Clip
		{
			get { return (RectangleGeometry)this.GetValue(ClipProperty); }
			set { this.SetValue(ClipProperty, value); }
		}

		public static DependencyProperty ClipProperty { get ; } =
			DependencyProperty.Register(
				"Clip",
				typeof(RectangleGeometry),
				typeof(UIElement),
				new FrameworkPropertyMetadata(
					null,
					(s, e) => ((UIElement)s)?.OnClipChanged(e)
				)
			);

		private void OnClipChanged(DependencyPropertyChangedEventArgs e)
		{
			var geometry = e.NewValue as RectangleGeometry;

			ApplyClip();
			_clipSubscription.Disposable = geometry.RegisterDisposableNestedPropertyChangedCallback(
				(_, __) => ApplyClip(),
				new[] { RectangleGeometry.RectProperty },
				new[] { Geometry.TransformProperty },
				new[] { Geometry.TransformProperty, TranslateTransform.XProperty },
				new[] { Geometry.TransformProperty, TranslateTransform.YProperty }
			);
		}

		#endregion

		#region RenderTransform Dependency Property

		/// <summary>
		/// This is a Transformation for a UIElement.  It binds the Render Transform to the View
		/// </summary>
		public Transform RenderTransform
		{
			get => (Transform)this.GetValue(RenderTransformProperty);
			set => this.SetValue(RenderTransformProperty, value);
		}

		/// <summary>
		/// Backing dependency property for <see cref="RenderTransform"/>
		/// </summary>
		public static DependencyProperty RenderTransformProperty { get ; } =
			DependencyProperty.Register("RenderTransform", typeof(Transform), typeof(UIElement), new FrameworkPropertyMetadata(null, (s, e) => OnRenderTransformChanged(s, e)));

		private static void OnRenderTransformChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var view = (UIElement)dependencyObject;

			view._renderTransform?.Dispose();

			if (args.NewValue is Transform transform)
			{
				view._renderTransform = new NativeRenderTransformAdapter(view, transform, view.RenderTransformOrigin);
				view.OnRenderTransformSet();
			}
			else
			{
				// Sanity
				view._renderTransform = null;
			}
		}

		internal NativeRenderTransformAdapter _renderTransform;

		partial void OnRenderTransformSet();
		#endregion

		#region RenderTransformOrigin Dependency Property

		/// <summary>
		/// This is a Transformation for a UIElement.  It binds the Render Transform to the View
		/// </summary>
		public Point RenderTransformOrigin
		{
			get => (Point)this.GetValue(RenderTransformOriginProperty);
			set => this.SetValue(RenderTransformOriginProperty, value);
		}

		// Using a DependencyProperty as the backing store for RenderTransformOrigin.  This enables animation, styling, binding, etc...
		public static DependencyProperty RenderTransformOriginProperty { get ; } =
			DependencyProperty.Register("RenderTransformOrigin", typeof(Point), typeof(UIElement), new FrameworkPropertyMetadata(default(Point), (s, e) => OnRenderTransformOriginChanged(s, e)));

		private static void OnRenderTransformOriginChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var view = (UIElement)dependencyObject;
			var point = (Point)args.NewValue;

			view._renderTransform?.UpdateOrigin(point);
		}
		#endregion

		public GeneralTransform TransformToVisual(UIElement visual)
			=> new MatrixTransform { Matrix = new Matrix(GetTransform(from: this, to: visual)) };

		internal static Matrix3x2 GetTransform(UIElement from, UIElement to)
		{
			if (from == to)
			{
				return Matrix3x2.Identity;
			}

			var matrix = Matrix3x2.Identity;
			double offsetX = 0.0, offsetY = 0.0;
			var elt = from;
			do
			{
				var layoutSlot = elt.LayoutSlotWithMarginsAndAlignments;
				var transform = elt.RenderTransform;
				if (transform == null)
				{
					// As this is the common case, avoid Matrix computation when a basic addition is sufficient
					offsetX += layoutSlot.X;
					offsetY += layoutSlot.Y;
				}
				else
				{
					var origin = elt.RenderTransformOrigin;
					var transformMatrix = origin == default
						? transform.MatrixCore
						: transform.ToMatrix(origin, layoutSlot.Size);

					// First apply any pending arrange offset that would have been impacted by this RenderTransform (eg. scaled)
					// Friendly reminder: Matrix multiplication is usually not commutative ;)
					matrix *= Matrix3x2.CreateTranslation((float)offsetX, (float)offsetY);
					matrix *= transformMatrix;

					offsetX = layoutSlot.X;
					offsetY = layoutSlot.Y;
				}

				if (elt is ScrollViewer sv)
				{
					var zoom = sv.ZoomFactor;
					if (zoom != 1)
					{
						matrix *= Matrix3x2.CreateTranslation((float)offsetX, (float)offsetY);
						matrix *= Matrix3x2.CreateScale(zoom);

						offsetX = -sv.HorizontalOffset;
						offsetY = -sv.VerticalOffset;
					}
					else
					{
						offsetX -= sv.HorizontalOffset;
						offsetY -= sv.VerticalOffset;
					}
				}
			} while (elt.TryGetParentUIElementForTransformToVisual(out elt, ref offsetX, ref offsetY) && elt != to); // If possible we stop as soon as we reach 'to'

			matrix *= Matrix3x2.CreateTranslation((float)offsetX, (float)offsetY);

			if (to != null && elt != to)
			{
				// Unfortunately we didn't find the 'to' in the parent hierarchy,
				// so matrix == fromToRoot and we now have to compute the transform 'toToRoot'.
				var toToRoot = GetTransform(to, null);
				Matrix3x2.Invert(toToRoot, out var rootToTo);

				matrix *= rootToTo;
			}

			return matrix;
		}

#if !__IOS__ && !__ANDROID__ // This is the default implementation, but it can be customized per platform
		/// <summary>
		/// Note: Offsets are only an approximation which does not take in consideration possible transformations
		///	applied by a 'UIView' between this element and its parent UIElement.
		/// </summary>
		private bool TryGetParentUIElementForTransformToVisual(out UIElement parentElement, ref double offsetX, ref double offsetY)
		{
			var parent = this.GetParent();
			switch (parent)
			{
				case UIElement elt:
					parentElement = elt;
					return true;

				case null:
					parentElement = null;
					return false;

				default:
					Application.Current.RaiseRecoverableUnhandledException(new InvalidOperationException("Found a parent which is NOT a UIElement."));

					parentElement = null;
					return false;
			}
		}
#endif




		protected virtual void OnIsHitTestVisibleChanged(bool oldValue, bool newValue)
		{
			OnIsHitTestVisibleChangedPartial(oldValue, newValue);
		}

		partial void OnIsHitTestVisibleChangedPartial(bool oldValue, bool newValue);

		partial void OnOpacityChanged(DependencyPropertyChangedEventArgs args);

		private protected virtual void OnContextFlyoutChanged(FlyoutBase oldValue, FlyoutBase newValue)
		{
			if(newValue != null)
			{
				RightTapped += OpenContextFlyout;
			}
			else
			{
				RightTapped -= OpenContextFlyout;
			}
		}

		private void OpenContextFlyout(object sender, RightTappedRoutedEventArgs args)
		{
			if (this is FrameworkElement fe)
			{
				ContextFlyout?.ShowAt(
					placementTarget: fe,
					showOptions: new FlyoutShowOptions()
					{
						Position = args.GetPosition(this)
					}
				);
			}
		}

		internal bool IsRenderingSuspended { get; set; }

		internal void ApplyClip()
		{
			Rect rect;

			if (Clip == null)
			{
				rect = Rect.Empty;
			}
			else
			{
				rect = Clip.Rect;

				// Currently only TranslateTransform is supported on a clipping mask
				// (because the calculated mask is a Rect right now...)
				if (Clip?.Transform is TranslateTransform translateTransform)
				{
					rect.X += translateTransform.X;
					rect.Y += translateTransform.Y;
				}
			}

			if (NeedsClipToSlot)
			{
#if __WASM__
				var boundsClipping = new Rect(0, 0, RenderSize.Width, RenderSize.Height);
#else
				var boundsClipping = ClippedFrame ?? Rect.Empty;
#endif
				if (rect.IsEmpty)
				{
					rect = boundsClipping;
				}
				else
				{
					rect.Intersect(boundsClipping);
				}
			}

			ApplyNativeClip(rect);
		}

		partial void ApplyNativeClip(Rect rect);

		internal static object GetDependencyPropertyValueInternal(DependencyObject owner, string dependencyPropertyName)
		{
			var dp = DependencyProperty.GetProperty(owner.GetType(), dependencyPropertyName);
			return dp == null ? null : owner.GetValue(dp);
		}

		/// <summary>
		/// Sets the specified dependency property value using the format "name|value"
		/// </summary>
		/// <param name="dependencyPropertyNameAndValue">The name and value of the property</param>
		/// <returns>The currenty set value at the Local precedence</returns>
		/// <remarks>
		/// The signature of this method was chosen to work around a limitation of Xamarin.UITest with regards to
		/// parameters passing on iOS, where the number of parameters follows a unconventional set of rules. Using
		/// a single parameter with a simple delimitation format fits all platforms with little overhead.
		/// </remarks>
		internal static string SetDependencyPropertyValueInternal(DependencyObject owner, string dependencyPropertyNameAndValue)
		{
			var s = dependencyPropertyNameAndValue;
			var index = s.IndexOf("|");

			if (index != -1)
			{
				var dependencyPropertyName = s.Substring(0, index);
				var value = s.Substring(index + 1);

				if (DependencyProperty.GetProperty(owner.GetType(), dependencyPropertyName) is DependencyProperty dp)
				{
					if (owner.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						owner.Log().LogDebug($"SetDependencyPropertyValue({dependencyPropertyName}) = {value}");
					}

					owner.SetValue(dp, XamlBindingHelper.ConvertValue(dp.Type, value));

					return owner.GetValue(dp)?.ToString();
				}
				else
				{
					if (owner.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						owner.Log().LogDebug($"Failed to find property [{dependencyPropertyName}] on [{owner}]");
					}
					return "**** Failed to find property";
				}
			}
			else
			{
				return "**** Invalid property and value format.";
			}
		}

		internal Rect LayoutSlot { get; set; } = default;

		internal Rect LayoutSlotWithMarginsAndAlignments { get; set; } = default;

		internal bool NeedsClipToSlot { get; set; }

#if !__WASM__
		/// <summary>
		/// Backing property for <see cref="Windows.UI.Xaml.Controls.Primitives.LayoutInformation.GetAvailableSize(UIElement)"/>
		/// </summary>
		internal Size LastAvailableSize { get; set; }

		/// <summary>
		/// Provides the size reported during the last call to Measure.
		/// </summary>
		/// <remarks>
		/// DesiredSize INCLUDES MARGINS.
		/// </remarks>
		public Size DesiredSize
		{
			get;
			internal set;
		}

		/// <summary>
		/// Provides the size reported during the last call to Arrange (i.e. the ActualSize)
		/// </summary>
		public Size RenderSize { get; internal set; }

		public virtual void Measure(Size availableSize)
		{
		}

#if !__WASM__
		/// <summary>
		/// This is the Frame that should be used as "available Size" for the Arrange phase.
		/// </summary>
		internal Rect? ClippedFrame;
#endif

		public virtual void Arrange(Rect finalRect)
		{
		}

		public void InvalidateMeasure()
		{
			var frameworkElement = this as IFrameworkElement;

			if (frameworkElement != null)
			{
				IFrameworkElementHelper.InvalidateMeasure(frameworkElement);
			}
			else
			{
				this.Log().Warn("Calling InvalidateMeasure on a UIElement that is not a FrameworkElement has no effect.");
			}

			OnInvalidateMeasure();
		}

		internal protected virtual void OnInvalidateMeasure()
		{
		}

		[global::Uno.NotImplemented]
		public void InvalidateArrange()
		{
			InvalidateMeasure();
#if !__WASM__
			ClippedFrame = null;
#endif
		}
#endif

		public void StartBringIntoView()
		{
			StartBringIntoView(new BringIntoViewOptions());
		}

		public void StartBringIntoView(BringIntoViewOptions options)
		{
#if __IOS__ || __ANDROID__
			Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				// This currently doesn't support nested scrolling.
				// This currently doesn't support BringIntoViewOptions.AnimationDesired.
				var scrollContentPresenter = this.FindFirstParent<IScrollContentPresenter>();
				scrollContentPresenter?.MakeVisible(this, options.TargetRect ?? Rect.Empty);
			});
#endif
		}

		internal virtual bool IsViewHit() => true;

#if HAS_UNO_WINUI
		#region FocusState DependencyProperty

		public FocusState FocusState
		{
			get { return (FocusState)GetValue(FocusStateProperty); }
			internal set { SetValue(FocusStateProperty, value); }
		}

		public static DependencyProperty FocusStateProperty =
			DependencyProperty.Register(
				"FocusState",
				typeof(FocusState),
				typeof(UIElement),
				new FrameworkPropertyMetadata(
					(FocusState)FocusState.Unfocused
				)
			);

		#endregion

		#region IsTabStop DependencyProperty

		public bool IsTabStop
		{
			get { return (bool)GetValue(IsTabStopProperty); }
			set { SetValue(IsTabStopProperty, value); }
		}

		public static DependencyProperty IsTabStopProperty =
			DependencyProperty.Register(
				"IsTabStop",
				typeof(bool),
				typeof(UIElement),
				new FrameworkPropertyMetadata(
					(bool)true,
					(s, e) => ((Control)s)?.OnIsTabStopChanged((bool)e.OldValue, (bool)e.NewValue)
				)
			);
		#endregion

		private protected virtual void OnIsTabStopChanged(bool oldValue, bool newValue) { }
#endif
	}
}
