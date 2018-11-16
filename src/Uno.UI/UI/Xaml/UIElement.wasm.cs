using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Windows.UI.Xaml.Controls;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Logging;
using Uno;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Uno.Core.Comparison;
using Windows.Devices.Input;

namespace Windows.UI.Xaml
{
	public partial class UIElement : DependencyObject
	{
		private readonly GCHandle _gcHandle;

		internal interface IHandlableEventArgs
		{
			bool Handled { get; set; }
		}

		private static class ClassNames
		{
			private static readonly Dictionary<Type, string> _classNames = new Dictionary<Type, string>();

			internal static string GetForType(Type type)
			{
				if(!_classNames.TryGetValue(type, out var names))
				{
					_classNames[type] = names = string.Join(",", GetClassesForType(type));
				}

				return names;
			}

			private static IEnumerable<string> GetClassesForType(Type type)
			{
				while (type != null && type != typeof(object))
				{
					yield return "\"" + type.Name.ToLowerInvariant() + "\"";
					type = type.BaseType;
				}
			}
		}

		private void CapturePointerNative(Pointer pointer)
		{
			var command = "Uno.UI.WindowManager.current.setPointerCapture(\"" + HtmlId + "\", " + pointer.PointerId + ");";
			WebAssemblyRuntime.InvokeJS(command);
		}

		private void ReleasePointerCaptureNative(Pointer pointer)
		{
			var command = "Uno.UI.WindowManager.current.releasePointerCapture(\"" + HtmlId + "\", " + pointer.PointerId + ");";
			WebAssemblyRuntime.InvokeJS(command);
		}

		public Size MeasureView(Size availableSize)
		{
			var w = double.IsInfinity(availableSize.Width) ? "null" : availableSize.Width.ToStringInvariant();
			var h = double.IsInfinity(availableSize.Height) ? "null" : availableSize.Height.ToStringInvariant();

			var command = "Uno.UI.WindowManager.current.measureView(\"" + HtmlId + "\", \"" + w + "\", \"" + h + "\");";
			var result = WebAssemblyRuntime.InvokeJS(command);

			var parts = result.Split(';');

			return new Size(
				double.Parse(parts[0], CultureInfo.InvariantCulture) + 0.5,
				double.Parse(parts[1], CultureInfo.InvariantCulture));
		}

		public Rect GetBBox()
		{
			if (!HtmlTagIsSvg)
			{
				throw new InvalidOperationException("GetBBox is available only for SVG elements.");
			}

			var sizeString = WebAssemblyRuntime.InvokeJS("Uno.UI.WindowManager.current.getBBox(\"" + HtmlId + "\");");
			var sizeParts = sizeString.Split(';');
			return new Rect(double.Parse(sizeParts[0]), double.Parse(sizeParts[1]), double.Parse(sizeParts[2]), double.Parse(sizeParts[3]));
		}

		private Rect GetBoundingClientRect()
		{
			var sizeString = WebAssemblyRuntime.InvokeJS("Uno.UI.WindowManager.current.getBoundingClientRect(\"" + HtmlId + "\");");
			var sizeParts = sizeString.Split(';');
			return new Rect(double.Parse(sizeParts[0]), double.Parse(sizeParts[1]), double.Parse(sizeParts[2]), double.Parse(sizeParts[3]));
		}

		public UIElement(string htmlTag = "div", bool isSvg = false)
		{
			_gcHandle = GCHandle.Alloc(this, GCHandleType.Weak);
			HtmlTag = htmlTag;
			HtmlTagIsSvg = isSvg;

			var type = GetType();

			Handle = GCHandle.ToIntPtr(_gcHandle);
			HtmlId = type.Name + "-" + Handle;

			var isSvgStr = HtmlTagIsSvg ? "true" : "false";
			var isFrameworkElementStr = this is FrameworkElement ? "true" : "false";
			const string isFocusable = "false"; // by default all control are not focusable, it has to be change latter by the control itself
			var classes = ClassNames.GetForType(type);

			WebAssemblyRuntime.InvokeJS(
				"Uno.UI.WindowManager.current.createContent({" +
				"id:\"" + HtmlId + "\"," +
				"tagName:\"" + HtmlTag + "\", " +
				"handle:" + Handle + ", " +
				"type:\"" + type.FullName + "\", " +
				"isSvg:" + isSvgStr + ", " +
				"isFrameworkElement:"  + isFrameworkElementStr + ", " +
				"isFocusable:"  + isFocusable + ", " +
				"classes:[" + classes + "]" +
				"});");

			UpdateHitTest();

			FocusManager.Track(this);
		}

		~UIElement()
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Collecting UIElement for [{HtmlId}]");
			}

			var command = "Uno.UI.WindowManager.current.destroyView(\"" + HtmlId + "\");";
			WebAssemblyRuntime.InvokeJS(command);

			_gcHandle.Free();
		}

		public IntPtr Handle { get; }

		public string HtmlId { get; }

		public string HtmlTag { get; }

		public bool HtmlTagIsSvg { get; }

		protected internal void SetStyle(string name, string value)
		{
			var escapedvalue = WebAssemblyRuntime.EscapeJs(value);

			var command = "Uno.UI.WindowManager.current.setStyle(\"" +
				HtmlId + "\", " +
				"{\"" + name +"\": \"" + escapedvalue + "\"}" +
			");";

			WebAssemblyRuntime.InvokeJS(command);
		}

		protected internal void SetStyle(params (string name, string value)[] styles)
		{
			if (styles == null || styles.Length == 0)
			{
				return; // nothing to do
			}

			var stylesStr = string.Join(", ", styles.Select(s => "\"" + s.name + "\": \"" + WebAssemblyRuntime.EscapeJs(s.value) + "\""));
			var command = "Uno.UI.WindowManager.current.setStyle(\"" + HtmlId + "\", {" + stylesStr + "});";

			WebAssemblyRuntime.InvokeJS(command);
		}

#if DEBUG
		private long _arrangeCount = 0;
#endif

		protected internal void SetStyleArranged(params (string name, string value)[] styles)
		{
			if (styles == null || styles.Length == 0)
			{
				return; // nothing to do
			}
			var stylesStr = string.Join(", ", styles.Select(s => "\"" + s.name + "\": \"" + WebAssemblyRuntime.EscapeJs(s.value) + "\""));

#if DEBUG
			var count = Interlocked.Increment(ref _arrangeCount);

			var command = "Uno.UI.WindowManager.current.setStyle(\"" + HtmlId + "\", {" + stylesStr + "}, true);" +
				"Uno.UI.WindowManager.current.setAttribute(\"" + HtmlId + "\", {\"xamlArrangeCount\": \"" + count + "\"});";
#else
			var command = "Uno.UI.WindowManager.current.setStyle(\"" + HtmlId + "\", {" + stylesStr + "}, true);";
#endif

			WebAssemblyRuntime.InvokeJS(command);
		}

		protected internal void ResetStyle(params string[] names)
		{
			if (names == null || names.Length == 0)
			{
				// nothing to do
			}
			else if (names.Length == 1)
			{
				var command = "Uno.UI.WindowManager.current.resetStyle(\"" + HtmlId + "\", [\"" + names[0] + "\"]);";
				WebAssemblyRuntime.InvokeJS(command);
			}
			else
			{
				var namesStr = string.Join(", ", names.Select(n => "\"" + n + "\""));
				var command = "Uno.UI.WindowManager.current.resetStyle(\"" + HtmlId + "\", [\"" + namesStr + "\"]);";
				WebAssemblyRuntime.InvokeJS(command);
			}

		}

		protected internal void SetAttribute(string name, string value)
		{
			var escapedvalue = WebAssemblyRuntime.EscapeJs(value);
			var command = "Uno.UI.WindowManager.current.setAttribute(\"" + HtmlId + "\", {\"" + name + "\": \"" + escapedvalue + "\"});";
			WebAssemblyRuntime.InvokeJS(command);
		}

		protected internal void SetAttribute(params (string name, string value)[] attributes)
		{
			if (attributes == null || attributes.Length == 0)
			{
				return; // nothing to do
			}

			var attributesStr = string.Join(", ", attributes.Select(s => "\"" +s.name + "\": \"" + WebAssemblyRuntime.EscapeJs(s.value) + "\""));
			var command = "Uno.UI.WindowManager.current.setAttribute(\"" + HtmlId + "\", {" + attributesStr + "});";

			WebAssemblyRuntime.InvokeJS(command);
		}

		protected internal string GetAttribute(string name)
		{
			var command = "Uno.UI.WindowManager.current.getAttribute(\"" + HtmlId + "\", \"" + name + "\");";
			return WebAssemblyRuntime.InvokeJS(command);
		}

		protected internal void SetProperty(string name, string value)
		{
			var escapedvalue = WebAssemblyRuntime.EscapeJs(value);
			var command = "Uno.UI.WindowManager.current.setProperty(\"" + HtmlId + "\", {\"" + name + "\": \"" + escapedvalue + "\"});";
			WebAssemblyRuntime.InvokeJS(command);
		}

		protected internal void SetProperty(params (string name, string value)[] properties)
		{
			if (properties == null || properties.Length == 0)
			{
				return; // nothing to do
			}

			var propertiesStr = string.Join(", ", properties.Select(s => "\"" + s.name + "\": \"" + WebAssemblyRuntime.EscapeJs(s.value) + "\""));
			var command = "Uno.UI.WindowManager.current.setAttribute(\"" + HtmlId + "\", {" + propertiesStr + "});";

			WebAssemblyRuntime.InvokeJS(command);
		}

		protected internal string GetProperty(string name)
		{
			var command = "Uno.UI.WindowManager.current.getProperty(\"" + HtmlId + "\", \"" + name + "\");";
			return WebAssemblyRuntime.InvokeJS(command);
		}

		protected internal void SetHtmlContent(string html)
		{
			var escapedHtml = WebAssemblyRuntime.EscapeJs(html);

			var command = "Uno.UI.WindowManager.current.setHtmlContent(\"" + HtmlId + "\", \"" + escapedHtml + "\");";
			WebAssemblyRuntime.InvokeJS(command);
		}

		protected internal void AddView(UIElement view)
		{
			if (view == null)
			{
				return;
			}

			var command = "Uno.UI.WindowManager.current.addView(\"" + HtmlId + "\", \"" + view.HtmlId + "\");";
			WebAssemblyRuntime.InvokeJS(command);

			InvalidateMeasure();
		}

		protected internal void AddView(UIElement view, int index)
		{
			if (view == null)
			{
				return;
			}

			var command = "Uno.UI.WindowManager.current.addView(\"" + HtmlId + "\", \"" + view.HtmlId + "\", " + index + ");";
			WebAssemblyRuntime.InvokeJS(command);

			InvalidateMeasure();
		}

		protected internal void RemoveView(UIElement view)
		{
			if (view == null)
			{
				return;
			}

			var command = "Uno.UI.WindowManager.current.removeView(\"" + HtmlId + "\", \"" + view.HtmlId + "\");";
			WebAssemblyRuntime.InvokeJS(command);

			InvalidateMeasure();
		}

		internal void MoveViewTo(int oldIndex, int newIndex)
		{
			var view = _children[oldIndex];
			
			var command = "Uno.UI.WindowManager.current.addView(\"" + HtmlId + "\", \"" + view.HtmlId + "\", " + newIndex + ");";
			WebAssemblyRuntime.InvokeJS(command);

			InvalidateMeasure();
		}

		partial void EnsureClip(Rect rect)
		{
			if (rect.IsEmpty)
			{
				SetStyle("clip", "");
				return;
			}

			SetStyle(
				"clip",
				"rect("
				+ Math.Floor(rect.Y) + "px,"
				+ Math.Ceiling(rect.X + rect.Width) + "px,"
				+ Math.Ceiling(rect.Y + rect.Height) + "px,"
				+ Math.Floor(rect.X) + "px"
				+ ")"
			);
		}

		private class EventRegistration
		{
			private static readonly string[] noRegistrationEventNames = { "loading", "loaded", "unloaded" };
			private static readonly Func<EventArgs, bool> _emptyFilter = _ => true;

			private readonly UIElement _owner;
			private readonly string _eventName;
			private readonly Func<string, EventArgs> _payloadConverter;
			private readonly Func<EventArgs, bool> _eventFilterManaged;
			private readonly string _subscribeCommand;

			private List<Delegate> _invocationList = new List<Delegate>();
			private List<Delegate> _pendingInvocationList;
			private bool _isSubscribed = false;
			private bool _isDispatching;

			public EventRegistration(
				UIElement owner,
				string eventName,
				bool onCapturePhase = false,
				string eventFilterScript = null,
				string eventExtractorScript = null,
				Func<string, EventArgs> payloadConverter = null,
				Func<EventArgs, bool> eventFilterManaged = null)
			{
				_owner = owner;
				_eventName = eventName;
				_payloadConverter = payloadConverter;
				_eventFilterManaged = eventFilterManaged ?? _emptyFilter;
				if (noRegistrationEventNames.Contains(eventName))
				{
					_subscribeCommand = null;
				}
				else
				{
					var onCapturePhaseStr = onCapturePhase
						? "true"
						: "false";
					var filterFunction = eventFilterScript == null
						? "null"
						: "function(evt) { return " + eventFilterScript + "; }";
					var extractorFunction = eventExtractorScript == null
						? "null"
						: "function(evt) { return " + eventExtractorScript + "; }";

					_subscribeCommand = $"Uno.UI.WindowManager.current.registerEventOnView(\"{_owner.HtmlId}\", \"{eventName}\", {onCapturePhaseStr}, {filterFunction}, {extractorFunction});";
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
					WebAssemblyRuntime.InvokeJS(_subscribeCommand);
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
					// Nothing to do (should not occures once we can remove handler in HTML)
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

					// We assume that all handlers are of the same type. So we peak the invocation list to check the type.
					switch (_invocationList[0])
					{
						case EventHandler eh:
							eventArgs = args ?? EventArgs.Empty;
							if (_eventFilterManaged(eventArgs))
							{
								foreach (var handler in _invocationList)
								{
									((EventHandler) handler).Invoke(_owner, eventArgs);
								}
							}

							return false;

						case RoutedEventHandler reh:
							var routedEventArgs = args as RoutedEventArgs ?? RoutedEventArgs.Empty;
							if (_eventFilterManaged(routedEventArgs))
							{
								foreach (var handler in _invocationList)
								{
									((RoutedEventHandler) handler).Invoke(_owner, routedEventArgs);
								}
							}

							return false;

						default:
							if (_eventFilterManaged(args))
							{
								foreach (var handler in _invocationList)
								{
									handler.DynamicInvoke(_owner, args);
								}
							}

							return args is IHandlableEventArgs handelable && handelable.Handled;
					}
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

		private readonly Dictionary<string, EventRegistration> _eventHandlers = new Dictionary<string, EventRegistration>();

		internal void RegisterEventHandler(
			string eventName,
			Delegate handler,
			bool onCapturePhase = false,
			string eventFilterScript = null,
			string eventExtractorScript = null,
			Func<string, EventArgs> payloadConverter = null,
			Func<EventArgs, bool> eventFilterManaged = null)
		{
			if(!_eventHandlers.TryGetValue(eventName, out var registartion))
			{
				_eventHandlers[eventName] = registartion = new EventRegistration(this, eventName, onCapturePhase, eventFilterScript, eventExtractorScript, payloadConverter);
			}

			registartion.Add(handler);
		}

		internal void RegisterEventHandlerEx(
			string managedEventIdentifier,
			string eventName,
			Delegate handler,
			bool onCapturePhase = false,
			string eventFilterScript = null,
			string eventExtractorScript = null,
			Func<string, EventArgs> payloadConverter = null,
			Func<EventArgs, bool> eventFilterManaged = null)
		{
			if (!_eventHandlers.TryGetValue(eventName, out var registartion))
			{
				_eventHandlers[managedEventIdentifier] = registartion = new EventRegistration(this, eventName, onCapturePhase, eventFilterScript, eventExtractorScript, payloadConverter);
			}

			registartion.Add(handler);
		}

		internal void UnregisterEventHandler(string eventName, Delegate handler)
		{
			if (_eventHandlers.TryGetValue(eventName, out var registartion))
			{
				registartion.Remove(handler);
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug(message: $"No handler registered for event {eventName}.");
				}
			}
		}

		internal bool InternalDispatchEvent(string eventName, EventArgs eventArgs = null, string nativeEventPayload = null)
		{
			try
			{
				if (_eventHandlers.TryGetValue(eventName, out var registration))
				{
					return registration.Dispatch(eventArgs, nativeEventPayload);
				}
			}
			catch(Exception e)
			{
				Application.Current.RaiseRecoverableUnhandledExceptionOrLog(e, this);
			}

			return false;
		}

		private static readonly Func<string, IntPtr> _strToIntPtr =
			Marshal.SizeOf<IntPtr>() == 4
				? (s => (IntPtr)int.Parse(s))
				: (Func<string, IntPtr>)(s => (IntPtr)long.Parse(s));

		[Preserve]
		public static string DispatchEvent(string htmlId, string eventName, string eventArgs)
		{
			// parse htmlId to IntPtr
			var handle = _strToIntPtr(htmlId);

			// Dispatch to right object... if we can find it
			var gcHandle = GCHandle.FromIntPtr(handle);
			if (gcHandle.IsAllocated && gcHandle.Target is UIElement element)
			{
				return element.InternalDispatchEvent(eventName, nativeEventPayload: eventArgs).ToString();
			}
			else
			{
				Console.Error.WriteLine($"No UIElement found for htmlId \"{htmlId}\" {gcHandle.IsAllocated}.");
			}

			return false.ToString();
		}

		private Rect _arranged;
		private string _name;
		internal List<UIElement> _children = new List<UIElement>();

		public string Name
		{
			get => _name;
			set
			{
				_name = value;

				var command = $"Uno.UI.WindowManager.current.setName(\"{HtmlId}\", \"{_name}\");";
				WebAssemblyRuntime.InvokeJS(command);
			}
		}

		partial void InitializeCapture();

		internal bool IsPointerCaptured { get; set; }

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
		{
			throw new NotSupportedException();
		}

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
				SetStyle("opacity", opacity.ToStringInvariant());
			}
		}

		static partial void OnRenderTransformChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var newValue = args.NewValue as Transform;
			var oldValue = args.OldValue as Transform;

			if (newValue != null)
			{
				var view = (UIElement)dependencyObject;

				newValue.View = view;
				newValue.Origin = view.RenderTransformOrigin;
			}
			if (oldValue != null)
			{
				oldValue.View = null;
			}
		}

		static partial void OnRenderTransformOriginChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var view = (UIElement)dependencyObject;
			var point = (Point)args.NewValue;

			if (view.RenderTransform != null)
			{
				view.RenderTransform.Origin = point;
			}
		}

		partial void OnIsHitTestVisibleChangedPartial(bool oldValue, bool newValue)
		{
			UpdateHitTest();
		}

		public override string ToString()
		{
			return HtmlId;
		}

		internal void RaiseTapped(TappedRoutedEventArgs args)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn("RaiseTapped is not supported");
			}
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
				visual.GetBoundingClientRect();
			}

			// TODO: UWP returns a MatrixTransform here. For now TransformToVisual doesn't support rotations, scalings, etc.
			return new TranslateTransform
			{
				X = bounds.X - otherBounds.X,
				Y = bounds.Y - otherBounds.Y
			};
		}

		internal void UpdateHitTest()
		{
			this.CoerceValue(HitTestVisibilityProperty);
		}

		internal virtual bool IsEnabledOverride() => true;

		public void AddChild(UIElement child, int? index = null)
		{
			if (child == null)
			{
				return;
			}

			child.SetParent(this);

			_children.Add(child);

			if (index.HasValue)
			{
				AddView(child, index.Value);
			}
			else
			{
				AddView(child);
			}
		}

		public void ClearChildren()
		{
			foreach (var child  in _children)
			{
				child.SetParent(null);
				RemoveView(child);
			}

			_children.Clear();
		}

		public bool RemoveChild(UIElement child)
		{
			if (_children.Remove(child))
			{
				child.SetParent(null);
				RemoveView(child);
				return true;
			}

			return false;
		}

		public UIElement FindFirstChild()
		{
			return _children.FirstOrDefault();
		}

		public virtual IEnumerable<UIElement> GetChildren()
		{
			return _children;
		}

		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.RoutedEvent DoubleTappedEvent { get; } = new RoutedEvent();

		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.RoutedEvent TappedEvent { get; } = new RoutedEvent();

		[global::Uno.NotImplemented]
		public void AddHandler(global::Windows.UI.Xaml.RoutedEvent routedEvent, object handler, bool handledEventsToo)
		{
			if (routedEvent == UIElement.TappedEvent)
			{
				var h = (TappedEventHandler)handler;
				var pointerHandler = new PointerEventHandler((snd, e) => h(snd, new TappedRoutedEventArgs(e.GetCurrentPoint())));

				this.PointerPressed += pointerHandler;
			}
			else if (routedEvent == UIElement.DoubleTappedEvent)
			{
				var h = (DoubleTappedEventHandler)handler;
				var lastTapped = DateTimeOffset.MinValue.AddDays(2);
				this.PointerPressed += (snd, e) =>
				{
					var now = DateTimeOffset.Now;
					if (lastTapped.AddMilliseconds(250) < now)
					{
						h(this, new DoubleTappedRoutedEventArgs(e.GetCurrentPoint()));
					}
					else
					{
						lastTapped = now;
					}
				};
			}
			else
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.UIElement", "void UIElement.AddHandler(RoutedEvent routedEvent, object handler, bool handledEventsToo)");
			}
		}

		private const string leftPointerEventFilter =
			"evt ? (!evt.button || evt.button == 0) : false";

		private const string pointerEventExtractor =
			"evt ? \"\"+evt.pointerId+\";\"+evt.clientX+\";\"+evt.clientY+\";\"+(evt.ctrlKey?\"1\":\"0\")+\";\"+(evt.shiftKey?\"1\":\"0\")+\";\"+evt.button+\";\"+evt.pointerType : \"\"";

		private EventArgs PayloadToPointerArgs(string payload)
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
			var buttons = int.Parse(parts[5], CultureInfo.InvariantCulture); // -1: none, 0:main, 1:middle, 2:other (commonly main=left, other=right)
			var typeStr = parts[6];

			var keys = ctrl ? VirtualKeyModifiers.Control : (shift ? VirtualKeyModifiers.Shift : VirtualKeyModifiers.None);
			var type = ConvertPointerTypeString(typeStr);

			var args = new PointerRoutedEventArgs(new Point(x, y))
			{
				KeyModifiers = keys,
				Pointer = new Pointer(pointerId, type)
			};

			return args;
		}


		private EventArgs PayloadToTappedArgs(string payload)
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
				PointerDeviceType = type
			};

			return args;
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
