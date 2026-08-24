using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Foundation.Logging;

namespace Windows.Devices.Input;

internal static class PointerIdentifierPool
{
	private static readonly Dictionary<PointerIdentifier, PointerIdentifier> _nativeToManagedPointerId = new();
	private static readonly Dictionary<PointerIdentifier, PointerIdentifier> _managedToNativePointerId = new();
	private static uint _lastUsedId;

	public static PointerIdentifier RentManaged(PointerIdentifier nativeId)
	{
		if (_nativeToManagedPointerId.TryGetValue(nativeId, out var managedId))
		{
			return managedId;
		}

		managedId = new PointerIdentifier(nativeId.Type, ++_lastUsedId);
		_managedToNativePointerId[managedId] = nativeId;
		_nativeToManagedPointerId[nativeId] = managedId;

		return managedId;
	}

	public static bool TryGetNative(PointerIdentifier managedId, out PointerIdentifier nativeId)
		=> _managedToNativePointerId.TryGetValue(managedId, out nativeId);

	public static void ReleaseManaged(PointerIdentifier managedId)
	{
		if (_managedToNativePointerId.TryGetValue(managedId, out var nativeId))
		{
			_managedToNativePointerId.Remove(managedId);
			_nativeToManagedPointerId.Remove(nativeId);

			if (_managedToNativePointerId.Count == 0)
			{
				_lastUsedId = 0; // We reset the pointer ID only when there is no active pointer.
			}
		}
		else if (typeof(PointerIdentifierPool).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(PointerIdentifierPool).Log().Warn($"Received an invalid managed pointer id {managedId}");
		}
	}
}
