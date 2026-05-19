#nullable enable

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;

using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Foundation;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Uno.Extensions;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;

namespace Uno.UI.Runtime.Skia.MacOS;

internal partial class MacOSDragDropExtension : IDragDropExtension
{
	private static readonly long _fakePointerId = Pointer.CreateUniqueIdForUnknownPointer();
	private static readonly ConcurrentDictionary<nint, MacOSDragDropExtension> _extensions = new();
	private static int _callbacksRegistered;

	private readonly DragDropManager _manager;
	private readonly CoreDragDropManager _coreDragDropManager;
	private readonly MacOSWindowHost _host;
	private readonly nint _windowHandle;

	// A single outbound session per window at a time — AppKit only supports one drag session per view.
	private Action<DataPackageOperation>? _pendingDragCompletion;

	public MacOSDragDropExtension(DragDropManager manager)
	{
		_manager = manager ?? throw new ArgumentNullException(nameof(manager));

		var xamlRoot = manager.ContentRoot.GetOrCreateXamlRoot();
		_host = XamlRootMap.GetHostForRoot(xamlRoot) as MacOSWindowHost
			?? throw new InvalidOperationException($"Couldn't find a {nameof(MacOSWindowHost)} for the current {nameof(XamlRoot)}.");
		_coreDragDropManager = XamlRoot.GetCoreDragDropManager(xamlRoot);
		_windowHandle = _host.NativeWindowHandle;

		_extensions[_windowHandle] = this;
		_host.Closed += OnHostClosed;
	}

	private void OnHostClosed(object? sender, EventArgs e)
	{
		_host.Closed -= OnHostClosed;
		_extensions.TryRemove(_windowHandle, out _);
		_pendingDragCompletion = null;
	}

	public static unsafe void Register()
	{
		ApiExtensibility.Register<DragDropManager>(typeof(IDragDropExtension), manager => new MacOSDragDropExtension(manager));

		if (Interlocked.Exchange(ref _callbacksRegistered, 1) == 0)
		{
			NativeUno.uno_drag_drop_set_callbacks(&OnDragEntered, &OnDragUpdated, &OnDragExited, &OnDragPerformed);
			NativeUno.uno_drag_drop_set_session_ended_callback(&OnDragSessionEnded);
		}
	}

	public void StartNativeDrag(CoreDragInfo info, Action<DataPackageOperation> onCompleted)
	{
		// AppKit's beginDraggingSession must run on the main thread and be called from
		// a mouse-event context, which is the case when DragOperation hands us control.
		_ = CoreDispatcher.Main.RunAsync(
			CoreDispatcherPriority.High,
			async () =>
			{
				try
				{
					await StartNativeDragCoreAsync(info, onCompleted);
				}
				catch (Exception e)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error("Failed to start native drag on macOS Skia.", e);
					}
					CompleteWith(onCompleted, DataPackageOperation.None);
				}
			});
	}

	private async Task StartNativeDragCoreAsync(CoreDragInfo info, Action<DataPackageOperation> onCompleted)
	{
		var view = info.Data;

		string? text = view.Contains(StandardDataFormats.Text) ? await view.GetTextAsync() : null;
		string? html = view.Contains(StandardDataFormats.Html) ? await view.GetHtmlFormatAsync() : null;
		string? rtf = view.Contains(StandardDataFormats.Rtf) ? await view.GetRtfAsync() : null;

		var uri = DataPackage.CombineUri(
			view.Contains(StandardDataFormats.WebLink) ? (await view.GetWebLinkAsync()).ToString() : null,
			view.Contains(StandardDataFormats.ApplicationLink) ? (await view.GetApplicationLinkAsync()).ToString() : null,
			view.Contains(StandardDataFormats.Uri) ? (await view.GetUriAsync()).ToString() : null);

		byte[]? bitmapBytes = null;
		if (view.Contains(StandardDataFormats.Bitmap))
		{
			try
			{
				var streamRef = await view.GetBitmapAsync();
				await using var stream = (await streamRef.OpenReadAsync()).AsStream();
				using var ms = new MemoryStream();
				await stream.CopyToAsync(ms);
				bitmapBytes = ms.ToArray();
			}
			catch (Exception e) when (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn("Failed to read bitmap from drag DataPackage.", e);
			}
		}

		string[] filePaths = Array.Empty<string>();
		if (view.Contains(StandardDataFormats.StorageItems))
		{
			try
			{
				var items = await view.GetStorageItemsAsync();
				var paths = new List<string>(items.Count);
				foreach (var item in items)
				{
					if (!string.IsNullOrEmpty(item.Path))
					{
						paths.Add(item.Path);
					}
				}
				filePaths = paths.ToArray();
			}
			catch (Exception e) when (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn("Failed to read StorageItems from drag DataPackage.", e);
			}
		}

		// Replace any previous unanswered completion — AppKit would have already
		// ended the prior session before a new one could start here.
		_pendingDragCompletion = onCompleted;

		var started = StartNativeDragUnsafe(info.AllowedOperations, text, html, rtf, string.IsNullOrEmpty(uri) ? null : uri, filePaths, bitmapBytes);
		if (!started)
		{
			_pendingDragCompletion = null;
			CompleteWith(onCompleted, DataPackageOperation.None);
		}
	}

	private unsafe bool StartNativeDragUnsafe(DataPackageOperation allowed, string? text, string? html, string? rtf, string? uri, string[] filePaths, byte[]? bitmapBytes)
	{
		byte* textPtr = null, htmlPtr = null, rtfPtr = null, uriPtr = null, bitmapPtr = null;
		byte** fileUrlsPtr = null;
		var allocatedFileUrls = 0;
		try
		{
			textPtr = Utf8Alloc(text);
			htmlPtr = Utf8Alloc(html);
			rtfPtr = Utf8Alloc(rtf);
			uriPtr = Utf8Alloc(uri);

			if (bitmapBytes is { Length: > 0 })
			{
				bitmapPtr = (byte*)NativeMemory.Alloc((nuint)bitmapBytes.Length);
				fixed (byte* src = bitmapBytes)
				{
					NativeMemory.Copy(src, bitmapPtr, (nuint)bitmapBytes.Length);
				}
			}

			if (filePaths.Length > 0)
			{
				fileUrlsPtr = (byte**)NativeMemory.Alloc((nuint)(filePaths.Length * sizeof(nint)));
				for (var i = 0; i < filePaths.Length; i++)
				{
					fileUrlsPtr[i] = Utf8Alloc(filePaths[i])!;
					allocatedFileUrls++;
				}
			}

			var data = new NativeDragSourceData
			{
				AllowedOperations = (uint)allowed,
				TextContent = textPtr,
				HtmlContent = htmlPtr,
				RtfContent = rtfPtr,
				Uri = uriPtr,
				FileUrls = fileUrlsPtr,
				FileCount = (uint)filePaths.Length,
				BitmapData = bitmapPtr,
				BitmapSize = bitmapBytes is null ? 0u : (uint)bitmapBytes.Length,
			};

			return NativeUno.uno_drag_start(_windowHandle, &data);
		}
		finally
		{
			if (textPtr != null) NativeMemory.Free(textPtr);
			if (htmlPtr != null) NativeMemory.Free(htmlPtr);
			if (rtfPtr != null) NativeMemory.Free(rtfPtr);
			if (uriPtr != null) NativeMemory.Free(uriPtr);
			if (bitmapPtr != null) NativeMemory.Free(bitmapPtr);
			if (fileUrlsPtr != null)
			{
				for (var i = 0; i < allocatedFileUrls; i++)
				{
					if (fileUrlsPtr[i] != null)
					{
						NativeMemory.Free(fileUrlsPtr[i]);
					}
				}
				NativeMemory.Free(fileUrlsPtr);
			}
		}
	}

	private static unsafe byte* Utf8Alloc(string? s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return null;
		}
		var byteCount = Encoding.UTF8.GetByteCount(s);
		var buffer = (byte*)NativeMemory.Alloc((nuint)(byteCount + 1));
		fixed (char* chars = s)
		{
			Encoding.UTF8.GetBytes(chars, s.Length, buffer, byteCount);
		}
		buffer[byteCount] = 0;
		return buffer;
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnDragSessionEnded(nint windowHandle, uint operation)
	{
		try
		{
			var extension = Get(windowHandle);
			var completion = extension?._pendingDragCompletion;
			if (extension is not null)
			{
				extension._pendingDragCompletion = null;
			}
			completion?.Invoke((DataPackageOperation)operation);
		}
		catch (Exception e)
		{
			Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	private static void CompleteWith(Action<DataPackageOperation> onCompleted, DataPackageOperation op)
	{
		try { onCompleted(op); }
		catch (Exception e) { Application.Current.RaiseRecoverableUnhandledException(e); }
	}

	private static MacOSDragDropExtension? Get(nint handle)
		=> _extensions.TryGetValue(handle, out var extension) ? extension : null;

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static unsafe uint OnDragEntered(nint windowHandle, NativeDragDropData* data)
	{
		try
		{
			var extension = Get(windowHandle);
			if (extension is null || data is null)
			{
				return 0;
			}
			return (uint)extension.HandleDragEntered(*data);
		}
		catch (Exception e)
		{
			Application.Current.RaiseRecoverableUnhandledException(e);
			return 0;
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static unsafe uint OnDragUpdated(nint windowHandle, NativeDragDropData* data)
	{
		try
		{
			var extension = Get(windowHandle);
			if (extension is null || data is null)
			{
				return 0;
			}
			return (uint)extension.HandleDragUpdated(*data);
		}
		catch (Exception e)
		{
			Application.Current.RaiseRecoverableUnhandledException(e);
			return 0;
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static unsafe uint OnDragExited(nint windowHandle, NativeDragDropData* data)
	{
		try
		{
			var extension = Get(windowHandle);
			extension?.HandleDragExited();
			return 0;
		}
		catch (Exception e)
		{
			Application.Current.RaiseRecoverableUnhandledException(e);
			return 0;
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static unsafe uint OnDragPerformed(nint windowHandle, NativeDragDropData* data)
	{
		try
		{
			var extension = Get(windowHandle);
			if (extension is null || data is null)
			{
				return 0;
			}
			return (uint)extension.HandleDragPerformed(*data);
		}
		catch (Exception e)
		{
			Application.Current.RaiseRecoverableUnhandledException(e);
			return 0;
		}
	}

	private DataPackageOperation HandleDragEntered(NativeDragDropData data)
	{
		var src = CreateSource(data);
		var package = BuildDataPackage(data);
		var allowed = (DataPackageOperation)data.AllowedOperations;
		var info = new CoreDragInfo(src, package.GetView(), allowed, dragUI: null);

		_coreDragDropManager.DragStarted(info);

		return _manager.ProcessMoved(src);
	}

	private DataPackageOperation HandleDragUpdated(NativeDragDropData data)
	{
		var src = CreateSource(data);
		return _manager.ProcessMoved(src);
	}

	private void HandleDragExited()
	{
		_manager.ProcessAborted(_fakePointerId);
	}

	private DataPackageOperation HandleDragPerformed(NativeDragDropData data)
	{
		var src = CreateSource(data);
		return _manager.ProcessReleased(src);
	}

	private static DragEventSource CreateSource(NativeDragDropData data)
		=> new DragEventSource(new Point(data.X, data.Y), data.Modifiers);

	private static unsafe DataPackage BuildDataPackage(NativeDragDropData data)
	{
		var package = new DataPackage();
		var paths = ExtractFilePaths(data);

		var text = ReadUtf8(data.TextContent);
		if (!string.IsNullOrEmpty(text))
		{
			package.SetText(text);
		}

		var html = ReadUtf8(data.HtmlContent);
		if (!string.IsNullOrEmpty(html))
		{
			package.SetHtmlFormat(html);
		}

		var rtf = ReadUtf8(data.RtfContent);
		if (!string.IsNullOrEmpty(rtf))
		{
			package.SetRtf(rtf);
		}

		var uri = ReadUtf8(data.Uri);
		if (!string.IsNullOrEmpty(uri))
		{
			DataPackage.SeparateUri(uri, out var webLink, out var applicationLink);
			if (webLink is not null)
			{
				package.SetWebLink(new Uri(webLink));
			}
			if (applicationLink is not null)
			{
				package.SetApplicationLink(new Uri(applicationLink));
			}
			package.SetUri(new Uri(uri));
		}

		if (paths.Length > 0)
		{
			package.SetDataProvider(
				StandardDataFormats.StorageItems,
				_ => ResolveStorageItems(paths));
		}

		return package;
	}

	private static unsafe string[] ExtractFilePaths(NativeDragDropData data)
	{
		if (data.FileCount == 0 || data.FileUrls == null)
		{
			return Array.Empty<string>();
		}
		var paths = new string[data.FileCount];
		for (uint i = 0; i < data.FileCount; i++)
		{
			paths[i] = Marshal.PtrToStringUTF8((nint)data.FileUrls[i]) ?? string.Empty;
		}
		return paths;
	}

	private static unsafe string? ReadUtf8(byte* ptr) => Marshal.PtrToStringUTF8((nint)ptr);

	private static async Task<object> ResolveStorageItems(string[] paths)
	{
		var items = new List<IStorageItem>(paths.Length);
		foreach (var path in paths)
		{
			if (string.IsNullOrEmpty(path))
			{
				continue;
			}
			try
			{
				if (Directory.Exists(path))
				{
					items.Add(await StorageFolder.GetFolderFromPathAsync(path));
				}
				else if (File.Exists(path))
				{
					items.Add(await StorageFile.GetFileFromPathAsync(path));
				}
			}
			catch (Exception e)
			{
				if (typeof(MacOSDragDropExtension).Log().IsEnabled(LogLevel.Warning))
				{
					typeof(MacOSDragDropExtension).Log().Warn($"Failed to create StorageItem for '{path}'", e);
				}
			}
		}
		return items.AsReadOnly();
	}

	private readonly struct DragEventSource : IDragEventSource
	{
		private static long _nextFrameId;

		private readonly Point _location;
		private readonly VirtualKeyModifiers _modifiers;

		public DragEventSource(Point location, VirtualKeyModifiers modifiers)
		{
			_location = location;
			_modifiers = modifiers;
			FrameId = (uint)Interlocked.Increment(ref _nextFrameId);
		}

		public long Id => _fakePointerId;

		public uint FrameId { get; }

		public (Point location, DragDropModifiers modifier) GetState()
		{
			var flags = DragDropModifiers.None;
			if ((_modifiers & VirtualKeyModifiers.Shift) != 0) flags |= DragDropModifiers.Shift;
			if ((_modifiers & VirtualKeyModifiers.Control) != 0) flags |= DragDropModifiers.Control;
			// macOS doesn't report mouse buttons during a drag — the drag itself implies LeftButton.
			flags |= DragDropModifiers.LeftButton;
			return (_location, flags);
		}

		public Point GetPosition(object? relativeTo)
		{
			if (relativeTo is null)
			{
				return _location;
			}
			if (relativeTo is UIElement elt)
			{
				var eltToRoot = UIElement.GetTransform(elt, null);
				var rootToElt = eltToRoot.Inverse();
				return rootToElt.Transform(_location);
			}
			throw new InvalidOperationException("The relative to must be a UIElement.");
		}
	}
}
