#nullable enable

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Foundation;
using Windows.Storage;
using Windows.System;

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
	private static readonly Dictionary<nint, MacOSDragDropExtension> _extensions = new();
	private static int _callbacksRegistered;

	private readonly DragDropManager _manager;
	private readonly CoreDragDropManager _coreDragDropManager;
	private readonly MacOSWindowHost _host;
	private readonly nint _windowHandle;

	public MacOSDragDropExtension(DragDropManager manager)
	{
		_manager = manager ?? throw new ArgumentNullException(nameof(manager));

		var xamlRoot = manager.ContentRoot.GetOrCreateXamlRoot();
		_host = XamlRootMap.GetHostForRoot(xamlRoot) as MacOSWindowHost
			?? throw new InvalidOperationException($"Couldn't find a {nameof(MacOSWindowHost)} for the current {nameof(XamlRoot)}.");
		_coreDragDropManager = XamlRoot.GetCoreDragDropManager(xamlRoot);
		_windowHandle = _host.NativeWindowHandle;

		_extensions[_windowHandle] = this;
	}

	public static unsafe void Register()
	{
		ApiExtensibility.Register<DragDropManager>(typeof(IDragDropExtension), manager => new MacOSDragDropExtension(manager));

		if (Interlocked.Exchange(ref _callbacksRegistered, 1) == 0)
		{
			NativeUno.uno_drag_drop_set_callbacks(&OnDragEntered, &OnDragUpdated, &OnDragExited, &OnDragPerformed);
		}
	}

	public void StartNativeDrag(CoreDragInfo info, Action<DataPackageOperation> onCompleted)
	{
		// Outbound drags initiated from Uno content are not yet implemented for the macOS Skia head,
		// matching the current behavior of the Win32 and X11 Skia heads.
		if (this.Log().IsEnabled(LogLevel.Warning))
		{
			this.Log().Warn($"{nameof(StartNativeDrag)} is not implemented on macOS Skia.");
		}
		onCompleted(DataPackageOperation.None);
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
