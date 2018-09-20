﻿#if NET46 || NETSTANDARD2_0
#pragma warning disable CS0067
#endif

using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Uno.Extensions;
using Uno.Logging;
using Uno.Disposables;
using System.Linq;
using Windows.Devices.Input;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Uno.UI;
using Uno;
using VirtualKeyModifiers = Windows.UI.Xaml.Input.VirtualKeyModifiers;

#if __IOS__
using UIKit;
#endif

namespace Windows.UI.Xaml
{
	public partial class UIElement : DependencyObject
	{
		private SerialDisposable _clipSubscription = new SerialDisposable();
		private readonly List<Pointer> _pointCaptures = new List<Pointer>();

		partial void InitializeCapture()
		{
			this.SetValue(PointerCapturesProperty, _pointCaptures);
		}


		/// <summary>
		/// Determines if an <see cref="UIElement"/> clips its children to its bounds.
		/// </summary>
		internal bool ClipChildrenToBounds { get; set; } = true;

		#region Routed Events

		public static RoutedEvent PointerPressedEvent { get; } = new RoutedEvent();
		public static RoutedEvent PointerReleasedEvent { get; } = new RoutedEvent();
		public static RoutedEvent PointerEnteredEvent { get; } = new RoutedEvent();
		public static RoutedEvent PointerExitedEvent { get; } = new RoutedEvent();
		public static RoutedEvent PointerMovedEvent { get; } = new RoutedEvent();
		public static RoutedEvent PointerCanceledEvent { get; } = new RoutedEvent();
		public static RoutedEvent PointerCaptureLostEvent { get; } = new RoutedEvent();
		public static RoutedEvent TappedEvent { get; } = new RoutedEvent();
		public static RoutedEvent DoubleTappedEvent { get; } = new RoutedEvent();
		public static RoutedEvent KeyDownEvent { get; } = new RoutedEvent();
		public static RoutedEvent KeyUpEvent { get; } = new RoutedEvent();
		internal static RoutedEvent GotFocusEvent { get; } = new RoutedEvent();
		internal static RoutedEvent LostFocusEvent { get; } = new RoutedEvent();

		private struct RoutedEventHandlerInfo
		{
			public RoutedEventHandlerInfo(object handler, bool handledEventsToo)
			{
				Handler = handler;
				HandledEventsToo = handledEventsToo;
			}

			public object Handler { get; }
			public bool HandledEventsToo { get; }
		}

		private Dictionary<RoutedEvent, List<RoutedEventHandlerInfo>> _eventHandlerStore = new Dictionary<RoutedEvent, List<RoutedEventHandlerInfo>>();

		public event RoutedEventHandler LostFocus
		{
			add { AddHandler(LostFocusEvent, value, false); }
			remove { RemoveHandler(LostFocusEvent, value); }
		}

		public event RoutedEventHandler GotFocus
		{
			add { AddHandler(GotFocusEvent, value, false); }
			remove { RemoveHandler(GotFocusEvent, value); }
		}

		public event DoubleTappedEventHandler DoubleTapped
		{
			add { AddHandler(DoubleTappedEvent, value, false); }
			remove { RemoveHandler(DoubleTappedEvent, value); }
		}

#pragma warning disable 67 // Unused member
		public event PointerEventHandler PointerCanceled
		{
			add { AddHandler(PointerCanceledEvent, value, false); }
			remove { RemoveHandler(PointerCanceledEvent, value); }
		}
#pragma warning restore 67 // Unused member

		public event PointerEventHandler PointerCaptureLost
		{
			add { AddHandler(PointerCaptureLostEvent, value, false); }
			remove { RemoveHandler(PointerCaptureLostEvent, value); }
		}
		
		public event PointerEventHandler PointerEntered
		{
			add { AddHandler(PointerEnteredEvent, value, false); }
			remove { RemoveHandler(PointerEnteredEvent, value); }
		}
		
		public event PointerEventHandler PointerExited
		{
			add { AddHandler(PointerExitedEvent, value, false); }
			remove { RemoveHandler(PointerExitedEvent, value); }
		}

		public event PointerEventHandler PointerMoved
		{
			add { AddHandler(PointerMovedEvent, value, false); }
			remove { RemoveHandler(PointerMovedEvent, value); }
		}

		public event PointerEventHandler PointerPressed
		{
			add { AddHandler(PointerPressedEvent, value, false); }
			remove { RemoveHandler(PointerPressedEvent, value); }
		}
		
		public event PointerEventHandler PointerReleased
		{
			add { AddHandler(PointerReleasedEvent, value, false); }
			remove { RemoveHandler(PointerReleasedEvent, value); }
		}

		public event TappedEventHandler Tapped
		{
			add { AddHandler(TappedEvent, value, false); }
			remove { RemoveHandler(TappedEvent, value); }
		}

#if __MACOS__
		public new event KeyEventHandler KeyDown
#else
		public event KeyEventHandler KeyDown
#endif
		{
			add { AddHandler(KeyDownEvent, value, false); }
			remove { RemoveHandler(KeyDownEvent, value); }
		}

#if __MACOS__
		public new event KeyEventHandler KeyUp
#else
		public event KeyEventHandler KeyUp
#endif
		{
			add { AddHandler(KeyUpEvent, value, false); }
			remove { RemoveHandler(KeyUpEvent, value); }
		}

		public void AddHandler(RoutedEvent routedEvent, object handler, bool handledEventsToo)
		{
			var handlers = _eventHandlerStore.FindOrCreate(routedEvent, () => new List<RoutedEventHandlerInfo>());
			handlers.Add(new RoutedEventHandlerInfo(handler, handledEventsToo));

			AddHandlerPartial(routedEvent, handler);
		}

		partial void AddHandlerPartial(RoutedEvent routedEvent, object handler);

		public void RemoveHandler(RoutedEvent routedEvent, object handler)
		{
			if (_eventHandlerStore.TryGetValue(routedEvent, out List<RoutedEventHandlerInfo> handlers))
			{
				handlers.Remove(handlerInfo => handlerInfo.Handler == handler);
			}

			RemoveHandlerPartial(routedEvent, handler);
		}

		partial void RemoveHandlerPartial(RoutedEvent routedEvent, object handler);

		internal void RaiseEvent(RoutedEvent routedEvent, RoutedEventArgs args)
		{
			if (_eventHandlerStore.TryGetValue(routedEvent, out List<RoutedEventHandlerInfo> handlers))
			{
				foreach (var handler in handlers)
				{
					if (!IsHandled(args) || handler.HandledEventsToo)
					{
						InvokeHandler(handler.Handler, args);
					}
				}
			}

			var isHandled = IsHandled(args);

			// We don't need to manually bubble up non-handled events on the managed side.
			// We already take care to bubble up non-handled events on the native side:
			// - Android: UnoViewGroup.java -> Return false in dispatchTouchEvent
			// - iOS: UIElement.iOS.cs -> Call base.TouchesBegan, base.TouchesEnded, etc.
			// - Wasm: WindowManager.ts -> Don't call event.stopPropagation()
			// In the future, we might decide to stop bubbling up events on the native side, and do it all on the managed side instead.
			// The first element hit by hit-testing would natively handle the event (stop native bubble up),
			// become the OriginalSource of the associated RoutedEventArgs, and we would manually bubble up the event on the managed side.
			var bubblesUpNatively = IsBubblingNatively(args);

			// According to Microsoft, handled events shouldn't bubble up:
			// https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/events-and-routed-events-overview#the-handled-property
			// However, an ancestor might have used AddHandler(..., handledEventsToo: true), in which case we need to route the event to it.
			// Here, we systematically bubble up events in case an ancestor used AddHandler(..., handledEventsToo: true).
			// This is a naive approach, and it's probably expensive. We should find a better solution.
			var bubbleUpHandledEventsToo = true;

			if (!bubblesUpNatively && (!isHandled || bubbleUpHandledEventsToo))
			{
				// We bubble up the event
#if __IOS__ || __ANDROID__
				var parent = this.FindFirstParent<UIElement>();
#else
					var parent = this.GetParent() as UIElement;
#endif
				parent?.RaiseEvent(routedEvent, args);
			}
		}

		private static bool IsHandled(RoutedEventArgs args)
		{
			// TODO: WPF reads Handled directly on RoutedEventArgs. 
			switch (args)
			{
				case PointerRoutedEventArgs pointer:
					return pointer.Handled;
				case TappedRoutedEventArgs tapped:
					return tapped.Handled;
				case DoubleTappedRoutedEventArgs doubleTapped:
					return doubleTapped.Handled;
				case KeyRoutedEventArgs key:
					return key.Handled;
				default:
					return false;
			}
		}

		private static bool IsBubblingNatively(RoutedEventArgs args)
		{
			if (args is ICancellableRoutedEventArgs cancellable)
			{
				return !cancellable.Handled;
			}

			return false;
		}

		private void InvokeHandler(object handler, RoutedEventArgs args)
		{
			// TODO: WPF calls a virtual RoutedEventArgs.InvokeEventHandler(Delegate handler, object target) method,
			// instead of going through all possible cases like we do here.
			switch (handler)
			{
				case RoutedEventHandler routedEventHandler:
					routedEventHandler(this, args);
					break;
				case PointerEventHandler pointerEventHandler:
					pointerEventHandler(this, (PointerRoutedEventArgs)args);
					break;
				case TappedEventHandler tappedEventHandler:
					tappedEventHandler(this, (TappedRoutedEventArgs)args);
					break;
				case DoubleTappedEventHandler doubleTappedEventHandler:
					doubleTappedEventHandler(this, (DoubleTappedRoutedEventArgs)args);
					break;
				case KeyEventHandler keyEventHandler:
					keyEventHandler(this, (KeyRoutedEventArgs)args);
					break;
			}
		}

#endregion

		protected internal bool IsPointerPressed { get; set; }

		protected internal bool IsPointerOver { get; set; }

#region Clip DependencyProperty

		public RectangleGeometry Clip
		{
			get { return (RectangleGeometry)this.GetValue(ClipProperty); }
			set { this.SetValue(ClipProperty, value); }
		}

		public static readonly DependencyProperty ClipProperty =
			DependencyProperty.Register(
				"Clip",
				typeof(RectangleGeometry),
				typeof(UIElement),
				new PropertyMetadata(
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
			get { return (Transform)this.GetValue(RenderTransformProperty); }
			set { this.SetValue(RenderTransformProperty, value); }
		}

		// Using a DependencyProperty as the backing store for RenderTransform.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty RenderTransformProperty =
			DependencyProperty.Register("RenderTransform", typeof(Transform), typeof(UIElement), new PropertyMetadata(null, (s, e) => OnRenderTransformChanged(s, e)));

		static partial void OnRenderTransformChanged(object dependencyObject, DependencyPropertyChangedEventArgs args);

#endregion

#region RenderTransformOrigin Dependency Property

		/// <summary>
		/// This is a Transformation for a UIElement.  It binds the Render Transform to the View
		/// </summary>
		public Point RenderTransformOrigin
		{
			get { return (Point)this.GetValue(RenderTransformOriginProperty); }
			set { this.SetValue(RenderTransformOriginProperty, value); }
		}

		// Using a DependencyProperty as the backing store for RenderTransformOrigin.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty RenderTransformOriginProperty =
			DependencyProperty.Register("RenderTransformOrigin", typeof(Point), typeof(UIElement), new PropertyMetadata(default(Point), (s, e) => OnRenderTransformOriginChanged(s, e)));


		static partial void OnRenderTransformOriginChanged(object dependencyObject, DependencyPropertyChangedEventArgs args);

#endregion

#region IsHitTestVisible Dependency Property

		public bool IsHitTestVisible
		{
			get { return (bool)this.GetValue(IsHitTestVisibleProperty); }
			set { this.SetValue(IsHitTestVisibleProperty, value); }
		}

		public static readonly DependencyProperty IsHitTestVisibleProperty =
			DependencyProperty.Register(
				nameof(IsHitTestVisible),
				typeof(bool),
				typeof(UIElement),
				new FrameworkPropertyMetadata(
					defaultValue: true,
					propertyChangedCallback: (s, e) => (s as UIElement).OnIsHitTestVisibleChanged((bool)e.OldValue, (bool)e.NewValue))
			);

		protected virtual void OnIsHitTestVisibleChanged(bool oldValue, bool newValue)
		{
			OnIsHitTestVisibleChangedPartial(oldValue, newValue);
		}

		partial void OnIsHitTestVisibleChangedPartial(bool oldValue, bool newValue);

#endregion

#region Opacity Dependency Property

		public double Opacity
		{
			get { return (double)this.GetValue(OpacityProperty); }
			set { this.SetValue(OpacityProperty, value); }
		}

		public static readonly DependencyProperty OpacityProperty =
			DependencyProperty.Register("Opacity", typeof(double), typeof(UIElement), new PropertyMetadata(1.0, (s, a) => ((UIElement)s).OnOpacityChanged(a)));

		partial void OnOpacityChanged(DependencyPropertyChangedEventArgs args);

#endregion

#region Visibility Dependency Property

		/// <summary>
		/// Sets the visibility of the current view
		/// </summary>
		public
#if __ANDROID__
		new
#endif
			Visibility Visibility
		{
			get { return (Visibility)this.GetValue(VisibilityProperty); }
			set { this.SetValue(VisibilityProperty, value); }
		}

		public static readonly DependencyProperty VisibilityProperty =
			DependencyProperty.Register(
				"Visibility",
				typeof(Visibility),
				typeof(UIElement),
				new PropertyMetadata(
					Visibility.Visible,
					(s, e) => (s as UIElement).OnVisibilityChanged((Visibility)e.OldValue, (Visibility)e.NewValue)
				)
			);
#endregion

		internal bool IsRenderingSuspended { get; set; }

		private void ApplyClip()
		{
			var rect = Clip?.Rect ?? Rect.Empty;

			if (Clip?.Transform is TranslateTransform translateTransform)
			{
				rect.X += translateTransform.X;
				rect.Y += translateTransform.Y;
			}

			EnsureClip(rect);
		}

		partial void EnsureClip(Rect rect);

		internal static object GetDependencyPropertyValueInternal(DependencyObject owner, string dependencyPropertyName)
		{
			var dp = DependencyProperty.GetProperty(owner.GetType(), dependencyPropertyName);
			return dp == null ? null : owner.GetValue(dp);
		}

#if !__WASM__
		/// <summary>
		/// Provides the size reported during the last call to Measure.
		/// </summary>
		public Size DesiredSize { get; internal set; }

		public Size RenderSize
		{
			get; internal set;
		}


		public virtual void Measure(Size availableSize)
		{
		}

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
		}
#endif

		public bool CapturePointer(Pointer value)
		{
			IsPointerCaptured = true;
			_pointCaptures.Add(value);
#if __WASM__
			CapturePointerNative(value);
#endif
			return true;
		}

		public void ReleasePointerCapture(Pointer value)
		{
			IsPointerCaptured = false;
			_pointCaptures.Remove(value);

#if __WASM__
			ReleasePointerCaptureNative(value);
#endif
		}

		public void ReleasePointerCaptures()
		{
			IsPointerCaptured = false;
#if __WASM__
			foreach (var pointer in _pointCaptures)
			{
				ReleasePointerCaptureNative(pointer);
			}
#endif
			_pointCaptures.Clear();
		}

		public global::System.Collections.Generic.IReadOnlyList<global::Windows.UI.Xaml.Input.Pointer> PointerCaptures
			=> (IReadOnlyList<global::Windows.UI.Xaml.Input.Pointer>)this.GetValue(PointerCapturesProperty);

		public static DependencyProperty PointerCapturesProperty { get; } =
		DependencyProperty.Register(
			"PointerCaptures", typeof(global::System.Collections.Generic.IReadOnlyList<global::Windows.UI.Xaml.Input.Pointer>),
			typeof(global::Windows.UI.Xaml.UIElement),
			new FrameworkPropertyMetadata(defaultValue: null)
		);

		public void StartBringIntoView()
		{
			StartBringIntoView(new BringIntoViewOptions());
		}

		public void StartBringIntoView(BringIntoViewOptions options)
		{
#if __IOS__ || __ANDROID__
			Dispatcher.RunAsync(Core.CoreDispatcherPriority.Normal, () =>
			{
				// This currently doesn't support nested scrolling.
				// This currently doesn't support BringIntoViewOptions.AnimationDesired.
				var scrollContentPresenter = this.FindFirstParent<IScrollContentPresenter>();
				scrollContentPresenter?.MakeVisible(this, options.TargetRect ?? Rect.Empty);
			});
#endif
		}

		internal virtual bool IsViewHit() => true;
	}
}
