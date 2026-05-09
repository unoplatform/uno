using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Uno.Foundation.Logging;
using Uno.WinUI.Runtime.Skia.X11.DBus.IBus;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// IBus D-Bus IME client. Connects to org.freedesktop.portal.IBus,
/// creates an input context, and processes key events/signals.
/// </summary>
internal sealed class IBusInputMethod : IX11InputMethod
{
	private const string IBusService = "org.freedesktop.portal.IBus";
	private const string IBusObjectPath = "/org/freedesktop/IBus";

	// IBus capabilities
	private const uint CapPreeditText = 1 << 0;
	private const uint CapFocus = 1 << 3;

	// IBus modifier masks
	private const uint ReleaseMask = 1 << 30;

	private readonly string _sessionBusAddress;
	private DBusConnection? _connection;
	private InputContext? _context;
	private Service? _service;
	private readonly List<IDisposable> _disposables = new();

	private bool _isEnabled;
	private int _insideReset;

	public bool IsEnabled => _isEnabled;

	public event Action<string>? Commit;
	public event Action<uint, uint, uint>? ForwardKey;
	public event Action<string?, int>? PreeditChanged;

	public IBusInputMethod(string sessionBusAddress)
	{
		_sessionBusAddress = sessionBusAddress;
		_ = InitAsync();
	}

	private async Task InitAsync()
	{
		try
		{
			_connection = new DBusConnection(_sessionBusAddress);
			await _connection.ConnectAsync();

			var dbusService = new DBusService(_connection, IBusService);
			var portal = dbusService.CreatePortal(IBusObjectPath);

			var contextPath = await portal.CreateInputContextAsync("Uno Platform");
			_service = dbusService.CreateService(contextPath);
			_context = dbusService.CreateInputContext(contextPath);

			// Subscribe to signals
			_disposables.Add(await _context.WatchCommitTextAsync(OnCommitText));
			_disposables.Add(await _context.WatchForwardKeyEventAsync(OnForwardKeyEvent));
			_disposables.Add(await _context.WatchUpdatePreeditTextAsync(OnUpdatePreeditText));
			_disposables.Add(await _context.WatchShowPreeditTextAsync(OnShowPreeditText));
			_disposables.Add(await _context.WatchHidePreeditTextAsync(OnHidePreeditText));

			// Set capabilities: we support focus tracking and preedit
			await _context.SetCapabilitiesAsync(CapFocus | CapPreeditText);

			_isEnabled = true;

			if (this.Log().IsEnabled(LogLevel.Information))
			{
				this.Log().Info($"IBus D-Bus IME connected. Context: {contextPath}");
			}
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Failed to initialize IBus D-Bus IME: {ex.Message}", ex);
			}
			_isEnabled = false;
		}
	}

	public async Task<bool> HandleKeyEventAsync(uint keyVal, uint keyCode, uint state, bool isRelease)
	{
		if (_context is null || !_isEnabled)
		{
			return false;
		}

		if (isRelease)
		{
			state |= ReleaseMask;
		}

		try
		{
			var handled = await _context.ProcessKeyEventAsync(keyVal, keyCode, state);

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"IBus ProcessKeyEvent: keyval=0x{keyVal:X} keycode={keyCode} state=0x{state:X} → handled={handled}");
			}

			return handled;
		}
		catch (DBusConnectionClosedException)
		{
			// IBus service disconnected — disable and fall back to XIM
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn("IBus D-Bus connection lost. Falling back to XIM.");
			}
			_isEnabled = false;
			return false;
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"IBus ProcessKeyEvent failed: {ex.Message}", ex);
			}
			return false;
		}
	}

	public void SetCursorLocation(int x, int y, int w, int h)
	{
		if (_context is null || !_isEnabled)
		{
			return;
		}

		_ = _context.SetCursorLocationAsync(x, y, w, h);
	}

	public void SetFocus(bool active)
	{
		if (_context is null || !_isEnabled)
		{
			return;
		}

		_ = active ? _context.FocusInAsync() : _context.FocusOutAsync();
	}

	public void Reset()
	{
		if (_context is null || !_isEnabled)
		{
			return;
		}

		_ = ResetAsync();
	}

	private async Task ResetAsync()
	{
		try
		{
			_insideReset++;
			await _context!.ResetAsync();
		}
		finally
		{
			_insideReset--;
		}
	}

	private void OnCommitText(Exception? exception, VariantValue text)
	{
		if (_insideReset > 0)
		{
			// IBus can trigger CommitText during Reset — ignore it.
			return;
		}

		if (exception is not null)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"IBus CommitText signal error: {exception.Message}", exception);
			}
			return;
		}

		// IBus commit text is a variant struct. The actual text string is at index 2.
		string? commitString = null;
		if (text is { Count: >= 3 })
		{
			var item2 = text.GetItem(2);
			if (item2.Type == VariantValueType.String)
			{
				commitString = item2.GetString();
			}
		}

		if (!string.IsNullOrEmpty(commitString))
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"IBus CommitText: '{commitString}'");
			}
			Commit?.Invoke(commitString);
		}
	}

	private void OnForwardKeyEvent(Exception? exception, (uint Keyval, uint Keycode, uint State) key)
	{
		if (exception is not null)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"IBus ForwardKeyEvent signal error: {exception.Message}", exception);
			}
			return;
		}

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"IBus ForwardKeyEvent: keyval=0x{key.Keyval:X} keycode={key.Keycode} state=0x{key.State:X}");
		}

		ForwardKey?.Invoke(key.Keyval, key.Keycode, key.State);
	}

	private void OnUpdatePreeditText(Exception? exception, (VariantValue Text, uint CursorPos, bool Visible) preedit)
	{
		if (exception is not null)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"IBus UpdatePreeditText signal error: {exception.Message}", exception);
			}
			return;
		}

		string? preeditText = null;
		if (preedit.Text is { Count: >= 3 })
		{
			var item2 = preedit.Text.GetItem(2);
			if (item2.Type == VariantValueType.String)
			{
				preeditText = item2.GetString();
			}
		}

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"IBus UpdatePreeditText: text='{preeditText}' cursor={preedit.CursorPos} visible={preedit.Visible}");
		}

		if (!preedit.Visible)
		{
			preeditText = null;
		}

		PreeditChanged?.Invoke(preeditText, (int)preedit.CursorPos);
	}

	private void OnShowPreeditText(Exception? exception)
	{
		// No action needed — preedit visibility is handled via UpdatePreeditText.Visible
	}

	private void OnHidePreeditText(Exception? exception)
	{
		if (exception is not null)
		{
			return;
		}

		PreeditChanged?.Invoke(null, 0);
	}

	public void Dispose()
	{
		_isEnabled = false;

		foreach (var disposable in _disposables)
		{
			disposable.Dispose();
		}
		_disposables.Clear();

		if (_service is not null)
		{
			_ = _service.DestroyAsync().ContinueWith(_ => { }, TaskContinuationOptions.OnlyOnFaulted);
		}

		_connection?.Dispose();
		_context = null;
		_service = null;
		_connection = null;
	}
}
