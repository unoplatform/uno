using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.Ole;
using Windows.Win32.UI.Shell;

namespace Uno.UI.Runtime.Skia.Win32;

// Clipboard-format readers consumed by Win32DragDropExtension, which feeds them HGLOBALs
// coming from an IDataObject instead of the clipboard.
internal partial class Win32ClipboardExtension
{
	internal static void ReadContentIntoPackage(DataPackage package, IEnumerable<CLIPBOARD_FORMAT> formats, Func<CLIPBOARD_FORMAT, HGLOBAL?> dataGetter)
	{
		ulong snapshotBytes = 0;
		var formatCount = 0;
		foreach (var format in formats)
		{
			if (++formatCount > MaxClipboardSnapshotFormats)
			{
				typeof(Win32ClipboardExtension).LogError()?.Error($"Data transfer contains more than {MaxClipboardSnapshotFormats} formats; remaining formats were ignored.");
				break;
			}
			if (Enum.IsDefined((CLIPBOARD_FORMAT)format) && dataGetter(format) is { } handle)
			{
				var formatBytes = (ulong)PInvoke.GlobalSize(handle);
				if (formatBytes == 0 || formatBytes > MaxClipboardFormatBytes || snapshotBytes > MaxClipboardSnapshotBytes - formatBytes)
				{
					typeof(Win32ClipboardExtension).LogError()?.Error($"Data transfer format {(uint)format} exceeds the snapshot budget and was ignored.");
					continue;
				}
				snapshotBytes += formatBytes;

				switch (format)
				{
					case CLIPBOARD_FORMAT.CF_UNICODETEXT:
						GetText(handle, package);
						break;
					case CLIPBOARD_FORMAT.CF_HDROP:
						var files = GetFileDropList(handle);
						if (files is not null)
						{
							package.SetStorageItems(files);
						}
						break;
					case CLIPBOARD_FORMAT.CF_DIB:
						GetBitmap(handle, package);
						break;
				}
			}
		}
	}
	private static unsafe void GetText(HGLOBAL handle, DataPackage package)
	{
		using var lockDisposable = Win32Helper.GlobalLock(handle, out var bytes);
		if (lockDisposable is null)
		{
			return;
		}

		var byteLength = checked((int)PInvoke.GlobalSize(handle));
		if (GetUnicodeString((IntPtr)bytes, byteLength) is { } text)
		{
			package.SetText(text);
		}
		else
		{
			typeof(Win32ClipboardExtension).LogError()?.Error("Transferred Unicode text is not null-terminated within its allocation.");
		}
	}
	private static unsafe void GetBitmap(HGLOBAL handle, DataPackage package)
	{
		using var lockDisposable = Win32Helper.GlobalLock(handle, out var dib);
		if (lockDisposable is null)
		{
			return;
		}

		var memSize = (uint)PInvoke.GlobalSize(handle);
		if (memSize <= Marshal.SizeOf<BITMAPINFOHEADER>())
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.GlobalSize)} returned {memSize}: {Win32Helper.GetErrorMessage()}");
			return;
		}

		package.SetBitmap(RandomAccessStreamReference.CreateFromStream(new MemoryStream(ConvertDibToBmp(dib, memSize)).AsRandomAccessStream()));
	}
	internal static unsafe List<string>? GetFileDropPaths(HGLOBAL handle)
	{
		var allocationSize = (ulong)PInvoke.GlobalSize(handle);
		if (allocationSize < DropFilesHeaderSize || allocationSize > MaxClipboardFormatBytes)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"The HDROP allocation size {allocationSize} is invalid.");
			return null;
		}

		using var lockDisposable = Win32Helper.GlobalLock(handle, out var firstByte);
		if (lockDisposable is null)
		{
			return null;
		}

		var hDrop = new HDROP((IntPtr)firstByte);

		var filesDropped = PInvoke.DragQueryFile(hDrop, 0xFFFFFFFF, new PWSTR(), 0);
		if (filesDropped == 0)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.DragQueryFile)} failed when querying total count: {Win32Helper.GetErrorMessage()}");
			return null;
		}

		if (filesDropped > MaxFileDropItems)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"HDROP contains more than {MaxFileDropItems} items.");
			return null;
		}

		var paths = new List<string>((int)filesDropped);
		uint totalCharacters = 0;
		for (uint i = 0; i < filesDropped; i++)
		{
			var charLength = PInvoke.DragQueryFile(hDrop, i, new PWSTR(), 0);
			if (charLength == 0)
			{
				typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.DragQueryFile)} failed when querying buffer length: {Win32Helper.GetErrorMessage()}");
				continue;
			}
			if (charLength > MaxFileDropPathCharacters || totalCharacters > MaxFileDropTotalCharacters - charLength)
			{
				typeof(Win32ClipboardExtension).LogError()?.Error("HDROP exceeds the file-path character budget.");
				return null;
			}
			totalCharacters += charLength;

			var bufferLength = charLength + 1; // + 1 for \0
			var buffer = Marshal.AllocHGlobal((IntPtr)(bufferLength * Unsafe.SizeOf<char>()));
			using var bufferDisposable = new DisposableStruct<IntPtr>(Marshal.FreeHGlobal, buffer);
			var charsWritten = PInvoke.DragQueryFile(hDrop, i, new PWSTR((char*)buffer), bufferLength);
			if (charsWritten == 0)
			{
				typeof(Win32ClipboardExtension).LogError()?.Error($"{nameof(PInvoke.DragQueryFile)} failed when querying file path: {Win32Helper.GetErrorMessage()}");
				return null;
			}
			paths.Add(Marshal.PtrToStringUni(buffer, (int)charsWritten));
		}

		return paths;
	}

	internal static List<IStorageItem>? GetFileDropList(HGLOBAL handle)
	{
		var paths = GetFileDropPaths(handle);
		if (paths is null)
		{
			return null;
		}

		var files = new List<IStorageItem>(paths.Count);
		var invalidPathCount = 0;
		foreach (var filePath in paths)
		{
			if (Directory.Exists(filePath))
			{
				files.Add(new StorageFolder(filePath));
			}
			else if (File.Exists(filePath))
			{
				files.Add(StorageFile.GetFileFromPath(filePath));
			}
			else
			{
				invalidPathCount++;
			}
		}

		if (invalidPathCount > 0)
		{
			typeof(Win32ClipboardExtension).LogError()?.Error($"HDROP contained {invalidPathCount} invalid file or directory paths.");
		}

		return files;
	}
}
