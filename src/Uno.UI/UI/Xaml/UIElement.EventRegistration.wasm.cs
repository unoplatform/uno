using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.Extensions.Logging;
using Uno;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml
{
	partial class UIElement
	{
		private delegate bool RawEventHandler(UIElement sender, string paylaod);

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
			private bool _isSubscribed = false;
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
						args = _payloadConverter(_owner, nativeEventPayload);
					}

					foreach (var invocationItem in _invocationList)
					{
						if (invocationItem.Handler is RawEventHandler rawHandler)
						{
							if (rawHandler(_owner, nativeEventPayload))
							{
								return true;
							}
						}
						else
						{
							var result = invocationItem.Invoker(invocationItem.Handler, _owner, args);

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

				this.Log().Warn(message: $"{this}-{HtmlId}: No Handler for {n}. Registered: {registered}");
			}
			catch (Exception e)
			{
				this.Log().Error(message: $"{this}-{HtmlId}/{eventName}/\"{nativeEventPayload}\": Error: {e}");
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

		private readonly Dictionary<string, EventRegistration> _eventHandlers = new Dictionary<string, EventRegistration>(StringComparer.OrdinalIgnoreCase);

		internal delegate EventArgs EventArgsParser(object sender, string payload);

		internal enum HtmlEventExtractor : int
		{
			None = 0,
			PointerEventExtractor = 1, // See PayloadToPointerArgs
			TappedEventExtractor = 2,
			KeyboardEventExtractor = 3,
			FocusEventExtractor = 4,
			CustomEventDetailStringExtractor = 5, // For use with CustomEvent("name", {detail:{string detail here}})
			CustomEventDetailJsonExtractor = 6, // For use with CustomEvent("name", {detail:{detail here}}) - will be JSON.stringify
		}
	}
}
