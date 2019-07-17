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

#if __IOS__
using UIKit;
#endif

namespace Windows.UI.Xaml
{
	public partial class UIElement : DependencyObject, IXUidProvider
	{
		private readonly SerialDisposable _clipSubscription = new SerialDisposable();
		private readonly List<Pointer> _pointCaptures = new List<Pointer>();
		private readonly List<KeyboardAccelerator> _keyboardAccelerators = new List<KeyboardAccelerator>();
		private string _uid;

		partial void InitializeCapture()
		{
			this.SetValue(PointerCapturesProperty, _pointCaptures);
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

		/// <summary>
		/// Determines if an <see cref="UIElement"/> clips its children to its bounds.
		/// </summary>
		internal bool ClipChildrenToBounds { get; set; } = true;

		internal bool IsPointerPressed { get; set; }

		internal bool IsPointerOver { get; set; }

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
			get => (Transform)this.GetValue(RenderTransformProperty);
			set => this.SetValue(RenderTransformProperty, value);
		}

		/// <summary>
		/// Backing dependency property for <see cref="RenderTransform"/>
		/// </summary>
		public static readonly DependencyProperty RenderTransformProperty =
			DependencyProperty.Register("RenderTransform", typeof(Transform), typeof(UIElement), new PropertyMetadata(null, (s, e) => OnRenderTransformChanged(s, e)));

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
		public static readonly DependencyProperty RenderTransformOriginProperty =
			DependencyProperty.Register("RenderTransformOrigin", typeof(Point), typeof(UIElement), new PropertyMetadata(default(Point), (s, e) => OnRenderTransformOriginChanged(s, e)));

		private static void OnRenderTransformOriginChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var view = (UIElement)dependencyObject;
			var point = (Point)args.NewValue;

			view._renderTransform?.UpdateOrigin(point);
		}
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

		internal Rect LayoutSlot { get; set; } = default;

#if !__WASM__
		/// <summary>
		/// Provides the size reported during the last call to Measure.
		/// </summary>
		public Size DesiredSize { get; internal set; }

		/// <summary>
		/// Provides the size reported during the last call to Arrange (i.e. the ActualSize)
		/// </summary>
		public Size RenderSize { get; internal set; }

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

		[global::Uno.NotImplemented]
		public IList<KeyboardAccelerator> KeyboardAccelerators => _keyboardAccelerators;
	}
}
