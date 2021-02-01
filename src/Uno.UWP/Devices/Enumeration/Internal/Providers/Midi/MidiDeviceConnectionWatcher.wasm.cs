using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;
using Uno.Extensions.Specialized;
using Uno.Foundation;

namespace Uno.Devices.Enumeration.Internal.Providers.Midi
{
	public static class MidiDeviceConnectionWatcher
	{
		private const string JsType = "Uno.Devices.Enumeration.Internal.Providers.Midi.MidiDeviceConnectionWatcher";

		private static readonly object _syncLock = new object();

		private static readonly List<MidiDeviceClassProviderBase> _observers = new List<MidiDeviceClassProviderBase>();

		internal static void AddObserver(MidiDeviceClassProviderBase provider)
		{
			lock (_syncLock)
			{
				_observers.Add(provider);
				if (_observers.Count == 1)
				{
					// start WASM device event subscription
					var command = $"{JsType}.startStateChanged()";
					WebAssemblyRuntime.InvokeJS(command);
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
					// stop WASM device event subscription
					var command = $"{JsType}.stopStateChanged()";
					WebAssemblyRuntime.InvokeJS(command);
				}
			}
		}

		[Preserve]
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
	}
}
