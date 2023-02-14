using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

using Uno;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml
{
	partial class UIElement
	{
		private delegate HtmlEventDispatchResult RawEventHandler(UIElement sender, string paylaod);

		internal delegate object GenericEventHandler(Delegate d, object sender, object args);

		private class EventRegistration
		{
			private static readonly string[] noRegistrationEventNames = { "loading", "loaded", "unloaded", "pointerenter", "pointerleave", "pointerdown", "pointerup", "pointercancel" };

			private class InvocationItem
			{
				public InvocationItem(Delegate handler, GenericEventHandler invoker)
				{
					Handler = handler;
					Invoker = invoker;
				}

				public Delegate Handler { get; }
				public GenericEventHandler Invoker { get; }
			}

			private readonly UIElement _owner;
			private readonly string _eventName;
			private readonly EventArgsParser _payloadConverter;
			private readonly Action _subscribeCommand;

			private List<InvocationItem> _invocationList = new List<InvocationItem>();
			private List<InvocationItem> _pendingInvocationList;
			private bool _isSubscribed;
			private bool _isDispatching;

			public EventRegistration(
				UIElement owner,
				string eventName,
				bool onCapturePhase = false,
				HtmlEventExtractor? eventExtractor = null,
				EventArgsParser payloadConverter = null)
			{
				_owner = owner;
				_eventName = eventName;
				_payloadConverter = payloadConverter;
				if (!noRegistrationEventNames.Contains(eventName))
				{
					_subscribeCommand = () => WindowManagerInterop.RegisterEventOnView(
						_owner.HtmlId,
						eventName,
						onCapturePhase,
						(int)(eventExtractor ?? HtmlEventExtractor.None)
					);
				}
			}

			public void Add(Delegate handler, GenericEventHandler invoker)
			{
				var invocationItem = new InvocationItem(handler, invoker);

				// Do not alter the invocation list while enumerating it (_isDispatching)
				var invocationList = _isDispatching
					? _pendingInvocationList ?? (_pendingInvocationList = new List<InvocationItem>(_invocationList))
					: _invocationList;

				if (invocationList.Contains(invocationItem))
				{
					return;
				}

				invocationList.Add(invocationItem);
				if (_subscribeCommand != null && invocationList.Count == 1 && !_isSubscribed)
				{
					_subscribeCommand();
					_isSubscribed = true;
				}
			}

			public void Remove(Delegate handler, GenericEventHandler invoker)
			{
				var invocationItem = new InvocationItem(handler, invoker);

				// Do not alter the invocation list while enumerating it (_isDispatching)
				var invocationList = _isDispatching
					? _pendingInvocationList ?? (_pendingInvocationList = new List<InvocationItem>(_invocationList))
					: _invocationList;

				invocationList.Remove(invocationItem);

				// TODO: Removing handler in HTML not supported yet
				// var command = $"Uno.UI.WindowManager.current.unregisterEventOnView(\"{HtmlId}\", \"{eventName}\");";
				// WebAssemblyRuntime.InvokeJS(command);
				// _isSubscribed = false;
			}

			public HtmlEventDispatchResult Dispatch(EventArgs eventArgs, string nativeEventPayload)
			{
				if (_invocationList.Count == 0)
				{
					// Nothing to do (should not occur once we can remove handler in HTML)
					return HtmlEventDispatchResult.NotDispatched;
				}

				try
				{
					return InnerDispatch(eventArgs, nativeEventPayload);
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

			/// <remarks>
			/// This method contains or is called by a try/catch containing method and
			/// can be significantly slower than other methods as a result on WebAssembly.
			/// See https://github.com/dotnet/runtime/issues/56309
			/// </remarks>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private HtmlEventDispatchResult InnerDispatch(EventArgs eventArgs, string nativeEventPayload)
			{
				_isDispatching = true;

				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"{_owner}: Dispatching event {_eventName}");
				}

				var args = eventArgs;
				if (_payloadConverter != null && nativeEventPayload != null)
				{
					args = _payloadConverter(_owner, nativeEventPayload);
				}

				var result = HtmlEventDispatchResult.Ok;
				foreach (var invocationItem in _invocationList)
				{
					if (invocationItem.Handler is RawEventHandler rawHandler)
					{
						var handlerResult = rawHandler(_owner, nativeEventPayload);

						if (handlerResult.HasFlag(HtmlEventDispatchResult.StopPropagation))
						{
							// We stop on first handler that requires to stop propagation (same behavior has Handled on UWP)
							return result | HtmlEventDispatchResult.StopPropagation;
						}
						else
						{
							result |= handlerResult;
						}
					}
					else
					{
						var handlerResult = invocationItem.Invoker(invocationItem.Handler, _owner, args);

						switch (handlerResult)
						{
							case bool isHandledInManaged when isHandledInManaged:
								if (args is IPreventDefaultHandling preventDefaultHandling &&
									preventDefaultHandling.DoNotPreventDefault)
								{
									return HtmlEventDispatchResult.StopPropagation;
								}
								return HtmlEventDispatchResult.StopPropagation | HtmlEventDispatchResult.PreventDefault;

							case HtmlEventDispatchResult dispatchResult when dispatchResult.HasFlag(HtmlEventDispatchResult.StopPropagation):
								// We stop on first handler that requires to stop propagation (same behavior has Handled on UWP)
								return result | HtmlEventDispatchResult.StopPropagation;

							case HtmlEventDispatchResult dispatchResult:
								result |= dispatchResult;
								break;
						}
					}
				}

				return HtmlEventDispatchResult.Ok; // let native bubbling in HTML
			}
		}

		internal void RegisterEventHandler(
			string eventName,
			Delegate handler,
			GenericEventHandler invoker,
			bool onCapturePhase = false,
			HtmlEventExtractor? eventExtractor = null,
			EventArgsParser payloadConverter = null)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Registering {eventName} on {this}.");
			}

			if (!_eventHandlers.TryGetValue(eventName, out var registration))
			{
				_eventHandlers[eventName] = registration = new EventRegistration(
					this,
					eventName,
					onCapturePhase,
					eventExtractor,
					payloadConverter);
			}

			registration.Add(handler, invoker);
		}

		internal void UnregisterEventHandler(string eventName, Delegate handler, GenericEventHandler invoker)
		{
			if (_eventHandlers.TryGetValue(eventName, out var registration))
			{
				registration.Remove(handler, invoker);
			}
			else if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug(message: $"No handler registered for event {eventName}.");
			}
		}

		internal HtmlEventDispatchResult InternalDispatchEvent(string eventName, EventArgs eventArgs = null, string nativeEventPayload = null)
		{
			var n = eventName;
			try
			{
				return InternalInnerDispatchEvent(eventArgs, nativeEventPayload, n);
			}
			catch (Exception e)
			{
				this.Log().Error(message: $"{this}-{HtmlId}/{eventName}/\"{nativeEventPayload}\": Error: {e}");
				Application.Current.RaiseRecoverableUnhandledExceptionOrLog(e, this);
			}

			return HtmlEventDispatchResult.Ok;
		}

		/// <remarks>
		/// This method contains or is called by a try/catch containing method and
		/// can be significantly slower than other methods as a result on WebAssembly.
		/// See https://github.com/dotnet/runtime/issues/56309
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private HtmlEventDispatchResult InternalInnerDispatchEvent(EventArgs eventArgs, string nativeEventPayload, string n)
		{
			if (_eventHandlers.TryGetValue(n, out var registration))
			{
				return registration.Dispatch(eventArgs, nativeEventPayload);
			}

			var registered = string.Join(", ", _eventHandlers.Keys);

			this.Log().Warn(message: $"{this}-{HtmlId}: No Handler for {n}. Registered: {registered}");


			return HtmlEventDispatchResult.Ok;
		}

		/// <summary>
		/// Dispatches a native event to managed code
		/// </summary>
		/// <param name="handle">HTML element ID</param>
		/// <param name="eventName">Event name</param>
		/// <param name="eventArgs">Serialized event args</param>
		/// <returns>The HtmlEventDispatchResult of the dispatch.</returns>
		/// <remarks>The return value is an integer for marshaling consideration, but is actually an HtmlEventDispatchResult.</remarks>
		[Preserve]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static int DispatchEvent(int handle, string eventName, string eventArgs)
		{
#if DEBUG
			try
#endif
			{
				// Dispatch to right object, if we can find it
				if (GetElementFromHandle(handle) is UIElement element)
				{
					return (int)element.InternalDispatchEvent(eventName, nativeEventPayload: eventArgs);
				}
				else
				{
					Console.Error.WriteLine($"No UIElement found for htmlId \"{handle}\"");
				}

				return (int)HtmlEventDispatchResult.NotDispatched;
			}
#if DEBUG
			catch (Exception error)
			{
				Console.Error.WriteLine("Failed to dispatch event: " + error);
				throw;
			}
#endif
		}

		private readonly Dictionary<string, EventRegistration> _eventHandlers = new Dictionary<string, EventRegistration>(StringComparer.OrdinalIgnoreCase);

		internal delegate EventArgs EventArgsParser(object sender, string payload);

		internal enum HtmlEventExtractor : int
		{
			None = 0,
			TappedEventExtractor = 2,
			KeyboardEventExtractor = 3,
			FocusEventExtractor = 4,
			CustomEventDetailStringExtractor = 5, // For use with CustomEvent("name", {detail:{string detail here}})
			CustomEventDetailJsonExtractor = 6, // For use with CustomEvent("name", {detail:{detail here}}) - will be JSON.stringify
		}

		[Flags]
		internal enum HtmlEventDispatchResult : byte
		{
			/// <summary>
			/// Event has been dispatched properly, but there is no specific action to take.
			/// </summary>
			Ok = 0,

			/// <summary>
			/// Stops **native** propagation of the event to parent elements (a.k.a. Handled).
			/// </summary>
			StopPropagation = 1, // a.k.a. Handled

			/// <summary>
			/// This prevents the default native behavior of the event.
			/// For instance mouse wheel to scroll the view, tab to changed focus, etc.
			/// WARNING: Cf. remarks
			/// </summary>
			/// <remarks>
			/// The "default behavior" is applied only once the event as reached the root element.
			/// This means that if a parent element requires to prevent the default behavior, it will also prevent the default for all its children.
			/// For instance preventing the default behavior for the wheel event on a `Popup`, will also disable the mouse wheel scrolling for its content.
			/// </remarks>
			PreventDefault = 2,

			/// <summary>
			/// The event has not been dispatch.
			/// WARNING: This must not be used by application.
			/// It only indicates that there is no active listener for that event and it should not be raised anymore.
			/// It should not in anyway indicates an error in event processing.
			/// </summary>
			NotDispatched = 128
		}
	}
}
