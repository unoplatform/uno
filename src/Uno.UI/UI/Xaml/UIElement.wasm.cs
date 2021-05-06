using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Uno.Collections;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Logging;
using Uno.UI;
using Uno.UI.Extensions;
using Uno.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.System;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Uno.Core.Comparison;
using Uno.Foundation.Runtime.WebAssembly.Interop;

namespace Windows.UI.Xaml
{
	public partial class UIElement : DependencyObject
	{
		internal const string DefaultHtmlTag = "div";

		private readonly GCHandle _gcHandle;

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

		public Size MeasureView(Size availableSize)
		{
			return Uno.UI.Xaml.WindowManagerInterop.MeasureView(HtmlId, availableSize);
		}

		internal Rect GetBBox()
		{
			if (!HtmlTagIsSvg)
			{
				throw new InvalidOperationException("GetBBox is available only for SVG elements.");
			}

			return Uno.UI.Xaml.WindowManagerInterop.GetBBox(HtmlId);
		}

		private Rect GetBoundingClientRect()
		{
			var sizeString = WebAssemblyRuntime.InvokeJS("Uno.UI.WindowManager.current.getBoundingClientRect(" + HtmlId + ");");
			var sizeParts = sizeString.Split(';');
			return new Rect(double.Parse(sizeParts[0]), double.Parse(sizeParts[1]), double.Parse(sizeParts[2]), double.Parse(sizeParts[3]));
		}

		public UIElement() : this(null, false) { }

		public UIElement(string htmlTag = DefaultHtmlTag) : this(htmlTag, false) { }

		public UIElement(string htmlTag, bool isSvg)
		{
			_log = this.Log();
			_logDebug = _log.IsEnabled(LogLevel.Debug) ? _log : null;

			Initialize();

			_gcHandle = GCHandle.Alloc(this, GCHandleType.Weak);
			_isFrameworkElement = this is FrameworkElement;

			HtmlTag = GetHtmlTag(htmlTag);
			HtmlTagIsSvg = isSvg;

			var type = GetType();

			Handle = GCHandle.ToIntPtr(_gcHandle);
			HtmlId = Handle;

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

		private static Dictionary<Type, string> _htmlTagCache = new Dictionary<Type, string>(FastTypeComparer.Default);
		private static Type _htmlElementAttribute;
		private static PropertyInfo _htmlTagAttributeTagGetter;
		private static readonly Assembly _unoUIAssembly = typeof(UIElement).Assembly;

		private string GetHtmlTag(string htmlTag)
		{
			var currentType = GetType();

			if (currentType.Assembly != _unoUIAssembly)
			{
				if (_htmlElementAttribute == null)
				{
					_htmlElementAttribute = GetUnoUIRuntimeWebAssembly().GetType("Uno.UI.Runtime.WebAssembly.HtmlElementAttribute", true);
					_htmlTagAttributeTagGetter = _htmlElementAttribute.GetProperty("Tag");
				}

				if (!_htmlTagCache.TryGetValue(currentType, out var htmlTagOverride))
				{
					// Set the tag from the internal explicit UIElement parameter
					htmlTagOverride = htmlTag;

					if (currentType.GetCustomAttribute(_htmlElementAttribute, true) is Attribute attr)
					{
						_htmlTagCache[currentType] = htmlTagOverride = _htmlTagAttributeTagGetter.GetValue(attr, Array.Empty<object>()) as string;
					}

					_htmlTagCache[currentType] = htmlTagOverride;
				}

				return htmlTagOverride;
			}
			else
			{
				return htmlTag;
			}
		}

		~UIElement()
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Collecting UIElement for [{HtmlId}]");
			}

			Cleanup();

			Uno.UI.Xaml.WindowManagerInterop.DestroyView(HtmlId);

			_gcHandle.Free();
		}

		public IntPtr Handle { get; }

		public IntPtr HtmlId { get; }

		public string HtmlTag { get; }

		public bool HtmlTagIsSvg { get; }

		protected internal void SetStyle(string name, string value)
		{
			Uno.UI.Xaml.WindowManagerInterop.SetStyles(HtmlId, new[] { (name, value) });
		}

		protected internal void SetStyle(string name, double value)
		{
			Uno.UI.Xaml.WindowManagerInterop.SetStyleDouble(HtmlId, name, value);
		}

		protected internal void SetStyle(params (string name, string value)[] styles)
		{
			if (styles == null || styles.Length == 0)
			{
				return; // nothing to do
			}

			Uno.UI.Xaml.WindowManagerInterop.SetStyles(HtmlId, styles);
		}

		/// <summary>
		/// Add/Set CSS classes to the HTML element.
		/// </summary>
		/// <remarks>
		/// No effect for classes already present on the element.
		/// </remarks>
		protected internal void SetCssClasses(params string[] classesToSet)
		{
			Uno.UI.Xaml.WindowManagerInterop.SetUnsetCssClasses(HtmlId, classesToSet, null);
		}

		/// <summary>
		/// Remove/Unset CSS classes to the HTML element.
		/// </summary>
		/// <remarks>
		/// No effect for classes already absent from the element.
		/// </remarks>
		protected internal void UnsetCssClasses(params string[] classesToUnset)
		{
			Uno.UI.Xaml.WindowManagerInterop.SetUnsetCssClasses(HtmlId, null, classesToUnset);

		}

		/// <summary>
		/// Set and Unset css classes on a HTML element in a single operation.
		/// </summary>
		/// <remarks>
		/// Identical to calling <see cref="SetCssClasses"/> followed by <see cref="UnsetCssClasses"/>.
		/// </remarks>
		protected internal void SetUnsetCssClasses(string[] classesToSet, string[] classesToUnset)
		{
			Uno.UI.Xaml.WindowManagerInterop.SetUnsetCssClasses(HtmlId, classesToSet, classesToUnset);
		}

		/// <summary>
		/// Set a specified CSS class to an element from a set of possible values.
		/// All other possible values will be removed from the element.
		/// </summary>
		/// <param name="cssClasses">All possible class values</param>
		/// <param name="index">The index of the value to set (-1: unset)</param>
		protected internal void SetClasses(string[] cssClasses, int index = -1)
		{
			Uno.UI.Xaml.WindowManagerInterop.SetClasses(HtmlId, cssClasses, index);
		}

#if DEBUG
		private long _arrangeCount = 0;
#endif

		/// <summary>
		/// Natively arranges and clips an element.
		/// </summary>
		/// <param name="rect">The dimensions to apply to the element</param>
		/// <param name="clipRect">The Clip rect to set, if any</param>
		protected internal void ArrangeVisual(Rect rect, Rect? clipRect)
		{
			LayoutSlotWithMarginsAndAlignments =
				VisualTreeHelper.GetParent(this) is UIElement parent
					? rect.DeflateBy(parent.GetBorderThickness())
					: rect;

			if (FeatureConfiguration.UIElement.AssignDOMXamlProperties)
			{
				UpdateDOMXamlProperty(nameof(LayoutSlotWithMarginsAndAlignments), LayoutSlotWithMarginsAndAlignments);
			}

			if (Visibility == Visibility.Collapsed)
			{
				// cf. OnVisibilityChanged
				rect.X = rect.Y = -100000;
			}

			Uno.UI.Xaml.WindowManagerInterop.ArrangeElement(HtmlId, rect, clipRect);
			OnViewportUpdated(clipRect ?? Rect.Empty);

#if DEBUG
			var count = ++_arrangeCount;

			SetAttribute(("xamlArrangeCount", count.ToString()));
#endif
		}

		protected internal void SetNativeTransform(Matrix3x2 matrix)
		{
			Uno.UI.Xaml.WindowManagerInterop.SetElementTransform(HtmlId, matrix);
		}

		protected internal void ResetStyle(params string[] names)
		{
			Uno.UI.Xaml.WindowManagerInterop.ResetStyle(HtmlId, names);

		}

		protected internal void SetAttribute(string name, string value)
		{
			Uno.UI.Xaml.WindowManagerInterop.SetAttribute(HtmlId, name, value);
		}

		protected internal void RemoveAttribute(string name)
		{
			Uno.UI.Xaml.WindowManagerInterop.RemoveAttribute(HtmlId, name);
		}

		protected internal void SetAttribute(params (string name, string value)[] attributes)
		{
			if (attributes == null || attributes.Length == 0)
			{
				return; // nothing to do
			}

			Uno.UI.Xaml.WindowManagerInterop.SetAttributes(HtmlId, attributes);
		}

		protected internal string GetAttribute(string name)
		{
			var command = "Uno.UI.WindowManager.current.getAttribute(" + HtmlId + ", \"" + name + "\");";
			return WebAssemblyRuntime.InvokeJS(command);
		}

		protected internal void SetProperty(string name, string value)
			=> SetProperty((name, value));

		protected internal void SetProperty(params (string name, string value)[] properties)
		{
			if (properties == null || properties.Length == 0)
			{
				return; // nothing to do
			}

			Uno.UI.Xaml.WindowManagerInterop.SetProperty(HtmlId, properties);
		}

		protected internal string GetProperty(string name)
		{
			var command = "Uno.UI.WindowManager.current.getProperty(" + HtmlId + ", \"" + name + "\");";
			return WebAssemblyRuntime.InvokeJS(command);
		}

		protected internal void SetHtmlContent(string html)
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

		internal static UIElement GetElementFromHandle(int handle)
		{
			var gcHandle = GCHandle.FromIntPtr((IntPtr)handle);

			if (gcHandle.IsAllocated && gcHandle.Target is UIElement element)
			{
				return element;
			}

			return null;
		}

		private Rect _arranged;
		private string _name;

		public string Name
		{
			get => _name;
			set
			{
				_name = value;

				if (FeatureConfiguration.UIElement.AssignDOMXamlName)
				{
					Uno.UI.Xaml.WindowManagerInterop.SetName(HtmlId, _name);
				}
			}
		}

		partial void OnUidChangedPartial()
		{
			if (FeatureConfiguration.UIElement.AssignDOMXamlName)
			{
				Uno.UI.Xaml.WindowManagerInterop.SetXUid(HtmlId, _uid);
			}
		}

		public int MeasureCallCount { get; protected set; }
		public int ArrangeCallCount { get; protected set; }

		public Size? RequestedDesiredSize { get; set; }
		public Size AvailableMeasureSize { get; protected set; }

		public Rect Arranged
		{
			get => _arranged;
			set
			{
				ArrangeCallCount++;
				_arranged = value;
			}
		}

		public Func<Size, Size> DesiredSizeSelector { get; set; }

		protected virtual void OnVisibilityChanged(Visibility oldValue, Visibility newVisibility)
		{
			InvalidateMeasure();
			UpdateHitTest();

			WindowManagerInterop.SetVisibility(HtmlId, newVisibility == Visibility.Visible);

			if (FeatureConfiguration.UIElement.AssignDOMXamlProperties)
			{
				UpdateDOMProperties();
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
		}

		public override string ToString()
		{
			if (FeatureConfiguration.UIElement.RenderToStringWithId)
			{
				return GetType().Name + "-" + HtmlId;
			}

			return base.ToString();
		}

		public UIElement FindFirstChild() => _children.FirstOrDefault();

		public virtual IEnumerable<UIElement> GetChildren() => _children;

		public void AddChild(UIElement child, int? index = null)
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

			OnAddingChild(child);

			if (index is { } i)
			{
				_children.Insert(i, child);
			}
			else
			{
				_children.Add(child);
			}

			Uno.UI.Xaml.WindowManagerInterop.AddView(HtmlId, child.HtmlId, index);

			OnChildAdded(child);

			child.InvalidateMeasure();

			// Arrange is required to unset the uno-unarranged CSS class
			child.InvalidateArrange();
		}

		public void ClearChildren()
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

		private void Cleanup()
		{
			if (this.GetParent() is UIElement originalParent)
			{
				originalParent.RemoveChild(this);
			}

			if (this is Windows.UI.Xaml.Controls.Panel panel)
			{
				panel.Children.Clear();
			}
			else
			{
				for (var i = 0; i < _children.Count; i++)
				{
					RemoveNativeView(_children[i]);
				}

				_children.Clear();
			}
		}

		public bool RemoveChild(UIElement child)
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
			if (routedEvent.Flag == RoutedEventFlag.KeyDown)
			{
				domEventName = "keydown";
			}
			else
			{
				domEventName = routedEvent.Flag == RoutedEventFlag.KeyUp
					? "keyup"
					: throw new ArgumentOutOfRangeException(nameof(routedEvent), "Not a keyboard event");
			}

			RegisterEventHandler(
				domEventName,
				handler: new RoutedEventHandlerWithHandled((snd, args) => RaiseEvent(routedEvent, args)),
				invoker: GenericEventHandlers.RaiseRoutedEventHandlerWithHandled,
				onCapturePhase: false,
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
			return new KeyRoutedEventArgs(src, VirtualKeyHelper.FromKey(payload)) {CanBubbleNatively = true};
		}

		private static RoutedEventArgs PayloadToFocusArgs(object src, string payload)
		{
			if (int.TryParse(payload, out int xamlHandle))
			{
				if (GetElementFromHandle(xamlHandle) is UIElement element)
				{
					return new RoutedEventArgs(element);
				}
			}

			return new RoutedEventArgs(src);
		}

		private static Assembly GetUnoUIRuntimeWebAssembly()
		{
			const string UnoUIRuntimeWebAssemblyName = "Uno.UI.Runtime.WebAssembly";

			if (PlatformHelper.IsNetCore)
			{
				// .NET Core fails to load assemblies property because of ALC issues: https://github.com/dotnet/runtime/issues/44269
				return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == UnoUIRuntimeWebAssemblyName)
					?? throw new InvalidOperationException($"Unable to find {UnoUIRuntimeWebAssemblyName} in the loaded assemblies");
			}
			else
			{
				return Assembly.Load(UnoUIRuntimeWebAssemblyName);
			}
		}
	}
}
