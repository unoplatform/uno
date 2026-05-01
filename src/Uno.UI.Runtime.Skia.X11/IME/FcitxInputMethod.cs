using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Uno.Foundation.Logging;
using Uno.WinUI.Runtime.Skia.X11.DBus.Fcitx;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// Fcitx D-Bus IME client. Tries fcitx5 (org.freedesktop.portal.Fcitx) first,
/// falls back to fcitx4 (org.fcitx.Fcitx).
/// </summary>
internal sealed class FcitxInputMethod : IX11InputMethod
{
	private const string Fcitx5Service = "org.freedesktop.portal.Fcitx";
	private const string Fcitx4Service = "org.fcitx.Fcitx";

	// Key event types
	private const int PressKey = 0;
	private const int ReleaseKey = 1;

	private readonly string _sessionBusAddress;
	private DBusConnection? _connection;
	private FcitxICWrapper? _context;
	private readonly List<IDisposable> _disposables = new();
	private bool _isEnabled;

	public bool IsEnabled => _isEnabled;

	public event Action<string>? Commit;
	public event Action<uint, uint, uint>? ForwardKey;
	public event Action<string?, int>? PreeditChanged;

	public FcitxInputMethod(string sessionBusAddress)
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

			// Try fcitx5 first, fall back to fcitx4
			if (!await TryConnectFcitx5() && !await TryConnectFcitx4())
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn("Failed to connect to both fcitx5 and fcitx4 D-Bus services.");
				}
				return;
			}

			// Subscribe to signals
			_disposables.Add(await _context!.WatchCommitStringAsync(OnCommitString));
			_disposables.Add(await _context!.WatchForwardKeyAsync(OnForwardKey));
			_disposables.Add(await _context!.WatchUpdateFormattedPreeditAsync(OnUpdateFormattedPreedit));

			_isEnabled = true;

			if (this.Log().IsEnabled(LogLevel.Information))
			{
				this.Log().Info("Fcitx D-Bus IME connected.");
			}
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Failed to initialize Fcitx D-Bus IME: {ex.Message}", ex);
			}
			_isEnabled = false;
		}
	}

	private async Task<bool> TryConnectFcitx5()
	{
		try
		{
			var service = new DBusService(_connection!, Fcitx5Service);
			var method = service.CreateInputMethod1("/inputmethod");
			var resp = await method.CreateInputContextAsync(new[] { ("appName", "Uno Platform") });
			var proxy = service.CreateInputContext1(resp.Item1);
			_context = new FcitxICWrapper(proxy);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Connected to fcitx5 D-Bus. Context: {resp.Item1}");
			}
			return true;
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"fcitx5 not available: {ex.Message}");
			}
			return false;
		}
	}

	private async Task<bool> TryConnectFcitx4()
	{
		try
		{
			var service = new DBusService(_connection!, Fcitx4Service);
			var method = service.CreateInputMethod("/inputmethod");
			var resp = await method.CreateICv3Async("Uno Platform", Environment.ProcessId);
			var proxy = service.CreateInputContext($"/inputcontext_{resp.Icid}");
			_context = new FcitxICWrapper(proxy);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Connected to fcitx4 D-Bus. IC ID: {resp.Icid}");
			}
			return true;
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"fcitx4 not available: {ex.Message}");
			}
			return false;
		}
	}

	public async Task<bool> HandleKeyEventAsync(uint keyVal, uint keyCode, uint state, bool isRelease)
	{
		if (_context is null || !_isEnabled)
		{
			return false;
		}

		try
		{
			var type = isRelease ? ReleaseKey : PressKey;
			var handled = await _context.ProcessKeyEventAsync(keyVal, keyCode, state, type, 0);

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Fcitx ProcessKeyEvent: keyval=0x{keyVal:X} keycode={keyCode} state=0x{state:X} type={type} → handled={handled}");
			}

			return handled;
		}
		catch (DBusConnectionClosedException)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn("Fcitx D-Bus connection lost. Falling back to XIM.");
			}
			_isEnabled = false;
			return false;
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Fcitx ProcessKeyEvent failed: {ex.Message}", ex);
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

		_ = _context.SetCursorRectAsync(x, y, Math.Max(1, w), Math.Max(1, h));
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

		_ = _context.ResetAsync();
	}

	private void OnCommitString(Exception? exception, string text)
	{
		if (exception is not null)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Fcitx CommitString signal error: {exception.Message}", exception);
			}
			return;
		}

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Fcitx CommitString: '{text}'");
		}

		Commit?.Invoke(text);
	}

	private void OnForwardKey(Exception? exception, (uint Keyval, uint State, int Type) key)
	{
		if (exception is not null)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Fcitx ForwardKey signal error: {exception.Message}", exception);
			}
			return;
		}

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Fcitx ForwardKey: keyval=0x{key.Keyval:X} state=0x{key.State:X} type={key.Type}");
		}

		ForwardKey?.Invoke(key.Keyval, 0, key.State);
	}

	private void OnUpdateFormattedPreedit(Exception? exception, ((string?, int)[]? Str, int Cursorpos) preedit)
	{
		if (exception is not null)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Fcitx UpdateFormattedPreedit signal error: {exception.Message}", exception);
			}
			return;
		}

		string? preeditText = null;
		int cursor = 0;

		if (preedit.Str is { Length: > 0 })
		{
			preeditText = string.Join("", preedit.Str.Select(x => x.Item1));

			if (!string.IsNullOrEmpty(preeditText) && preedit.Cursorpos >= 0)
			{
				// cursorpos is a byte offset in UTF-8. Convert to character offset.
				var utf8Bytes = Encoding.UTF8.GetBytes(preeditText);
				if (utf8Bytes.Length >= preedit.Cursorpos)
				{
					cursor = Encoding.UTF8.GetCharCount(utf8Bytes, 0, preedit.Cursorpos);
				}
			}
		}

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Fcitx UpdateFormattedPreedit: text='{preeditText}' cursor={cursor} (raw cursor byte offset={preedit.Cursorpos})");
		}

		PreeditChanged?.Invoke(preeditText, cursor);
	}

	public void Dispose()
	{
		_isEnabled = false;

		foreach (var disposable in _disposables)
		{
			disposable.Dispose();
		}
		_disposables.Clear();

		if (_context is not null)
		{
			_ = _context.DestroyICAsync().ContinueWith(_ => { }, TaskContinuationOptions.OnlyOnFaulted);
		}

		_connection?.Dispose();
		_context = null;
		_connection = null;
	}
}
