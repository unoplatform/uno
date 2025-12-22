using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Foundation;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Ole;
using Windows.Win32.System.SystemServices;
using Windows.Win32.UI.Shell;
using Uno.Disposables;
using Uno.Foundation.Logging;
using DATADIR = Windows.Win32.System.Com.DATADIR;
using FORMATETC = Windows.Win32.System.Com.FORMATETC;
using IDataObject = Windows.Win32.System.Com.IDataObject;
using IEnumFORMATETC = Windows.Win32.System.Com.IEnumFORMATETC;
using IStream = Windows.Win32.System.Com.IStream;
using STGMEDIUM = Windows.Win32.System.Com.STGMEDIUM;
using TYMED = Windows.Win32.System.Com.TYMED;

namespace Uno.UI.Runtime.Skia.Win32;

// IDropTarget implementation for handling drag-and-drop operations
internal partial class Win32DragDropExtension
{
	unsafe HRESULT IDropTarget.Interface.DragEnter(IDataObject* dataObject, MODIFIERKEYS_FLAGS grfKeyState, POINTL pt, DROPEFFECT* pdwEffect)
	{
		Debug.Assert(_manager is not null && _coreDragDropManager is not null);

		var position = new System.Drawing.Point(pt.x, pt.y);

		var success = PInvoke.ScreenToClient(_hwnd, ref position);
		if (!success)
		{
			this.LogError()?.Error($"{nameof(PInvoke.ScreenToClient)} failed: {Win32Helper.GetErrorMessage()}");
		}
		var scaledPosition = GetScaledPosition(position.X, position.Y);

		var src = new DragEventSource(scaledPosition, grfKeyState);

		var formats = EnumerateFormats(dataObject);
		if (formats is null)
		{
			return HRESULT.E_UNEXPECTED;
		}

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			var log = $"{nameof(IDropTarget.Interface.DragEnter)} @ {position}, formats: ";
			foreach (var format in formats)
			{
				log += $"'{GetClipboardFormatDisplayName(format.cfFormat)}',";
			}
			this.Log().Trace(log[..^1]);
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
					typeof(Win32DragDropExtension).LogWarn()?.Warn(
						$"{nameof(IDropTarget.Interface.DragEnter)} found {Enum.GetName((CLIPBOARD_FORMAT)formatetc.cfFormat)}, " +
						$"but {nameof(TYMED)} is not {nameof(TYMED.TYMED_HGLOBAL)} and is not yet supported.");
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

		TryhandleFileDescriptor(dataObject, formatEtcList, package);
		var handled = TryhandleAsyncHDrop(dataObject, formatEtcList, package);
		Win32ClipboardExtension.ReadContentIntoPackage(package, formatList, format =>
		{
			var formatEtc = formatEtcList.First(f => f.cfFormat == (int)format);
			if (formatEtc.cfFormat == (int)CLIPBOARD_FORMAT.CF_HDROP && handled)
			{
				return null;
			}
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
		if (!success)
		{
			this.LogError()?.Error($"{nameof(PInvoke.ScreenToClient)} failed: {Win32Helper.GetErrorMessage()}");
		}
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
		_lastAsyncHDropHandler?.Leave();
		_lastAsyncHDropHandler = null;

		return HRESULT.S_OK;
	}

	unsafe HRESULT IDropTarget.Interface.Drop(IDataObject* dataObject, MODIFIERKEYS_FLAGS grfKeyState, POINTL pt, DROPEFFECT* pdwEffect)
	{
		var position = new System.Drawing.Point(pt.x, pt.y);
		var success = PInvoke.ScreenToClient(_hwnd, ref position);
		if (!success)
		{
			this.LogError()?.Error($"{nameof(PInvoke.ScreenToClient)} failed: {Win32Helper.GetErrorMessage()}");
		}
		var scaledPosition = GetScaledPosition(position.X, position.Y);
		var src = new DragEventSource(scaledPosition, grfKeyState);

		this.LogTrace()?.Trace($"{nameof(IDropTarget.Interface.Drop)} @ {position}");

		_lastAsyncHDropHandler?.Drop(dataObject);
		*pdwEffect = (DROPEFFECT)_manager.ProcessReleased(src);
		if (_lastAsyncHDropHandler != null)
		{
			_lastAsyncHDropHandler.DropEffect = *pdwEffect;
			_lastAsyncHDropHandler = null;
		}

		return HRESULT.S_OK;
	}

	private Point GetScaledPosition(float x, float y)
	{
		var xamlRoot = _manager.ContentRoot.GetOrCreateXamlRoot();
		return new Point(x / xamlRoot.RasterizationScale, y / xamlRoot.RasterizationScale);
	}

	private static unsafe string GetClipboardFormatDisplayName(ushort formatId)
	{
		if (Enum.IsDefined(typeof(CLIPBOARD_FORMAT), (CLIPBOARD_FORMAT)formatId))
		{
			return Enum.GetName(typeof(CLIPBOARD_FORMAT), (CLIPBOARD_FORMAT)formatId)!;
		}

		Span<char> buffer = stackalloc char[256];
		fixed (char* bufferPtr = buffer)
		{
			var length = PInvoke.GetClipboardFormatName(formatId, bufferPtr, buffer.Length);
			if (length > 0)
			{
				return new string(buffer[..length]);
			}
		}

		return $"0x{formatId:X4}";
	}

	private unsafe FORMATETC[]? EnumerateFormats(IDataObject* dataObject)
	{
		IEnumFORMATETC* enumFormatEtc;
		var hResult = dataObject->EnumFormatEtc((uint)DATADIR.DATADIR_GET, &enumFormatEtc);
		if (hResult.Failed)
		{
			this.LogError()?.Error($"{nameof(IDataObject.EnumFormatEtc)} failed: {Win32Helper.GetErrorMessage(hResult)}");
			return null;
		}

		using var enumFormatDisposable = new DisposableStruct<IntPtr>(static p => ((IEnumFORMATETC*)p)->Release(), (IntPtr)enumFormatEtc);

		enumFormatEtc->Reset();
		const int formatBufferLength = 100;
		var formatBuffer = stackalloc FORMATETC[formatBufferLength];
		uint fetchedFormatCount;
		hResult = enumFormatEtc->Next(formatBufferLength, formatBuffer, &fetchedFormatCount);
		if (hResult.Failed)
		{
			this.LogError()?.Error($"{nameof(IEnumFORMATETC.Next)} failed: {Win32Helper.GetErrorMessage(hResult)}");
			return null;
		}

		var formats = new Span<FORMATETC>(formatBuffer, (int)fetchedFormatCount);
		return formats.ToArray();
	}

	private unsafe bool TryhandleAsyncHDrop(IDataObject* dataObject, FORMATETC[] formatEtcList, DataPackage package)
	{
		var formatEtcNullable = formatEtcList.Cast<FORMATETC?>().FirstOrDefault(f => f!.Value.cfFormat == (int)CLIPBOARD_FORMAT.CF_HDROP, null);
		if (formatEtcNullable is null)
		{
			return false;
		}
		var formatEtc = formatEtcNullable.Value;

		using ComScope<IDataObjectAsyncCapability> asyncCapabilityScope = new(null);
		HRESULT queryInterfaceHResult;
		fixed (Guid* guidPtr = &_asyncCapabilityGuid)
		{
			queryInterfaceHResult = dataObject->QueryInterface(guidPtr, asyncCapabilityScope);
		}
		if (!queryInterfaceHResult.Succeeded || !asyncCapabilityScope.Value->GetAsyncMode(out var isAsync).Succeeded || !isAsync)
		{
			return false;
		}

		var asyncHDropHandler = new AsyncHDropHandler(formatEtc);
		_lastAsyncHDropHandler = asyncHDropHandler;
		package.SetDataProvider(StandardDataFormats.StorageItems, ct => DelayRenderer(ct, asyncHDropHandler));
		return true;
	}

	private async Task<object> DelayRenderer(CancellationToken ct, AsyncHDropHandler asyncHDropHandler) => await asyncHDropHandler.Task;

	private unsafe void TryhandleFileDescriptor(IDataObject* dataObject, FORMATETC[] formatEtcList, DataPackage package)
	{
		var formatEtcNullable = formatEtcList.Cast<FORMATETC?>().FirstOrDefault(f => f!.Value.cfFormat == CFSTR_FILEDESCRIPTOR, null);
		if (formatEtcNullable is null || formatEtcNullable.Value.tymed != (uint)TYMED.TYMED_HGLOBAL)
		{
			return;
		}
		var formatEtc = formatEtcNullable.Value;

		var getDataHResult = dataObject->GetData(formatEtc, out STGMEDIUM medium);
		if (getDataHResult.Failed)
		{
			this.LogError()?.Error($"{nameof(IDataObject)}.{nameof(IDataObject.GetData)} failed to get {nameof(CFSTR_FILEDESCRIPTOR)} data: {Win32Helper.GetErrorMessage(getDataHResult)}");
			return;
		}
		using var mediumDisposable = new DisposableStruct<STGMEDIUM>(static medium => PInvoke.ReleaseStgMedium(&medium), medium);

		using var lockDisposable = Win32Helper.GlobalLock(medium.u.hGlobal, out var firstByte);
		if (lockDisposable is null)
		{
			this.LogError()?.Error($"Failed to lock {nameof(HGLOBAL)} contents when reading {nameof(CFSTR_FILEDESCRIPTOR)} data.");
			return;
		}

		var fileGroupDescriptor = (FILEGROUPDESCRIPTORW*)firstByte;
		var fileCount = fileGroupDescriptor->cItems;

		if (fileCount == 0)
		{
			return;
		}

		var fileDescriptors = new ReadOnlySpan<FILEDESCRIPTORW>((byte*)firstByte + sizeof(uint), (int)fileCount);

		// Now retrieve the file contents for each file descriptor
		var storageItems = new List<IStorageItem>();

		for (int i = 0; i < fileCount; i++)
		{
			ref readonly var fileDescriptor = ref fileDescriptors[i];

			// Get the file name from the descriptor using fixed statement
			string fileName;
			fixed (char* pFileName = fileDescriptor.cFileName)
			{
				fileName = Marshal.PtrToStringAuto((IntPtr)pFileName) ?? string.Empty;
			}

			if (string.IsNullOrEmpty(fileName))
			{
				this.LogWarn()?.Warn($"File descriptor at index {i} has no file name, skipping.");
				continue;
			}

			var fileContentsFormatEtc = new FORMATETC
			{
				cfFormat = (ushort)CFSTR_FILECONTENTS,
				ptd = null,
				dwAspect = (uint)DVASPECT.DVASPECT_CONTENT,
				lindex = i, // Index of the file in the file group descriptor
				tymed = (uint)TYMED.TYMED_ISTREAM | (uint)TYMED.TYMED_HGLOBAL
			};

			var getFileContentResult = dataObject->GetData(fileContentsFormatEtc, out STGMEDIUM fileContentMedium);
			if (getFileContentResult.Failed)
			{
				this.LogError()?.Error($"Failed to get file contents for '{fileName}' at index {i}: {Win32Helper.GetErrorMessage(getFileContentResult)}");
				continue;
			}

			using var fileContentMediumDisposable = new DisposableStruct<STGMEDIUM>(static medium => PInvoke.ReleaseStgMedium(&medium), fileContentMedium);

			var tempDir = Path.Combine(Path.GetTempPath(), "unoplatform-dragdrop");
			Directory.CreateDirectory(tempDir);
			var tempPath = Path.Combine(tempDir, fileName);
			using var fileStream = File.Create(tempPath);

			storageItems.Add((fileDescriptor.dwFileAttributes & (uint)System.IO.FileAttributes.Directory) != 0
				? new StorageFolder(tempPath)
				: StorageFile.GetFileFromPath(tempPath));

			if (((uint)fileContentMedium.tymed & (uint)TYMED.TYMED_ISTREAM) != 0 && fileContentMedium.u.pstm != null)
			{
				ReadFromIStream(fileContentMedium.u.pstm, fileStream);
			}
			else if (((uint)fileContentMedium.tymed & (uint)TYMED.TYMED_HGLOBAL) != 0 && fileContentMedium.u.hGlobal.Value != (void*)IntPtr.Zero)
			{
				ReadFromHGlobal(fileContentMedium.u.hGlobal, fileDescriptor, fileStream);
			}
			else
			{
				this.LogError()?.Error($"Unsupported storage medium type {fileContentMedium.tymed} for file '{fileName}'");
			}
		}

		if (storageItems.Count > 0)
		{
			package.SetStorageItems(storageItems, readOnly: true);
		}
	}

	private static unsafe void ReadFromIStream(IStream* pStream, FileStream fileStream)
	{
		const int bufferSize = 8192;
		byte* buffer = stackalloc byte[bufferSize];
		while (true)
		{
			uint bytesRead = 0;
			var readResult = pStream->Read(buffer, bufferSize, &bytesRead);
			if (readResult.Failed || bytesRead == 0)
			{
				break;
			}
			fileStream.Write(new ReadOnlySpan<byte>(buffer, (int)bytesRead));
		}
	}

	private unsafe void ReadFromHGlobal(HGLOBAL hGlobal, FILEDESCRIPTORW fileDescriptor, FileStream fileStream)
	{
		using var lockDisposable = Win32Helper.GlobalLock(hGlobal, out var dataPtr);
		if (lockDisposable is null)
		{
			this.LogError()?.Error($"Failed to lock HGLOBAL for file '{fileStream.Name}'");
			return;
		}

		long fileSize = ((long)fileDescriptor.nFileSizeHigh << 32) | fileDescriptor.nFileSizeLow;

		if (fileSize <= 0)
		{
			var globalSize = (long)PInvoke.GlobalSize(hGlobal);
			if (globalSize > 0)
			{
				fileSize = globalSize;
			}
		}

		if (fileSize <= 0)
		{
			this.LogWarn()?.Warn($"File size is 0 or unknown for '{fileStream.Name}'");
			return;
		}

		fileStream.Write(new ReadOnlySpan<byte>(dataPtr, (int)fileSize));
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct FILEGROUPDESCRIPTORW
	{
		public uint cItems;
	}

	[StructLayout(LayoutKind.Sequential)]
	private unsafe struct FILEDESCRIPTORW
	{
		public uint dwFlags;
		public Guid clsid;
		public int /* SIZE */ cx;
		public int /* SIZE */ cy;
		public int /* POINTL */ x;
		public int /* POINTL */ y;
		public uint dwFileAttributes;
		public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
		public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
		public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
		public uint nFileSizeHigh;
		public uint nFileSizeLow;
		public fixed char cFileName[260]; // MAX_PATH
	}
}
