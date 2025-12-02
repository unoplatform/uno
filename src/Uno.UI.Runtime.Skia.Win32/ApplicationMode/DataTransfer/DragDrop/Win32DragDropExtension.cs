using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.Com;
using Windows.Win32.System.Ole;
using Windows.Win32.System.SystemServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.UI.NativeElementHosting;
using IDataObject = Windows.Win32.System.Com.IDataObject;
using Microsoft.UI.Input;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32DragDropExtension : IDragDropExtension, IDropTarget.Interface, IDropSource.Interface
{
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

	unsafe HRESULT IDropTarget.Interface.DragEnter(IDataObject* dataObject, MODIFIERKEYS_FLAGS keyState, POINTL nativePoint, DROPEFFECT* dropEffect)
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

		GetDragPoint(nativePoint, out var nativeDragPoint, out var scaledDragPoint);
		var src = new DragEventSource(scaledDragPoint, keyState);

		var formats = new Span<FORMATETC>(formatBuffer, (int)fetchedFormatCount);
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			var log = $"{nameof(IDropTarget.Interface.DragEnter)} @ {nativeDragPoint}, formats: ";
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
		var info = new CoreDragInfo(src, package.GetView(), (DataPackageOperation)(*dropEffect), dragUI);
		_coreDragDropManager.DragStarted(info);

		*dropEffect = (DROPEFFECT)_manager.ProcessMoved(src);

		return HRESULT.S_OK;
	}

	unsafe HRESULT IDropTarget.Interface.DragOver(MODIFIERKEYS_FLAGS keyState, POINTL nativePoint, DROPEFFECT* dropEffect)
	{
		GetDragPoint(nativePoint, out var nativeDragPoint, out var scaledDragPoint);
		var src = new DragEventSource(scaledDragPoint, keyState);

		this.LogTrace()?.Trace($"{nameof(IDropTarget.Interface.DragOver)} @ {nativeDragPoint}");

		*dropEffect = (DROPEFFECT)_manager.ProcessMoved(src);

		return HRESULT.S_OK;
	}

	HRESULT IDropTarget.Interface.DragLeave()
	{
		this.LogTrace()?.Trace($"{nameof(IDropTarget.Interface.DragLeave)}");

		_manager.ProcessAborted(_fakePointerId);

		return HRESULT.S_OK;
	}

	unsafe HRESULT IDropTarget.Interface.Drop(IDataObject* dataObject, MODIFIERKEYS_FLAGS keyState, POINTL nativePoint, DROPEFFECT* dropEffect)
	{
		GetDragPoint(nativePoint, out var nativeDragPoint, out var scaledDragPoint);
		var src = new DragEventSource(scaledDragPoint, keyState);

		this.LogTrace()?.Trace($"{nameof(IDropTarget.Interface.Drop)} @ {nativeDragPoint}");

		*dropEffect = (DROPEFFECT)_manager.ProcessReleased(src);

		return HRESULT.S_OK;
	}

	public unsafe void StartNativeDrag(CoreDragInfo info, Action<DataPackageOperation> action)
	{
		// Create IDataObject wrapper from DataPackage
		var dataObjectWrapper = CreateDataObjectFromPackage(info.Data);
		if (dataObjectWrapper.Value == null)
		{
			this.LogError()?.Error($"{nameof(StartNativeDrag)}: Failed to create IDataObject from DataPackage");
			action(DataPackageOperation.None);
			return;
		}

		using (dataObjectWrapper)
		{
			// Create IDropSource implementation
			using var dropSource = ComHelpers.TryGetComScope<IDropSource>(this, out HRESULT hResult);
			if (hResult.Failed)
			{
				this.LogError()?.Error($"{nameof(ComHelpers.TryGetComScope)}<{nameof(IDropSource)}> failed: {Win32Helper.GetErrorMessage(hResult)}");
				action(DataPackageOperation.None);
				return;
			}

			// Convert allowed operations to DROPEFFECT
			var allowedEffects = ConvertOperationToDropEffect(info.AllowedOperations);

			// Perform the drag operation using Windows.Win32.UI.Shell
			DROPEFFECT resultEffect;
			hResult = Windows.Win32.PInvoke.DoDragDrop(
				(IDataObject*)dataObjectWrapper.Value,
				(Windows.Win32.System.Ole.IDropSource*)dropSource.Value,
				allowedEffects,
				&resultEffect);

			if (hResult.Failed && hResult != (HRESULT)DRAGDROP_S_DROP && hResult != (HRESULT)DRAGDROP_S_CANCEL)
			{
				this.LogError()?.Error($"DoDragDrop failed: {Win32Helper.GetErrorMessage(hResult)}");
				action(DataPackageOperation.None);
				return;
			}

			// Convert result back to DataPackageOperation
			var resultOperation = ConvertDropEffectToOperation(resultEffect);
			action(resultOperation);
		}
	}

	// IDropSource implementation
	unsafe HRESULT IDropSource.Interface.QueryContinueDrag(BOOL fEscapePressed, MODIFIERKEYS_FLAGS grfKeyState)
	{
		// Cancel if escape was pressed
		if (fEscapePressed)
		{
			return (HRESULT)DRAGDROP_S_CANCEL;
		}

		// Count pressed mouse buttons
		int pressedButtons = 0;
		if ((grfKeyState & MODIFIERKEYS_FLAGS.MK_LBUTTON) != 0) pressedButtons++;
		if ((grfKeyState & MODIFIERKEYS_FLAGS.MK_MBUTTON) != 0) pressedButtons++;
		if ((grfKeyState & MODIFIERKEYS_FLAGS.MK_RBUTTON) != 0) pressedButtons++;

		// Cancel if multiple buttons pressed
		if (pressedButtons >= 2)
		{
			return (HRESULT)DRAGDROP_S_CANCEL;
		}

		// Drop if no buttons pressed
		if (pressedButtons == 0)
		{
			return (HRESULT)DRAGDROP_S_DROP;
		}

		return HRESULT.S_OK;
	}

	unsafe HRESULT IDropSource.Interface.GiveFeedback(DROPEFFECT dwEffect)
	{
		// Use default system cursors
		return (HRESULT)DRAGDROP_S_USEDEFAULTCURSORS;
	}

	private static DROPEFFECT ConvertOperationToDropEffect(DataPackageOperation operation)
	{
		var effect = DROPEFFECT.DROPEFFECT_NONE;
		if ((operation & DataPackageOperation.Copy) != 0)
			effect |= DROPEFFECT.DROPEFFECT_COPY;
		if ((operation & DataPackageOperation.Move) != 0)
			effect |= DROPEFFECT.DROPEFFECT_MOVE;
		if ((operation & DataPackageOperation.Link) != 0)
			effect |= DROPEFFECT.DROPEFFECT_LINK;
		return effect;
	}

	private static DataPackageOperation ConvertDropEffectToOperation(DROPEFFECT effect)
	{
		var operation = DataPackageOperation.None;
		if ((effect & DROPEFFECT.DROPEFFECT_COPY) != 0)
			operation |= DataPackageOperation.Copy;
		if ((effect & DROPEFFECT.DROPEFFECT_MOVE) != 0)
			operation |= DataPackageOperation.Move;
		if ((effect & DROPEFFECT.DROPEFFECT_LINK) != 0)
			operation |= DataPackageOperation.Link;
		return operation;
	}

	private unsafe ComScope<IDataObject> CreateDataObjectFromPackage(DataPackageView data)
	{
		// Create a COM-visible data object implementation
		var dataObject = new DataPackageDataObject(data);
		var comScope = ComHelpers.TryGetComScope<IDataObject>(dataObject, out HRESULT hResult);

		if (hResult.Failed)
		{
			this.LogError()?.Error($"{nameof(ComHelpers.TryGetComScope)}<{nameof(IDataObject)}> failed: {Win32Helper.GetErrorMessage(hResult)}");
			return default;
		}

		return comScope;
	}

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

	private void GetDragPoint(POINTL pt, out System.Drawing.Point nativePosition, out Point scaledPosition)
	{
		nativePosition = new System.Drawing.Point(pt.x, pt.y);
		var success = PInvoke.ScreenToClient(_hwnd, ref nativePosition);
		if (!success)
		{
			this.LogError()?.Error($"{nameof(PInvoke.ScreenToClient)} failed: {Win32Helper.GetErrorMessage()}");
		}

		scaledPosition = GetScaledPosition(nativePosition.X, nativePosition.Y);
	}

	// Constants for IDropSource
	private const int DRAGDROP_S_DROP = 0x00040100;
	private const int DRAGDROP_S_CANCEL = 0x00040101;
	private const int DRAGDROP_S_USEDEFAULTCURSORS = 0x00040102;
}
