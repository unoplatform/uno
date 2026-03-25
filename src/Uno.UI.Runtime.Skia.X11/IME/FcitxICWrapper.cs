using System;
using System.Threading.Tasks;
using Uno.WinUI.Runtime.Skia.X11.DBus.Fcitx;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// Wraps fcitx4 and fcitx5 D-Bus input context proxies behind a unified API.
/// Handles API differences: fcitx5 uses bool for key type, fcitx4 uses int;
/// fcitx5 uses uint64 for capabilities, fcitx4 uses uint.
/// </summary>
internal sealed class FcitxICWrapper
{
	private readonly InputContext1? _modern;
	private readonly InputContext? _old;

	public FcitxICWrapper(InputContext old) => _old = old;

	public FcitxICWrapper(InputContext1 modern) => _modern = modern;

	public Task FocusInAsync() => _old?.FocusInAsync() ?? _modern?.FocusInAsync() ?? Task.CompletedTask;

	public Task FocusOutAsync() => _old?.FocusOutAsync() ?? _modern?.FocusOutAsync() ?? Task.CompletedTask;

	public Task ResetAsync() => _old?.ResetAsync() ?? _modern?.ResetAsync() ?? Task.CompletedTask;

	public Task SetCursorRectAsync(int x, int y, int w, int h) =>
		_old?.SetCursorRectAsync(x, y, w, h) ?? _modern?.SetCursorRectAsync(x, y, w, h) ?? Task.CompletedTask;

	public Task DestroyICAsync() => _old?.DestroyICAsync() ?? _modern?.DestroyICAsync() ?? Task.CompletedTask;

	public async Task<bool> ProcessKeyEventAsync(uint keyVal, uint keyCode, uint state, int type, uint time)
	{
		if (_old is not null)
		{
			// fcitx4: type is int (0=press, 1=release), returns int (nonzero = handled)
			return await _old.ProcessKeyEventAsync(keyVal, keyCode, state, type, time) != 0;
		}

		if (_modern is not null)
		{
			// fcitx5: type is bool (true=release), returns bool
			return await _modern.ProcessKeyEventAsync(keyVal, keyCode, state, type > 0, time);
		}

		return false;
	}

	public ValueTask<IDisposable> WatchCommitStringAsync(Action<Exception?, string> handler)
	{
		if (_old is not null)
		{
			return _old.WatchCommitStringAsync(handler);
		}

		if (_modern is not null)
		{
			return _modern.WatchCommitStringAsync(handler);
		}

		return new ValueTask<IDisposable>(NoOpDisposable.Instance);
	}

	public ValueTask<IDisposable> WatchForwardKeyAsync(Action<Exception?, (uint Keyval, uint State, int Type)> handler)
	{
		if (_old is not null)
		{
			return _old.WatchForwardKeyAsync(handler);
		}

		if (_modern is not null)
		{
			// fcitx5 ForwardKey uses bool for type; convert to int for unified API
			return _modern.WatchForwardKeyAsync((e, ev) => handler(e, (ev.Keyval, ev.State, ev.Type ? 1 : 0)));
		}

		return new ValueTask<IDisposable>(NoOpDisposable.Instance);
	}

	public ValueTask<IDisposable> WatchUpdateFormattedPreeditAsync(
		Action<Exception?, ((string?, int)[]? Str, int Cursorpos)> handler)
	{
		if (_old is not null)
		{
			return _old.WatchUpdateFormattedPreeditAsync(handler!);
		}

		if (_modern is not null)
		{
			return _modern.WatchUpdateFormattedPreeditAsync(handler!);
		}

		return new ValueTask<IDisposable>(NoOpDisposable.Instance);
	}

	public Task SetCapacityAsync(uint flags) =>
		_old?.SetCapacityAsync(flags) ?? _modern?.SetCapabilityAsync(flags) ?? Task.CompletedTask;

	private sealed class NoOpDisposable : IDisposable
	{
		public static NoOpDisposable Instance { get; } = new();
		public void Dispose() { }
	}
}
