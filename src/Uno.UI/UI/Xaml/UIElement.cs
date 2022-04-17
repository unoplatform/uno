#if NET461 || __WASM__
#pragma warning disable CS0067
#endif

using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using System.Collections.Generic;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.Disposables;
using Windows.UI.Xaml.Controls;
using Uno.UI;
using Uno;
using Uno.UI.Controls;
using Uno.UI.Media;
using System;
using System.Collections;
using System.Numerics;
using System.Reflection;
using Windows.UI.Xaml.Markup;

using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Core;
using System.Text;
using Uno.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using System.Runtime.CompilerServices;
using Windows.Graphics.Display;
using Uno.UI.Extensions;

#if __IOS__
using UIKit;
#endif

namespace Windows.UI.Xaml
{
	public partial class UIElement : DependencyObject, IXUidProvider, IUIElement
	{
		private static readonly Dictionary<Type, RoutedEventFlag> ImplementedRoutedEvents
			= new Dictionary<Type, RoutedEventFlag>();

		private static readonly TypedEventHandler<UIElement, BringIntoViewRequestedEventArgs> OnBringIntoViewRequestedHandler =
			(UIElement sender, BringIntoViewRequestedEventArgs args) => sender.OnBringIntoViewRequested(args);

		private static readonly Type[] _bringIntoViewRequestedArgs = new[] { typeof(BringIntoViewRequestedEventArgs) };

		private readonly SerialDisposable _clipSubscription = new SerialDisposable();
		private XamlRoot _xamlRoot = null;
		private string _uid;
		private WeakReference<VisualTree> _visualTreeWeakReference;

		//private protected virtual void PrepareState() 
		//{
		//	// This is part of the WinUI internal API and is invoked at the end of DXamlCore.GetPeerPrivate
		//	// but to avoid invoking a virtual method in ctor ** THIS IS NOT INVOKED BY DEFAULT IN UNO **.
		//	// It has to be manually invoked at the end of the ctor of your control
		//}

		#region EffectiveViewport
		public static void RegisterAsScrollPort(UIElement element)
			=> element.IsScrollPort = true;

		internal bool IsScrollPort { get; private set; }

		// This are fulfilled by the ScrollViewer for the EffectiveViewport computation,
		// but it should actually be computed based on clipping vs desired size.
		internal Point ScrollOffsets { get; private protected set; }
		#endregion

		/// <summary>
		/// Is this view set to Window.Current.Content?
		/// </summary>
		internal bool IsWindowRoot { get; set; }

		/// <summary>
		/// Is this view the top of the managed visual tree
		/// </summary>
		/// <remarks>This differs from the XamlRoot be being true for the root element of a native Popup.</remarks>
		internal bool IsVisualTreeRoot { get; set; }

		internal VisualTree VisualTree
		{
			get => _visualTreeWeakReference?.TryGetTarget(out var target) == true ? target : null;
			set => _visualTreeWeakReference = new WeakReference<VisualTree>(value);
		}

		private void Initialize()
		{
			SubscribeToOverridenRoutedEvents();
		}

		private void SubscribeToOverridenRoutedEvents()
		{
			// Overridden Events are registered from constructor to ensure they are
			// registered first in event handlers.
			// https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.control.onpointerpressed#remarks

			var implementedEvents = GetImplementedRoutedEventsForType(GetType());

			if (implementedEvents.HasFlag(RoutedEventFlag.BringIntoViewRequested))
			{
				BringIntoViewRequested += OnBringIntoViewRequestedHandler;
			}
		}

		internal static RoutedEventFlag GetImplementedRoutedEventsForType(Type type)
		{
			// TODO: GetImplementedRoutedEvents() should be evaluated at compile-time
			// and the result placed in a partial file.
			if (ImplementedRoutedEvents.TryGetValue(type, out var result))
			{
				return result;
			}

			RoutedEventFlag implementedRoutedEvents;

			var baseClass = type.BaseType;
			if (baseClass == null || type == typeof(Control) || type == typeof(UIElement))
			{
				implementedRoutedEvents = RoutedEventFlag.None;
			}
			else
			{
				implementedRoutedEvents = EvaluateImplementedUIElementRoutedEvents(type);

				if (typeof(Control).IsAssignableFrom(type))
				{
					implementedRoutedEvents |= Control.EvaluateImplementedControlRoutedEvents(type);
				}
			}

			return ImplementedRoutedEvents[type] = implementedRoutedEvents;
		}

		internal static RoutedEventFlag EvaluateImplementedUIElementRoutedEvents(Type type)
		{
			RoutedEventFlag result = RoutedEventFlag.None;

			if (GetIsEventOverrideImplemented(type, nameof(OnBringIntoViewRequested), _bringIntoViewRequestedArgs))
			{
				result |= RoutedEventFlag.BringIntoViewRequested;
			}

			return result;
		}

		private protected static bool GetIsEventOverrideImplemented(Type type, string name, Type[] args)
		{
			var method = type
				.GetMethod(
					name,
					BindingFlags.NonPublic | BindingFlags.Instance,
					null,
					args,
					null);

			return method != null
				&& method.IsVirtual
				&& method.DeclaringType != typeof(UIElement)
				&& method.DeclaringType != typeof(Control);
		}

		private protected virtual bool IsTabStopDefaultValue => false;

		/// <summary>
		/// Provide an instance-specific default value for the specified property
		/// </summary>
		/// <remarks>
		/// In general, it is best do define the property default value using <see cref="PropertyMetadata"/>.
		/// </remarks>
		internal virtual bool GetDefaultValue2(DependencyProperty property, out object defaultValue)
		{
			if (property == KeyboardAcceleratorsProperty)
			{
				defaultValue = new List<KeyboardAccelerator>(0);
				return true;
			}
			else if (property == IsTabStopProperty)
			{
				defaultValue = IsTabStopDefaultValue;
				return true;
			}

			defaultValue = null;
			return false;
		}

		public Vector2 ActualSize => new Vector2((float)GetActualWidth(), (float)GetActualHeight());

		internal Size AssignedActualSize { get; set; }

		internal bool IsLeavingFrame { get; set; }

		private protected virtual double GetActualWidth() => AssignedActualSize.Width;

		private protected virtual double GetActualHeight() => AssignedActualSize.Height;

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

		public XamlRoot XamlRoot
		{
			get => _xamlRoot ?? XamlRoot.Current;
			set => _xamlRoot = value;
		}

		#region VirtualizationInformation
		private VirtualizationInformation _virtualizationInformation;
		internal VirtualizationInformation GetVirtualizationInformation() => _virtualizationInformation ??= new VirtualizationInformation();

		/// <summary>
		/// Marks this control as a container generated by, eg, a <see cref="Primitives.Selector"/>, rather than an element explicitly 
		/// defined in xaml.
		/// </summary>
		internal bool IsGeneratedContainer
		{
			get => _virtualizationInformation?.IsGeneratedContainer ?? false;
			set => GetVirtualizationInformation().IsGeneratedContainer = value;
		}

		/// <summary>
		/// Marks this as a container defined in the root of an ItemTemplate, so that it can be handled appropriately when recycled.
		/// </summary>
		internal bool IsContainerFromTemplateRoot
		{
			get => _virtualizationInformation?.IsContainerFromTemplateRoot ?? false;
			set => GetVirtualizationInformation().IsContainerFromTemplateRoot = value;
		}
		#endregion

		#region Clip DependencyProperty

		public RectangleGeometry Clip
		{
			get { return (RectangleGeometry)this.GetValue(ClipProperty); }
			set { this.SetValue(ClipProperty, value); }
		}

		public static DependencyProperty ClipProperty { get; } =
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
		public static DependencyProperty RenderTransformProperty { get; } =
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
		public static DependencyProperty RenderTransformOriginProperty { get; } =
			DependencyProperty.Register("RenderTransformOrigin", typeof(Point), typeof(UIElement), new FrameworkPropertyMetadata(default(Point), (s, e) => OnRenderTransformOriginChanged(s, e)));

		private static void OnRenderTransformOriginChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var view = (UIElement)dependencyObject;
			var point = (Point)args.NewValue;

			view._renderTransform?.UpdateOrigin(point);
		}
		#endregion

		/// <summary>
		/// Attempts to set the focus on the UIElement.
		/// </summary>
		/// <param name="value">Specifies how focus was set, as a value of the enumeration.</param>
		/// <returns>True if focus was set to the UIElement, or focus was already on the UIElement. False if the UIElement is not focusable.</returns>
		public bool Focus(FocusState value) => FocusImpl(value);

		internal void Unfocus()
		{
			var hasFocus = FocusProperties.HasFocusedElement(this);
			if (hasFocus)
			{
				var focusManager = VisualTree.GetFocusManagerForElement(this);

				// Set the focus on the next focusable control.
				// If we are trying to set focus in a changing focus event handler, we will end up leaving focus on the disabled control.
				// As a result, we fail fast here. This is being tracked by Bug 9840123
				focusManager?.ClearFocus();
			}
		}

		public GeneralTransform TransformToVisual(UIElement visual)
			=> new MatrixTransform { Matrix = new Matrix(GetTransform(from: this, to: visual)) };

		protected virtual void OnVisibilityChanged(Visibility oldValue, Visibility newVisibility)
		{
			OnVisibilityChangedPartial(oldValue, newVisibility);

			// Part of the logic from MUX uielement.cpp VisibilityState
			if (newVisibility != Visibility.Visible)
			{
				var hasFocus = FocusProperties.HasFocusedElement(this);
				if (hasFocus)
				{
					var focusManager = VisualTree.GetFocusManagerForElement(this);

					// Set the focus on the next focusable control.
					// If we are trying to set focus in a changing focus event handler, we will end up leaving focus on the disabled control.
					// As a result, we fail fast here. This is being tracked by Bug 9840123
					focusManager?.SetFocusOnNextFocusableElement(focusManager.GetRealFocusStateForFocusedElement(), true);
				}
			}

			if (this.GetParent() is UIElement parent)
			{
				// Sometimes the measure algorithms are using the Visibility
				// of their children. So we need to make sure they are reevaluated
				// when visibility changes.
				parent.InvalidateMeasure();
			}
		}

		partial void OnVisibilityChangedPartial(Visibility oldValue, Visibility newValue);

		[NotImplemented]
		protected virtual AutomationPeer OnCreateAutomationPeer() => new AutomationPeer();

		internal AutomationPeer OnCreateAutomationPeerInternal() => OnCreateAutomationPeer();

		internal static Matrix3x2 GetTransform(UIElement from, UIElement to)
		{
			var logInfoString = from.Log().IsEnabled(LogLevel.Debug) ? new StringBuilder() : null;
			logInfoString?.Append($"{nameof(GetTransform)}(from: {from}, to: {to?.ToString() ?? "<null>"}) Offsets: [");

			if (from == to)
			{
				return Matrix3x2.Identity;
			}

#if UNO_REFERENCE_API // Depth is defined properly only on WASM and Skia
			// If possible we try to navigate the tree upward so we have a greater chance
			// to find an element in the parent hierarchy of the other element.
			if (to is { } && from.Depth < to.Depth)
			{
				return GetTransform(to, from).Inverse();
			}
#endif

			var matrix = Matrix3x2.Identity;
			double offsetX = 0.0, offsetY = 0.0;
			var elt = from;
			do
			{
				var layoutSlot = elt.LayoutSlotWithMarginsAndAlignments;
				var transform = elt.RenderTransform;
				if (transform is null)
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

#if !__MACOS__ // On macOS the SCP is using RenderTransforms for scrolling and zooming which has already been included.
				if (elt is ScrollViewer sv
					// Don't adjust for scroll offsets if it's the ScrollViewer itself calling TransformToVisual
					&& elt != from)
				{
					// Scroll offsets are handled at SCP level using the IsScrollPort

					var zoom = sv.ZoomFactor;
					if (zoom != 1)
					{
						matrix *= Matrix3x2.CreateTranslation((float)offsetX, (float)offsetY);
						matrix *= Matrix3x2.CreateScale(zoom);
					}
				}

				if (elt.IsScrollPort) // Managed SCP or custom scroller
				{
					offsetX -= elt.ScrollOffsets.X;
					offsetY -= elt.ScrollOffsets.Y;
				}
#endif

				logInfoString?.Append($"{elt}: ({offsetX}, {offsetY}), ");
			} while (elt.TryGetParentUIElementForTransformToVisual(out elt, ref offsetX, ref offsetY) && elt != to); // If possible we stop as soon as we reach 'to'

			matrix *= Matrix3x2.CreateTranslation((float)offsetX, (float)offsetY);

			if (to != null && elt != to)
			{
				// Unfortunately we didn't find the 'to' in the parent hierarchy,
				// so matrix == fromToRoot and we now have to compute the transform 'toToRoot'.
				// Note: We do not propagate the 'intermediatesSelector' as cached transforms would be irrelevant
				var toToRoot = GetTransform(to, null);
				var rootToTo = toToRoot.Inverse();

				matrix *= rootToTo;
			}

			if (logInfoString != null)
			{
				logInfoString.Append($"], matrix: {matrix}");
				from.Log().LogDebug(logInfoString.ToString());
			}
			return matrix;
		}

#if !__IOS__ && !__ANDROID__ && !__MACOS__ // This is the default implementation, but it can be customized per platform
		/// <summary>
		/// Note: Offsets are only an approximation which does not take in consideration possible transformations
		///	applied by a 'UIView' between this element and its parent UIElement.
		/// </summary>
		private bool TryGetParentUIElementForTransformToVisual(out UIElement parentElement, ref double offsetX, ref double offsetY)
		{
			var parent = VisualTreeHelper.GetParent(this);
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
			if (newValue != null)
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

		[ThreadStatic]
		private static bool _isInUpdateLayout; // Currently within the UpdateLayout() method (explicit managed layout request)

#pragma warning disable CS0649 // Field not used on Desktop/Tests
		[ThreadStatic]
		private static bool _isLayoutingVisualTreeRoot; // Currently in Measure or Arrange of the element flagged with IsVisualTreeRoot (layout requested by the system)
#pragma warning restore CS0649

#if !__NETSTD__ // We need an internal accessor for the Layouter
		internal static bool IsLayoutingVisualTreeRoot
		{
			get => _isLayoutingVisualTreeRoot;
			set => _isLayoutingVisualTreeRoot = value;
		}
#endif

		internal const int MaxLayoutIterations = 250;

		public void UpdateLayout()
		{
			if (_isInUpdateLayout || _isLayoutingVisualTreeRoot)
			{
				return;
			}

			var root = Windows.UI.Xaml.Window.Current.RootElement;
			if (root is null)
			{
				return;
			}

			try
			{
				InnerUpdateLayout(root);
				return;
			}
			finally
			{
				_isInUpdateLayout = false;
			}
		}

		/// <remarks>
		/// This method contains or is called by a try/catch containing method and
		/// can be significantly slower than other methods as a result on WebAssembly.
		/// See https://github.com/dotnet/runtime/issues/56309
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void InnerUpdateLayout(UIElement root)
		{
			_isInUpdateLayout = true;

			// On UWP, the UpdateLayout method has an overload which accepts the desired size used by the window/app to layout the visual tree,
			// then this overload without parameter is only using the internally cached last desired size.
			// With Uno this method is not used for standard layouting passes, so we cannot properly internally cache the value,
			// and we instead could use the LayoutInformation.GetLayoutSlot(root).
			//
			// The issue is that unlike UWP which will ends by requesting an UpdateLayout with the right window bounds,
			// Uno instead exclusively relies on measure/arrange invalidation.
			// So if we invoke the `UpdateLayout()` **before** the tree has been measured at least once
			// (which is the case when using a MUX.NavigationView in the "MainPage" on iOS as OnApplyTemplate is invoked too early),
			// then the whole tree will be measured at the last known value which is 0x0 and will never be invalidated.
			//
			// To avoid this we are instead using the Window Bounds as anyway they are the same as the root's slot.
			var bounds = Windows.UI.Xaml.Window.Current.Bounds;

#if __MACOS__ || __IOS__ // IsMeasureDirty and IsArrangeDirty are not available on iOS / macOS
				root.Measure(bounds.Size);
				root.Arrange(bounds);
#elif __ANDROID__
				for (var i = 0; i < MaxLayoutIterations; i++)
				{
					// On Android, Measure and arrange are the same
					if (root.IsMeasureDirtyOrMeasureDirtyPath)
					{
						root.Measure(bounds.Size);
						root.Arrange(bounds);
					}
					else
					{
						return;
					}
				}
#else
			for (var i = MaxLayoutIterations; i > 0; i--)
			{
				if (root.IsMeasureDirtyOrMeasureDirtyPath)
				{
					root.Measure(bounds.Size);
				}
				else if (root.IsArrangeDirtyOrArrangeDirtyPath)
				{
					root.Arrange(bounds);
				}
				else
				{
					return;
				}
			}

			throw new InvalidOperationException("Layout cycle detected.");
#endif
		}

		internal void ApplyClip()
		{
			Rect rect;

			if (Clip == null)
			{
				rect = Rect.Empty;

				if (NeedsClipToSlot)
				{
#if UNO_REFERENCE_API
					rect = new Rect(0, 0, RenderSize.Width, RenderSize.Height);
#else
					rect = ClippedFrame ?? Rect.Empty;
#endif
				}
			}
			else
			{
				rect = Clip.Rect;

				// Apply transform to clipping mask, if any
				if (Clip.Transform != null)
				{
					rect = Clip.Transform.TransformBounds(rect);
				}
			}

			ApplyNativeClip(rect);
			OnViewportUpdated(rect);
		}

		partial void ApplyNativeClip(Rect rect);
		private protected virtual void OnViewportUpdated(Rect viewport) { } // Not "Changed" as it might be the same as previous

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
					if (owner.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
					{
						owner.Log().LogDebug($"SetDependencyPropertyValue({dependencyPropertyName}) = {value}");
					}

					owner.SetValue(dp, XamlBindingHelper.ConvertValue(dp.Type, value));

					return owner.GetValue(dp)?.ToString();
				}
				else
				{
					if (owner.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
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

		/// <summary>
		/// Backing property for <see cref="LayoutInformation.GetAvailableSize(UIElement)"/>
		/// </summary>
		Size IUIElement.LastAvailableSize { get; set; }

		/// <summary>
		/// Gets the 'availableSize' of the last Measure
		/// </summary>
		internal Size LastAvailableSize => ((IUIElement)this).LastAvailableSize;

		/// <summary>
		/// Backing property for <see cref="LayoutInformation.GetLayoutSlot(FrameworkElement)"/>
		/// </summary>
		Rect IUIElement.LayoutSlot { get; set; }

		/// <summary>
		/// Gets the 'finalSize' of the last Arrange.
		/// Be aware that it's the rect provided by the parent, **before** margins and alignment are being applied,
		/// so the size of that rect can be different to the size get in the `ArrangeOverride`.
		/// </summary>
		/// <remarks>This is expressed in parent's coordinate space.</remarks>
		internal Rect LayoutSlot => ((IUIElement)this).LayoutSlot;

		/// <summary>
		/// This is the <see cref="LayoutSlot"/> **after** margins and alignments has been applied.
		/// It's somehow the region into which an element renders itself in its parent (before any RenderTransform).
		/// This is the 'finalRect' of the last Arrange.
		/// </summary>
		/// <remarks>This is expressed in parent's coordinate space.</remarks>
		internal Rect LayoutSlotWithMarginsAndAlignments { get; set; } = default;

		internal bool NeedsClipToSlot { get; set; }

		/// <summary>
		/// Backing property for <see cref="LayoutInformation.GetDesiredSize(UIElement)"/>
		/// </summary>
		Size IUIElement.DesiredSize { get; set; }

#if !UNO_REFERENCE_API
		/// <summary>
		/// Provides the size reported during the last call to Measure.
		/// </summary>
		/// <remarks>
		/// DesiredSize INCLUDES MARGINS.
		/// </remarks>
		public Size DesiredSize => ((IUIElement)this).DesiredSize;

		/// <summary>
		/// Provides the size reported during the last call to Arrange (i.e. the ActualSize)
		/// </summary>
		public Size RenderSize { get; internal set; }

#if !UNO_REFERENCE_API
		/// <summary>
		/// This is the Frame that should be used as "available Size" for the Arrange phase.
		/// </summary>
		internal Rect? ClippedFrame;
#endif

		public virtual void Measure(Size availableSize)
		{
		}

		public virtual void Arrange(Rect finalRect)
		{
		}

		public void InvalidateMeasure()
		{
#if XAMARIN_ANDROID
			// Use a non-virtual version of the RequestLayout method, for performance.
			base.RequestLayout();
			SetLayoutFlags(LayoutFlag.MeasureDirty);
#elif XAMARIN_IOS
			SetNeedsLayout();
			SetLayoutFlags(LayoutFlag.MeasureDirty);
#elif __MACOS__
			base.NeedsLayout = true;
			SetLayoutFlags(LayoutFlag.MeasureDirty);
#endif

			OnInvalidateMeasure();
		}

		protected internal virtual void OnInvalidateMeasure()
		{
		}

		[global::Uno.NotImplemented]
		public void InvalidateArrange()
		{
			InvalidateMeasure();
#if __IOS__ || __MACOS__
			SetLayoutFlags(LayoutFlag.ArrangeDirty);
#endif
		}
#endif

		internal virtual bool IsViewHit() => true;

		internal virtual bool IsEnabledOverride() => true;

		internal bool GetUseLayoutRounding()
		{
#if __SKIA__
			return true;
#else
			return false;
#endif
		}

		internal double LayoutRound(double value)
		{
#if __SKIA__
			double scaleFactor = GetScaleFactorForLayoutRounding();

			return LayoutRound(value, scaleFactor);
#else
			return value;
#endif
		}

		internal Rect LayoutRound(Rect value)
		{
#if __SKIA__
			double scaleFactor = GetScaleFactorForLayoutRounding();

			return new Rect(
				x: LayoutRound(value.X, scaleFactor),
				y: LayoutRound(value.Y, scaleFactor),
				width: LayoutRound(value.Width, scaleFactor),
				height: LayoutRound(value.Height, scaleFactor)
			);
#else
			return value;
#endif
		}

		internal Thickness LayoutRound(Thickness value)
		{
#if __SKIA__
			double scaleFactor = GetScaleFactorForLayoutRounding();

			return new Thickness(
				top: LayoutRound(value.Top, scaleFactor),
				bottom: LayoutRound(value.Bottom, scaleFactor),
				left: LayoutRound(value.Left, scaleFactor),
				right: LayoutRound(value.Right, scaleFactor)
			);
#else
			return value;
#endif
		}

		internal Vector2 LayoutRound(Vector2 value)
		{
#if __SKIA__
			double scaleFactor = GetScaleFactorForLayoutRounding();

			return new Vector2(
				x: (float)LayoutRound(value.X, scaleFactor),
				y: (float)LayoutRound(value.Y, scaleFactor)
			);
#else
			return value;
#endif
		}

		internal Size LayoutRound(Size value)
		{
#if __SKIA__
			double scaleFactor = GetScaleFactorForLayoutRounding();

			return new Size(
				width: LayoutRound(value.Width, scaleFactor),
				height: LayoutRound(value.Height, scaleFactor)
			);
#else
			return value;
#endif
		}

		private double LayoutRound(double value, double scaleFactor)
		{
			double returnValue = value;

			// Plateau scale is applied as a scale transform on the root element. All values computed by layout
			// will be multiplied by this scale. Layout assumes a plateau of 1, and values rounded to
			// integers at layout plateau of 1 will not be integer values when scaled by plateau transform, causing
			// sub-pixel rendering at plateau != 1. To correctly put element edges at device pixel boundaries, layout rounding
			// needs to take plateau into account and produce values that will be rounded after plateau scaling is applied,
			// i.e. multiples of 1/Plateau.
			if (scaleFactor != 1.0)
			{
				returnValue = XcpRound(returnValue * scaleFactor) / scaleFactor;
			}
			else
			{
				// Avoid unnecessary multiply/divide at scale factor 1.
				returnValue = XcpRound(returnValue);
			}

			return returnValue;
		}

		// GetScaleFactorForLayoutRounding() returns the plateau scale in most cases. For ScrollContentPresenter children though,
		// the plateau scale gets combined with the owning ScrollViewer's ZoomFactor if headers are present.
		internal double GetScaleFactorForLayoutRounding()
		{
			// TODO use actual scaling based on current transforms.
			return global::Windows.Graphics.Display.DisplayInformation.GetForCurrentView().LogicalDpi / DisplayInformation.BaseDpi; // 100%
		}

		double XcpRound(double x)
		{
			return Math.Round(x);
		}

		public XYFocusKeyboardNavigationMode XYFocusKeyboardNavigation
		{
			get => GetXYFocusKeyboardNavigationValue();
			set => SetXYFocusKeyboardNavigationValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = default(XYFocusKeyboardNavigationMode), Options = FrameworkPropertyMetadataOptions.Inherits)]
		public static DependencyProperty XYFocusKeyboardNavigationProperty { get; } = CreateXYFocusKeyboardNavigationProperty();

		public XYFocusNavigationStrategy XYFocusDownNavigationStrategy
		{
			get => GetXYFocusDownNavigationStrategyValue();
			set => SetXYFocusDownNavigationStrategyValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = default(XYFocusNavigationStrategy))]
		public static DependencyProperty XYFocusDownNavigationStrategyProperty { get; } = CreateXYFocusDownNavigationStrategyProperty();

		public XYFocusNavigationStrategy XYFocusLeftNavigationStrategy
		{
			get => GetXYFocusLeftNavigationStrategyValue();
			set => SetXYFocusLeftNavigationStrategyValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = default(XYFocusNavigationStrategy))]
		public static DependencyProperty XYFocusLeftNavigationStrategyProperty { get; } = CreateXYFocusLeftNavigationStrategyProperty();

		public XYFocusNavigationStrategy XYFocusRightNavigationStrategy
		{
			get => GetXYFocusRightNavigationStrategyValue();
			set => SetXYFocusRightNavigationStrategyValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = default(XYFocusNavigationStrategy))]
		public static DependencyProperty XYFocusRightNavigationStrategyProperty { get; } = CreateXYFocusRightNavigationStrategyProperty();

		public XYFocusNavigationStrategy XYFocusUpNavigationStrategy
		{
			get => GetXYFocusUpNavigationStrategyValue();
			set => SetXYFocusUpNavigationStrategyValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = default(XYFocusNavigationStrategy))]
		public static DependencyProperty XYFocusUpNavigationStrategyProperty { get; } = CreateXYFocusUpNavigationStrategyProperty();

		public KeyboardNavigationMode TabFocusNavigation
		{
			get => GetTabFocusNavigationValue();
			set => SetTabFocusNavigationValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = default(KeyboardNavigationMode))]
		public static DependencyProperty TabFocusNavigationProperty { get; } = CreateTabFocusNavigationProperty();

#if HAS_UNO_WINUI
		// Skipping already declared property ActualSize
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public UI.Input.InputCursor ProtectedCursor
		{
			get;
			set;
		}
#endif
	}
}
