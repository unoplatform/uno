#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;
using Uno.Extensions.Specialized;

namespace Windows.UI.Input.Preview.Injection;

public partial class InputInjector 
{
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public static InputInjector? TryCreate()
#if UNO_HAS_MANAGED_POINTERS
		=> CoreWindow.GetForCurrentThread() is { } window ? new InputInjector(window) : null;
#else
		=> null;
#endif

	private readonly InjectedInputState _mouse = new(PointerDeviceType.Mouse);
	private (InjectedInputState state, bool isAdded)? _touch;

	/// <summary>
	/// Gets the current state of the mouse pointer
	/// </summary>
	internal InjectedInputState Mouse => _mouse;

#if UNO_HAS_MANAGED_POINTERS
	private readonly CoreWindow _window;

	private InputInjector(CoreWindow window)
	{
		_window = window;
	}
#else
	private InputInjector()
	{
	}
#endif

	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void InitializeTouchInjection(InjectedInputVisualizationMode visualMode)
	{
		UninitializeTouchInjection();

		_touch = (new InjectedInputState(PointerDeviceType.Touch), isAdded: false);
	}

	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void UninitializeTouchInjection()
	{
		if (_touch is not null)
		{
			var cancel = new InjectedInputTouchInfo { PointerInfo = new() { PointerOptions = InjectedInputPointerOptions.Canceled } };

			InjectPointerRemoved(cancel.ToEventArgs(_touch.Value.state));

			_touch = null;
		}
	}

	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__NETSTD_REFERENCE__", "__MACOS__")]
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
				InjectPointerAdded(args);
				_touch = (touch, isAdded: true);
			}

			touch.Update(args);

			InjectPointerUpdated(args);
		}
	}

	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void InjectMouseInput(IEnumerable<InjectedInputMouseInfo> input)
	{
		foreach (var info in input)
		{
			var args = info.ToEventArgs(_mouse!);
			_mouse!.Update(args);

			InjectPointerUpdated(args);
		}
	}

#if UNO_HAS_MANAGED_POINTERS
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void InjectPointerAdded(PointerEventArgs args)
	{
		_window.InjectPointerAdded(args);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void InjectPointerUpdated(PointerEventArgs args)
		=> _window.InjectPointerUpdated(args);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void InjectPointerRemoved(PointerEventArgs args)
		=> _window.InjectPointerRemoved(args);
#else
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void InjectPointerAdded(PointerEventArgs args)
		=> throw new InvalidOperationException("Input injection is not supported on this platform.");

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void InjectPointerUpdated(PointerEventArgs args)
		=> throw new InvalidOperationException("Input injection is not supported on this platform.");

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void InjectPointerRemoved(PointerEventArgs args)
		=> throw new InvalidOperationException("Input injection is not supported on this platform.");
#endif
}
