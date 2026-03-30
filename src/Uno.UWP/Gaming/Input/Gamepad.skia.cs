#nullable enable

using System;
using System.Collections.Generic;
using Uno.Foundation.Extensibility;
using Uno.Gaming.Input.Internal;

namespace Windows.Gaming.Input;

public partial class Gamepad
{
	private static IGamepadExtension? _extension;
	private static bool _extensionInitialized;

	private readonly int _userIndex;

	internal Gamepad(int userIndex)
	{
		_userIndex = userIndex;
	}

	internal int UserIndex => _userIndex;

	public GamepadReading GetCurrentReading()
	{
		var extension = EnsureExtension();
		if (extension is not null)
		{
			return extension.GetCurrentReading(this);
		}

		return default;
	}

	private static IReadOnlyList<Gamepad> GetGamepadsInternal()
	{
		var extension = EnsureExtension();
		if (extension is not null)
		{
			return extension.GetGamepads();
		}

		return Array.Empty<Gamepad>();
	}

	private static void StartGamepadAdded()
	{
		var extension = EnsureExtension();
		extension?.StartMonitoring();
	}

	private static void EndGamepadAdded()
	{
		if (!_gamepadRemovedWrapper.IsActive)
		{
			var extension = EnsureExtension();
			extension?.StopMonitoring();
		}
	}

	private static void StartGamepadRemoved()
	{
		var extension = EnsureExtension();
		extension?.StartMonitoring();
	}

	private static void EndGamepadRemoved()
	{
		if (!_gamepadAddedWrapper.IsActive)
		{
			var extension = EnsureExtension();
			extension?.StopMonitoring();
		}
	}

	private static IGamepadExtension? EnsureExtension()
	{
		if (!_extensionInitialized)
		{
			ApiExtensibility.CreateInstance<IGamepadExtension>(
				typeof(Gamepad),
				out _extension);
			_extensionInitialized = true;
		}

		return _extension;
	}
}
