using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Uno.Collections;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Extensions;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Core;
using Windows.UI.Xaml.Controls;
using Windows.System;
using Color = Windows.UI.Color;
using Microsoft.UI.Input;

namespace Windows.UI.Xaml
{
	public partial class UIElement : DependencyObject
	{
		internal const string DefaultHtmlTag = "div";

		private readonly GCHandle _gcHandle;
		private readonly WasmConfig _wasmConfig;

		/// <summary>
		/// Config flags for UIElement specific to WASM platform
		/// </summary>
		[Flags]
		private enum WasmConfig : int
		{
			IsExternalElement = 1,
			IsSvg = 1 << 1,
		}

		public UIElement() : this(null, false) { }

		internal UIElement(string htmlTag = DefaultHtmlTag) : this(htmlTag, false) { }

		internal UIElement(string htmlTag, bool isSvg)
		{
			Initialize();

			_gcHandle = GCHandle.Alloc(this, GCHandleType.Weak);
			_isFrameworkElement = this is FrameworkElement;

			var type = GetType();
			var tag = HtmlElementHelper.GetHtmlTag(type, htmlTag);

			HtmlTag = tag.Name;
			Handle = GCHandle.ToIntPtr(_gcHandle);
			HtmlId = Handle;
			if (isSvg)
			{
				_wasmConfig |= WasmConfig.IsSvg;
			}
			if (tag.IsExternallyDefined)
			{
				_wasmConfig |= WasmConfig.IsExternalElement;
			}

			Uno.UI.Xaml.WindowManagerInterop.CreateContent(
				htmlId: HtmlId,
				htmlTag: HtmlTag,
				handle: Handle,
				uiElementRegistrationId: UIElementNativeRegistrar.GetForType(type),
				htmlTagIsSvg: HtmlTagIsSvg,
				isFocusable: false
			);

			InitializePointers();
			UpdateHitTest();
		}

		~UIElement()
		{
			try
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Collecting UIElement for [{HtmlId}]");
				}

				Uno.UI.Xaml.WindowManagerInterop.DestroyView(HtmlId);
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Warning))
				{
					this.Log().Warn($"Collection of UIElement for [{HtmlId}] failed", e);
				}
			}

			_gcHandle.Free();
		}

		public IntPtr Handle { get; }

		internal IntPtr HtmlId { get; }

		internal string HtmlTag { get; }

		internal bool HtmlTagIsSvg => _wasmConfig.HasFlag(WasmConfig.IsSvg);

		internal bool HtmlTagIsExternallyDefined => _wasmConfig.HasFlag(WasmConfig.IsExternalElement);

		internal Size MeasureView(Size availableSize, bool measureContent = true)
		{
			return Uno.UI.Xaml.WindowManagerInterop.MeasureView(HtmlId, availableSize, measureContent);
		}

		internal Rect GetBBox()
		{
			if (!HtmlTagIsSvg)
			{
				throw new InvalidOperationException("GetBBox is available only for SVG elements.");
			}

			return Uno.UI.Xaml.WindowManagerInterop.GetBBox(HtmlId);
		}

		internal void SetStyle(string name, string value)
		{
			Uno.UI.Xaml.WindowManagerInterop.SetStyleString(HtmlId, name, value);
		}

		internal void SetStyle(string name, double value)
		{
			Uno.UI.Xaml.WindowManagerInterop.SetStyleDouble(HtmlId, name, value);
		}

		internal void SetStyle(params (string name, string value)[] styles)
		{
			if (styles == null || styles.Length == 0)
			{
				return; // nothing to do
			}

			Uno.UI.Xaml.WindowManagerInterop.SetStyles(HtmlId, styles);
		}

		internal void SetSelectionHighlight(Color backgroundColor, Color foregroundColor)
		{
			Uno.UI.Xaml.WindowManagerInterop.SetSelectionHighlight(HtmlId, backgroundColor, foregroundColor);
		}

		internal void UnsetSelectionHighlight()
		{
			UnsetCssClasses("selection-highlight");
		}

		/// <summary>
		/// Add/Set CSS classes to the HTML element.
		/// </summary>
		/// <remarks>
		/// No effect for classes already present on the element.
		/// </remarks>
		internal void SetCssClasses(params string[] classesToSet)
		{
			Uno.UI.Xaml.WindowManagerInterop.SetUnsetCssClasses(HtmlId, classesToSet, null);
		}

		/// <summary>
		/// Remove/Unset CSS classes to the HTML element.
		/// </summary>
		/// <remarks>
		/// No effect for classes already absent from the element.
		/// </remarks>
		internal void UnsetCssClasses(params string[] classesToUnset)
		{
			Uno.UI.Xaml.WindowManagerInterop.SetUnsetCssClasses(HtmlId, null, classesToUnset);

		}

		/// <summary>
		/// Set and Unset css classes on a HTML element in a single operation.
		/// </summary>
		/// <remarks>
		/// Identical to calling <see cref="SetCssClasses"/> followed by <see cref="UnsetCssClasses"/>.
		/// </remarks>
		internal void SetUnsetCssClasses(string[] classesToSet, string[] classesToUnset)
		{
			Uno.UI.Xaml.WindowManagerInterop.SetUnsetCssClasses(HtmlId, classesToSet, classesToUnset);
		}

		/// <summary>
		/// Set a specified CSS class to an element from a set of possible values.
		/// All other possible values will be removed from the element.
		/// </summary>
		/// <param name="cssClasses">All possible class values</param>
		/// <param name="index">The index of the value to set (-1: unset)</param>
		internal void SetClasses(string[] cssClasses, int index = -1)
		{
			Uno.UI.Xaml.WindowManagerInterop.SetClasses(HtmlId, cssClasses, index);
		}

#if DEBUG
		private long _arrangeCount;
#endif

		/// <summary>
		/// Natively arranges and clips an element.
		/// </summary>
		/// <param name="rect">The dimensions to apply to the element</param>
		/// <param name="clipRect">The Clip rect to set, if any</param>
		internal void ArrangeVisual(Rect rect, Rect? clipRect)
		{
			LayoutSlotWithMarginsAndAlignments = rect;

			if (FeatureConfiguration.UIElement.AssignDOMXamlProperties)
			{
				UpdateDOMXamlProperty(nameof(LayoutSlotWithMarginsAndAlignments), LayoutSlotWithMarginsAndAlignments);
			}

			if (Visibility == Visibility.Collapsed)
			{
				// cf. OnVisibilityChanged
				rect.X = rect.Y = -100000;
			}
			else if (VisualTreeHelper.GetParent(this) is FrameworkElement parent)
			{
				// HTML moves the origin along with the border thickness.
				// Adjust this element based on this its parent border thickness.
				_ = parent.TryGetActualBorderThickness(out var adjust);
				rect.X = rect.X - adjust.Left;
				rect.Y = rect.Y - adjust.Top;
			}

			Uno.UI.Xaml.WindowManagerInterop.ArrangeElement(HtmlId, rect, clipRect);
			OnViewportUpdated(clipRect ?? Rect.Empty);

#if DEBUG
			var count = ++_arrangeCount;

			SetAttribute(("xamlArrangeCount", count.ToString(CultureInfo.InvariantCulture)));
#endif
		}

		internal void SetNativeTransform(Matrix3x2 matrix)
		{
			Uno.UI.Xaml.WindowManagerInterop.SetElementTransform(HtmlId, matrix);
		}

		internal void ResetStyle(params string[] names)
		{
			Uno.UI.Xaml.WindowManagerInterop.ResetStyle(HtmlId, names);

		}

		internal void SetAttribute(string name, string value)
		{
			Uno.UI.Xaml.WindowManagerInterop.SetAttribute(HtmlId, name, value);
		}

		internal void RemoveAttribute(string name)
		{
			Uno.UI.Xaml.WindowManagerInterop.RemoveAttribute(HtmlId, name);
		}

		internal void SetAttribute(params (string name, string value)[] attributes)
		{
			if (attributes == null || attributes.Length == 0)
			{
				return; // nothing to do
			}

			Uno.UI.Xaml.WindowManagerInterop.SetAttributes(HtmlId, attributes);
		}

		internal string GetAttribute(string name)
		{
			return WindowManagerInterop.GetAttribute(HtmlId, name);
		}

		internal void SetProperty(string name, string value)
			=> Uno.UI.Xaml.WindowManagerInterop.SetProperty(HtmlId, name, value);

		internal void SetProperty(params (string name, string value)[] properties)
		{
			if (properties == null || properties.Length == 0)
			{
				return; // nothing to do
			}

			Uno.UI.Xaml.WindowManagerInterop.SetProperty(HtmlId, properties);
		}

		internal string GetProperty(string name)
		{
			return WindowManagerInterop.GetProperty(HtmlId, name);
		}

		internal void SetHtmlContent(string html)
		{
			Uno.UI.Xaml.WindowManagerInterop.SetContentHtml(HtmlId, html);
		}

		partial void ApplyNativeClip(Rect rect)
		{
			if (rect.IsEmpty)
			{
				ResetStyle("clip");
				return;
			}

			var width = double.IsInfinity(rect.Width) ? 100000.0f : rect.Width;
			var height = double.IsInfinity(rect.Height) ? 100000.0f : rect.Height;

			SetStyle(
				"clip",
				string.Concat(
					"rect(",
					Math.Floor(rect.Y).ToStringInvariant(), "px,",
					Math.Ceiling(rect.X + width).ToStringInvariant(), "px,",
					Math.Ceiling(rect.Y + height).ToStringInvariant(), "px,",
					Math.Floor(rect.X).ToStringInvariant(), "px)"
				)
			);
		}

		internal static UIElement GetElementFromHandle(IntPtr handle)
		{
			if (handle != IntPtr.Zero)
			{
				var gcHandle = GCHandle.FromIntPtr(handle);

				if (gcHandle.IsAllocated && gcHandle.Target is UIElement element)
				{
					return element;
				}
			}
			else
			{
				if (typeof(UIElement).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					typeof(UIElement).Log().Debug($"Unable to get element handle for uninitialized handle");
				}
			}

			return null;
		}

		partial void OnUidChangedPartial()
		{
			if (FeatureConfiguration.UIElement.AssignDOMXamlName)
			{
				Uno.UI.Xaml.WindowManagerInterop.SetXUid(HtmlId, _uid);
			}
		}

		partial void OnVisibilityChangedPartial(Visibility oldValue, Visibility newValue)
		{
			InvalidateMeasure();
			UpdateHitTest();

			WindowManagerInterop.SetVisibility(HtmlId, newValue == Visibility.Visible);

			if (FeatureConfiguration.UIElement.AssignDOMXamlProperties)
			{
				UpdateDOMProperties();
			}

			if (IsLoaded && FeatureConfiguration.UIElement.UseInvalidateMeasurePath && this.GetParent() is UIElement parent)
			{
				// Need to invalidate the parent when the visibility changes to ensure its
				// algorithm is doing its layout properly.
				parent.InvalidateMeasure();
			}
		}

		partial void OnOpacityChanged(DependencyPropertyChangedEventArgs args)
		{
			var opacity = Opacity;

			if (opacity >= 1)
			{
				ResetStyle("opacity");
			}
			else
			{
				SetStyle("opacity", opacity);
			}
		}

		partial void OnIsHitTestVisibleChangedPartial(bool oldValue, bool newValue)
		{
			UpdateHitTest();

			if (FeatureConfiguration.UIElement.AssignDOMXamlProperties)
			{
				UpdateDOMProperties();
			}

			if (IsLoaded && FeatureConfiguration.UIElement.UseInvalidateMeasurePath && this.GetParent() is UIElement parent)
			{
				// Need to invalidate the parent when the visibility changes to ensure its
				// algorithm is doing its layout properly.
				parent.InvalidateMeasure();
			}
		}

		public override string ToString()
		{
			if (FeatureConfiguration.UIElement.RenderToStringWithId)
			{
				return GetType().Name + "-" + HtmlId;
			}

			return base.ToString();
		}

		internal UIElement FindFirstChild() => _children.FirstOrDefault();

		internal MaterializableList<UIElement> GetChildren() => _children;

		internal void AddChild(UIElement child, int? index = null)
		{
			if (child == null)
			{
				return;
			}

			var currentParent = child.GetParent() as UIElement;

			// Remove child from current parent, if any
			if (currentParent != this && currentParent != null)
			{
				// ---IMPORTANT---
				// This behavior is different than UWP:
				// On UWP the behavior would be to throw an "Element already has a logical parent" exception.

				// It is done here to align Wasm with Android and iOS where the control is
				// simply "moved" when attached to another parent.

				// This could lead to "child kidnapping", like the one happening in ComboBox & ComboBoxItem

				this.Log().Info($"{this}.AddChild({child}): Removing child {child} from its current parent {currentParent}.");
				currentParent.RemoveChild(child);
			}

			child.SetParent(this);

			if (index is { } i)
			{
				_children.Insert(i, child);
			}
			else
			{
				_children.Add(child);
			}

			Uno.UI.Xaml.WindowManagerInterop.AddView(HtmlId, child.HtmlId, index);

			var enterParams = new EnterParams(IsActiveInVisualTree);
			ChildEnter(child, enterParams);

			OnChildAdded(child);

			child.ResetLayoutFlags();

			if (IsMeasureDirtyPathDisabled)
			{
				FrameworkElementHelper.SetUseMeasurePathDisabled(child, eager: true, invalidate: true);
			}
			else
			{
				child.InvalidateMeasure();
			}

			// Arrange is required to unset the uno-unarranged CSS class
			child.InvalidateArrange();

			if (child.IsArrangeDirty && !IsArrangeDirty)
			{
				InvalidateArrange();
			}

			// Force a new measure of this element (the parent of the new child)
			InvalidateMeasure();
		}

		internal void ClearChildren()
		{
			for (var i = 0; i < _children.Count; i++)
			{
				var child = _children[i];

				RemoveNativeView(child);
				OnChildRemoved(child);
			}

			_children.Clear();
			InvalidateMeasure();
		}

		private void RemoveNativeView(UIElement child)
		{
			var childParent = child.GetParent();

			child.SetParent(null);

			// The parent may already be null if the parent has already been collected.
			// In such case, there is no need to remove the child from its parent in the DOM.
			if (childParent != null)
			{
				Uno.UI.Xaml.WindowManagerInterop.RemoveView(HtmlId, child.HtmlId);
			}
		}

		internal bool RemoveChild(UIElement child)
		{
			if (child != null && _children.Remove(child))
			{
				child.SetParent(null);
				Uno.UI.Xaml.WindowManagerInterop.RemoveView(HtmlId, child.HtmlId);

				OnChildRemoved(child);

				InvalidateMeasure();

				return true;
			}

			return false;
		}

		internal UIElement ReplaceChild(int index, UIElement child)
		{
			var previous = _children[index];
			RemoveChild(previous);
			AddChild(child, index);
			return previous;
		}

		internal void MoveChildTo(int oldIndex, int newIndex)
		{
			var view = _children[oldIndex];

			_children.RemoveAt(oldIndex);
			if (newIndex == _children.Count)
			{
				_children.Add(view);
			}
			else
			{
				_children.Insert(newIndex, view);
			}

			Uno.UI.Xaml.WindowManagerInterop.AddView(HtmlId, view.HtmlId, newIndex);

			InvalidateMeasure();
		}

		// We keep track of registered routed events to avoid registering the same one twice (mainly because RemoveHandler is not implemented)
		private RoutedEventFlag _registeredRoutedEvents;

		partial void AddKeyHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo)
		{
			if (handlersCount != 1
				// We do not remove event handlers for now, so do not rely only on the handlersCount and keep track of registered events
				|| _registeredRoutedEvents.HasFlag(routedEvent.Flag))
			{
				return;
			}
			_registeredRoutedEvents |= routedEvent.Flag;

			string domEventName;
			bool onCapturePhase = false;
			switch (routedEvent.Flag)
			{
				case RoutedEventFlag.PreviewKeyDown:
					domEventName = "keydown";
					onCapturePhase = true;
					break;
				case RoutedEventFlag.KeyDown:
					domEventName = "keydown";
					break;
				case RoutedEventFlag.PreviewKeyUp:
					domEventName = "keyup";
					onCapturePhase = true;
					break;
				case RoutedEventFlag.KeyUp:
					domEventName = "keyup";
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(routedEvent), "Not a keyboard event");
			}

			RegisterEventHandler(
				domEventName,
				handler: new RoutedEventHandlerWithHandled((snd, args) => RaiseEvent(routedEvent, args)),
				invoker: GenericEventHandlers.RaiseRoutedEventHandlerWithHandled,
				onCapturePhase,
				eventExtractor: HtmlEventExtractor.KeyboardEventExtractor,
				payloadConverter: PayloadToKeyArgs
			);
		}

		/// <summary>
		/// If corresponding feature flag is enabled, set layout properties as DOM attributes to aid in debugging.
		/// </summary>
		/// <remarks>
		/// Calls to this method should be wrapped in a check of the feature flag, to avoid the expense of a virtual method call
		/// that will most of the time do nothing in hot code paths.
		/// </remarks>
		private protected virtual void UpdateDOMProperties()
		{
			if (FeatureConfiguration.UIElement.AssignDOMXamlProperties)
			{
				UpdateDOMXamlProperty(nameof(Visibility), Visibility);
				UpdateDOMXamlProperty(nameof(IsHitTestVisible), IsHitTestVisible);
			}
		}

		/// <summary>
		/// Sets a Xaml property as a DOM attribute for debugging.
		/// </summary>
		/// <param name="propertyName">The property's name</param>
		/// <param name="value">The current property value</param>
		internal void UpdateDOMXamlProperty(string propertyName, object value)
		{
			WindowManagerInterop.SetAttribute(HtmlId, "xaml" + propertyName.ToLowerInvariant().Replace('.', '_'), value?.ToString() ?? "[null]");
		}

		private static KeyRoutedEventArgs PayloadToKeyArgs(object src, string payload)
		{
			var key = VirtualKey.None;
			var modifiers = VirtualKeyModifiers.None;
			if (payload.Length > 4)
			{
				// Modifiers are in this order Ctrl, Alt, Windows, Shift
				if (payload[0] == '1')
				{
					modifiers |= VirtualKeyModifiers.Control;
				}
				if (payload[1] == '1')
				{
					modifiers |= VirtualKeyModifiers.Menu;
				}
				if (payload[2] == '1')
				{
					modifiers |= VirtualKeyModifiers.Windows;
				}
				if (payload[3] == '1')
				{
					modifiers |= VirtualKeyModifiers.Shift;
				}

				// Remaining characters are the pressed key
				key = VirtualKeyHelper.FromKey(payload[4..]);

			}
			return new KeyRoutedEventArgs(src, key, modifiers) { CanBubbleNatively = true };
		}

		private static RoutedEventArgs PayloadToFocusArgs(object src, string payload)
		{
			if (int.TryParse(payload, CultureInfo.InvariantCulture, out int xamlHandle))
			{
				if (GetElementFromHandle(xamlHandle) is UIElement element)
				{
					return new RoutedEventArgs(element);
				}
			}

			return new RoutedEventArgs(src);
		}

		private static class UIElementNativeRegistrar
		{
			private static readonly Dictionary<Type, int> _classNames = new Dictionary<Type, int>();

			internal static int GetForType(Type type)
			{
				if (!_classNames.TryGetValue(type, out var classNamesRegistrationId))
				{
					_classNames[type] = classNamesRegistrationId = WindowManagerInterop.RegisterUIElement(type.FullName, GetClassesForType(type).ToArray(), type.Is<FrameworkElement>());
				}

				return classNamesRegistrationId;
			}

			private static IEnumerable<string> GetClassesForType(Type type)
			{
				while (type != null && type != typeof(object))
				{
					yield return type.Name.ToLowerInvariant();
					type = type.BaseType;
				}
			}
		}
	}
}
