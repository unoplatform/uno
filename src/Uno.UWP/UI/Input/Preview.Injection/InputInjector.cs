#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Logging;
using Windows.Devices.Input;
using Windows.System;

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

	private readonly InjectedInputState _mouse = new(PointerDeviceType.Mouse);
	private (InjectedInputState state, bool isAdded)? _touch;
	private (InjectedInputState state, bool isAdded)? _pen;

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

			_target.InjectPointerRemoved(cancel.ToEventArgs(_touch.Value.state));

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
				_target.InjectPointerAdded(args);
				_touch = (touch, isAdded: true);
				await WaitForIdle(ct);
			}

			touch.Update(args);

			_target.InjectPointerUpdated(args);
			await WaitForIdle(ct);

					if (info.PointerInfo.PointerOptions.HasFlag(InjectedInputPointerOptions.PointerUp))
					{
						_target.InjectPointerRemoved(args);
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
					var cancel = new InjectedInputPenInfo
					{
						PointerInfo = new()
						{
							PointerId = 1,
							PointerOptions = InjectedInputPointerOptions.Canceled
						}
					};

					_target.InjectPointerRemoved(cancel.ToEventArgs(_pen.Value.state));

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
					_target.InjectPointerAdded(args);
					_pen = (pen, isAdded: true);
				}

				pen.Update(args);

				_target.InjectPointerUpdated(args);

				if (input.PointerInfo.PointerOptions.HasFlag(InjectedInputPointerOptions.PointerUp))
				{
					_target.InjectPointerRemoved(args);
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

			_target.InjectPointerUpdated(args);
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

			_target.InjectPointerUpdated(args);
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

			_target.InjectPointerUpdated(args);
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

			_target.InjectPointerUpdated(args);
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
}
