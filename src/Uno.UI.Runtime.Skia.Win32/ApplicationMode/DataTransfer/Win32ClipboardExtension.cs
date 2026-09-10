using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;
using Uno.ApplicationModel.DataTransfer;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.Memory;
using Windows.Win32.System.Ole;
using Windows.Win32.UI.WindowsAndMessaging;
using Buffer = System.Buffer;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32ClipboardExtension : IClipboardExtension
{
	public static Win32ClipboardExtension Instance { get; } = new();


	private static readonly Dictionary<string, (PointerToString FromPointer, StringToPointer ToPointer)> _knownTextBasedClipboardFormats = new()
	{
		["HTML Format"] = (Marshal.PtrToStringUTF8, Marshal.StringToCoTaskMemUTF8), // HTML fragment with header metadata
		["Rich Text Format"] = (Marshal.PtrToStringAnsi, Marshal.StringToCoTaskMemAnsi), // RTF document
		["Rich Text & Unicode"] = (Marshal.PtrToStringUni, Marshal.StringToCoTaskMemUni), // RTF with Unicode support
		["Rich Text Format Without Objects"] = (Marshal.PtrToStringAnsi, Marshal.StringToCoTaskMemAnsi), // RTF without embedded objects
		["XML Spreadsheet"] = (Marshal.PtrToStringUTF8, Marshal.StringToCoTaskMemUTF8), // Excel XML format
		["CSV"] = (Marshal.PtrToStringAnsi, Marshal.StringToCoTaskMemAnsi), // Comma-separated values
		["Csv"] = (Marshal.PtrToStringAnsi, Marshal.StringToCoTaskMemAnsi), // Alternate CSV registration (Excel)
		["MIME:text/plain"] = (Marshal.PtrToStringUTF8, Marshal.StringToCoTaskMemUTF8), // Plain text via MIME
		["MIME:text/html"] = (Marshal.PtrToStringUTF8, Marshal.StringToCoTaskMemUTF8), // HTML via MIME
		["text/html"] = (Marshal.PtrToStringUTF8, Marshal.StringToCoTaskMemUTF8), // Raw HTML (Chromium/browsers)
		["text/plain"] = (Marshal.PtrToStringUTF8, Marshal.StringToCoTaskMemUTF8), // Raw plain text (Chromium/browsers)
		["text/uri-list"] = (Marshal.PtrToStringUTF8, Marshal.StringToCoTaskMemUTF8), // Newline-separated URIs
		["UniformResourceLocator"] = (Marshal.PtrToStringAnsi, Marshal.StringToCoTaskMemAnsi), // Single URL
		["UniformResourceLocatorW"] = (Marshal.PtrToStringUni, Marshal.StringToCoTaskMemUni), // Single URL (wide)
		["FileName"] = (Marshal.PtrToStringAnsi, Marshal.StringToCoTaskMemAnsi), // File path
		["FileNameW"] = (Marshal.PtrToStringUni, Marshal.StringToCoTaskMemUni), // File path (wide)
	};
	private static readonly Lazy<Encoding> _oemEncoding = new(() =>
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
	});

	// _windowClass must be statically stored, otherwise lpfnWndProc will get collected and the CLR will throw some weird exceptions
	// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
	private readonly WNDCLASSEXW _windowClass;
	private readonly HWND _hwnd;

	private bool _observeContentChanged;

	/// <summary>A completed read, together with the clipboard generation it was read from.</summary>
	/// <remarks>
	/// Keyed on the sequence number rather than on WM_CLIPBOARDUPDATE alone because
	/// <see cref="PInvoke.GetClipboardSequenceNumber"/> is lock-free and authoritative: it reports a
	/// change even if we missed the message or have not processed it yet.
	/// </remarks>
	private sealed record CachedContent(DataPackage Package, uint Sequence);

	// One reference field rather than a package/sequence pair, so that WndProc invalidating the cache
	// cannot be seen half-applied by a reader on another thread. Only ever set to a COMPLETE read
	// (see BuildPackage), and a reader revalidates the sequence anyway, so a lost update is benign.
	private CachedContent? _cachedContent;

	// Cancels the pending warm-up when the clipboard changes again before it ran.
	private CancellationTokenSource? _warmUpCts;

	private unsafe Win32ClipboardExtension()
	{
		using var lpClassName = new Win32Helper.NativeNulTerminatedUtf16String("UnoPlatformClipboardWindow");
		using var windowTitle = new Win32Helper.NativeNulTerminatedUtf16String("");

		_windowClass = new WNDCLASSEXW
		{
			cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
			lpfnWndProc = &WndProc,
			hInstance = Win32Helper.GetHInstance(),
			lpszClassName = lpClassName,
		};

		var classAtom = PInvoke.RegisterClassEx(_windowClass);
		if (classAtom is 0)
		{
			throw new InvalidOperationException($"{nameof(PInvoke.RegisterClassEx)} failed: {Win32Helper.GetErrorMessage()}");
		}

		_hwnd = PInvoke.CreateWindowEx(
			0,
			lpClassName,
			windowTitle,
			WINDOW_STYLE.WS_OVERLAPPED,
			0,
			0,
			0,
			0,
			HWND.HWND_MESSAGE,
			HMENU.Null,
			Win32Helper.GetHInstance(),
			null);

		if (_hwnd == HWND.Null)
		{
			throw new InvalidOperationException($"{nameof(PInvoke.CreateWindowEx)} failed: {Win32Helper.GetErrorMessage()}");
		}

		// No need to unregister. This class lasts the lifetime on the app.
		var success = PInvoke.AddClipboardFormatListener(_hwnd);
		if (!success) { this.LogError()?.Error($"{nameof(PInvoke.AddClipboardFormatListener)} failed: {Win32Helper.GetErrorMessage()}"); }
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
	internal static LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
	{
		try
		{
			if (msg is PInvoke.WM_CLIPBOARDUPDATE)
			{
				Instance._cachedContent = null;
				Instance.ScheduleWarmUp();
				if (Instance._observeContentChanged)
				{
					Instance.ContentChanged?.Invoke(Instance, EventArgs.Empty);
				}
				return new LRESULT(0);
			}
		}
		catch (Exception e)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"Exception in {nameof(WndProc)}", e);
		}
		return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
	}

	public event EventHandler<object>? ContentChanged;

	public void StartContentChanged() => _observeContentChanged = true;

	public void StopContentChanged() => _observeContentChanged = false;

	public void Clear()
	{
		using var clipboardDisposable = new ClipboardDisposable(_hwnd, true, ClipboardRetry.Blocking);
		if (!clipboardDisposable.IsOpen)
		{
			// Previously this silently no-oped: EmptyClipboard was skipped and nothing reported it.
			this.LogError()?.Error($"{nameof(Clear)} failed: could not take the clipboard, it is held by another application.");
		}
	}

	public void Flush() { }

	private static string GetClipboardFormatName(CLIPBOARD_FORMAT format) =>
		Enum.GetName(format) ?? // cant call GetClipboardFormatName on these
		GetClipboardFormatNameCore(format) ??
		format.ToString();

	private static unsafe string? GetClipboardFormatNameCore(CLIPBOARD_FORMAT format)
	{
		const int MAX_PATH = 260;
		const int BufferSize = MAX_PATH + 1;

		var buffer = Marshal.AllocHGlobal((IntPtr)(BufferSize * Unsafe.SizeOf<char>()));
		using var bufferDisposable = new DisposableStruct<IntPtr>(Marshal.FreeHGlobal, buffer);
		var length = PInvoke.GetClipboardFormatName((uint)format, new PWSTR((char*)buffer), BufferSize);
		if (length == 0)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.GetClipboardFormatName)} failed (format={format}): {Win32Helper.GetErrorMessage()} ");
			return null;
		}

		return Marshal.PtrToStringUni(buffer);
	}

	/// <summary>
	/// How hard to try to take the clipboard.
	/// </summary>
	/// <remarks>
	/// <see cref="PInvoke.OpenClipboard"/> is a global exclusive lock with a single winner, so being
	/// denied is a normal outcome under contention rather than an error. It fails quickly, which is
	/// what makes retrying cheap; the right budget depends entirely on who is asking.
	/// </remarks>
	private enum ClipboardRetry
	{
		/// <summary>
		/// A single attempt, never waiting. For callers on the UI hot path, where a denial is
		/// recovered by the next call instead of by waiting.
		/// </summary>
		/// <remarks>
		/// Blocking here would put <c>TextBox.CanPasteClipboardContent</c> — which is re-evaluated in
		/// every process on every clipboard change — back onto a global exclusive lock.
		/// </remarks>
		Once,

		/// <summary>
		/// Up to <see cref="MaxOpenAttempts"/> attempts, sleeping in between. For user-initiated
		/// writes, where losing the operation is worse than a delay and where the first attempt
		/// almost always succeeds anyway.
		/// </summary>
		Blocking,
	}

	private const int MaxOpenAttempts = 10;
	private const int OpenRetryDelayMs = 100;

	/// <remarks>
	/// Every listening process is woken by the same <see cref="PInvoke.WM_CLIPBOARDUPDATE"/> broadcast,
	/// so a fixed backoff has them all retry in lockstep and collide again. The jitter is what breaks
	/// up the herd, and is not decoration.
	/// </remarks>
	private static int NextRetryDelayMs(int baseDelayMs) =>
		baseDelayMs + Random.Shared.Next(-baseDelayMs / 2, (baseDelayMs / 2) + 1);

	private static bool TryOpenClipboard(HWND hwnd, ClipboardRetry retry)
	{
		var maxAttempts = retry is ClipboardRetry.Blocking ? MaxOpenAttempts : 1;
		for (var attempt = 1; ; attempt++)
		{
			if (PInvoke.OpenClipboard(hwnd))
			{
				return true;
			}

			if (attempt >= maxAttempts)
			{
				// Deliberately not an error: under contention this is the expected outcome, and the
				// callers that cannot proceed without the clipboard log it themselves.
				typeof(Win32ClipboardExtension).LogDebug()?.Debug($"{nameof(PInvoke.OpenClipboard)} denied after {attempt} attempt(s): {Win32Helper.GetErrorMessage()}");
				return false;
			}

			Thread.Sleep(NextRetryDelayMs(OpenRetryDelayMs));
		}
	}

	private readonly ref struct ClipboardDisposable
	{
		public ClipboardDisposable(HWND hwnd, bool ownClipboard, ClipboardRetry retry)
		{
			IsOpen = TryOpenClipboard(hwnd, retry);
			if (ownClipboard && IsOpen)
			{
				var success = PInvoke.EmptyClipboard();
				if (!success) { typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.EmptyClipboard)} failed: {Win32Helper.GetErrorMessage()}"); }
			}
		}

		/// <summary>
		/// Whether the clipboard was actually opened. Callers MUST check this before calling anything
		/// that needs the lock — <see cref="PInvoke.EnumClipboardFormats"/>,
		/// <see cref="PInvoke.GetClipboardData"/>, <see cref="PInvoke.SetClipboardData"/> — all of
		/// which fail with "Thread does not have a clipboard open" otherwise.
		/// </summary>
		public bool IsOpen { get; }

		public void Dispose()
		{
			if (IsOpen)
			{
				var success = PInvoke.CloseClipboard();
				if (!success) { typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.CloseClipboard)} failed: {Win32Helper.GetErrorMessage()}"); }
			}
		}
	}
	private delegate string? PointerToString(nint p);
	private delegate nint StringToPointer(string s);
}

partial class Win32ClipboardExtension // from clipboard
{
	/// <summary>
	/// The standard formats this extension can decode. Anything else that happens to be on the
	/// clipboard is surfaced by name via <see cref="DecodeUnknownData"/>, once
	/// <see cref="PInvoke.EnumClipboardFormats"/> has told us it is there.
	/// </summary>
	private static readonly CLIPBOARD_FORMAT[] _knownStandardFormats =
	[
		CLIPBOARD_FORMAT.CF_UNICODETEXT,
		CLIPBOARD_FORMAT.CF_OEMTEXT,
		CLIPBOARD_FORMAT.CF_LOCALE,
		CLIPBOARD_FORMAT.CF_DIB,
	];

	/// <summary>
	/// The registered ids of <see cref="_knownTextBasedClipboardFormats"/>, so they can be probed
	/// without the lock.
	/// </summary>
	/// <remarks>
	/// <see cref="PInvoke.RegisterClipboardFormat"/> returns the existing id when the name is already
	/// registered, and ids are stable for the lifetime of the session, so this is safe to cache.
	/// </remarks>
	private static readonly Lazy<CLIPBOARD_FORMAT[]> _knownTextBasedFormatIds = new(() =>
		_knownTextBasedClipboardFormats.Keys
			.Select(name => (CLIPBOARD_FORMAT)PInvoke.RegisterClipboardFormat(name))
			.Where(id => id != 0)
			.Distinct()
			.ToArray());

	/// <summary>How long to wait before each warm-up attempt. See <see cref="ScheduleWarmUp"/>.</summary>
	private static readonly int[] _warmUpDelaysMs = [60, 150, 400];

	/// <summary>Decodes one clipboard format's payload from its handle, or null if it could not be read.</summary>
	private delegate object? ClipboardDecoder(CLIPBOARD_FORMAT format, string name, HGLOBAL handle);

	public DataPackageView GetContent()
	{
		// Lock-free, so this is trustworthy even while another process holds the clipboard, and it
		// notices a change we haven't been told about yet.
		var sequence = PInvoke.GetClipboardSequenceNumber();
		if (_cachedContent is { } cached && cached.Sequence == sequence)
		{
			return cached.Package.GetView();
		}

		var package = BuildPackage(sequence, out var complete);
		if (complete)
		{
			// Only a complete read is worth remembering. Caching an incomplete one is what used to turn
			// a single denied OpenClipboard into paste being dead until the next clipboard change.
			_cachedContent = new CachedContent(package, sequence);
		}

		return package.GetView();
	}

	/// <summary>
	/// Builds the package describing the current clipboard content, without materialising any payload.
	/// </summary>
	/// <param name="complete">
	/// Whether the full format list could be enumerated. When false, the clipboard was held by someone
	/// else and only the formats we can name by ourselves were probed - so the result is usable but
	/// must not be cached.
	/// </param>
	private static DataPackage BuildPackage(uint sequence, out bool complete)
	{
		var package = new DataPackage();
		var registered = new HashSet<string>();

		// 1. What can be learned without the lock. IsClipboardFormatAvailable cannot be denied, and it
		//    reports a delay-rendered format as available without forcing the (potentially very slow)
		//    render. This alone covers every format this extension knows how to decode.
		foreach (var format in _knownStandardFormats)
		{
			if (PInvoke.IsClipboardFormatAvailable((uint)format))
			{
				RegisterFormat(package, registered, format, sequence);
			}
		}

		foreach (var format in _knownTextBasedFormatIds.Value)
		{
			if (PInvoke.IsClipboardFormatAvailable((uint)format))
			{
				RegisterFormat(package, registered, format, sequence);
			}
		}

		// 2. Anything else needs EnumClipboardFormats, which needs the lock. Exactly one attempt: this
		//    runs on the UI thread on every clipboard change in every process, so it must never wait.
		//    Being denied only costs the app-specific formats we have no name for, and only until the
		//    next call - or until the warm-up gets there first.
		using var clipboardDisposable = new ClipboardDisposable(Instance._hwnd, false, ClipboardRetry.Once);
		if (!clipboardDisposable.IsOpen)
		{
			complete = false;
			return package;
		}

		// Collected before registering anything: GetClipboardFormatName, reached via ResolveFormat,
		// overwrites the last Win32 error, which would make the check below meaningless.
		var formats = new List<CLIPBOARD_FORMAT>();
		for (uint lastFormat = 0; (lastFormat = PInvoke.EnumClipboardFormats(lastFormat)) != 0;)
		{
			formats.Add((CLIPBOARD_FORMAT)lastFormat);
		}

		if (Marshal.GetLastWin32Error() != (int)WIN32_ERROR.ERROR_SUCCESS)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.EnumClipboardFormats)} failed: {Win32Helper.GetErrorMessage()}");
			complete = false;
			return package;
		}

		foreach (var format in formats)
		{
			RegisterFormat(package, registered, format, sequence);
		}

		complete = true;
		return package;
	}

	/// <summary>
	/// Maps a clipboard format to the <see cref="DataPackage"/> key it is surfaced under and the
	/// decoder for its payload, or null for the formats this extension does not surface.
	/// </summary>
	private static (string Name, ClipboardDecoder Decoder)? ResolveFormat(CLIPBOARD_FORMAT format) => format switch
	{
		// https://learn.microsoft.com/en-us/windows/win32/dataxchg/standard-clipboard-formats#constants
		// https://learn.microsoft.com/en-us/windows/win32/dataxchg/clipboard-formats#synthesized-clipboard-formats

		// synthesized text formats
		CLIPBOARD_FORMAT.CF_TEXT => null,
		CLIPBOARD_FORMAT.CF_LOCALE => (GetClipboardFormatName(format), DecodeUnknownData), // 4 bytes CultureInfo.LCID
		CLIPBOARD_FORMAT.CF_UNICODETEXT => (StandardDataFormats.Text, DecodeText),
		CLIPBOARD_FORMAT.CF_OEMTEXT => (GetClipboardFormatName(format), DecodeOemText),

		// synthesized image formats
		CLIPBOARD_FORMAT.CF_BITMAP => null, // Windows synthesizes CF_DIB from CF_BITMAP; handled below
		CLIPBOARD_FORMAT.CF_DIB => (StandardDataFormats.Bitmap, DecodeDib),
		CLIPBOARD_FORMAT.CF_DIBV5 => null,
		CLIPBOARD_FORMAT.CF_PALETTE => null,

		// synthesized meta-file formats
		CLIPBOARD_FORMAT.CF_METAFILEPICT => null,
		CLIPBOARD_FORMAT.CF_ENHMETAFILE => null,

		CLIPBOARD_FORMAT.CF_HDROP => null,

		CLIPBOARD_FORMAT.CF_SYLK => null,
		CLIPBOARD_FORMAT.CF_DIF => null,
		CLIPBOARD_FORMAT.CF_TIFF => null,
		CLIPBOARD_FORMAT.CF_PENDATA => null,
		CLIPBOARD_FORMAT.CF_RIFF => null,
		CLIPBOARD_FORMAT.CF_WAVE => null,

		_ => (GetClipboardFormatName(format), DecodeUnknownData),
	};

	/// <summary>
	/// Registers a format key against a provider that will fetch the payload if anyone asks for it.
	/// </summary>
	/// <remarks>
	/// Registering the key is all that <see cref="DataPackageView.Contains"/> and
	/// <see cref="DataPackageView.AvailableFormats"/> need - they read only the keys - so "can I
	/// paste?" is answered with no payload work and no clipboard lock at all.
	/// </remarks>
	private static void RegisterFormat(DataPackage package, HashSet<string> registered, CLIPBOARD_FORMAT format, uint sequence)
	{
		if (ResolveFormat(format) is not { } resolved || !registered.Add(resolved.Name))
		{
			return;
		}

		package.SetDataProvider(resolved.Name, ct => FetchPayloadAsync(format, resolved.Name, resolved.Decoder, sequence, ct));
	}

	/// <summary>
	/// Fetches and decodes one format's payload, on demand.
	/// </summary>
	/// <remarks>
	/// This is where the clipboard lock is actually needed, and where the full retry budget belongs: a
	/// paste is user-initiated and infrequent, and a slow paste beats a failed one.
	/// </remarks>
	private static async Task<object> FetchPayloadAsync(CLIPBOARD_FORMAT format, string name, ClipboardDecoder decoder, uint sequence, CancellationToken ct)
	{
		for (var attempt = 1; ; attempt++)
		{
			// The open/read/close cycle must not span an await - CloseClipboard has to be called by the
			// thread that opened - so the whole cycle runs as one unit on the dispatcher.
			var (outcome, value) = await RunOnDispatcherAsync(() => TryFetchPayload(format, name, decoder, sequence));

			switch (outcome)
			{
				case FetchOutcome.Success:
					return value!;
				case FetchOutcome.Stale:
					throw new InvalidOperationException($"The clipboard content changed while reading format '{name}'.");
				case FetchOutcome.Failed:
					throw new InvalidOperationException($"Failed to read format '{name}' from the clipboard.");
				case FetchOutcome.Denied when attempt >= MaxOpenAttempts:
					throw new InvalidOperationException($"Could not take the clipboard to read format '{name}' after {attempt} attempts, it is held by another application.");
			}

			// Awaiting instead of sleeping: this can be running on the UI thread, and a paste must not
			// freeze it for the length of the retry budget.
			await Task.Delay(NextRetryDelayMs(OpenRetryDelayMs), ct);
		}
	}

	private enum FetchOutcome
	{
		Success,

		/// <summary>Another process held the clipboard. Worth retrying.</summary>
		Denied,

		/// <summary>The clipboard content changed since the package was built. Retrying cannot help.</summary>
		Stale,

		/// <summary>The data was there but could not be read or decoded. Retrying cannot help.</summary>
		Failed,
	}

	private static (FetchOutcome Outcome, object? Value) TryFetchPayload(CLIPBOARD_FORMAT format, string name, ClipboardDecoder decoder, uint sequence)
	{
		if (PInvoke.GetClipboardSequenceNumber() != sequence)
		{
			// Serving content from a different clipboard generation would be worse than failing: the
			// caller is asking about the formats it saw in this view.
			return (FetchOutcome.Stale, null);
		}

		using var clipboardDisposable = new ClipboardDisposable(Instance._hwnd, false, ClipboardRetry.Once);
		if (!clipboardDisposable.IsOpen)
		{
			return (FetchOutcome.Denied, null);
		}

		// Deliberately re-read from GetClipboardData rather than capturing a handle when the format was
		// discovered: a clipboard data handle is owned by the clipboard and is only valid while it is
		// open, so using a captured one later reads freed memory.
		var handle = PInvoke.GetClipboardData((uint)format);
		if (handle == default)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.GetClipboardData)} failed (format={name}): {Win32Helper.GetErrorMessage()}");
			return (FetchOutcome.Failed, null);
		}

		return decoder.Invoke(format, name, (HGLOBAL)(IntPtr)handle) is { } value
			? (FetchOutcome.Success, value)
			: (FetchOutcome.Failed, null);
	}

	private static async Task<T> RunOnDispatcherAsync<T>(Func<T> func)
	{
		if (NativeDispatcher.Main.HasThreadAccess)
		{
			return func();
		}

		var result = default(T)!;
		await NativeDispatcher.Main.EnqueueAsync(() => result = func());
		return result;
	}

	/// <summary>
	/// Re-reads the clipboard shortly after it changed, off the message that announced the change.
	/// </summary>
	/// <remarks>
	/// Every listening process handles WM_CLIPBOARDUPDATE at the same instant, which makes that the
	/// worst possible moment to ask for the lock - all N of them collide. Waiting a jittered moment
	/// and reading then means the complete format list is usually cached before anything asks for it.
	/// This is fidelity only: <see cref="GetContent"/> is correct without it.
	/// </remarks>
	private void ScheduleWarmUp()
	{
		// Not disposed on purpose: the pending WarmUpAsync still holds the token, and a cancelled
		// source with no registrations is cheap enough to leave to the GC.
		_warmUpCts?.Cancel();

		var cts = _warmUpCts = new CancellationTokenSource();
		_ = WarmUpAsync(cts.Token);
	}

	private async Task WarmUpAsync(CancellationToken ct)
	{
		try
		{
			foreach (var baseDelayMs in _warmUpDelaysMs)
			{
				await Task.Delay(NextRetryDelayMs(baseDelayMs), ct);

				var done = false;
				await NativeDispatcher.Main.EnqueueAsync(() => done = TryWarmUp(ct));
				if (done)
				{
					return;
				}
			}
		}
		catch (OperationCanceledException)
		{
			// The clipboard changed again; the newer warm-up supersedes this one.
		}
		catch (Exception e)
		{
			this.LogError()?.Error($"Exception while warming up the clipboard cache", e);
		}
	}

	/// <returns>Whether the warm-up is finished, either because it succeeded or because it is moot.</returns>
	private bool TryWarmUp(CancellationToken ct)
	{
		var sequence = PInvoke.GetClipboardSequenceNumber();
		if (ct.IsCancellationRequested || _cachedContent?.Sequence == sequence)
		{
			return true;
		}

		var package = BuildPackage(sequence, out var complete);
		if (!complete)
		{
			return false;
		}

		// The clipboard may have changed while we were reading it, in which case this package describes
		// a generation nobody is asking about any more.
		if (!ct.IsCancellationRequested && PInvoke.GetClipboardSequenceNumber() == sequence)
		{
			_cachedContent = new CachedContent(package, sequence);
		}

		return true;
	}

	private static unsafe object? DecodeText(CLIPBOARD_FORMAT format, string name, HGLOBAL handle)
	{
		using var lockDisposable = Win32Helper.GlobalLock(handle, out var ptr);
		if (lockDisposable is null) return null;

		return Marshal.PtrToStringUni((IntPtr)ptr);
	}
	private static unsafe object? DecodeOemText(CLIPBOARD_FORMAT format, string name, HGLOBAL handle)
	{
		using var lockDisposable = Win32Helper.GlobalLock(handle, out var ptr);
		if (lockDisposable is null) return null;

		var length = (int)PInvoke.GlobalSize((HGLOBAL)(IntPtr)handle);

		return length > 1
			? _oemEncoding.Value.GetString((byte*)ptr, length - 1)
			: string.Empty;
	}
#if false // this would require System.Drawing.Common
	private static void GetBitmap(DataPackage package, CLIPBOARD_FORMAT format, HGLOBAL handle) => package
		.SetDataProvider(StandardDataFormats.Bitmap, _ =>
		{
			// CF_BITMAP handle is an HBITMAP, not an HGLOBAL — GlobalLock must not be called on it.

			var image = Image.FromHbitmap(handle);

			var ras = new InMemoryRandomAccessStream();
			var stream = ras.AsStreamForWrite(); // dont dispose
			{
				image.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
				stream.Flush(); // without this, only the file header is written
				stream.Position = 0;
			}

			return Task.FromResult<object>(RandomAccessStreamReference.CreateFromStream(ras));
		});
#endif
	private static unsafe object? DecodeDib(CLIPBOARD_FORMAT format, string name, HGLOBAL handle)
	{
		using var lockDisposable = Win32Helper.GlobalLock(handle, out var ptr, logLastError: false);
		if (lockDisposable is null)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.GlobalLock)} failed (format={name}): {Win32Helper.GetErrorMessage()}");
			return null;
		}

		var memSize = (uint)PInvoke.GlobalSize((HGLOBAL)(IntPtr)handle);
		if (memSize <= Marshal.SizeOf<BITMAPINFOHEADER>())
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.GlobalSize)} returned {memSize} (format={name}): {Win32Helper.GetErrorMessage()}");
			return null;
		}

		return RandomAccessStreamReference.CreateFromStream(new MemoryStream(ConvertDibToBmp(ptr, memSize)).AsRandomAccessStream());
	}

	// Derived from SDL's WIN_ConvertDIBtoBMP, translated to C# and modified:
	// https://github.com/libsdl-org/SDL/blob/9f8157f42cc0351833c030febe8a559719c875bd/src/video/windows/SDL_windowsclipboard.c
	//
	// Copyright (C) 1997-2024 Sam Lantinga <slouken@libsdl.org>
	//
	// This software is provided 'as-is', without any express or implied
	// warranty.  In no event will the authors be held liable for any damages
	// arising from the use of this software.
	//
	// Permission is granted to anyone to use this software for any purpose,
	// including commercial applications, and to alter it and redistribute it
	// freely, subject to the following restrictions:
	//
	// 1. The origin of this software must not be misrepresented; you must not
	//    claim that you wrote the original software. If you use this software
	//    in a product, an acknowledgment in the product documentation would be
	//    appreciated but is not required.
	// 2. Altered source versions must be plainly marked as such, and must not be
	//    misrepresented as being the original software.
	// 3. This notice may not be removed or altered from any source distribution.
	/// <summary>
	/// Wraps a raw CF_DIB payload in a <see cref="BITMAPFILEHEADER"/>, yielding a self-contained BMP file.
	/// </summary>
	private static unsafe byte[] ConvertDibToBmp(void* dib, uint dibSize)
	{
		var srcBitmapInfo = (BITMAPINFO*)dib;

		// https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-bitmapinfoheader#color-tables
		int colorTableSize = srcBitmapInfo->bmiHeader.biCompression switch
		{
			// BI_RGB
			0 when srcBitmapInfo->bmiHeader.biBitCount <= 8 => Marshal.SizeOf<RGBQUAD>() * (srcBitmapInfo->bmiHeader.biClrUsed == 0 ? 1 << srcBitmapInfo->bmiHeader.biBitCount : (int)srcBitmapInfo->bmiHeader.biClrUsed),
			0 => 0,
			// BI_BITFIELDS
			3 => 3 * Marshal.SizeOf<uint>(),
			// FOURCC
			_ => Marshal.SizeOf<RGBQUAD>() * (int)srcBitmapInfo->bmiHeader.biClrUsed
		};

		var fileHeaderSize = Marshal.SizeOf<BITMAPFILEHEADER>();
		var bitmapfileheader = new BITMAPFILEHEADER
		{
			bfType = /* BM */ 0x4d42,
			bfSize = (uint)(fileHeaderSize + dibSize),
			bfOffBits = (uint)(fileHeaderSize + Marshal.SizeOf<BITMAPINFOHEADER>() + colorTableSize)
		};

		var arr = new byte[fileHeaderSize + dibSize];
		fixed (byte* bmp = arr)
		{
			Buffer.MemoryCopy(&bitmapfileheader, bmp, arr.Length, fileHeaderSize);
			Buffer.MemoryCopy(dib, bmp + fileHeaderSize, dibSize, dibSize);
		}

		return arr;
	}
	private static unsafe object? DecodeUnknownData(CLIPBOARD_FORMAT format, string name, HGLOBAL handle)
	{
		using var lockDisposable = Win32Helper.GlobalLock(handle, out var ptr, logLastError: false);
		if (lockDisposable is null)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.GlobalLock)} failed (format={name}): {Win32Helper.GetErrorMessage()}");
			return null;
		}

		var size = (uint)PInvoke.GlobalSize((HGLOBAL)(IntPtr)handle);
		if (size == 0 || size > int.MaxValue)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.GlobalSize)} returned {size} (format={name}): {Win32Helper.GetErrorMessage()}");
			return null;
		}

		var bufferLength = checked((int)size);

		// note: WinUI Clipboard seem to detect certain named format as string, it is unknown by which mechanism.
		// since HGlobal itself doesnt carry any type metadata, presumably this is done with a white list.
		if (_knownTextBasedClipboardFormats.TryGetValue(name, out var marshaler))
		{
			return marshaler.FromPointer.Invoke((IntPtr)ptr) ?? string.Empty;
		}

		var buffer = new byte[bufferLength];
		fixed (byte* pBuffer = buffer)
		{
			System.Buffer.MemoryCopy(ptr, pBuffer, bufferLength, bufferLength);
		}

		return new MemoryStream(buffer).AsRandomAccessStream();
	}
}

partial class Win32ClipboardExtension // to clipboard
{
	/// <summary>A payload resolved outside the clipboard lock, ready to be handed straight to Windows.</summary>
	/// <remarks>
	/// Exactly one of <paramref name="Bytes"/> and <paramref name="CoTaskMem"/> carries the payload;
	/// <paramref name="CoTaskMem"/> being non-zero selects it.
	/// </remarks>
	private readonly record struct PendingWrite(CLIPBOARD_FORMAT Format, ReadOnlyMemory<byte> Bytes, IntPtr CoTaskMem);

	public void SetContent(DataPackage content)
	{
		var view = content.GetView();

		// Phase 1, OUTSIDE the lock. Resolving a payload pumps the message loop (see ResolveText), and
		// pumping while holding the global clipboard lock dispatches arbitrary UI work - up to and
		// including a re-entrant WM_CLIPBOARDUPDATE - while every other application is locked out.
		var writes = new List<PendingWrite>();
		foreach (var format in view.AvailableFormats)
		{
			var resolver = (Action<List<PendingWrite>, DataPackageView, string>?)(format switch
			{
				_ when format == StandardDataFormats.Text => ResolveText,
				_ when format == StandardDataFormats.Bitmap => ResolveBitmap,
				_ => ResolveUnknownData,
			});
			resolver?.Invoke(writes, view, format);
		}

		// Phase 2, INSIDE the lock. No pumping and no async work: just hand the payloads over.
		// Entered even with nothing to write: an empty DataPackage still means "empty the clipboard".
		using var clipboardDisposable = new ClipboardDisposable(_hwnd, true, ClipboardRetry.Blocking);
		if (!clipboardDisposable.IsOpen)
		{
			// This used to be silent: EmptyClipboard was skipped, every SetClipboardData ran anyway and
			// failed with "Thread does not have a clipboard open", and the copy was lost without a word.
			this.LogError()?.Error($"{nameof(SetContent)} failed: could not take the clipboard, it is held by another application.");
			FreePendingWrites(writes);
			return;
		}

		foreach (var write in writes)
		{
			WritePending(write);
		}
	}

	private static void WritePending(PendingWrite write)
	{
		if (write.CoTaskMem != IntPtr.Zero)
		{
			SetClipboardCoTaskMemData(write.Format, write.CoTaskMem);
		}
		else
		{
			SetClipboardData(write.Format, write.Bytes.Span);
		}
	}

	/// <summary>Releases payloads that were resolved but never handed over, so they don't leak.</summary>
	private static void FreePendingWrites(List<PendingWrite> writes)
	{
		foreach (var write in writes)
		{
			if (write.CoTaskMem != IntPtr.Zero)
			{
				Marshal.FreeCoTaskMem(write.CoTaskMem);
			}
		}
	}
	private static void ResolveText(List<PendingWrite> writes, DataPackageView view, string format)
	{
		var task = view.GetTextAsync().AsTask();
		while (!task.IsCompleted)
		{
			Win32EventLoop.RunOnce();
		}

		if (!task.IsCompletedSuccessfully)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(view.GetTextAsync)} failed to fetch data to be copied to the clipboard: {task.Status}", task.Exception);
			return;
		}

		var str = task.Result;
		var bytes = new byte[(str.Length + 1) * sizeof(char)]; // +1 char: last 2 bytes remain 0 as null terminator
		MemoryMarshal.Cast<char, byte>(str.AsSpan()).CopyTo(bytes);
		writes.Add(new PendingWrite(CLIPBOARD_FORMAT.CF_UNICODETEXT, bytes, default));
	}
	private static unsafe void ResolveBitmap(List<PendingWrite> writes, DataPackageView view, string format)
	{
		var task = view.GetBitmapAsync().AsTask();
		while (!task.IsCompleted)
		{
			Win32EventLoop.RunOnce();
		}

		if (!task.IsCompletedSuccessfully)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(view.GetBitmapAsync)} failed to fetch data to be copied to the clipboard: {task.Status}", task.Exception);
			return;
		}

		var task2 = task.Result.OpenReadAsync().AsTask();
		while (!task2.IsCompleted)
		{
			Win32EventLoop.RunOnce();
		}

		if (!task2.IsCompletedSuccessfully)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(RandomAccessStreamReference.OpenReadAsync)} failed to fetch data to be copied to the clipboard: {task2.Status}", task2.Exception);
			return;
		}

		var stream = task2.Result;
		Debug.Assert(stream.CanRead);
		stream.Seek(0);

#if false
		// this would require System.Drawing.Common
		var readStream = stream.AsStreamForRead();

		var bitmap = new Bitmap(readStream);
		var handle = (HBITMAP)bitmap.GetHbitmap();

		SetClipboardHBitmapData(CLIPBOARD_FORMAT.CF_BITMAP, handle);
#else
		// since we couldn't create a HBITMAP here, we will just write CF_DIB data directly to the clipboard,
		// which Windows will synthesize CF_BITMAP for us.

		var size = stream.Size;
		if (size > int.MaxValue)
		{
			throw new InvalidOperationException("Clipboard bitmap data is too large to be processed.");
		}

		var bytes = new byte[(int)size];
		stream.AsStreamForRead().ReadExactly(bytes);

		// check for 'BM' file signature, if we got a bitmap image, we can just strip the header and send the pixel data (in DIB format)
		if (bytes.Length > Marshal.SizeOf<BITMAPFILEHEADER>() &&
			bytes[0] == 'B' && bytes[1] == 'M')
		{
			writes.Add(new PendingWrite(CLIPBOARD_FORMAT.CF_DIB, bytes.AsMemory(/* start after: */ Marshal.SizeOf<BITMAPFILEHEADER>()), default));
		}
		else
		{
			// Unknown image format — decode via SkiaSharp and convert to CF_DIB
			using var skBitmap = SKBitmap.Decode(bytes);
			if (skBitmap is null)
			{
				typeof(Win32ClipboardExtension).LogError()?.Error("SetBitmap: SkiaSharp failed to decode image.");
				return;
			}

			// Ensure BGRA8888 so pixel layout matches what CF_DIB BI_RGB 32bpp expects (BGRX, alpha ignored)
			using var bgra = skBitmap.ColorType == SKColorType.Bgra8888
				? null
				: skBitmap.Copy(SKColorType.Bgra8888);
			var src = bgra ?? skBitmap;

			var width = src.Width;
			var height = src.Height;
			var stride = width * 4;
			var headerSize = Marshal.SizeOf<BITMAPINFOHEADER>();
			var pixelDataSize = stride * height;
			var dib = new byte[headerSize + pixelDataSize];

			fixed (byte* pDib = dib)
			{
				var header = (BITMAPINFOHEADER*)pDib;
				header->biSize = (uint)headerSize;
				header->biWidth = width;
				header->biHeight = height; // positive = bottom-up storage
				header->biPlanes = 1;
				header->biBitCount = 32;
				header->biCompression = 0; // BI_RGB
				header->biSizeImage = (uint)pixelDataSize;

				// SkiaSharp rows are top-down; CF_DIB with positive biHeight expects bottom-up
				var pixelSrc = (byte*)src.GetPixels();
				var pixelDst = pDib + headerSize;
				for (var row = 0; row < height; row++)
				{
					Buffer.MemoryCopy(
						pixelSrc + (long)(height - 1 - row) * stride,
						pixelDst + (long)row * stride,
						stride, stride);
				}
			}

			writes.Add(new PendingWrite(CLIPBOARD_FORMAT.CF_DIB, dib, default));
		}
#endif
	}
	private static void ResolveUnknownData(List<PendingWrite> writes, DataPackageView view, string format)
	{
		if (!WaitForAsyncOperation(view.GetDataAsync(format), out var task))
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(view.GetDataAsync)} failed to fetch data to be copied to the clipboard: {task.Status}", task.Exception!);
			return;
		}

		var cfid = (CLIPBOARD_FORMAT)PInvoke.RegisterClipboardFormat(format);
		if (cfid == 0)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.RegisterClipboardFormat)} failed: {Win32Helper.GetErrorMessage()}");
			return;
		}

		if (task.Result is IRandomAccessStream ras)
		{
			ras.Seek(0);
			var size = ras.Size;
			if (size > int.MaxValue)
			{
				typeof(Win32ClipboardExtension).LogError()?.Error($"Clipboard data for format '{format}' is too large to copy (size={size} bytes).");
				return;
			}

			var bytes = new byte[checked((int)size)];
			ras.AsStreamForRead().ReadExactly(bytes);

			writes.Add(new PendingWrite(cfid, bytes, default));
		}
		else if (task.Result is string str)
		{
			var p = _knownTextBasedClipboardFormats.TryGetValue(format, out var marshaler)
				? marshaler.ToPointer(str)
				: Marshal.StringToCoTaskMemUni(str);

			writes.Add(new PendingWrite(cfid, default, p));
		}
	}

	private static unsafe void SetClipboardData(CLIPBOARD_FORMAT format, ReadOnlySpan<byte> data)
	{
		// If the hMem parameter identifies a memory object, the object must have been allocated using the function with the GMEM_MOVEABLE flag
		var shouldFree = true;
		using var allocDisposable = Win32Helper.GlobalAlloc(
			GLOBAL_ALLOC_FLAGS.GMEM_MOVEABLE,
			(UIntPtr)data.Length,
			out var handle,
			// ReSharper disable once AccessToModifiedClosure
			() => shouldFree);

		if (allocDisposable is null) return;

		using var lockDisposable = Win32Helper.GlobalLock(handle, out var dst);
		fixed (byte* src = &MemoryMarshal.GetReference(data))
		{
			Buffer.MemoryCopy(src, dst, data.Length, data.Length);
		}

		var result = PInvoke.SetClipboardData((uint)format, new HANDLE(handle));
		if (result == HANDLE.Null)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.SetClipboardData)} failed: {Win32Helper.GetErrorMessage()}");
		}
		else
		{
			// If SetClipboardData succeeds, the system owns the object identified by the hMem parameter.
			// The application may not write to or free the data once ownership has been transferred to the system
			shouldFree = false;
		}
	}
	private static void SetClipboardHBitmapData(CLIPBOARD_FORMAT format, HBITMAP hbitmap)
	{
		var result = PInvoke.SetClipboardData((uint)format, new HANDLE(hbitmap));
		if (result == HANDLE.Null)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.SetClipboardData)} failed: {Win32Helper.GetErrorMessage()}");

			// System did not take ownership — free the HBITMAP ourselves
			PInvoke.DeleteObject(hbitmap);
		}
		else
		{
			// On success the system owns the HBITMAP; do not delete it
		}
	}
	private static void SetClipboardCoTaskMemData(CLIPBOARD_FORMAT format, IntPtr p)
	{
		var result = PInvoke.SetClipboardData((uint)format, new HANDLE(p));
		if (result == HANDLE.Null)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.SetClipboardData)} failed: {Win32Helper.GetErrorMessage()}");

			Marshal.FreeCoTaskMem(p);
		}
		else
		{
			// If SetClipboardData succeeds, the system owns the object identified by the hMem parameter.
			// The application may not write to or free the data once ownership has been transferred to the system
		}
	}

	private static bool WaitForAsyncOperation<T>(IAsyncOperation<T> operation, out Task<T> task)
	{
		task = operation.AsTask();
		while (!task.IsCompleted)
		{
			Win32EventLoop.RunOnce();
		}

		return task.IsCompletedSuccessfully;
	}
}

