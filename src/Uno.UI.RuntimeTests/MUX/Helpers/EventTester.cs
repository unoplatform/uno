// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// EventTester.cs

using Common;
using Private.Infrastructure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using UIExecutor = MUXControlsTestApp.Utilities.RunOnUIThread;
using MUXControlsTestApp.Utilities;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Tests.Common
{
	[Flags]
	public enum EventTesterOptions : int
	{
		Default = None,
		None = 0x000,
		CaptureWindowOnTimeout = 0x001,
		CaptureWindowBefore = 0x002,
		CaptureWindowAfter = 0x004,
		CaptureScreenOnTimeout = 0x011,
		CaptureScreenBefore = 0x012,
		CaptureScreenAfter = 0x014,
		ThrowOnTimeout = 0x100,
		BVTEvent = CaptureScreenBefore | CaptureScreenAfter | CaptureScreenOnTimeout | ThrowOnTimeout,
	}

	public static class EventTesterConfig
	{
		public static readonly TimeSpan Default_Timeout = TimeSpan.FromSeconds(5);
		public static readonly TimeSpan BVT_Timeout = TimeSpan.FromMinutes(2);
		public static TimeSpan Timeout = Default_Timeout;
	}

	public class EventTester<TSender, TEventArgs> : IDisposable
	{
		protected readonly TSender sender;
		protected readonly Type senderType;
		protected readonly String eventName;
		protected readonly Delegate handlerInvocationDelegate;
		private readonly List<Tuple<object, TEventArgs>> eventData;
		private readonly UnoAutoResetEvent resetEvent;
		private readonly Action<object, TEventArgs> Action;
		private Action<object> removeMethod;
		private readonly EventTesterOptions options;


		protected EventTester(TSender sender, Type senderType, string eventName, Action<object, TEventArgs> action, EventTesterOptions options, bool setBVTflags)
		{
			this.eventName = eventName;
			this.eventData = new List<Tuple<object, TEventArgs>>();
			this.sender = sender;
			this.resetEvent = new UnoAutoResetEvent(false);
			this.Action = action;
			this.options = options;
			if (setBVTflags)
			{
				this.options |= EventTesterOptions.BVTEvent;
			}

			this.senderType = senderType;
			var delegateType = GetDelegateType(eventName);

			Type eventArgType = delegateType.GetTypeInfo().GetDeclaredMethod("Invoke").GetParameters()[1].ParameterType;

			// Check to ensure that the caller passed in the correct event args.
			if (typeof(TEventArgs) != eventArgType)
			{
				throw new ArgumentException($"The event arg '{typeof(TEventArgs).Name}' does not match the event arg for {typeof(TSender)}.{eventName}. Expected '{eventArgType.Name}");
			}


			// Because we use different Event Handlers for different events (i.e. GotFocus uses RoutedEventHandler while Flyout.Click uses EventHandler<object>),
			// we need to convert them to a delegate and use the delegate as our handler.
			Action<object, TEventArgs> handler = this.OnEventFired;
			MethodInfo handlerInvoke = handler.GetType().GetRuntimeMethod("Invoke", new[] { typeof(object), typeof(TEventArgs) });
			this.handlerInvocationDelegate = handlerInvoke.CreateDelegate(delegateType, handler);

			UIExecutor.Execute(() =>
			{
				this.AddEvent();
			});

#if !__WASM__
			if (this.options.HasFlag(EventTesterOptions.CaptureWindowBefore))
			{
				this.CaptureWindowAsync("Before").Wait(this.Timeout);
			}
			if (this.options.HasFlag(EventTesterOptions.CaptureScreenBefore))
			{
				this.CaptureScreenAsync("Before").Wait(this.Timeout);
			}
#endif
		}

		protected EventTester(TSender sender, Type senderType, string eventName, Action<object, TEventArgs> action, EventTesterOptions options)
			: this(sender, senderType, eventName, action, options, true)
		{
		}

		protected virtual Type GetDelegateType(string eventName)
		{
			var eventInfo = this.senderType.GetRuntimeEvent(eventName);
			// We should never be in a situation where we are trying to reflect against an event that isn't part of the class
			if (eventInfo == null)
			{
				throw new ArgumentException($"This class does not have an event named '{eventName}'");
			}
			this.removeMethod = ert => InvokeRemoveMethod(eventInfo.RemoveMethod, this.sender, new object[] { ert });
			return eventInfo.AddMethod.GetParameters()[0].ParameterType;
		}

		public EventTester(TSender sender, string eventName, Action<object, TEventArgs> action)
			: this(sender, sender.GetType(), eventName, action, EventTesterOptions.Default)
		{
		}

		public EventTester(TSender sender, string eventName)
			: this(sender, eventName, (s, e) => { })
		{
		}

		public EventTester(TSender sender, string eventName, EventTesterOptions options)
			: this(sender, sender.GetType(), eventName, (s, e) => { }, options)
		{
		}

		public EventTester(TSender sender, string eventName, EventTesterOptions options, bool setBVTflags)
			: this(sender, sender.GetType(), eventName, (s, e) => { }, options, setBVTflags)
		{
		}

		protected EventTester(TSender sender, Type senderType, string eventName)
			: this(sender, senderType, eventName, (s, e) => { }, EventTesterOptions.Default)
		{
		}

		public static EventTester<object, TEventArgs> FromStaticEvent<T>(string eventName)
		{
			return new EventTester<object, TEventArgs>(null, typeof(T), eventName, (s, e) => { }, EventTesterOptions.Default);
		}

		public static EventTester<UIElement, TEventArgs> FromRoutedEvent(UIElement sender, string eventName, Action<object, TEventArgs> action)
		{
			return new RoutedEventTester<TEventArgs>(sender, eventName, action);
		}

		public TimeSpan DefaultTimeout = EventTesterConfig.Timeout;

		private TimeSpan Timeout
		{
			get
			{
				if (Debugger.IsAttached)
				{
					return TimeSpan.FromMilliseconds(-1); // Wait indefinitely if debugger is attached.
				}
				else
				{
					return this.DefaultTimeout;
				}
			}
		}

		private TSender Sender
		{
			get
			{
				return this.sender;
			}
		}

		protected void OnEventFired(object sender, TEventArgs e)
		{
			// Sometimes this is getting called even after this object has been disposed. Issue tracked by:
			// Task 29916043: EventTester (used by some managed MUX tests) is sometimes unreliable due to a race condition
			if (!(this.isDisposing || this.disposedValue))
			{
				this.eventData.Add(Tuple.Create(sender, e));
				this.resetEvent.Set();
				this.Action(sender, e);
			}
			else
			{
				Log.Warning($"Event {this.eventName} fired but its getting disposed");
			}
		}

		private static T InvokeAddMethod<T>(MethodInfo method, object obj, object[] args)
		{
			return (T)method.Invoke(obj, args);
		}

		private static void InvokeRemoveMethod(MethodInfo method, object obj, object[] args)
		{
			method.Invoke(obj, args);
		}

		protected virtual void AddEvent()
		{
			var eventInfo = this.senderType.GetRuntimeEvent(eventName);
			eventInfo.AddMethod.Invoke(this.sender, new object[] { handlerInvocationDelegate });
		}

		protected virtual void RemoveEvent()
		{
			var eventInfo = this.senderType.GetRuntimeEvent(eventName);
			eventInfo.RemoveMethod.Invoke(this.sender, new object[] { handlerInvocationDelegate });
		}

		public int ExecuteCount
		{
			get
			{
				return this.eventData.Count;
			}
		}

		public bool HasFired
		{
			get
			{
				return this.eventData.Count >= 1;
			}
		}

		public object LastSender
		{
			get
			{
				return this.eventData.LastOrDefault()?.Item1;
			}
		}

		public TEventArgs LastArgs
		{
			get
			{
				if (HasFired)
				{
					return this.eventData.LastOrDefault().Item2;
				}
				else
				{
					return default(TEventArgs);
				}
			}
		}


		public async Task<bool> Wait()
		{
			return await this.Wait(this.Timeout);
		}

		public async Task<bool> Wait(TimeSpan timeout)
		{
			var result = await this.resetEvent.WaitOne(timeout);
			if (!result)
			{
				Verify.Fail($"Event '{eventName}' was not raised before timeout.");
			}
			return result;
		}

		public void Reset()
		{
			this.resetEvent.Reset();
		}

		public async Task<bool> WaitForNoThrow(TimeSpan timeout)
		{
			return await this.resetEvent.WaitOne(timeout);
		}

		private async Task CaptureScreenAsync(string prefix = "")
		{
			try
			{
				await Task.Run(() =>
				{
					Log.Comment($"Capturing screen {prefix} event test {eventName}EventTester");
					//global::Private.Infrastructure.TestServices.Utilities.CaptureScreen($"{prefix}{eventName}EventTester");
				});
			}
			catch
			{
				Log.Warning($"CaptureScreenAsync failed");
			}
		}

		private async Task CaptureWindowAsync(string prefix = "")
		{
			Log.Comment($"Capturing window from event test {eventName}EventTester");
			var current = default(Window);
			var failed = false;
			try
			{
				Log.Comment($"Capturing screen {prefix} event test {eventName}EventTester");
				UIExecutor.Execute(() => { current = Window.Current; });
				if (current != null)
				{
					//await current.CaptureWindowAsync($"{prefix}{eventName}EventTester");
				}
			}
			catch
			{
				failed = true;
			}

			if (current == null || failed)
			{
				if (current == null)
				{
					Log.Warning($"No active Window, fallback to CaptureScreen");
				}
				else if (failed)
				{
					Log.Warning($"Previuos CaptureWindow failed, fallback to CaptureScreen");
				}
				await CaptureScreenAsync(prefix);
			}
		}

		public async Task<bool> WaitAsync()
		{
			return await this.WaitAsync(this.Timeout);
		}

		public async Task<bool> WaitAsync(TimeSpan timeout)
		{
			var tcs = new TaskCompletionSource<bool>();

#if __WASM__
			var sw = Stopwatch.StartNew();

			var result = false;
			while (!(result = await this.resetEvent.WaitOne(0)) && sw.Elapsed < timeout)
			{
				Console.WriteLine("waiting...");
				await Task.Delay(100);
			}

#else
			Console.WriteLine("Before StartNew...");
			await Task.Factory.StartNew(async () =>
			{
				try
				{
					tcs.SetResult(await this.WaitForNoThrow(timeout));
				}
				catch
				{
					tcs.SetResult(false);
				}
			});

			var result = await tcs.Task;
#endif

			if (!result)
			{
				if (this.options.HasFlag(EventTesterOptions.CaptureWindowOnTimeout))
				{
					await this.CaptureWindowAsync("Timeout");
				}
				if (this.options.HasFlag(EventTesterOptions.CaptureScreenOnTimeout))
				{
					await this.CaptureScreenAsync("Timeout");
				}
				if (this.options.HasFlag(EventTesterOptions.ThrowOnTimeout))
				{
					throw new TimeoutException($"Event '{this.eventName}' was not raised before timeout.");
				}
			}

			return result;
		}

		public async Task VerifyEventRaised()
		{
			string prefix = string.Empty;
			UIExecutor.Execute(() =>
			{
				var element = this.sender as FrameworkElement;
				if (element != null && !string.IsNullOrEmpty(element.Name))
				{
					prefix = element.Name + ".";
				}
			});
			Verify.IsTrue(await this.WaitAsync(), $"Event '{prefix}{this.eventName}' was not raised before timeout.");
		}

		public async Task VerifyEventNotRaised()
		{
			string prefix = string.Empty;
			UIExecutor.Execute(() =>
			{
				var element = this.sender as FrameworkElement;
				if (element != null && !string.IsNullOrEmpty(element.Name))
				{
					prefix = element.Name + ".";
				}
			});
			Verify.IsFalse(await this.WaitAsync(TimeSpan.FromMilliseconds(100)), $"Event '{prefix}{this.eventName}' was raised before timeout.");
		}

		#region IDisposable Support

		private bool disposedValue; // To detect redundant calls
		private bool isDisposing;

		private void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				this.isDisposing = disposing;
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
					UIExecutor.Execute(() =>
					{
						this.RemoveEvent();
					});

					_ = TestServices.WindowHelper.WaitForIdle();

					//if (XamlTestsBase.IsBVT)
					//{
					//	Log.Comment($"Disposing Event '{this.eventName}', executions count: {this.ExecuteCount}");
					//}

#if !__WASM__
					if (this.options.HasFlag(EventTesterOptions.CaptureWindowAfter))
					{
						this.CaptureWindowAsync("After").Wait(this.Timeout);
					}
					if (this.options.HasFlag(EventTesterOptions.CaptureScreenAfter))
					{
						this.CaptureScreenAsync("After").Wait(this.Timeout);
					}
#endif

					resetEvent.Dispose();
					this.isDisposing = false;
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}

		#endregion
	}

	public sealed class RoutedEventTester<TEventArgs> : EventTester<UIElement, TEventArgs>
	{
		public RoutedEventTester(UIElement sender, string eventName, Action<object, TEventArgs> action) : base(sender, eventName, action)
		{
		}

		protected override void AddEvent()
		{
			//We pass null to GetValue to look for a static property
			RoutedEvent routedEvent = typeof(UIElement).GetTypeInfo().DeclaredProperties.Where(p => p.Name == (this.eventName + "Event")).First().GetValue(null) as RoutedEvent;

			sender.AddHandler(
				routedEvent,
				this.handlerInvocationDelegate,
				true);
		}

		protected override void RemoveEvent()
		{
			RoutedEvent routedEvent = typeof(UIElement).GetTypeInfo().DeclaredProperties.Where(p => p.Name == (this.eventName + "Event")).First().GetValue(null) as RoutedEvent;

			sender.RemoveHandler(
				routedEvent,
				this.handlerInvocationDelegate);
		}
	}

	public class AttachedEventTester<TEventArgs> : EventTester<UIElement, TEventArgs>
	{
		public AttachedEventTester(UIElement sender, Type attachedOn, string eventName) : base(sender, attachedOn, eventName)
		{
		}


		public AttachedEventTester(UIElement sender, Type attachedOn, string eventName, Action<object, TEventArgs> action) : base(sender, attachedOn, eventName, action, EventTesterOptions.Default)
		{
		}

		protected override void AddEvent()
		{
			//We pass null to GetValue to look for a static property
			var addHandlerMethod = senderType.GetTypeInfo().DeclaredMethods.Where(p => p.Name == ($"Add{this.eventName}Handler")).First();
			addHandlerMethod.Invoke(null, new object[] { sender, this.handlerInvocationDelegate });
		}

		protected override void RemoveEvent()
		{
			var removeHandlerMethod = senderType.GetTypeInfo().DeclaredMethods.Where(p => p.Name == ($"Remove{this.eventName}Handler")).First();
			removeHandlerMethod.Invoke(null, new object[] { sender, this.handlerInvocationDelegate });
		}

		protected override Type GetDelegateType(string eventName)
		{
			var addHandlerMethod = senderType.GetTypeInfo().DeclaredMethods.Where(p => p.Name == ($"Add{this.eventName}Handler")).First();

			return addHandlerMethod.GetParameters()[1].ParameterType;
		}

	}

	public class AttachedRoutedEventTester<TEventArgs> : AttachedEventTester<TEventArgs>
	{
		public AttachedRoutedEventTester(UIElement sender, Type attachedOn, string eventName) : base(sender, attachedOn, eventName)
		{
		}

		protected override void AddEvent()
		{
			var routedEvent = senderType.GetTypeInfo().DeclaredProperties.Where(p => p.Name == (this.eventName + "Event")).First().GetValue(null) as RoutedEvent;

			sender.AddHandler(
				routedEvent,
				this.handlerInvocationDelegate,
				true);

		}

		protected override void RemoveEvent()
		{
			var routedEvent = senderType.GetTypeInfo().DeclaredProperties.Where(p => p.Name == (this.eventName + "Event")).First().GetValue(null) as RoutedEvent;

			sender.RemoveHandler(
				routedEvent,
				this.handlerInvocationDelegate);

		}

	}
}
