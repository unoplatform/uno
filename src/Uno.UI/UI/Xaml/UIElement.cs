#if IS_UNIT_TESTS || __WASM__
#pragma warning disable CS0067
#endif

using Windows.Foundation;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.Disposables;
using Microsoft.UI.Xaml.Controls;
using Uno.UI;
using Uno;
using Uno.UI.Controls;
using Uno.UI.Media;
using System;
using System.Collections;
using System.Numerics;
using System.Reflection;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.UI.Core;
using System.Runtime.CompilerServices;
using System.Text;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Composition;
using Windows.Graphics.Display;
using Uno.UI.Extensions;
using Microsoft.UI.Xaml.Documents;
using Windows.ApplicationModel.Core;
using Microsoft.UI.Input;
using Uno.UI.Xaml.Media;
using Uno.UI.Xaml.Core.Scaling;

#if __IOS__
using UIKit;
#endif

namespace Microsoft.UI.Xaml
{
	public partial class UIElement : DependencyObject, IXUidProvider, IUIElement
	{
		private protected static bool _traceLayoutCycle;

		private static readonly TypedEventHandler<UIElement, BringIntoViewRequestedEventArgs> OnBringIntoViewRequestedHandler =
			(UIElement sender, BringIntoViewRequestedEventArgs args) => sender.OnBringIntoViewRequested(args);

		private static readonly Type[] _bringIntoViewRequestedArgs = new[] { typeof(BringIntoViewRequestedEventArgs) };

		private string _uid;

		private Vector3 _translation = Vector3.Zero;

		private InputCursor _protectedCursor;
		private SerialDisposable _disposedEventDisposable = new();

		internal void FreezeTemplatedParent() =>
			((IDependencyObjectStoreProvider)this).Store.IsTemplatedParentFrozen = true;

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

#if !__SKIA__
		// This is the local viewport of the element, i.e. where the element can draw content once clipping has been applied.
		// This is expressed in local coordinate space.
		internal Rect Viewport { get; private set; } = Rect.Infinite;
#endif
		#endregion

		/// <summary>
		/// Is this view the top of the managed visual tree
		/// </summary>
		/// <remarks>This differs from the XamlRoot be being true for the root element of a native Popup.</remarks>
		internal bool IsVisualTreeRoot { get; set; }

		private void Initialize()
		{
			SubscribeToOverridenRoutedEvents();
		}

#if SUPPORTS_RTL
		internal Matrix3x2 GetFlowDirectionTransform()
			=> ShouldMirrorVisual() ? new Matrix3x2(-1.0f, 0.0f, 0.0f, 1.0f, (float)RenderSize.Width, 0.0f) : Matrix3x2.Identity;

		private bool ShouldMirrorVisual()
		{
			if (this is not FrameworkElement fe)
			{
				return false;
			}

			var parent = VisualTreeHelper.GetParent(this);
			while (parent is not null)
			{
				if (parent is FrameworkElement feParent)
				{
					return feParent is not PopupPanel && fe.FlowDirection != feParent.FlowDirection;
				}

				parent = VisualTreeHelper.GetParent(parent);
			}

			return false;
		}
#endif

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
			if (UIElementGeneratedProxy.TryGetImplementedRoutedEvents(type, out var result))
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

			UIElementGeneratedProxy.RegisterImplementedRoutedEvents(type, implementedRoutedEvents);

			return implementedRoutedEvents;
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

		private protected virtual void OnChildDesiredSizeChanged(UIElement child)
		{
			InvalidateMeasure();
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
				defaultValue = new KeyboardAcceleratorCollection(this);
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

		/// <summary>
		/// Gets the size that this UIElement computed during the arrange pass of the layout process.
		/// </summary>
		public Vector2 ActualSize => new Vector2((float)GetActualWidth(), (float)GetActualHeight());

		/// <summary>
		/// Gets the position of this UIElement, relative to its parent, computed during the arrange pass of the layout process.
		/// </summary>
		public Vector3 ActualOffset
		{
			get
			{
#if __ANDROID__
				var parent = this.GetVisualTreeParent();

				if (parent is NativeListViewBase lv)
				{
					// TODO Uno: Issue with LayoutSlot for list items
					// https://github.com/unoplatform/uno/issues/2754
					var sv = lv.FindFirstParent<ScrollViewer>();
					var offset = GetPosition(this, relativeTo: sv);
					return new Vector3((float)offset.X, (float)offset.Y, 0f);
				}
#endif
				return new Vector3((float)LayoutSlotWithMarginsAndAlignments.X, (float)LayoutSlotWithMarginsAndAlignments.Y, 0f);
			}
		}

		/// <summary>
		/// Gets or sets the x, y, and z rendering position of the element.
		/// </summary>
		public Vector3 Translation
		{
			get => _translation;
			set
			{
				if (_translation != value)
				{
					_translation = value;
					UpdateShadow();
					InvalidateArrange();
				}
			}
		}

		public
#if __MACOS__
			new
#endif
			Shadow Shadow
		{
			get => (Shadow)GetValue(ShadowProperty);
			set => SetValue(ShadowProperty, value);
		}

		public static DependencyProperty ShadowProperty { get; } =
			DependencyProperty.Register(
				nameof(Shadow),
				typeof(Shadow),
				typeof(UIElement),
				new FrameworkPropertyMetadata(default(Shadow), OnShadowChanged));

		private static void OnShadowChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyObject is UIElement uiElement)
			{
				uiElement.UpdateShadow();
			}
		}

		private void UpdateShadow()
		{
			if (Shadow == null || Translation.Z <= 0)
			{
				UnsetShadow();
			}
			else
			{
				SetShadow();
			}
		}

		partial void SetShadow();

		partial void UnsetShadow();

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

		#region VirtualizationInformation
		private VirtualizationInformation _virtualizationInformation;
		internal VirtualizationInformation GetVirtualizationInformation() => _virtualizationInformation ??= new VirtualizationInformation();

		/// <summary>
		/// Marks this control as a container generated by, eg, a <see cref="Selector"/>, rather than an element explicitly
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

		/// <summary>
		/// Marks this as a container defined in the root of an ItemTemplate, so that it can be handled appropriately when cleared.
		/// </summary>
		internal bool IsOwnContainer
		{
			get => _virtualizationInformation?.IsOwnContainer ?? false;
			set => GetVirtualizationInformation().IsOwnContainer = value;
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
			if (e.OldValue is RectangleGeometry oldValue)
			{
				oldValue.GeometryChanged -= ApplyClip;
			}

			ApplyClip();

			if (e.NewValue is RectangleGeometry newValue)
			{
				newValue.GeometryChanged += ApplyClip;
			}
		}

		#endregion

		#region RenderTransform Dependency Property

		/// <summary>
		/// This is a Transformation for a UIElement.  It binds the Render Transform to the View
		/// </summary>
		public Transform RenderTransform
		{
			get => GetRenderTransformValue();
			set => SetRenderTransformValue(value);
		}

		/// <summary>
		/// Backing dependency property for <see cref="RenderTransform"/>
		/// </summary>
		[GeneratedDependencyProperty(DefaultValue = null, ChangedCallback = true)]
		public static DependencyProperty RenderTransformProperty { get; } = CreateRenderTransformProperty();

		private void OnRenderTransformChanged(Transform _, Transform transform)
		{
			var flowDirectionTransform = _renderTransform?.FlowDirectionTransform ?? Matrix3x2.Identity;

			_renderTransform?.Dispose();

			if (transform is not null || !flowDirectionTransform.IsIdentity)
			{
				_renderTransform = new NativeRenderTransformAdapter(this, transform, RenderTransformOrigin, flowDirectionTransform);
				OnRenderTransformSet();
			}
			else
			{
				// Sanity
				_renderTransform = null;
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
			get => GetRenderTransformOriginValue();
			set => SetRenderTransformOriginValue(value);
		}

		[GeneratedDependencyProperty(ChangedCallback = true)]
		public static DependencyProperty RenderTransformOriginProperty { get; } = CreateRenderTransformOriginProperty();
		private static object GetRenderTransformOriginDefaultValue() => default(Point);

		private void OnRenderTransformOriginChanged(Point _, Point origin)
			=> _renderTransform?.UpdateOrigin(origin);
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

		/// <summary>
		/// Set correct default foreground for the current theme.
		/// </summary>
		/// <param name="foregroundProperty">The appropriate property for the calling instance.</param>
		private protected void SetDefaultForeground(DependencyProperty foregroundProperty)
		{
			this.SetValue(foregroundProperty, DefaultBrushes.TextForegroundBrush, DependencyPropertyValuePrecedences.DefaultValue);
			((IDependencyObjectStoreProvider)this).Store.SetLastUsedTheme(Application.Current?.RequestedThemeForResources);
		}

		[NotImplemented]
		protected virtual AutomationPeer OnCreateAutomationPeer() => new AutomationPeer();

		internal AutomationPeer OnCreateAutomationPeerInternal() => OnCreateAutomationPeer();

		internal static Matrix3x2 GetTransform(UIElement from, UIElement to)
		{
			if (from == to || !from.IsInLiveTree || (to is { IsVisualTreeRoot: false, IsInLiveTree: false }))
			{
				return Matrix3x2.Identity;
			}

#if __SKIA__
			Matrix4x4.Invert(to?.Visual.TotalMatrix ?? Matrix4x4.Identity, out var invertedTotalMatrix);
			var finalTransform = (from.Visual.TotalMatrix * invertedTotalMatrix).ToMatrix3x2();

			if (from.Log().IsEnabled(LogLevel.Trace))
			{
				from.Log().Trace($"{nameof(GetTransform)} SKIA FAST PATH (from: {from.GetDebugName()}, to: {to.GetDebugName()}) = {finalTransform}");
			}

			return finalTransform;
#else
#if UNO_REFERENCE_API // Depth is defined properly only on WASM and Skia
			// If possible we try to navigate the tree upward so we have a greater chance
			// to find an element in the parent hierarchy of the other element.
			if (to is not null && from.Depth < to.Depth)
			{
				return GetTransform(to, from).Inverse();
			}
#endif

			var matrix = Matrix3x2.Identity;

			var elt = from;
			do
			{
				elt.ApplyRenderTransform(ref matrix);
				elt.ApplyLayoutTransform(ref matrix);
				elt.ApplyElementCustomTransform(ref matrix);
				elt.ApplyFlowDirectionTransform(ref matrix);
			} while (elt.TryGetParentUIElementForTransformToVisual(out elt, ref matrix) && elt != to); // If possible we stop as soon as we reach 'to'

			if (to is not null && elt != to)
			{
				// Unfortunately we didn't find the 'to' in the parent hierarchy,
				// so matrix == fromToRoot and we now have to compute the transform 'toToRoot'.
				var toToRoot = GetTransform(to, null);

				var rootToTo = toToRoot.Inverse();
				matrix *= rootToTo;
			}

			if (from.Log().IsEnabled(LogLevel.Trace))
			{
				from.Log().Trace($"{nameof(GetTransform)}(from: {from.GetDebugName()}, to: {to.GetDebugName()}) = {matrix}");
			}

			return matrix;
#endif
		}

#if !__SKIA__
		/// <summary>
		/// Applies to the given matrix the transformation needed to convert from parent to local element coordinates space.
		/// </summary>
		/// <param name="matrix">The matrix into which the layout constraints should be written</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void ApplyLayoutTransform(ref Matrix3x2 matrix)
		{
			var layoutSlot = LayoutSlotWithMarginsAndAlignments;
			matrix.M31 += (float)layoutSlot.X;
			matrix.M32 += (float)layoutSlot.Y;
		}

		/// <summary>
		/// Applies to the given matrix the <see cref="RenderTransform"/>.
		/// </summary>
		/// <param name="matrix">The matrix into which the render transform should be written</param>
		/// <param name="ignoreOrigin">Indicates if the <see cref="RenderTransformOrigin"/> should be ignored or not.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void ApplyRenderTransform(ref Matrix3x2 matrix, bool ignoreOrigin = false)
		{
			if (RenderTransform is { } transform)
			{
				var transformMatrix = transform.MatrixCore;
				if (!ignoreOrigin)
				{
					transformMatrix = transformMatrix.CenterOn(RenderTransformOrigin, LayoutSlotWithMarginsAndAlignments.Size);
				}

				matrix *= transformMatrix;
			}
		}

		/// <summary>
		/// Applies to the given matrix the constrains specific to the current element (like ScrollOffsets).
		/// </summary>
		/// <param name="matrix">The matrix into which the transformations should be written</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void ApplyElementCustomTransform(ref Matrix3x2 matrix)
		{
#if !__MACOS__ // On macOS the SCP is using RenderTransforms for scrolling and zooming which has already been included.
			if (this is ScrollViewer sv)
			{
				// Scroll offsets are handled at the SCP level using the IsScrollPort
				// TODO: ZoomFactor should also be handled at the SCP level!

				var zoom = sv.ZoomFactor;
				if (zoom != 1)
				{
					matrix *= Matrix3x2.CreateScale(zoom);
				}
			}

			if (IsScrollPort) // Managed SCP or custom scroller
			{
				matrix.M31 -= (float)ScrollOffsets.X;
				matrix.M32 -= (float)ScrollOffsets.Y;
			}
#endif
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void ApplyFlowDirectionTransform(ref Matrix3x2 matrix)
		{
#if SUPPORTS_RTL
			if (this is FrameworkElement fe
				&& VisualTreeHelper.GetParent(this) is FrameworkElement parent
				&& fe.FlowDirection != parent.FlowDirection)
			{
				matrix *= Matrix3x2.CreateScale(-1.0f, 1.0f, new Vector2(.5f, 0f));
			}
#endif
		}
#endif

#if !__IOS__ && !__ANDROID__ && !__MACOS__ // This is the default implementation, but it can be customized per platform
		/// <summary>
		/// Note: Offsets are only an approximation that does not take into consideration possible transformations
		///	applied by a 'UIView' between this element and its parent UIElement.
		/// </summary>
		private bool TryGetParentUIElementForTransformToVisual(out UIElement parentElement, ref Matrix3x2 _)
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

#if !__CROSSRUNTIME__ // We need an internal accessor for the Layouter
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

			var root = XamlRoot?.VisualTree.RootElement;
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

			if (root.XamlRoot is null)
			{
				// Element is not in the visual tree.
				return;
			}

			var bounds = root.XamlRoot.Bounds;

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
#elif !__NETSTD_REFERENCE__

#if UNO_HAS_ENHANCED_LIFECYCLE
			var eventManager = root.GetContext().EventManager;
			if (!root.IsMeasureDirtyOrMeasureDirtyPath &&
				!root.IsArrangeDirtyOrArrangeDirtyPath &&
				!eventManager.HasPendingViewportChangedEvents)
			{
				return;
			}
#endif

			var tracingThisCall = false;
			for (var i = MaxLayoutIterations; i > 0; i--)
			{
#if HAS_UNO_WINUI
				if (i <= 10 && Application.Current is { DebugSettings.LayoutCycleTracingLevel: not LayoutCycleTracingLevel.None })
				{
					_traceLayoutCycle = true;
					tracingThisCall = true;
					if (typeof(UIElement).Log().IsEnabled(LogLevel.Warning))
					{
						typeof(UIElement).Log().LogWarning($"[LayoutCycleTracing] Low on countdown ({i}).");
					}
				}
#endif

				if (root.IsMeasureDirtyOrMeasureDirtyPath)
				{
					root.Measure(bounds.Size);
				}
				else if (root.IsArrangeDirtyOrArrangeDirtyPath)
				{
					root.Arrange(bounds);
#if !IS_UNIT_TESTS
					// Workaround: Without this, the managed Skia TextBox breaks.
					// For example, keyboard selection or double clicking to select breaks
					// It's probably an issue with TextBox implementation itself, but for now we workaround it here.
					root.XamlRoot.RaiseInvalidateRender();
#endif
				}
#if UNO_HAS_ENHANCED_LIFECYCLE
				else if (eventManager.HasPendingViewportChangedEvents)
				{
					eventManager.RaiseEffectiveViewportChangedEvents();
				}
				else
				{
					if (eventManager.HasPendingSizeChangedEvents)
					{
						eventManager.RaiseSizeChangedEvents();
					}

					if (root.IsMeasureDirtyOrMeasureDirtyPath ||
						root.IsArrangeDirtyOrArrangeDirtyPath ||
						eventManager.HasPendingViewportChangedEvents)
					{
						continue;
					}

					eventManager.RaiseLayoutUpdated();

					if (!root.IsMeasureDirtyOrMeasureDirtyPath &&
						!root.IsArrangeDirtyOrArrangeDirtyPath &&
						!eventManager.HasPendingViewportChangedEvents)
					{
						if (tracingThisCall)
						{
							// Avoid setting _traceLayoutCycle to false for re-entrant calls in case it happens.
							_traceLayoutCycle = false;
						}

						return;
					}
				}
#else
				else
				{
					if (tracingThisCall)
					{
						// Avoid setting _traceLayoutCycle to false for re-entrant calls in case it happens.
						_traceLayoutCycle = false;
					}

					return;
				}
#endif
			}

			if (tracingThisCall)
			{
				// Avoid setting _traceLayoutCycle to false for re-entrant calls in case it happens.
				_traceLayoutCycle = false;
			}

			throw new InvalidOperationException("Layout cycle detected. For more information, see https://aka.platform.uno/layout-cycle");
#endif
		}

		internal void ApplyClip()
		{
#if __SKIA__
			// On Skia specifically, we separate the two types of clipping.
			// First, from Clip DP (handled in this code path)
			// That clipping propagates to Visual.Clip through ApplyNativeClip.
			// Second is clipping calculated from FrameworkElement.GetClipRect during arrange.
			// That clipping propagates to ViewBox during arrange.
			var clip = Clip;
			if (clip is null)
			{
				ApplyNativeClip(Rect.Empty, transform: null);
			}
			else
			{
				ApplyNativeClip(clip.Rect, clip.Transform);
			}

			OnViewportUpdated();

#elif __WASM__
			InvalidateArrange();
#else
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
#endif
		}

		partial void ApplyNativeClip(Rect rect
#if __SKIA__
			, Transform transform
#endif
			);

		private protected virtual void OnViewportUpdated(
#if !__SKIA__
			Rect viewport
#endif
			) // Not "Changed" as it might be the same as previous
		{
#if !__SKIA__
			// If not clipped, we consider the viewport as infinite.
			Viewport = viewport.IsEmpty ? Rect.Infinite : viewport;
#endif
		}

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
			var index = s.IndexOf('|');

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
		/// This is the 'finalRect' of the last Arrange. However, this doesn't affect clipping (even for children).
		/// </summary>
		/// <remarks>This is expressed in parent's coordinate space.</remarks>
		internal Rect LayoutSlotWithMarginsAndAlignments { get; set; }

		internal bool NeedsClipToSlot { get; set; }

		/// <summary>
		/// Backing property for <see cref="LayoutInformation.GetDesiredSize(UIElement)"/>
		/// </summary>
		Size IUIElement.DesiredSize { get; set; }

		private Size _size;

		/// <summary>
		/// Provides the size reported during the last call to Arrange (i.e. the ActualSize)
		/// </summary>
		public Size RenderSize
		{
			get => Visibility == Visibility.Collapsed ? new Size() : _size;
			internal set
			{
				global::System.Diagnostics.Debug.Assert(value.Width >= 0, $"Invalid width ({value.Width})");
				global::System.Diagnostics.Debug.Assert(value.Height >= 0, $"Invalid height ({value.Height})");
				var previousSize = _size;
				_size = value;
				if (_size != previousSize)
				{
					if (this is FrameworkElement frameworkElement)
					{
						frameworkElement.SetActualSize(_size);
#if !UNO_HAS_ENHANCED_LIFECYCLE // Handled by EventManager with enhanced lifecycle.
						frameworkElement.RaiseSizeChanged(new SizeChangedEventArgs(this, previousSize, _size));
#endif
					}
				}
			}
		}

#if !UNO_REFERENCE_API
		/// <summary>
		/// Provides the size reported during the last call to Measure.
		/// </summary>
		/// <remarks>
		/// DesiredSize INCLUDES MARGINS.
		/// </remarks>
		public Size DesiredSize => ((IUIElement)this).DesiredSize;


#if !UNO_REFERENCE_API
		/// <summary>
		/// This is the Frame that should be used as "available Size" for the Arrange phase.
		/// </summary>
		internal Rect? ClippedFrame;
#endif

		/// <summary>
		/// Updates the DesiredSize of a UIElement. Typically, objects that implement custom layout for their
		/// layout children call this method from their own MeasureOverride implementations to form a recursive layout update.
		/// </summary>
		/// <param name="availableSize">
		/// The available space that a parent can allocate to a child object. A child object can request a larger
		/// space than what is available; the provided size might be accommodated if scrolling or other resize behavior is
		/// possible in that particular container.
		/// </param>
		/// <returns>The measured size.</returns>
		/// <remarks>
		/// Under Uno.UI, this method should not be called during the normal layouting phase. Instead, use the
		/// <see cref="MeasureElement(View, Size)"/> methods, which handles native view properly.
		/// </remarks>
		public void Measure(Size availableSize)
		{
#if !UNO_REFERENCE_API
			if (this is not FrameworkElement fwe)
			{
				return;
			}

			if (double.IsNaN(availableSize.Width) || double.IsNaN(availableSize.Height))
			{
				throw new InvalidOperationException($"Cannot measure [{GetType()}] with NaN");
			}

			((ILayouterElement)fwe).Layouter.Measure(availableSize);
#if IS_UNIT_TESTS
			OnMeasurePartial(availableSize);
#endif
#endif
		}

#if IS_UNIT_TESTS
		partial void OnMeasurePartial(Size slotSize);
#endif

		/// <summary>
		/// Positions child objects and determines a size for a UIElement. Parent objects that implement custom layout
		/// for their child elements should call this method from their layout override implementations to form a recursive layout update.
		/// </summary>
		/// <param name="finalRect">The final size that the parent computes for the child in layout, provided as a <see cref="Windows.Foundation.Rect"/> value.</param>
		public void Arrange(Rect finalRect)
		{
#if !UNO_REFERENCE_API
			if (this is not FrameworkElement fwe)
			{
				return;
			}

			var layouter = ((ILayouterElement)fwe).Layouter;
			layouter.Arrange(finalRect.DeflateBy(fwe.Margin));
			layouter.ArrangeChild(fwe, finalRect);
#endif
		}

		public void InvalidateMeasure()
		{
#if __ANDROID__
			// Use a non-virtual version of the RequestLayout method, for performance.
			base.RequestLayout();
			SetLayoutFlags(LayoutFlag.MeasureDirty);
#elif __IOS__
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

		/// <summary>
		/// This method has to be invoked for elements that are going to be recycled WITHOUT necessarily being unloaded / loaded.
		/// For instance, this is not expected to be invoked for elements recycled by the template pool as they are always unloaded.
		/// The main use case is for ListView and is expected to be invoked by ListView.CleanUpContainer.
		/// </summary>
		/// <remarks>This will walk the tree down to invoke this on all children!</remarks>
		internal static void PrepareForRecycle(object view)
		{
			if (view is UIElement elt)
			{
				elt.PrepareForRecycle();
			}
			else
			{
				foreach (var child in VisualTreeHelper.GetManagedVisualChildren(view))
				{
					child.PrepareForRecycle();
				}
			}
		}

		/// <summary>
		/// This method has to be invoked on elements that are going to be recycled WITHOUT necessarily being unloaded / loaded.
		/// For instance, this is not expected to be invoked for elements recycled by the template pool as they are always unloaded.
		/// The main use case is for ListView and is expected to be invoked by ListView.CleanUpContainer.
		/// </summary>
		/// <remarks>This will walk the tree down to invoke this on all children!</remarks>
		internal virtual void PrepareForRecycle()
		{
			ClearPointerStateOnRecycle();

			foreach (var child in VisualTreeHelper.GetManagedVisualChildren(this))
			{
				child.PrepareForRecycle();
			}
		}

		private partial void ClearPointerStateOnRecycle();

		internal virtual bool IsViewHit() => true;

		internal virtual bool IsEnabledOverride() => true;

		internal bool GetUseLayoutRounding()
		{
#if __SKIA__
			return UseLayoutRounding;
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

		private static double LayoutRound(double value, double scaleFactor)
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
		internal double GetScaleFactorForLayoutRounding() => RootScale.GetRasterizationScaleForElement(this);

		private static double XcpRound(double x)
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

		// This depends on the implementation of ICorePointerInputSource.PointerCursor.
		/// <summary>
		/// Gets or sets the cursor that displays when the pointer is over this element. Defaults to null, indicating no change to the cursor.
		/// </summary>
#if HAS_UNO_WINUI
		protected InputCursor ProtectedCursor
#else
		private protected Microsoft.UI.Input.InputCursor ProtectedCursor
#endif
		{
			get => _protectedCursor;
			set
			{
				_protectedCursor = value;
				// On WinUI, a disposed InputCursor causes the cursor to be hidden. The ProtectedCursor isn't cleared.
				// The the InputCursor is disposed while the cursor is currently inside the UIElement, the cursor is only
				// hidden when the cursor moves. Our implementation matches this.
				if (value is { IsDisposed: true })
				{
					CalculatedFinalCursor = null;
				}
				else if (value is InputSystemCursor c)
				{
					CalculatedFinalCursor = c.CursorShape;
				}
				else if (value is null)
				{
					this.ClearValue(CalculatedFinalCursorProperty, DependencyPropertyValuePrecedences.Local);
				}
				else
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error($"Setting UIElement.ProtectedCursor to value of type {value.GetType().FullName} is not supported. Only Values of type {nameof(InputSystemCursor)} are currently supported.");
					}
				}

				if (value is { } cursor)
				{
					_disposedEventDisposable.Disposable = cursor.RegisterDisposedEvent((_, _) =>
					{
						CalculatedFinalCursor = null;
						_disposedEventDisposable.Disposable?.Dispose();
					});
				}
			}
		}

		internal void SetProtectedCursor(Microsoft /* UWP don't rename */.UI.Input.InputCursor cursor)
		{
			ProtectedCursor = cursor;
		}

		/// <summary>
		/// This event is not yet implemented in Uno Platform.
		/// </summary>
		/// <remarks>
		/// The code was moved here to override the LogLevel.
		/// </remarks>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public event global::Windows.Foundation.TypedEventHandler<global::Microsoft.UI.Xaml.UIElement, global::Microsoft.UI.Xaml.Input.AccessKeyInvokedEventArgs> AccessKeyInvoked
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.UIElement", "event TypedEventHandler<UIElement, AccessKeyInvokedEventArgs> UIElement.AccessKeyInvoked", LogLevel.Debug);
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.UIElement", "event TypedEventHandler<UIElement, AccessKeyInvokedEventArgs> UIElement.AccessKeyInvoked", LogLevel.Debug);
			}
		}
	}
}
