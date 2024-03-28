using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Serialization;

using Uno;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Xaml;
using Windows.UI.Xaml.Controls;


namespace Windows.UI.Xaml
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

				var result = new HtmlEventDispatchResultHelper();
				foreach (var invocationItem in _invocationList)
				{
					if (invocationItem.Handler is RawEventHandler rawHandler)
					{
						var handlerResult = rawHandler(_owner, nativeEventPayload);
						result.Add(handlerResult);
					}
					else
					{
						var handlerResult = invocationItem.Invoker(invocationItem.Handler, _owner, args);
						result.Add(args, handlerResult);
					}

					if (result.ShouldStop)
					{
						// We stop on first handler that requires to stop propagation (same behavior has Handled on UWP)
						return result.Value;
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

			if (!_eventHandlers.TryGetValue((eventName, onCapturePhase), out var registration))
			{
				_eventHandlers[(eventName, onCapturePhase)] = registration = new EventRegistration(
					this,
					eventName,
					onCapturePhase,
					eventExtractor,
					payloadConverter);
			}

			registration.Add(handler, invoker);
		}

		internal void UnregisterEventHandler(string eventName, Delegate handler, GenericEventHandler invoker, bool onCapturePhase = false)
		{
			if (_eventHandlers.TryGetValue((eventName, onCapturePhase), out var registration))
			{
				registration.Remove(handler, invoker);
			}
			else if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug(message: $"No handler registered for event {eventName}.");
			}
		}

		internal HtmlEventDispatchResult InternalDispatchEvent(string eventName, EventArgs eventArgs = null, string nativeEventPayload = null, bool onCapturePhase = false)
		{
			var n = eventName;
			try
			{
				return InternalInnerDispatchEvent(eventArgs, nativeEventPayload, n, onCapturePhase);
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
		private HtmlEventDispatchResult InternalInnerDispatchEvent(EventArgs eventArgs, string nativeEventPayload, string n, bool onCapturePhase = false)
		{
			if (_eventHandlers.TryGetValue((n, onCapturePhase), out var registration))
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
		[JSExport]
		[Preserve]
		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static int DispatchEvent(int handle, string eventName, string eventArgs, bool onCapturePhase)
		{
#if DEBUG
			try
#endif
			{
				// Dispatch to right object, if we can find it
				if (GetElementFromHandle(handle) is UIElement element)
				{
					return (int)element.InternalDispatchEvent(eventName, nativeEventPayload: eventArgs, onCapturePhase: onCapturePhase);
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

		private class TupleComparer : IEqualityComparer<(string, bool)>
		{
			public bool Equals((string, bool) x, (string, bool) y)
			{
				return StringComparer.OrdinalIgnoreCase.Equals(x.Item1, y.Item1) && x.Item2 == y.Item2;
			}

			public int GetHashCode((string, bool) obj)
			{
				return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item1) ^ obj.Item2.GetHashCode();
			}
		}

		private readonly Dictionary<(string name, bool onCapturePhase), EventRegistration> _eventHandlers = new Dictionary<(string, bool), EventRegistration>(new TupleComparer());

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
	}
}
