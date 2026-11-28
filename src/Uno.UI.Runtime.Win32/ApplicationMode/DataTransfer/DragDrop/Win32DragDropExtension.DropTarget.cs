using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
using Windows.Win32.Security;
using Windows.Win32.System.Com;
using Windows.Win32.System.Com.StructuredStorage;
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
using IStorage = Windows.Win32.System.Com.StructuredStorage.IStorage;
using STGMEDIUM = Windows.Win32.System.Com.STGMEDIUM;
using TYMED = Windows.Win32.System.Com.TYMED;

namespace Uno.UI.Runtime.Win32;

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

		_lastVirtualFilesHandler?.Cancel();
		_lastVirtualFilesHandler = null;

		// Real files come with CF_HDROP, and the app gets their original paths, as in WinUI.
		// The shell offers FileGroupDescriptorW and FileContents for them too, which would copy them.
		if (!formatEtcList.Any(static f => f.cfFormat == (int)CLIPBOARD_FORMAT.CF_HDROP))
		{
			TryHandleFileDescriptor(dataObject, formatEtcList, package);
		}
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

		// DROPEFFECT and DataPackageOperation have the same binary representation.
		// No DragUI: the shell draws the source's own drag image, as in WinUI.
		var info = new CoreDragInfo(src, package.GetView(), (DataPackageOperation)(*pdwEffect));
		_coreDragDropManager.DragStarted(info);

		*pdwEffect = (DROPEFFECT)_manager.ProcessMoved(src);
		DropTargetHelperDragEnter(dataObject, pt, *pdwEffect);

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
		DropTargetHelperDragOver(pt, *pdwEffect);

		return HRESULT.S_OK;
	}

	HRESULT IDropTarget.Interface.DragLeave()
	{
		this.LogTrace()?.Trace($"{nameof(IDropTarget.Interface.DragLeave)}");

		// As in Drop, end the shell's drag image first.
		DropTargetHelperDragLeave();

		_manager.ProcessAborted(_fakePointerId);
		_lastAsyncHDropHandler?.Leave();
		_lastAsyncHDropHandler = null;
		_lastVirtualFilesHandler?.Cancel();
		_lastVirtualFilesHandler = null;

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

		// End the shell's drag image first, so the work below (up to a second for an async file drop) can't freeze it or, by throwing, leak it.
		DropTargetHelperDrop(dataObject, pt, *pdwEffect);

		_lastAsyncHDropHandler?.Drop(dataObject);
		*pdwEffect = (DROPEFFECT)_manager.ProcessReleased(src);
		if (_lastAsyncHDropHandler != null)
		{
			_lastAsyncHDropHandler.DropEffect = *pdwEffect;
			_lastAsyncHDropHandler = null;
		}

		// The app's Drop handler runs after this returns, and so after DoDragDrop returns in the source,
		// which may then delete what it served (a move out of a zip folder), so the files are copied now.
		if (_lastVirtualFilesHandler is { } virtualFilesHandler)
		{
			_lastVirtualFilesHandler = null;
			if (*pdwEffect == DROPEFFECT.DROPEFFECT_NONE)
			{
				virtualFilesHandler.Cancel();
			}
			else
			{
				virtualFilesHandler.Drop(this, dataObject);
			}
		}

		return HRESULT.S_OK;
	}

	// WinUI shows the source's own image for an external drag. The shell's drag-image manager (IDropTargetHelper) draws it
	// when the drop target forwards its calls to it.
	private unsafe void DropTargetHelperDragEnter(IDataObject* dataObject, POINTL pt, DROPEFFECT effect)
	{
		DropTargetHelperDragLeave(); // A DragEnter without a DragLeave or a Drop ends the previous drag.
		if (_isDropTargetHelperUnavailable)
		{
			return;
		}

		var clsid = CLSID.DragDropHelper;
		var iid = IDropTargetHelper.IID_Guid;
		IDropTargetHelper* helper;
		var hResult = PInvoke.CoCreateInstance(&clsid, null, CLSCTX.CLSCTX_INPROC_SERVER, &iid, (void**)&helper);
		if (hResult.Failed)
		{
			_isDropTargetHelperUnavailable = true;
			this.LogWarn()?.Warn(
				$"{nameof(PInvoke.CoCreateInstance)} failed for the shell's drag-image manager, so external drags show no drag image: " +
				Win32Helper.GetErrorMessage(hResult));
			return;
		}

		_dropTargetHelper = helper;
		// Kept to enter the manager again mid-drag (see UpdateDropTargetHelper).
		dataObject->AddRef();
		_dropTargetHelperDataObject = dataObject;
		UpdateDropTargetHelper(pt, effect);
	}

	private unsafe void DropTargetHelperDragOver(POINTL pt, DROPEFFECT effect)
	{
		if (_dropTargetHelper is not null)
		{
			UpdateDropTargetHelper(pt, effect);
		}
	}

	private unsafe void DropTargetHelperDragLeave()
	{
		if (_isDropTargetHelperEntered)
		{
			_dropTargetHelper->DragLeave();
		}
		ReleaseDropTargetHelper();
	}

	private unsafe void DropTargetHelperDrop(IDataObject* dataObject, POINTL pt, DROPEFFECT effect)
	{
		if (_isDropTargetHelperEntered)
		{
			var point = new System.Drawing.Point(pt.x, pt.y);
			_dropTargetHelper->Drop(dataObject, &point, effect);
		}
		ReleaseDropTargetHelper();
	}

	// As in WinUI, a DragUIOverride content (drawn by DragView) or IsContentVisible = false replaces the source's image.
	// Meanwhile the manager is told the drag left, which hides the image, and is entered again once the override goes away.
	private unsafe void UpdateDropTargetHelper(POINTL pt, DROPEFFECT effect)
	{
		Debug.Assert(_dropTargetHelper is not null && _dropTargetHelperDataObject is not null);
		var point = new System.Drawing.Point(pt.x, pt.y);
		if (_manager.IsContentReplacedOrHidden(_fakePointerId))
		{
			if (_isDropTargetHelperEntered)
			{
				_dropTargetHelper->DragLeave();
				_isDropTargetHelperEntered = false;
			}
		}
		else if (_isDropTargetHelperEntered)
		{
			_dropTargetHelper->DragOver(&point, effect);
		}
		else
		{
			var hResult = _dropTargetHelper->DragEnter(_hwnd, _dropTargetHelperDataObject, &point, effect);
			if (hResult.Failed)
			{
				// It fails when the source set no drag image, but keeps the data object until DragLeave or Drop, so those are still forwarded.
				this.LogTrace()?.Trace($"IDropTargetHelper::DragEnter failed: {Win32Helper.GetErrorMessage(hResult)}");
			}
			_isDropTargetHelperEntered = true;
		}
	}

	private unsafe void ReleaseDropTargetHelper()
	{
		_isDropTargetHelperEntered = false;
		if (_dropTargetHelperDataObject is not null)
		{
			_dropTargetHelperDataObject->Release();
			_dropTargetHelperDataObject = null;
		}
		if (_dropTargetHelper is not null)
		{
			_dropTargetHelper->Release();
			_dropTargetHelper = null;
		}
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

	private unsafe void TryHandleFileDescriptor(IDataObject* dataObject, FORMATETC[] formatEtcList, DataPackage package)
	{
		var formatEtcNullable = formatEtcList.Cast<FORMATETC?>().FirstOrDefault(f => f!.Value.cfFormat == CFSTR_FILEDESCRIPTOR, null);
		if (formatEtcNullable is null || formatEtcNullable.Value.tymed != (uint)TYMED.TYMED_HGLOBAL)
		{
			return;
		}

		var virtualFilesHandler = new VirtualFilesHandler(formatEtcNullable.Value);
		_lastVirtualFilesHandler = virtualFilesHandler;
		package.SetDataProvider(StandardDataFormats.StorageItems, virtualFilesHandler.GetStorageItemsAsync);
	}

	private unsafe List<IStorageItem> ReadVirtualFiles(IDataObject* dataObject, FORMATETC formatEtc)
	{
		var storageItems = new List<IStorageItem>();

		var getDataHResult = dataObject->GetData(formatEtc, out STGMEDIUM medium);
		if (getDataHResult.Failed)
		{
			throw new InvalidOperationException($"{nameof(IDataObject)}.{nameof(IDataObject.GetData)} failed to get {nameof(CFSTR_FILEDESCRIPTOR)} data: {Win32Helper.GetErrorMessage(getDataHResult)}");
		}
		using var mediumDisposable = new DisposableStruct<STGMEDIUM>(static medium => PInvoke.ReleaseStgMedium(&medium), medium);

		using var lockDisposable = Win32Helper.GlobalLock(medium.u.hGlobal, out var firstByte);
		if (lockDisposable is null)
		{
			throw new InvalidOperationException($"Failed to lock {nameof(HGLOBAL)} contents when reading {nameof(CFSTR_FILEDESCRIPTOR)} data.");
		}

		var fileGroupDescriptor = (FILEGROUPDESCRIPTORW*)firstByte;
		var fileCount = fileGroupDescriptor->cItems;

		if (fileCount == 0)
		{
			return storageItems;
		}

		var fileDescriptors = new ReadOnlySpan<FILEDESCRIPTORW>((byte*)firstByte + sizeof(uint), (int)fileCount);
		var dropDirectory = CreateVirtualFileDirectory();

		for (int i = 0; i < fileCount; i++)
		{
			try
			{
				ref readonly var fileDescriptor = ref fileDescriptors[i];
				var isDirectory = (fileDescriptor.dwFileAttributes & (uint)System.IO.FileAttributes.Directory) != 0;

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

				// Each item gets its own folder, so items with the same name don't overwrite each other.
				var tempDir = Path.Combine(dropDirectory, i.ToString(CultureInfo.InvariantCulture));
				var tempPath = Path.GetFullPath(Path.Combine(tempDir, fileName));
				if (!tempPath.StartsWith(tempDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
				{
					this.LogWarn()?.Warn($"File descriptor at index {i} has a name outside of its folder, skipping.");
					continue;
				}

				if (isDirectory)
				{
					Directory.CreateDirectory(tempPath);
					storageItems.Add(new StorageFolder(tempPath));
				}
				else
				{
					Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

					var fileContentsFormatEtc = new FORMATETC
					{
						cfFormat = (ushort)CFSTR_FILECONTENTS,
						ptd = null,
						dwAspect = (uint)DVASPECT.DVASPECT_CONTENT,
						lindex = i, // Index of the file in the file group descriptor
						tymed = (uint)TYMED.TYMED_ISTREAM | (uint)TYMED.TYMED_HGLOBAL | (uint)TYMED.TYMED_ISTORAGE
					};

					var getFileContentResult = dataObject->GetData(fileContentsFormatEtc, out STGMEDIUM fileContentMedium);
					if (getFileContentResult.Failed)
					{
						this.LogError()?.Error($"Failed to get file contents for '{fileName}' at index {i}: {Win32Helper.GetErrorMessage(getFileContentResult)}");
						continue;
					}

					using var fileContentMediumDisposable = new DisposableStruct<STGMEDIUM>(static medium => PInvoke.ReleaseStgMedium(&medium), fileContentMedium);

					if (((uint)fileContentMedium.tymed & (uint)TYMED.TYMED_ISTREAM) != 0 && fileContentMedium.u.pstm != null)
					{
						using var fileStream = File.Create(tempPath);
						ReadFromIStream(fileContentMedium.u.pstm, fileStream);
						storageItems.Add(StorageFile.GetFileFromPath(tempPath));
					}
					else if (((uint)fileContentMedium.tymed & (uint)TYMED.TYMED_HGLOBAL) != 0 && fileContentMedium.u.hGlobal.Value != (void*)IntPtr.Zero)
					{
						using var fileStream = File.Create(tempPath);
						if (ReadFromHGlobal(fileContentMedium.u.hGlobal, fileDescriptor, fileStream))
						{
							storageItems.Add(StorageFile.GetFileFromPath(tempPath));
						}
					}
					else if (((uint)fileContentMedium.tymed & (uint)TYMED.TYMED_ISTORAGE) != 0 && fileContentMedium.u.pstg != null)
					{
						if (ReadFromIStorage(fileContentMedium.u.pstg, tempPath))
						{
							storageItems.Add(StorageFile.GetFileFromPath(tempPath));
						}
					}
					else
					{
						this.LogError()?.Error($"Unsupported storage medium type {fileContentMedium.tymed} for file '{fileName}'");
					}
				}
			}
			catch (Exception e) when (e is not OutOfMemoryException and not StackOverflowException and not AccessViolationException)
			{
				this.LogError()?.Error($"Failed to copy the dragged item at index {i}.", e);
			}
		}

		return storageItems;
	}

	// The app may use the copies at any time while it runs, so they're deleted when it exits.
	private static string CreateVirtualFileDirectory()
	{
		var directory = Path.Combine(Path.GetTempPath(), "unoplatform-dragdrop", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);

		lock (_virtualFileDirectories)
		{
			if (_virtualFileDirectories.Count == 0)
			{
				AppDomain.CurrentDomain.ProcessExit += static (_, _) =>
				{
					lock (_virtualFileDirectories)
					{
						foreach (var path in _virtualFileDirectories)
						{
							try
							{
								Directory.Delete(path, recursive: true);
							}
							catch (Exception e) when (e is IOException or UnauthorizedAccessException)
							{
								// A file the app or another process still has open.
							}
						}
					}
				};
			}
			_virtualFileDirectories.Add(directory);
		}

		return directory;
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

	private unsafe bool ReadFromHGlobal(HGLOBAL hGlobal, FILEDESCRIPTORW fileDescriptor, FileStream fileStream)
	{
		using var lockDisposable = Win32Helper.GlobalLock(hGlobal, out var dataPtr);
		if (lockDisposable is null)
		{
			this.LogError()?.Error($"Failed to lock HGLOBAL for file '{fileStream.Name}'");
			return false;
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
			return false;
		}

		const int chunkSize = 1024 * 1024 * 1024; // 1 GiB
		var remaining = fileSize;
		var currentPtr = (byte*)dataPtr;
		while (remaining > 0)
		{
			var bytesToWrite = remaining > chunkSize ? chunkSize : (int)remaining;
			fileStream.Write(new ReadOnlySpan<byte>(currentPtr, bytesToWrite));
			currentPtr += bytesToWrite;
			remaining -= bytesToWrite;
		}
		return true;
	}

	private unsafe bool ReadFromIStorage(IStorage* pStorage, string filePath)
	{
		using ComScope<IStorage> destStorage = new(null);
		using var filePathUnmanaged = new Win32Helper.NativeNulTerminatedUtf16String(filePath);

		var riid = IStorage.IID_Guid;

		HRESULT hr = PInvoke.StgCreateStorageEx(
			filePathUnmanaged,
			STGM.STGM_CREATE | STGM.STGM_WRITE | STGM.STGM_SHARE_EXCLUSIVE,
			STGFMT.STGFMT_DOCFILE,
			0,
			null,
			new PSECURITY_DESCRIPTOR(),
			&riid,
			destStorage
		);

		if (hr.Failed)
		{
			this.LogError()?.Error($"{nameof(PInvoke.StgCreateStorageEx)} failed for file path {filePath}: {Win32Helper.GetErrorMessage(hr)}");
			return false;
		}

		hr = pStorage->CopyTo(0, null, null, destStorage);
		if (hr.Failed)
		{
			this.LogError()?.Error($"{nameof(IStorage.CopyTo)} failed for file path {filePath}: {Win32Helper.GetErrorMessage(hr)}");
			return false;
		}

		hr = destStorage.Value->Commit((uint)STGC.STGC_DEFAULT);
		if (hr.Failed)
		{
			this.LogError()?.Error($"{nameof(IStorage.Commit)} failed for file path {filePath}: {Win32Helper.GetErrorMessage(hr)}");
			return false;
		}
		return true;
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
		public int cx;
		public int cy;
		public int x;
		public int y;
		public uint dwFileAttributes;
		public FILETIME ftCreationTime;
		public FILETIME ftLastAccessTime;
		public FILETIME ftLastWriteTime;
		public uint nFileSizeHigh;
		public uint nFileSizeLow;
		public fixed char cFileName[260]; // MAX_PATH
	}
}
