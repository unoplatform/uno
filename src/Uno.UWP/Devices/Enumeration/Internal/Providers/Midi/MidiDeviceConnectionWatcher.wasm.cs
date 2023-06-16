using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;
using Uno.Extensions.Specialized;
using Uno.Foundation;

#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.JavaScript;
#endif

namespace Uno.Devices.Enumeration.Internal.Providers.Midi
{
	public static partial class MidiDeviceConnectionWatcher
	{
#if !NET7_0_OR_GREATER
		private const string JsType = "Uno.Devices.Enumeration.Internal.Providers.Midi.MidiDeviceConnectionWatcher";
#endif

		private static readonly object _syncLock = new object();

		private static readonly List<MidiDeviceClassProviderBase> _observers = new List<MidiDeviceClassProviderBase>();

		internal static void AddObserver(MidiDeviceClassProviderBase provider)
		{
			lock (_syncLock)
			{
				_observers.Add(provider);
				if (_observers.Count == 1)
				{
#if NET7_0_OR_GREATER
					NativeMethods.StartStateChanged();
#else
					// start WASM device event subscription
					var command = $"{JsType}.startStateChanged()";
					WebAssemblyRuntime.InvokeJS(command);
#endif
				}
			}
		}

		internal static void RemoveObserver(MidiDeviceClassProviderBase provider)
		{
			lock (_syncLock)
			{
				_observers.Remove(provider);
				if (_observers.Count == 0)
				{
#if NET7_0_OR_GREATER
					NativeMethods.StopStateChanged();
#else
					// stop WASM device event subscription
					var command = $"{JsType}.stopStateChanged()";
					WebAssemblyRuntime.InvokeJS(command);
#endif
				}
			}
		}

#if NET7_0_OR_GREATER
		[JSExport]
#endif
		public static int DispatchStateChanged(string id, string name, bool isInput, bool isConnected)
		{
			if (isInput)
			{
				var inputs = _observers.OfType<MidiInDeviceClassProvider>();
				if (isConnected)
				{
					inputs.ForEach(provider => provider.OnDeviceAdded(id, name));
				}
				else
				{
					inputs.ForEach(provider => provider.OnDeviceRemoved(id));
				}
			}
			else
			{
				var outputs = _observers.OfType<MidiOutDeviceClassProvider>();
				if (isConnected)
				{
					outputs.ForEach(provider => provider.OnDeviceAdded(id, name));
				}
				else
				{
					outputs.ForEach(provider => provider.OnDeviceRemoved(id));
				}
			}

			return 0;
		}

#if NET7_0_OR_GREATER
		internal static partial class NativeMethods
		{
			[JSImport($"globalThis.Uno.Devices.Enumeration.Internal.Providers.Midi.MidiDeviceConnectionWatcher.startStateChanged")]
			internal static partial void StartStateChanged();

			[JSImport($"globalThis.Uno.Devices.Enumeration.Internal.Providers.Midi.MidiDeviceConnectionWatcher.stopStateChanged")]
			internal static partial void StopStateChanged();
		}
#endif
	}
}
