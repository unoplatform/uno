#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Logging;
using Windows.Devices.Input;
using Windows.System;
using Windows.UI.Core;

namespace Windows.UI.Input.Preview.Injection;

public partial class InputInjector
{
	[ThreadStatic] private static IInputInjectorTarget? _inputManager;

	internal static void SetTargetForCurrentThread(IInputInjectorTarget manager)
	{
		if (_inputManager is not null &&
			_inputManager != manager &&
			manager.Log().IsEnabled(LogLevel.Warning))
		{
			manager.Log().LogWarning($"InputInjector is already set for this thread.");
		}

		_inputManager ??= manager; // Set only once per thread.
	}

	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__NETSTD_REFERENCE__")]
	public static InputInjector? TryCreate()
		=> _inputManager is not null ? new InputInjector(_inputManager) : null;

	/// <summary>
	/// Creates an <see cref="InputInjector"/> whose injected pointer events carry
	/// a relative root. The <see cref="InputManager"/> uses that root to scope
	/// hit-testing, letting automation bypass design-time overlays (e.g. Hot Design)
	/// to reach the inner application underneath.
	/// </summary>
	/// <param name="relativeRoot">
	/// The UI element that should be used as the hit-test root for every pointer
	/// event produced by this injector. Typed as <c>object</c> because this package
	/// cannot reference <c>Microsoft.UI.Xaml.UIElement</c>; the input manager casts
	/// via <c>as UIElement</c>.
	/// </param>
	[EditorBrowsable(EditorBrowsableState.Never)]
	[global::Uno.UnoOnly]
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__NETSTD_REFERENCE__")]
	public static InputInjector? TryCreate(object relativeRoot)
	{
		var injector = TryCreate();
		if (injector is not null)
		{
			injector._relativeRoot = relativeRoot;
		}

		return injector;
	}

	private readonly InjectedInputState _mouse = new(PointerDeviceType.Mouse);
	private (InjectedInputState state, bool isAdded)? _touch;
	private (InjectedInputState state, bool isAdded)? _pen;
	private object? _relativeRoot;

	/// <summary>
	/// Gets the current state of the mouse pointer
	/// </summary>
	internal InjectedInputState Mouse => _mouse;

	/// <summary>
	/// Gets the current state of the pen pointer
	/// </summary>
	internal InjectedInputState? Pen => _pen?.state;

	private readonly IInputInjectorTarget _target;

	private InputInjector(IInputInjectorTarget target)
	{
		_target = target;
	}

	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "__TVOS__", "IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
	public void InitializeTouchInjection(InjectedInputVisualizationMode visualMode)
	{
		UninitializeTouchInjection();

		_touch = (new InjectedInputState(PointerDeviceType.Touch), isAdded: false);
	}

	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "__TVOS__", "IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
	public void UninitializeTouchInjection()
	{
		if (_touch is not null)
		{
			var cancel = new InjectedInputTouchInfo
			{
				PointerInfo = new()
				{
					PointerId = 42,
					PointerOptions = InjectedInputPointerOptions.Canceled
				}
			};

			DispatchPointerRemoved(cancel.ToEventArgs(_touch.Value.state));

			_touch = null;
		}
	}

	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "__TVOS__", "IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
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
				DispatchPointerAdded(args);
				_touch = (touch, isAdded: true);
			}

			touch.Update(args);

			DispatchPointerUpdated(args);

			if (info.PointerInfo.PointerOptions.HasFlag(InjectedInputPointerOptions.PointerUp))
			{
				DispatchPointerRemoved(args);
				_touch = (touch, isAdded: false);
			}
		}
	}

	// TODO: Move as extension method
	internal async ValueTask InjectTouchInputAsync(IEnumerable<InjectedInputTouchInfo> input, CancellationToken ct)
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
				DispatchPointerAdded(args);
				_touch = (touch, isAdded: true);
				await WaitForIdle(ct);
			}

			touch.Update(args);

			DispatchPointerUpdated(args);
			await WaitForIdle(ct);

			if (info.PointerInfo.PointerOptions.HasFlag(InjectedInputPointerOptions.PointerUp))
			{
				DispatchPointerRemoved(args);
				_touch = (touch, isAdded: false);
				await WaitForIdle(ct);
			}
		}
	}

	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "__TVOS__", "IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
	public void InitializePenInjection(InjectedInputVisualizationMode visualMode)
	{
		UninitializePenInjection();

		_pen = (new InjectedInputState(PointerDeviceType.Pen), isAdded: false);
	}

	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "__TVOS__", "IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
	public void UninitializePenInjection()
	{
		if (_pen is not null)
		{
			var pen = _pen.Value;

			if (pen.isAdded)
			{
				var cancel = new InjectedInputPenInfo
				{
					PointerInfo = new()
					{
						PointerId = pen.state.PointerId,
						PointerOptions = InjectedInputPointerOptions.Canceled
					}
				};

				DispatchPointerRemoved(cancel.ToEventArgs(pen.state));
			}

			_pen = null;
		}
	}

	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "__TVOS__", "IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
	public void InjectPenInput(InjectedInputPenInfo input)
	{
		if (_pen is null)
		{
			InitializePenInjection(InjectedInputVisualizationMode.Default);
		}

		var pen = _pen!.Value.state;
		var args = input.ToEventArgs(pen);

		if (_pen is { isAdded: false })
		{
			DispatchPointerAdded(args);
			_pen = (pen, isAdded: true);
		}

		pen.Update(args);

		DispatchPointerUpdated(args);

		if (input.PointerInfo.PointerOptions.HasFlag(InjectedInputPointerOptions.PointerUp))
		{
			DispatchPointerRemoved(args);
			_pen = (pen, isAdded: false);
		}
	}

	// TODO: Move as extension method
	internal void InjectPenInput(IEnumerable<InjectedInputPenInfo> input)
	{
		foreach (var info in input)
		{
			InjectPenInput(info);
		}
	}

	private const InjectedInputMouseOptions _mouseButtonDown = InjectedInputMouseOptions.LeftDown | InjectedInputMouseOptions.MiddleDown | InjectedInputMouseOptions.RightDown | InjectedInputMouseOptions.XDown;

	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
	public void InjectMouseInput(IEnumerable<InjectedInputMouseInfo> input)
	{
		foreach (var info in input)
		{
			if ((info.MouseOptions & _mouseButtonDown) != 0 && !_mouse.Properties.HasPressedButton)
			{
				_mouse.StartNewSequence();
			}

			var args = info.ToEventArgs(_mouse!, VirtualKeyModifiers.None);
			_mouse!.Update(args);

			DispatchPointerUpdated(args);
		}
	}

	// TODO: Move as extension method
	internal async ValueTask InjectMouseInputAsync(IEnumerable<InjectedInputMouseInfo> input, CancellationToken ct)
	{
		foreach (var info in input)
		{
			if ((info.MouseOptions & _mouseButtonDown) != 0 && !_mouse.Properties.HasPressedButton)
			{
				_mouse.StartNewSequence();
			}

			var args = info.ToEventArgs(_mouse!, VirtualKeyModifiers.None);
			_mouse!.Update(args);

			DispatchPointerUpdated(args);
			await WaitForIdle(ct);
		}
	}

	// TODO: Move as extension method
	internal void InjectMouseInput(IEnumerable<(InjectedInputMouseInfo, VirtualKeyModifiers)> input)
	{
		foreach (var (info, modifiers) in input)
		{
			var args = info.ToEventArgs(_mouse!, modifiers);
			_mouse!.Update(args);

			DispatchPointerUpdated(args);
		}
	}

	// TODO: Move as extension method
	internal async ValueTask InjectMouseInputAsync(IEnumerable<(InjectedInputMouseInfo, VirtualKeyModifiers)> input, CancellationToken ct)
	{
		foreach (var (info, modifiers) in input)
		{
			if ((info.MouseOptions & _mouseButtonDown) != 0 && !_mouse.Properties.HasPressedButton)
			{
				_mouse.StartNewSequence();
			}

			var args = info.ToEventArgs(_mouse!, modifiers);
			_mouse!.Update(args);

			DispatchPointerUpdated(args);
			await WaitForIdle(ct);
		}
	}

	private async ValueTask WaitForIdle(CancellationToken ct)
	{
		var dispatcher = DispatcherQueue.GetForCurrentThread() ?? throw new InvalidOperationException();
		var tcs = new TaskCompletionSource();
		await using var _ = ct.Register(() => tcs.TrySetCanceled());

		Enqueue(() => Enqueue(() => tcs.TrySetResult()));

		await tcs.Task;

		void Enqueue(DispatcherQueueHandler action)
		{
			if (!dispatcher.TryEnqueue(DispatcherQueuePriority.Low, action))
			{
				tcs.TrySetException(new Exception("Cannot enqueue work item on dispatcher"));
			}
		}
	}

	// Dispatch wrappers — stamp the relative root on every PointerEventArgs
	// before forwarding to the target so InputManager can scope hit-testing
	// to a subtree when one was set via TryCreate(object).
	private void DispatchPointerAdded(PointerEventArgs args)
	{
		args.RelativeRoot = _relativeRoot;
		_target.InjectPointerAdded(args);
	}

	private void DispatchPointerUpdated(PointerEventArgs args)
	{
		args.RelativeRoot = _relativeRoot;
		_target.InjectPointerUpdated(args);
	}

	private void DispatchPointerRemoved(PointerEventArgs args)
	{
		args.RelativeRoot = _relativeRoot;
		_target.InjectPointerRemoved(args);
	}
}
