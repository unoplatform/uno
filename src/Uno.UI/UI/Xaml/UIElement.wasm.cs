using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using Uno;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Logging;
using Uno.UI.Xaml.Input;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.System;
using Uno.Collections;
using Uno.UI;
using System.Numerics;
using Windows.UI.Input;
using Uno.UI.Xaml;

namespace Windows.UI.Xaml
{
	public partial class UIElement : DependencyObject
	{
		// Even if this a concept of FrameworkElement, the loaded state is handled by the UIElement in order to avoid
		// to cast to FrameworkElement each time a child is added or removed.
		internal bool IsLoaded;

		private readonly GCHandle _gcHandle;
		private readonly bool _isFrameworkElement;


		private static class ClassNames
		{
			private static readonly Dictionary<Type, string[]> _classNames = new Dictionary<Type, string[]>();

			internal static string[] GetForType(Type type)
			{
				if(!_classNames.TryGetValue(type, out var names))
				{
					_classNames[type] = names = GetClassesForType(type).ToArray();
				}

				return names;
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

		public UIElement(string htmlTag = "div", bool isSvg = false)
		{
			_gcHandle = GCHandle.Alloc(this, GCHandleType.Weak);
			_isFrameworkElement = this is FrameworkElement;
			HtmlTag = htmlTag;
			HtmlTagIsSvg = isSvg;

			var type = GetType();

			Handle = GCHandle.ToIntPtr(_gcHandle);
			HtmlId = Handle;


			Uno.UI.Xaml.WindowManagerInterop.CreateContent(
				htmlId: HtmlId,
				htmlTag: HtmlTag,
				handle: Handle,
				fullName: type.FullName,
				htmlTagIsSvg: HtmlTagIsSvg,
				isFrameworkElement: this is FrameworkElement,
				isFocusable: false,
				classes: ClassNames.GetForType(type)
			);

			InitializePointers();
			UpdateHitTest();

			FocusManager.Track(this);
		}

		~UIElement()
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Collecting UIElement for [{HtmlId}]");
			}

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

		protected internal void ArrangeElementNative(Rect rect, Rect? clipRect)
		{
			Uno.UI.Xaml.WindowManagerInterop.ArrangeElement(HtmlId, rect, clipRect);

#if DEBUG
			var count = Interlocked.Increment(ref _arrangeCount);

			SetAttribute(("xamlArrangeCount", count.ToString()));
#endif
		}

		protected internal void SetNativeTransform(Matrix3x2 matrix)
		{
			Uno.UI.Xaml.WindowManagerInterop.SetElementTransform(HtmlId, matrix);
		}

		protected internal void ResetStyle(params string[] names)
		{
			if (names == null || names.Length == 0)
			{
				// nothing to do
			}

			Uno.UI.Xaml.WindowManagerInterop.ResetStyle(HtmlId, names);

		}

		protected internal void SetAttribute(string name, string value)
		{
			Uno.UI.Xaml.WindowManagerInterop.SetAttribute(HtmlId, name, value);
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

		partial void EnsureClip(Rect rect)
		{
			if (rect.IsEmpty)
			{
				SetStyle("clip", "");
				return;
			}

			var width = double.IsInfinity(rect.Width) ? 100000.0f : rect.Width;
			var height = double.IsInfinity(rect.Height) ? 100000.0f : rect.Height;

			SetStyle(
				"clip",
				"rect("
				+ Math.Floor(rect.Y) + "px,"
				+ Math.Ceiling(rect.X + width) + "px,"
				+ Math.Ceiling(rect.Y + height) + "px,"
				+ Math.Floor(rect.X) + "px"
				+ ")"
			);
		}

		internal enum HtmlEventFilter
		{
			Default,
			LeftPointerEventFilter,
		}

		internal enum HtmlEventExtractor
		{
			PointerEventExtractor, // See PayloadToPointerArgs
			TappedEventExtractor,
			KeyboardEventExtractor,
			FocusEventExtractor,
			CustomEventDetailStringExtractor, // For use with CustomEvent("name", {detail:{string detail here}})
			CustomEventDetailJsonExtractor, // For use with CustomEvent("name", {detail:{detail here}}) - will be JSON.stringify
		}

		private class EventRegistration
		{
			private static readonly string[] noRegistrationEventNames = { "loading", "loaded", "unloaded" };
			private static readonly Func<EventArgs, bool> _emptyFilter = _ => true;

			private readonly UIElement _owner;
			private readonly string _eventName;
			private RoutedEvent _routedEvent;
			private readonly bool _canBubbleNatively;
			private readonly Func<string, EventArgs> _payloadConverter;
			private readonly Func<EventArgs, bool> _eventFilterManaged;
			private readonly Action _subscribeCommand;

			private List<Delegate> _invocationList = new List<Delegate>();
			private List<Delegate> _pendingInvocationList;
			private bool _isSubscribed = false;
			private bool _isDispatching;

			public EventRegistration(
				UIElement owner,
				string eventName,
				RoutedEvent routedEvent,
				bool onCapturePhase = false,
				bool canBubbleNatively = false,
				HtmlEventFilter? eventFilter = null,
				HtmlEventExtractor? eventExtractor = null,
				Func<string, EventArgs> payloadConverter = null,
				Func<EventArgs, bool> eventFilterManaged = null)
			{
				_owner = owner;
				_eventName = eventName;
				_routedEvent = routedEvent;
				_canBubbleNatively = canBubbleNatively;
				_payloadConverter = payloadConverter;
				_eventFilterManaged = eventFilterManaged ?? _emptyFilter;
				if (noRegistrationEventNames.Contains(eventName))
				{
					_subscribeCommand = null;
				}
				else
				{
					_subscribeCommand = () =>
						Uno.UI.Xaml.WindowManagerInterop.RegisterEventOnView(_owner.HtmlId, eventName, onCapturePhase, eventFilter?.ToString(), eventExtractor?.ToString());
				}
			}

			public void Add(Delegate handler)
			{
				// Do not alter the invocation list while enumerating it (_isDispatching)
				var invocationList = _isDispatching
					? _pendingInvocationList ?? (_pendingInvocationList = new List<Delegate>(_invocationList))
					: _invocationList;

				if (invocationList.Contains(handler))
				{
					return;
				}

				invocationList.Add(handler);
				if (_subscribeCommand != null && invocationList.Count == 1 && !_isSubscribed)
				{
					_subscribeCommand();
					_isSubscribed = true;
				}
			}

			public void Remove(Delegate handler)
			{
				// Do not alter the invocation list while enumerating it (_isDispatching)
				var invocationList = _isDispatching
					? _pendingInvocationList ?? (_pendingInvocationList = new List<Delegate>(_invocationList))
					: _invocationList;

				invocationList.Remove(handler);

				// TODO: Removing handler in HTML not supported yet
				// var command = $"Uno.UI.WindowManager.current.unregisterEventOnView(\"{HtmlId}\", \"{eventName}\");";
				// WebAssemblyRuntime.InvokeJS(command);
				// _isSubscribed = false;
			}

			public bool Dispatch(EventArgs eventArgs, string nativeEventPayload)
			{
				if (_invocationList.Count == 0)
				{
					// Nothing to do (should not occur once we can remove handler in HTML)
					return false;
				}

				try
				{
					_isDispatching = true;

					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().Debug($"{_owner}: Dispatching event {_eventName}");
					}

					var args = eventArgs;
					if (_payloadConverter != null && nativeEventPayload != null)
					{
						args = _payloadConverter(nativeEventPayload);
					}

					if (args is RoutedEventArgs routedArgs)
					{
						routedArgs.CanBubbleNatively = _canBubbleNatively;

						if (_routedEvent?.Flag == RoutedEventFlag.Tapped)
						{
							_owner.PreRaiseTapped?.Invoke(_owner, null);
						}
					}

					if (_eventFilterManaged(args))
					{
						foreach (var handler in _invocationList)
						{
							var result = handler.DynamicInvoke(_owner, args);

							if (result is bool isHandedInManaged && isHandedInManaged)
							{
								return true; // will call ".preventDefault()" in JS to prevent native bubbling
							}
						}
					}

					return false; // let native bubbling in HTML
				}
				catch (Exception e)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error($"Failed to dispatch event {_eventName} on {_owner.HtmlId} to {_invocationList.Count} handlers.", e);
					}

					throw;
				}
				finally
				{
					_isDispatching = false;

					// An handler was added / removed while dispatching the event, so apply the change.
					if (_pendingInvocationList != null)
					{
						_invocationList = _pendingInvocationList;
						_pendingInvocationList = null;
					}
				}
			}
		}

		private readonly Dictionary<string, EventRegistration> _eventHandlers = new Dictionary<string, EventRegistration>(StringComparer.InvariantCultureIgnoreCase);

		internal void RegisterEventHandler(
			string eventName,
			Delegate handler,
			RoutedEvent routedEvent = null,
			bool onCapturePhase = false,
			bool canBubbleNatively = false,
			HtmlEventFilter? eventFilter = null,
			HtmlEventExtractor? eventExtractor = null,
			Func<string, EventArgs> payloadConverter = null)
		{
			if (!_eventHandlers.TryGetValue(eventName, out var registration))
			{
				_eventHandlers[eventName] = registration = new EventRegistration(
					this,
					eventName,
					routedEvent,
					onCapturePhase,
					canBubbleNatively,
					eventFilter,
					eventExtractor,
					payloadConverter);
			}

			registration.Add(handler);
		}

		internal void UnregisterEventHandler(string eventName, Delegate handler)
		{
			if (_eventHandlers.TryGetValue(eventName, out var registration))
			{
				registration.Remove(handler);
			}
			else if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug(message: $"No handler registered for event {eventName}.");
			}
		}

		internal bool InternalDispatchEvent(string eventName, EventArgs eventArgs = null, string nativeEventPayload = null)
		{
			var n = eventName;
			try
			{
				if (_eventHandlers.TryGetValue(n, out var registration))
				{
					return registration.Dispatch(eventArgs, nativeEventPayload);
				}

				var registered = string.Join(", ", _eventHandlers.Keys);

				this.Log().Warn(message: $"{this}: No Handler for {n}. Registered: {registered}");
			}
			catch (Exception e)
			{
				this.Log().Error(message: $"{this}/{eventName}/\"{nativeEventPayload}\": Error: {e}");
				Application.Current.RaiseRecoverableUnhandledExceptionOrLog(e, this);
			}

			return false;
		}

		[Preserve]
		public static bool DispatchEvent(int handle, string eventName, string eventArgs)
		{
			// Dispatch to right object, if we can find it
			if (GetElementFromHandle(handle) is UIElement element)
			{
				return element.InternalDispatchEvent(eventName, nativeEventPayload: eventArgs);
			}
			else
			{
				Console.Error.WriteLine($"No UIElement found for htmlId \"{handle}\"");
			}

			return false;
		}

		internal static UIElement GetElementFromHandle(int handle)
		{
			var gcHandle = GCHandle.FromIntPtr((IntPtr)handle);

			if(gcHandle.IsAllocated && gcHandle.Target is UIElement element)
			{
				return element;
			}

			return null;
		}

		private Rect _arranged;
		private string _name;
		internal IList<UIElement> _children = new MaterializableList<UIElement>();

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

		partial void InitializeCapture();

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

		internal Windows.Foundation.Point GetPosition(Point position, global::Windows.UI.Xaml.UIElement relativeTo)
			=> TransformToVisual(relativeTo).TransformPoint(position);

		protected virtual void OnVisibilityChanged(Visibility oldValue, Visibility newVisibility)
		{
			InvalidateMeasure();
			UpdateHitTest();

			if (newVisibility == Visibility.Visible)
			{
				ResetStyle("visibility");
			}
			else
			{
				SetStyle("visibility", "hidden");
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
		}

		public override string ToString()
		{
			return GetType().Name + "-" + HtmlId;
		}

		public GeneralTransform TransformToVisual(UIElement visual)
		{
			var bounds = GetBoundingClientRect();
			var otherBounds = new Rect(0, 0, 0, 0);

			// If visual is null, we transform the element to the window
			if (visual == null)
			{
				// Do nothing (leave at 0,0)
			}
			else
			{
				otherBounds = visual.GetBoundingClientRect();
			}

			return new MatrixTransform
			{
				Matrix = new Matrix(
					m11: 1,
					m12: 0,
					m21: 0,
					m22: 1,
					offsetX: bounds.X - otherBounds.X,
					offsetY: bounds.Y - otherBounds.Y
				)
			};
		}

		internal void UpdateHitTest()
		{
			this.CoerceValue(HitTestVisibilityProperty);
		}

		internal virtual bool IsEnabledOverride() => true;

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

			_children.Add(child);

			if (index.HasValue)
			{
				Uno.UI.Xaml.WindowManagerInterop.AddView(HtmlId, child.HtmlId, index);
			}
			else
			{
				Uno.UI.Xaml.WindowManagerInterop.AddView(HtmlId, child.HtmlId);
			}

			OnChildAdded(child);

			child.InvalidateMeasure();

			// Arrange is required to unset the uno-unarranged CSS class
			child.InvalidateArrange();
		}

		public void ClearChildren()
		{
			foreach (var child in _children)
			{
				child.SetParent(null);
				Uno.UI.Xaml.WindowManagerInterop.RemoveView(HtmlId, child.HtmlId);

				OnChildRemoved(child);
			}

			_children.Clear();
			InvalidateMeasure();
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

		private void OnAddingChild(UIElement child)
		{
			if (FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded
				&& IsLoaded
				&& child._isFrameworkElement)
			{
				if (child.IsLoaded)
				{
					this.Log().Error($"{this}: Inconsistent state: child {child} is already loaded (OnAddingChild)");
				}
				else
				{
					child.ManagedOnLoading();
				}
			}
		}

		private void OnChildAdded(UIElement child)
		{
			if (FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded
				&& IsLoaded
				&& child._isFrameworkElement)
			{
				if (child.IsLoaded)
				{
					this.Log().Error($"{this}: Inconsistent state: child {child} is already loaded (OnChildAdded)");
				}
				else
				{
					child.ManagedOnLoaded();
				}
			}
		}

		private void OnChildRemoved(UIElement child)
		{
			if (FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded
				&& IsLoaded
				&& child._isFrameworkElement)
			{
				if (child.IsLoaded)
				{
					child.ManagedOnUnloaded();
				}
				else
				{
					this.Log().Error($"{this}: Inconsistent state: child {child} is not loaded (OnChildRemoved)");
				}
			}
		}

		internal virtual void ManagedOnLoading()
		{
			foreach (var child in _children)
			{
				child.ManagedOnLoading();
			}
		}

		internal virtual void ManagedOnLoaded()
		{
			IsLoaded = true;

			foreach (var child in _children)
			{
				child.ManagedOnLoaded();
			}
		}

		internal virtual void ManagedOnUnloaded()
		{
			IsLoaded = false;

			foreach (var child in _children)
			{
				child.ManagedOnUnloaded();
			}
		}

		private static Dictionary<RoutedEvent, (string eventName, string domEventName)> RoutedEventNames
			= new Dictionary<RoutedEvent, (string, string)>
			{
				// Add more events
				{ PointerPressedEvent, (nameof(PointerPressedEvent), "pointerdown") },
				{ PointerReleasedEvent, (nameof(PointerReleasedEvent), "pointerup") },
				{ PointerMovedEvent, (nameof(PointerMovedEvent), "pointermove") },
				{ PointerEnteredEvent, (nameof(PointerEnteredEvent), "pointerenter") },
				{ PointerExitedEvent, (nameof(PointerExitedEvent), "pointerleave") },
				{ KeyDownEvent, (nameof(KeyDownEvent), "keydown") },
				{ KeyUpEvent, (nameof(KeyUpEvent), "keyup") },
				{ GotFocusEvent, (nameof(GotFocusEvent), "focus") },
				{ LostFocusEvent, (nameof(LostFocusEvent), "focusout") },
				{ TappedEvent, (nameof(TappedEvent), "click") },
				{ DoubleTappedEvent, (nameof(DoubleTappedEvent), "dblclick") }
			};

		// We keep track of registered routed events to avoid registering the same one twice (mainly because RemoveHandler is not implemented)
		private HashSet<RoutedEvent> _registeredRoutedEvents = new HashSet<RoutedEvent>();

		partial void AddHandlerPartial(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo)
		{
			if (!_registeredRoutedEvents.Contains(routedEvent))
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Registering {routedEvent.Name} on {this}.");
				}

				_registeredRoutedEvents.Add(routedEvent);
				if (RoutedEventNames.TryGetValue(routedEvent, out var eventDescription))
				{
					HtmlEventFilter? eventFilter;
					HtmlEventExtractor? eventExtractor;
					Func<string, EventArgs> payloadConverter;

					switch (eventDescription.eventName)
					{
						case nameof(PointerPressedEvent):
							eventFilter = HtmlEventFilter.LeftPointerEventFilter;
							eventExtractor = HtmlEventExtractor.PointerEventExtractor;
							payloadConverter = PayloadToPressedPointerArgs;
							break;
						case nameof(PointerReleasedEvent):
							eventFilter = HtmlEventFilter.LeftPointerEventFilter;
							eventExtractor = HtmlEventExtractor.PointerEventExtractor;
							payloadConverter = PayloadToReleasedPointerArgs;
							break;
						case nameof(PointerMovedEvent):
							eventFilter = null;
							eventExtractor = HtmlEventExtractor.PointerEventExtractor;
							payloadConverter = PayloadToMovedPointerArgs;
							break;
						case nameof(PointerEnteredEvent):
							eventFilter = null;
							eventExtractor = HtmlEventExtractor.PointerEventExtractor;
							payloadConverter = PayloadToEnteredPointerArgs;
							break;
						case nameof(PointerExitedEvent):
							eventFilter = null;
							eventExtractor = HtmlEventExtractor.PointerEventExtractor;
							payloadConverter = PayloadToExitedPointerArgs;
							break;

						case nameof(TappedEvent):
							eventFilter = HtmlEventFilter.LeftPointerEventFilter;
							eventExtractor = HtmlEventExtractor.TappedEventExtractor;
							payloadConverter = PayloadToTappedArgs;
							break;

						case nameof(DoubleTappedEvent):
							eventFilter = HtmlEventFilter.LeftPointerEventFilter;
							eventExtractor = HtmlEventExtractor.TappedEventExtractor;
							payloadConverter = PayloadToTappedArgs;
							break;

						case nameof(KeyDownEvent):
						case nameof(KeyUpEvent):
							eventFilter = null;
							eventExtractor = HtmlEventExtractor.KeyboardEventExtractor;
							payloadConverter = PayloadToKeyArgs;
							break;

						case nameof(GotFocusEvent):
						case nameof(LostFocusEvent):
							eventFilter = null;
							eventExtractor = HtmlEventExtractor.FocusEventExtractor;
							payloadConverter = PayloadToFocusArgs;
							break;

						default:
							eventFilter = null;
							eventExtractor = null;
							payloadConverter = s => new RoutedEventArgs { OriginalSource = this };
							break;
					}

					bool RoutedEventHandler(object sender, RoutedEventArgs args)
						=> RaiseEvent(routedEvent, args);

					RegisterEventHandler(
						eventDescription.domEventName,
						routedEvent: routedEvent,
						handler: new RoutedEventHandlerWithHandled(RoutedEventHandler),
						onCapturePhase: false,
						canBubbleNatively: true,
						eventFilter: eventFilter ?? HtmlEventFilter.Default,
						eventExtractor: eventExtractor,
						payloadConverter: payloadConverter
					);
				}
			}
		}

		private PointerRoutedEventArgs PayloadToPressedPointerArgs(string payload) => PayloadToPointerArgs(payload, isInContact: true, pressed: true);
		private PointerRoutedEventArgs PayloadToMovedPointerArgs(string payload) => PayloadToPointerArgs(payload, isInContact: true);
		private PointerRoutedEventArgs PayloadToReleasedPointerArgs(string payload) => PayloadToPointerArgs(payload, isInContact: true, pressed: false);
		private PointerRoutedEventArgs PayloadToEnteredPointerArgs(string payload) => PayloadToPointerArgs(payload, isInContact: false);
		private PointerRoutedEventArgs PayloadToExitedPointerArgs(string payload) => PayloadToPointerArgs(payload, isInContact: false);

		private PointerRoutedEventArgs PayloadToPointerArgs(string payload, bool isInContact, bool? pressed = null)
		{
			var parts = payload?.Split(';');
			if (parts?.Length != 7)
			{
				return null;
			}

			var pointerId = uint.Parse(parts[0], CultureInfo.InvariantCulture);
			var x = double.Parse(parts[1], CultureInfo.InvariantCulture);
			var y = double.Parse(parts[2], CultureInfo.InvariantCulture);
			var ctrl = parts[3] == "1";
			var shift = parts[4] == "1";
			var button = int.Parse(parts[5], CultureInfo.InvariantCulture); // -1: none, 0:main, 1:middle, 2:other (commonly main=left, other=right)
			var typeStr = parts[6];

			var position = new Point(x, y);
			var pointerType = ConvertPointerTypeString(typeStr);
			var key =
				button == 0 ? VirtualKey.LeftButton
				: button == 1 ? VirtualKey.MiddleButton
				: button == 2 ? VirtualKey.RightButton
				: VirtualKey.None; // includes -1 == none
			var keyModifiers = VirtualKeyModifiers.None;
			if (ctrl) keyModifiers |= VirtualKeyModifiers.Control;
			if (shift) keyModifiers |= VirtualKeyModifiers.Shift;
			var update = PointerUpdateKind.Other;
			if (pressed.HasValue)
			{
				if (pressed.Value)
				{
					update = key == VirtualKey.LeftButton ? PointerUpdateKind.LeftButtonPressed
						: key == VirtualKey.MiddleButton ? PointerUpdateKind.MiddleButtonPressed
						: key == VirtualKey.RightButton ? PointerUpdateKind.RightButtonPressed
						: PointerUpdateKind.Other;
				}
				else
				{
					update = key == VirtualKey.LeftButton ? PointerUpdateKind.LeftButtonReleased
						: key == VirtualKey.MiddleButton ? PointerUpdateKind.MiddleButtonReleased
						: key == VirtualKey.RightButton ? PointerUpdateKind.RightButtonReleased
						: PointerUpdateKind.Other;
				}
			}

			return new PointerRoutedEventArgs(
				pointerId,
				pointerType,
				position,
				isInContact,
				key,
				keyModifiers,
				update,
				this);
		}

		private TappedRoutedEventArgs PayloadToTappedArgs(string payload)
		{
			var parts = payload?.Split(';');
			if (parts?.Length != 7)
			{
				return null;
			}

			var pointerId = uint.Parse(parts[0], CultureInfo.InvariantCulture);
			var x = double.Parse(parts[1], CultureInfo.InvariantCulture);
			var y = double.Parse(parts[2], CultureInfo.InvariantCulture);
			var typeStr = parts[6];

			var type = ConvertPointerTypeString(typeStr);

			var args = new TappedRoutedEventArgs(new Point(x, y))
			{
				OriginalSource = this,
				PointerDeviceType = type
			};

			return args;
		}

		private KeyRoutedEventArgs PayloadToKeyArgs(string payload)
		{
			return new KeyRoutedEventArgs
			{
				OriginalSource = this,
				Key = System.VirtualKeyHelper.FromKey(payload),
			};
		}

		private RoutedEventArgs PayloadToFocusArgs(string payload)
		{
			if(int.TryParse(payload, out int xamlHandle))
			{
				if(GetElementFromHandle(xamlHandle) is UIElement element)
				{
					return new RoutedEventArgs
					{
						OriginalSource = element,
					};
				}
			}

			return new KeyRoutedEventArgs
			{
				OriginalSource = this,
			};
		}

		private static PointerDeviceType ConvertPointerTypeString(string typeStr)
		{
			PointerDeviceType type;
			switch (typeStr.ToUpper())
			{
				case "MOUSE":
				default:
					type = PointerDeviceType.Mouse;
					break;
				case "PEN":
					type = PointerDeviceType.Pen;
					break;
				case "TOUCH":
					type = PointerDeviceType.Touch;
					break;
			}

			return type;
		}

		#region HitTestVisibility

		private enum HitTestVisibility
		{
			/// <summary>
			/// The element and its children can't be targeted by hit-testing.
			/// </summary>
			/// <remarks>
			/// This occurs when IsHitTestVisible="False", IsEnabled="False", or Visibility="Collapsed".
			/// </remarks>
			Collapsed,

			/// <summary>
			/// The element can't be targeted by hit-testing.
			/// </summary>
			/// <remarks>
			/// This usually occurs if an element doesn't have a Background/Fill.
			/// </remarks>
			Invisible,

			/// <summary>
			/// The element can be targeted by hit-testing.
			/// </summary>
			Visible,
		}

		/// <summary>
		/// Represents the final calculated hit-test visibility of the element.
		/// </summary>
		/// <remarks>
		/// This property should never be directly set, and its value should always be calculated through coercion (see <see cref="CoerceHitTestVisibility(DependencyObject, object, bool)"/>.
		/// </remarks>
		private static readonly DependencyProperty HitTestVisibilityProperty =
			DependencyProperty.Register(
				"HitTestVisibility",
				typeof(HitTestVisibility),
				typeof(UIElement),
				new FrameworkPropertyMetadata(
					HitTestVisibility.Visible,
					FrameworkPropertyMetadataOptions.Inherits,
					coerceValueCallback: (s, e) => CoerceHitTestVisibility(s, e),
					propertyChangedCallback: (s, e) => OnHitTestVisibilityChanged(s, e)
				)
			);

		/// <summary>
		/// This calculates the final hit-test visibility of an element.
		/// </summary>
		/// <returns></returns>
		private static object CoerceHitTestVisibility(DependencyObject dependencyObject, object baseValue)
		{
			var element = (UIElement)dependencyObject;

			// The HitTestVisibilityProperty is never set directly. This means that baseValue is always the result of the parent's CoerceHitTestVisibility.
			var baseHitTestVisibility = (HitTestVisibility)baseValue;

			// If the parent is collapsed, we should be collapsed as well. This takes priority over everything else, even if we would be visible otherwise.
			if (baseHitTestVisibility == HitTestVisibility.Collapsed)
			{
				return HitTestVisibility.Collapsed;
			}

			// If we're not locally hit-test visible, visible, or enabled, we should be collapsed. Our children will be collapsed as well.
			if (!element.IsHitTestVisible || element.Visibility != Visibility.Visible || !element.IsEnabledOverride())
			{
				return HitTestVisibility.Collapsed;
			}

			// If we're not hit (usually means we don't have a Background/Fill), we're invisible. Our children will be visible or not, depending on their state.
			if (!element.IsViewHit())
			{
				return HitTestVisibility.Invisible;
			}

			// If we're not collapsed or invisible, we can be targeted by hit-testing. This means that we can be the source of pointer events.
			return HitTestVisibility.Visible;
		}

		private static void OnHitTestVisibilityChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyObject is UIElement element && args.NewValue is HitTestVisibility hitTestVisibility)
			{
				if (hitTestVisibility == HitTestVisibility.Visible)
				{
					// By default, elements have 'pointer-event' set to 'auto' (see Uno.UI.css .uno-uielement class).
					// This means that they can be the target of hit-testing and will raise pointer events when interacted with.
					// This is aligned with HitTestVisibilityProperty's default value of Visible.
					element.SetStyle("pointer-events", "auto");
				}
				else
				{
					// If HitTestVisibilityProperty is calculated to Invisible or Collapsed,
					// we don't want to be the target of hit-testing and raise any pointer events.
					// This is done by setting 'pointer-events' to 'none'.
					element.SetStyle("pointer-events", "none");
				}
			}
		}

#endregion
	}
}
