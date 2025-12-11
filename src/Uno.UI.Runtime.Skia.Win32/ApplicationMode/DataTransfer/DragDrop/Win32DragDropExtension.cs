using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.Com;
using Windows.Win32.System.Ole;
using Windows.Win32.System.SystemServices;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.UI.NativeElementHosting;
using IDataObject = Windows.Win32.System.Com.IDataObject;
using Microsoft.UI.Input;

namespace Uno.UI.Runtime.Skia.Win32;

internal class Win32DragDropExtension : IDragDropExtension, IDropTarget.Interface
{
	[DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
	private static extern unsafe uint ExtractIconExW(string lpszFile, int nIconIndex, HICON* phiconLarge, HICON* phiconSmall, uint nIcons);

	[DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
	private static extern unsafe IntPtr SHGetFileInfoW(string pszPath, uint dwFileAttributes, IntPtr psfi, uint cbFileInfo, uint uFlags);

	// Constants for SHGetFileInfo flags
	private const uint SHGFI_ICON = 0x100;
	private const uint SHGFI_LARGEICON = 0x0;
	private const uint SHGFI_SMALLICON = 0x1;
	private const uint SHGFI_USEFILEATTRIBUTES = 0x10;
	private const uint FILE_ATTRIBUTE_NORMAL = 0x80;

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct SHFILEINFOW
	{
		public IntPtr hIcon;
		public int iIcon;
		public uint dwAttributes;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		public string szDisplayName;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
		public string szTypeName;
	}

	private static readonly long _fakePointerId = Pointer.CreateUniqueIdForUnknownPointer();

	private readonly DragDropManager _manager;
	private readonly CoreDragDropManager _coreDragDropManager;
	private readonly HWND _hwnd;
	private readonly ComScope<IDropTarget> _dropTarget;

	public unsafe Win32DragDropExtension(DragDropManager manager)
	{
		var host = XamlRootMap.GetHostForRoot(manager.ContentRoot.GetOrCreateXamlRoot()) as Win32WindowWrapper ?? throw new InvalidOperationException($"Couldn't find an {nameof(Win32WindowWrapper)} instance associated with this {nameof(XamlRoot)}.");
		_coreDragDropManager = XamlRoot.GetCoreDragDropManager(((IXamlRootHost)host).RootElement!.XamlRoot);
		_manager = manager;
		_hwnd = (HWND)((Win32NativeWindow)host.NativeWindow).Hwnd;

		// Note: we're deliberately not disposing the ComScope (which calls ReleaseRef()) here because the IDragDropExtension instance
		// should last as long as the window that created it.
		_dropTarget = ComHelpers.TryGetComScope<IDropTarget>(this, out HRESULT hResult);
		if (hResult.Failed)
		{
			this.LogError()?.Error($"{nameof(ComHelpers.TryGetComScope)}<{nameof(IDropTarget)}> failed: {Win32Helper.GetErrorMessage(hResult)}");
			return;
		}

		// RegisterDragDrop calls AddRef()
		hResult = PInvoke.RegisterDragDrop(_hwnd, _dropTarget);
		if (hResult.Failed)
		{
			this.LogError()?.Error($"{nameof(PInvoke.RegisterDragDrop)} failed: {Win32Helper.GetErrorMessage(hResult)}");
		}
	}

	~Win32DragDropExtension()
	{
		_dropTarget.Dispose();
	}

	private Point GetScaledPosition(float x, float y)
	{
		var xamlRoot = _manager.ContentRoot.GetOrCreateXamlRoot();
		return new Point(x / xamlRoot.RasterizationScale, y / xamlRoot.RasterizationScale);
	}

	unsafe HRESULT IDropTarget.Interface.DragEnter(IDataObject* dataObject, MODIFIERKEYS_FLAGS grfKeyState, POINTL pt, DROPEFFECT* pdwEffect)
	{
		Debug.Assert(_manager is not null && _coreDragDropManager is not null);

		IEnumFORMATETC* enumFormatEtc;
		var hResult = dataObject->EnumFormatEtc((uint)DATADIR.DATADIR_GET, &enumFormatEtc);
		if (hResult.Failed)
		{
			this.LogError()?.Error($"{nameof(IDataObject.EnumFormatEtc)} failed: {Win32Helper.GetErrorMessage(hResult)}");
			return HRESULT.E_UNEXPECTED;
		}

		using var enumFormatDisposable = new DisposableStruct<IntPtr>(static p => ((IEnumFORMATETC*)p)->Release(), (IntPtr)enumFormatEtc);

		enumFormatEtc->Reset();
		const int formatBufferLength = 100;
		var formatBuffer = stackalloc FORMATETC[formatBufferLength];
		uint fetchedFormatCount;
		hResult = enumFormatEtc->Next(formatBufferLength, formatBuffer, &fetchedFormatCount);
		if (hResult.Failed)
		{
			this.LogError()?.Error($"{nameof(PInvoke.RegisterDragDrop)} failed: {Win32Helper.GetErrorMessage(hResult)}");
			return HRESULT.E_UNEXPECTED;
		}

		var position = new System.Drawing.Point(pt.x, pt.y);

		var success = PInvoke.ScreenToClient(_hwnd, ref position);
		if (!success) { this.LogError()?.Error($"{nameof(PInvoke.ScreenToClient)} failed: {Win32Helper.GetErrorMessage()}"); }
		var scaledPosition = GetScaledPosition(position.X, position.Y);

		var src = new DragEventSource(scaledPosition, grfKeyState);

		var formats = new Span<FORMATETC>(formatBuffer, (int)fetchedFormatCount);
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			var log = $"{nameof(IDropTarget.Interface.DragEnter)} @ {position}, formats: ";
			foreach (var format in formats)
			{
				log += (CLIPBOARD_FORMAT)format.cfFormat + " ";
			}
			this.Log().Trace(log);
		}

		var package = new DataPackage();

		var formatEtcList = formats.ToArray();
		var formatList =
			formatEtcList
			.Where(static formatetc =>
			{
				if (!Enum.IsDefined((CLIPBOARD_FORMAT)formatetc.cfFormat))
				{
					return false;
				}
				if (formatetc.tymed != (uint)TYMED.TYMED_HGLOBAL)
				{
					typeof(Win32DragDropExtension).LogError()?.Error($"{nameof(IDropTarget.Interface.DragEnter)} found {Enum.GetName((CLIPBOARD_FORMAT)formatetc.cfFormat)}, but {nameof(TYMED)} is not {nameof(TYMED.TYMED_HGLOBAL)}");
					return false;
				}

				return true;
			})
			.Select(f => (CLIPBOARD_FORMAT)f.cfFormat)
			.ToList();

		var mediumsToDispose = new List<STGMEDIUM>();
		using var mediumsDisposable = new DisposableStruct<List<STGMEDIUM>>(static list =>
		{
			foreach (var medium in list)
			{
				PInvoke.ReleaseStgMedium(&medium);
			}
		}, mediumsToDispose);
		Win32ClipboardExtension.ReadContentIntoPackage(package, formatList, format =>
		{
			var formatEtc = formatEtcList.First(f => f.cfFormat == (int)format);
			dataObject->GetData(formatEtc, out STGMEDIUM medium);
			mediumsToDispose.Add(medium);
			return medium.u.hGlobal;
		});

		// Create DragUI for visual feedback during drag operation
		var dragUI = CreateDragUIForExternalDrag(dataObject, formatEtcList);

		// DROPEFFECT and DataPackageOperation have the same binary representation
		var info = new CoreDragInfo(src, package.GetView(), (DataPackageOperation)(*pdwEffect), dragUI);
		_coreDragDropManager.DragStarted(info);

		*pdwEffect = (DROPEFFECT)_manager.ProcessMoved(src);

		return HRESULT.S_OK;
	}

	unsafe HRESULT IDropTarget.Interface.DragOver(MODIFIERKEYS_FLAGS grfKeyState, POINTL pt, DROPEFFECT* pdwEffect)
	{
		var position = new System.Drawing.Point(pt.x, pt.y);
		var success = PInvoke.ScreenToClient(_hwnd, ref position);
		if (!success) { this.LogError()?.Error($"{nameof(PInvoke.ScreenToClient)} failed: {Win32Helper.GetErrorMessage()}"); }
		var scaledPosition = GetScaledPosition(position.X, position.Y);
		var src = new DragEventSource(scaledPosition, grfKeyState);

		this.LogTrace()?.Trace($"{nameof(IDropTarget.Interface.DragOver)} @ {position}");

		*pdwEffect = (DROPEFFECT)_manager.ProcessMoved(src);

		return HRESULT.S_OK;
	}

	HRESULT IDropTarget.Interface.DragLeave()
	{
		this.LogTrace()?.Trace($"{nameof(IDropTarget.Interface.DragLeave)}");

		_manager.ProcessAborted(_fakePointerId);

		return HRESULT.S_OK;
	}

	unsafe HRESULT IDropTarget.Interface.Drop(IDataObject* dataObject, MODIFIERKEYS_FLAGS grfKeyState, POINTL pt, DROPEFFECT* pdwEffect)
	{
		var position = new System.Drawing.Point(pt.x, pt.y);
		var success = PInvoke.ScreenToClient(_hwnd, ref position);
		if (!success) { this.LogError()?.Error($"{nameof(PInvoke.ScreenToClient)} failed: {Win32Helper.GetErrorMessage()}"); }
		var scaledPosition = GetScaledPosition(position.X, position.Y);
		var src = new DragEventSource(scaledPosition, grfKeyState);

		this.LogTrace()?.Trace($"{nameof(IDropTarget.Interface.Drop)} @ {position}");

		*pdwEffect = (DROPEFFECT)_manager.ProcessReleased(src);

		return HRESULT.S_OK;
	}

	public void StartNativeDrag(CoreDragInfo info, Action<DataPackageOperation> action) => throw new System.NotImplementedException();

	private static unsafe DragUI? CreateDragUIForExternalDrag(IDataObject* dataObject, FORMATETC[] formatEtcList)
	{
		var dragUI = new DragUI();

		// Check if we have a DIB (Device Independent Bitmap) format
		var dibFormatIndex = Array.FindIndex(formatEtcList, f => f.cfFormat == (int)CLIPBOARD_FORMAT.CF_DIB);
		if (dibFormatIndex >= 0)
		{
			var dibFormat = formatEtcList[dibFormatIndex];
			// Try to get the DIB data directly
			var hResult = dataObject->GetData(dibFormat, out STGMEDIUM dibMedium);
			if (hResult.Succeeded && dibMedium.tymed == TYMED.TYMED_HGLOBAL && dibMedium.u.hGlobal != IntPtr.Zero)
			{
				try
				{
					var unoImage = ConvertDibToUnoBitmapImage(dibMedium.u.hGlobal);
					if (unoImage is not null)
					{
						dragUI.SetContentFromExternalBitmapImage(unoImage);
						return dragUI;
					}
				}
				catch (Exception ex)
				{
					// If we can't load the image, continue without visual feedback
					var logger = typeof(Win32DragDropExtension).Log();
					if (logger.IsEnabled(LogLevel.Debug))
					{
						logger.LogDebug($"Failed to load image thumbnail for drag operation: {ex.Message}");
					}
				}
				finally
				{
					PInvoke.ReleaseStgMedium(&dibMedium);
				}
			}
		}

		// Check if we have file drop format
		var hdropFormatIndex = Array.FindIndex(formatEtcList, f => f.cfFormat == (int)CLIPBOARD_FORMAT.CF_HDROP);
		if (hdropFormatIndex >= 0)
		{
			var hdropFormat = formatEtcList[hdropFormatIndex];
			// Try to get the HDROP data directly
			var hResult = dataObject->GetData(hdropFormat, out STGMEDIUM hdropMedium);
			if (hResult.Succeeded && hdropMedium.u.hGlobal != IntPtr.Zero)
			{
				try
				{
					var filePaths = ExtractFilePathsFromHDrop(hdropMedium.u.hGlobal);
					var imageFile = filePaths.FirstOrDefault(f => IsImageFile(f));
					if (imageFile is not null)
					{
						try
						{
							var unoImage = LoadImageFromFile(imageFile);
							if (unoImage is not null)
							{
								dragUI.SetContentFromExternalBitmapImage(unoImage);
								return dragUI;
							}
						}
						catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or UriFormatException)
						{
							// If we can't load the image, continue without visual feedback
							var logger = typeof(Win32DragDropExtension).Log();
							if (logger.IsEnabled(LogLevel.Debug))
							{
								logger.LogDebug($"Failed to load image thumbnail for drag operation: {ex.Message}");
							}
						}
					}

					// For non-image files, try to get the file icon
					var firstFile = filePaths.FirstOrDefault();
					if (firstFile is not null)
					{
						try
						{
							var iconImage = ExtractFileIcon(firstFile);
							if (iconImage is null)
							{
								var icon = GetFileTypeIcon(firstFile);
								try
								{
									// Convert HICON to BitmapImage
									iconImage = ConvertHIconToBitmapImage(icon);
								}
								finally
								{
									// Cleanup: destroy the icon handle
									PInvoke.DestroyIcon(icon);
								}
							}
							if (iconImage is not null)
							{
								dragUI.SetContentFromExternalBitmapImage(iconImage);
								return dragUI;
							}
						}
						catch (Exception ex)
						{
							var logger = typeof(Win32DragDropExtension).Log();
							if (logger.IsEnabled(LogLevel.Debug))
							{
								logger.LogDebug($"Failed to extract file icon for drag operation: {ex.Message}");
							}
						}
					}
				}
				finally
				{
					PInvoke.ReleaseStgMedium(&hdropMedium);
				}
			}
		}

		return dragUI;
	}

	private static bool IsImageFile(string filePath)
	{
		// Common image formats
		// Note: Additional formats can be added here as needed
		var extension = Path.GetExtension(filePath).ToLowerInvariant();
		return extension is ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".tiff" or ".ico";
	}

	private static unsafe List<string> ExtractFilePathsFromHDrop(HGLOBAL handle)
	{
		var filePaths = new List<string>();

		using var lockDisposable = Win32Helper.GlobalLock(handle, out var firstByte);
		if (lockDisposable is null)
		{
			return filePaths;
		}

		var hDrop = new Windows.Win32.UI.Shell.HDROP((IntPtr)firstByte);
		var filesDropped = PInvoke.DragQueryFile(hDrop, 0xFFFFFFFF, new PWSTR(), 0);

		for (uint i = 0; i < filesDropped; i++)
		{
			var charLength = PInvoke.DragQueryFile(hDrop, i, new PWSTR(), 0);
			if (charLength == 0)
			{
				continue;
			}
			charLength++; // + 1 for \0

			var buffer = Marshal.AllocHGlobal((IntPtr)(charLength * sizeof(char)));
			try
			{
				var charsWritten = PInvoke.DragQueryFile(hDrop, i, new PWSTR((char*)buffer), charLength);
				if (charsWritten > 0)
				{
					var path = Marshal.PtrToStringUni(buffer);
					if (!string.IsNullOrEmpty(path))
					{
						filePaths.Add(path);
					}
				}
			}
			finally
			{
				Marshal.FreeHGlobal(buffer);
			}
		}

		return filePaths;
	}

	private static Microsoft.UI.Xaml.Media.Imaging.BitmapImage? LoadImageFromFile(string filePath)
	{
		try
		{
			// Validate file path to prevent potential security issues
			if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
			{
				return null;
			}

			// Load image from file
			using var fileStream = File.OpenRead(filePath);
			var unoBitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
			unoBitmap.SetSource(fileStream.AsRandomAccessStream());

			return unoBitmap;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or UriFormatException)
		{
			// Failed to load image - file might not exist, no access, unsupported format, or invalid path
			return null;
		}
	}

	private static unsafe Microsoft.UI.Xaml.Media.Imaging.BitmapImage? ConvertDibToUnoBitmapImage(HGLOBAL handle)
	{
		try
		{
			using var lockDisposable = Win32Helper.GlobalLock(handle, out var dib);
			if (lockDisposable is null)
			{
				return null;
			}

			var memSize = (uint)PInvoke.GlobalSize(handle);
			if (memSize <= Marshal.SizeOf<BITMAPINFOHEADER>())
			{
				return null;
			}

			// Convert DIB to a stream that can be used by BitmapImage
			// Pre-allocate buffer for typical thumbnail size to avoid reallocations
			using var memoryStream = new MemoryStream(capacity: 8192);

			// Copy the DIB data to the memory stream
			var dibBytes = new Span<byte>(dib, (int)memSize);
			memoryStream.Write(dibBytes);
			memoryStream.Position = 0;

			// Create Uno BitmapImage from the stream
			var unoBitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
			unoBitmap.SetSource(memoryStream.AsRandomAccessStream());

			return unoBitmap;
		}
		catch (Exception ex) when (ex is IOException or NotSupportedException or InvalidOperationException)
		{
			// Failed to convert bitmap - encoding or stream operations failed
			return null;
		}
	}

	private static unsafe Microsoft.UI.Xaml.Media.Imaging.BitmapImage? ExtractFileIcon(string filePath)
	{
		try
		{
			// Validate file path
			if (string.IsNullOrWhiteSpace(filePath))
			{
				return null;
			}

			// Use ExtractIconExW to get the large icon for the file
			HICON largeIcon = default;
			HICON smallIcon = default;
			var result = ExtractIconExW(filePath, 0, &largeIcon, &smallIcon, 1);

			if (result == 0 || largeIcon == default)
			{
				// ExtractIconExW returns 0 if no icons were extracted
				if (smallIcon != default)
				{
					PInvoke.DestroyIcon(smallIcon);
				}
			}

			// Clean up the small icon if extracted
			if (smallIcon != default)
			{
				PInvoke.DestroyIcon(smallIcon);
			}

			var hIcon = largeIcon;

			try
			{
				// Convert HICON to BitmapImage
				return ConvertHIconToBitmapImage(hIcon);
			}
			finally
			{
				// Cleanup: destroy the icon handle
				PInvoke.DestroyIcon(hIcon);
			}
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
		{
			// Failed to extract icon
			return null;
		}
	}

	private static unsafe HICON GetFileTypeIcon(string filePath)
	{
		try
		{
			var fileInfo = new SHFILEINFOW();
			var flags = SHGFI_ICON | SHGFI_LARGEICON;

			// If the file doesn't exist, use SHGFI_USEFILEATTRIBUTES to get icon based on extension
			if (!File.Exists(filePath))
			{
				flags |= SHGFI_USEFILEATTRIBUTES;
			}

			var pFileInfo = Marshal.AllocHGlobal(Marshal.SizeOf<SHFILEINFOW>());
			try
			{
				Marshal.StructureToPtr(fileInfo, pFileInfo, false);

				var result = SHGetFileInfoW(
					filePath,
					File.Exists(filePath) ? 0 : FILE_ATTRIBUTE_NORMAL,
					pFileInfo,
					(uint)Marshal.SizeOf<SHFILEINFOW>(),
					flags
				);

				if (result != IntPtr.Zero)
				{
					fileInfo = Marshal.PtrToStructure<SHFILEINFOW>(pFileInfo);
					if (fileInfo.hIcon != IntPtr.Zero)
					{
						return new HICON(fileInfo.hIcon);
					}
				}

				return default;
			}
			finally
			{
				Marshal.FreeHGlobal(pFileInfo);
			}
		}
		catch
		{
			// If SHGetFileInfo fails, return default
			return default;
		}
	}

	private static unsafe Microsoft.UI.Xaml.Media.Imaging.BitmapImage? ConvertHIconToBitmapImage(HICON hIcon)
	{
		try
		{
			// Get icon info
			ICONINFO iconInfo;
			if (!PInvoke.GetIconInfo(hIcon, &iconInfo))
			{
				return null;
			}

			try
			{
				// Get bitmap info
				var bmp = new BITMAP();
				if (PInvoke.GetObject((HGDIOBJ)iconInfo.hbmColor, Marshal.SizeOf<BITMAP>(), &bmp) == 0)
				{
					return null;
				}

				var width = bmp.bmWidth;
				var height = bmp.bmHeight;

				// Create a device context
				var hdc = PInvoke.GetDC(HWND.Null);
				if (hdc == default)
				{
					return null;
				}

				try
				{
					var memDc = PInvoke.CreateCompatibleDC(hdc);
					if (memDc == default)
					{
						return null;
					}

					try
					{
						// Create bitmap info header for 32-bit RGBA
						var bitmapInfo = new BITMAPINFO
						{
							bmiHeader = new BITMAPINFOHEADER
							{
								biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
								biWidth = width,
								biHeight = -height, // Negative height for top-down DIB
								biPlanes = 1,
								biBitCount = 32,
								biCompression = (uint)BI_COMPRESSION.BI_RGB
							}
						};

						// Create DIB section
						void* bits;
						var hBitmap = PInvoke.CreateDIBSection(memDc, &bitmapInfo, DIB_USAGE.DIB_RGB_COLORS, &bits, HANDLE.Null, 0);
						if (hBitmap == default)
						{
							return null;
						}

						try
						{
							var oldBitmap = PInvoke.SelectObject(memDc, (HGDIOBJ)hBitmap);

							// Draw icon onto the bitmap
							if (!PInvoke.DrawIconEx(memDc, 0, 0, hIcon, width, height, 0, HBRUSH.Null, DI_FLAGS.DI_NORMAL))
							{
								return null;
							}

							PInvoke.SelectObject(memDc, oldBitmap);

							// Copy bitmap data to a byte array
							var stride = width * 4; // 4 bytes per pixel (RGBA)
							var dataSize = stride * height;
							var pixelData = new byte[dataSize];
							Marshal.Copy((IntPtr)bits, pixelData, 0, dataSize);

							// Create memory stream and write BMP file format
							using var memoryStream = new MemoryStream();

							// Write BMP file header
							var fileHeaderSize = 14;
							var infoHeaderSize = Marshal.SizeOf<BITMAPINFOHEADER>();
							var fileSize = fileHeaderSize + infoHeaderSize + dataSize;

							memoryStream.WriteByte((byte)'B');
							memoryStream.WriteByte((byte)'M');
							memoryStream.Write(BitConverter.GetBytes(fileSize), 0, 4);
							memoryStream.Write(BitConverter.GetBytes((int)0), 0, 4); // Reserved
							memoryStream.Write(BitConverter.GetBytes(fileHeaderSize + infoHeaderSize), 0, 4); // Offset to pixel data

							// Write BITMAPINFOHEADER
							memoryStream.Write(BitConverter.GetBytes(bitmapInfo.bmiHeader.biSize), 0, 4);
							memoryStream.Write(BitConverter.GetBytes(bitmapInfo.bmiHeader.biWidth), 0, 4);
							memoryStream.Write(BitConverter.GetBytes(-bitmapInfo.bmiHeader.biHeight), 0, 4); // Flip back to positive
							memoryStream.Write(BitConverter.GetBytes(bitmapInfo.bmiHeader.biPlanes), 0, 2);
							memoryStream.Write(BitConverter.GetBytes(bitmapInfo.bmiHeader.biBitCount), 0, 2);
							memoryStream.Write(BitConverter.GetBytes(bitmapInfo.bmiHeader.biCompression), 0, 4);
							memoryStream.Write(BitConverter.GetBytes(dataSize), 0, 4); // Image size
							memoryStream.Write(BitConverter.GetBytes((int)0), 0, 4); // X pixels per meter
							memoryStream.Write(BitConverter.GetBytes((int)0), 0, 4); // Y pixels per meter
							memoryStream.Write(BitConverter.GetBytes((int)0), 0, 4); // Colors used
							memoryStream.Write(BitConverter.GetBytes((int)0), 0, 4); // Important colors

							// Write pixel data (need to flip vertically for BMP format)
							for (int y = height - 1; y >= 0; y--)
							{
								memoryStream.Write(pixelData, y * stride, stride);
							}

							memoryStream.Position = 0;

							// Create Uno BitmapImage from the stream
							var unoBitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
							unoBitmap.SetSource(memoryStream.AsRandomAccessStream());

							return unoBitmap;
						}
						finally
						{
							PInvoke.DeleteObject((HGDIOBJ)hBitmap);
						}
					}
					finally
					{
						PInvoke.DeleteDC(memDc);
					}
				}
				finally
				{
					_ = PInvoke.ReleaseDC(HWND.Null, hdc);
				}
			}
			finally
			{
				// Cleanup icon info bitmaps
				if (iconInfo.hbmColor != default)
				{
					PInvoke.DeleteObject((HGDIOBJ)iconInfo.hbmColor);
				}
				if (iconInfo.hbmMask != default)
				{
					PInvoke.DeleteObject((HGDIOBJ)iconInfo.hbmMask);
				}
			}
		}
		catch (Exception ex) when (ex is IOException or NotSupportedException or InvalidOperationException)
		{
			// Failed to convert icon to bitmap
			return null;
		}
	}

	private readonly struct DragEventSource(Point point, MODIFIERKEYS_FLAGS modifierFlags) : IDragEventSource
	{
		private static long _nextFrameId;
		private readonly Point _location = point;

		public long Id => _fakePointerId;

		public uint FrameId { get; } = (uint)Interlocked.Increment(ref _nextFrameId);

		/// <inheritdoc />
		public (Point location, DragDropModifiers modifier) GetState()
		{
			var flags = DragDropModifiers.None;
			if ((modifierFlags & MODIFIERKEYS_FLAGS.MK_SHIFT) != 0)
			{
				flags |= DragDropModifiers.Shift;
			}
			if ((modifierFlags & MODIFIERKEYS_FLAGS.MK_CONTROL) != 0)
			{
				flags |= DragDropModifiers.Control;
			}
			if ((modifierFlags & MODIFIERKEYS_FLAGS.MK_LBUTTON) != 0)
			{
				flags |= DragDropModifiers.LeftButton;
			}
			if ((modifierFlags & MODIFIERKEYS_FLAGS.MK_RBUTTON) != 0)
			{
				flags |= DragDropModifiers.RightButton;
			}
			if ((modifierFlags & MODIFIERKEYS_FLAGS.MK_MBUTTON) != 0)
			{
				flags |= DragDropModifiers.MiddleButton;
			}
			return (_location, flags);
		}

		/// <inheritdoc />
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
