#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Uno.Extensions.Specialized;

namespace Windows.UI.Input.Preview.Injection;

public partial class InputInjector
{
	[ThreadStatic] private static IInputInjectorTarget? _inputManager;

	internal static void SetTargetForCurrentThread(IInputInjectorTarget manager)
		=> _inputManager ??= manager; // Set only once per thread.

	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public static InputInjector? TryCreate()
		=> _inputManager is not null ? new InputInjector(_inputManager) : null;

	private readonly InjectedInputState _mouse = new(PointerDeviceType.Mouse);
	private (InjectedInputState state, bool isAdded)? _touch;

	/// <summary>
	/// Gets the current state of the mouse pointer
	/// </summary>
	internal InjectedInputState Mouse => _mouse;

	private readonly IInputInjectorTarget _target;

	private InputInjector(IInputInjectorTarget target)
	{
		_target = target;
	}

	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void InitializeTouchInjection(InjectedInputVisualizationMode visualMode)
	{
		UninitializeTouchInjection();

		_touch = (new InjectedInputState(PointerDeviceType.Touch), isAdded: false);
	}

	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void UninitializeTouchInjection()
	{
		if (_touch is not null)
		{
			var cancel = new InjectedInputTouchInfo { PointerInfo = new() { PointerOptions = InjectedInputPointerOptions.Canceled } };

			_target.InjectPointerRemoved(cancel.ToEventArgs(_touch.Value.state));

			_touch = null;
		}
	}

	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void InjectTouchInput(IEnumerable<InjectedInputTouchInfo> input)
	{
		if (_touch is null)
		{
			InitializeTouchInjection(InjectedInputVisualizationMode.Default);
		}

		var touch = _touch!.Value.state;
		foreach (var info in input)
		{
			var args = info.ToEventArgs(touch);

			if (_touch is { isAdded: false })
			{
				_target.InjectPointerAdded(args);
				_touch = (touch, isAdded: true);
			}

			touch.Update(args);

			_target.InjectPointerUpdated(args);

			if (info.PointerInfo.PointerOptions.HasFlag(InjectedInputPointerOptions.PointerUp))
			{
				_target.InjectPointerRemoved(args);
				_touch = (touch, isAdded: false);
			}
		}
	}

	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void InjectMouseInput(IEnumerable<InjectedInputMouseInfo> input)
	{
		foreach (var info in input)
		{
			var args = info.ToEventArgs(_mouse!, VirtualKeyModifiers.None);
			_mouse!.Update(args);

			_target.InjectPointerUpdated(args);
		}
	}

	internal void InjectMouseInput(IEnumerable<(InjectedInputMouseInfo, VirtualKeyModifiers)> input)
	{
		foreach (var (info, modifiers) in input)
		{
			var args = info.ToEventArgs(_mouse!, modifiers);
			_mouse!.Update(args);

			_target.InjectPointerUpdated(args);
		}
	}
}
