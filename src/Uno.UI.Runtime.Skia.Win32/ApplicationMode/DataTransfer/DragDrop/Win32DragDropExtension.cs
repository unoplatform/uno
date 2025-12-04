using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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

	// Store the data object and original FORMATETC temporarily during drag operation
	private unsafe IDataObject* _currentDataObject;
	private FORMATETC? _hdropFormatEtc;

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

		// Store the data object for later use in Drop event
		_currentDataObject = dataObject;

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

		// Log all available formats with their names for debugging
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			var log = $"{nameof(IDropTarget.Interface.DragEnter)} @ {nativeDragPoint}, found {fetchedFormatCount} formats:\n";
			foreach (var format in formats)
			{
				var formatName = GetClipboardFormatName(format.cfFormat);

				// Check if data is actually available
				var queryResult = dataObject->QueryGetData(&format);
				var available = queryResult.Succeeded ? "Available" : $"Not Available ({Win32Helper.GetErrorMessage(queryResult)})";

				log += $"  - Format {format.cfFormat} ({formatName}), tymed: {(TYMED)format.tymed}, dwAspect: {format.dwAspect} - {available}\n";
			}
			this.Log().Debug(log);
		}

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

		// Check if we have virtual files (e.g., classic Outlook attachments)
		// Modern Outlook (Chromium-based) provides CF_HDROP directly, so we handle that in standard file handling
		var hasVirtualFiles = VirtualFileHelper.ContainsVirtualFiles(dataObject);
		if (hasVirtualFiles)
		{
			this.Log().LogInfo("Detected virtual files (classic Outlook attachments)");

			try
			{
				// Extract virtual files synchronously
				var files = VirtualFileHelper.ExtractVirtualFiles(dataObject);
				if (files.Count > 0)
				{
					this.Log().LogInfo($"Successfully extracted {files.Count} virtual file(s)");
					package.SetStorageItems(files);
				}
				else
				{
					this.Log().LogWarning("Virtual files detected but extraction returned 0 files");
				}
			}
			catch (Exception ex)
			{
				this.LogError()?.Error($"Failed to extract virtual files: {ex.Message}", ex);
			}
		}
		else
		{
			this.Log().LogDebug("No virtual files detected, checking for delayed rendering (data might be available in Drop event)");

			// Standard file handling (existing code)
			// Note: For Chromium-based Outlook, CF_HDROP data is not available until Drop event
			var formatEtcList = formats.ToArray();

			// Check if CF_HDROP is available but will use delayed rendering
			var hasCfHdrop = formatEtcList.Any(f => f.cfFormat == (int)CLIPBOARD_FORMAT.CF_HDROP);
			if (hasCfHdrop)
			{
				// Store the exact FORMATETC for CF_HDROP as announced by the source
				_hdropFormatEtc = formatEtcList.First(f => f.cfFormat == (int)CLIPBOARD_FORMAT.CF_HDROP);

				this.Log().LogInfo($"CF_HDROP format detected - tymed: {(TYMED)_hdropFormatEtc.Value.tymed}, dwAspect: {_hdropFormatEtc.Value.dwAspect}, lindex: {_hdropFormatEtc.Value.lindex}");

				// Check if the data source supports asynchronous operations (Chromium-based apps)
				if (AsyncDataHelper.SupportsAsyncOperation(dataObject))
				{
					this.Log().LogInfo("Data source supports asynchronous operations (Chromium/Outlook) - starting async operation");

					// Start the async operation - this tells the source to begin rendering data
					if (AsyncDataHelper.StartAsyncOperation(dataObject))
					{
						this.Log().LogInfo("Async operation started - data will be available in Drop event");
					}
					else
					{
						this.Log().LogWarning("Failed to start async operation - will retry in Drop event");
					}
				}
				else
				{
					this.Log().LogInfo("Will retrieve file data in Drop event (delayed rendering)");
				}

				// Don't try to get the data now, it will be available in Drop
				// Just create an empty package, the real data will be retrieved on drop
			}
			else
			{
				// Process other formats that are immediately available
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

				if (formatList.Count > 0)
				{
					this.Log().LogDebug($"Processing {formatList.Count} standard formats: {string.Join(", ", formatList)}");
				}

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
					var hr = dataObject->GetData(formatEtc, out STGMEDIUM medium);

					if (hr.Failed)
					{
						this.Log().LogError($"GetData failed for format {format} ({GetClipboardFormatName((int)format)}): {Win32Helper.GetErrorMessage(hr)} (0x{hr.Value:X8})");
						return default(HGLOBAL);
					}

					if (medium.tymed == TYMED.TYMED_NULL)
					{
						this.Log().LogWarning($"GetData returned TYMED_NULL for format {format} ({GetClipboardFormatName((int)format)})");
						return default(HGLOBAL);
					}

					this.Log().LogDebug($"GetData succeeded for format {format} ({GetClipboardFormatName((int)format)}), tymed: {medium.tymed}");

					mediumsToDispose.Add(medium);
					return medium.u.hGlobal;
				});
			}
		}

		// Create DragUI for visual feedback during drag operation
		var dragUI = CreateDragUIForExternalDrag(dataObject, formats.ToArray());

		// DROPEFFECT and DataPackageOperation have the same binary representation
		var info = new CoreDragInfo(src, package.GetView(), (DataPackageOperation)(*dropEffect), dragUI);
		_coreDragDropManager.DragStarted(info);

		*dropEffect = (DROPEFFECT)_manager.ProcessMoved(src);

		return HRESULT.S_OK;
	}

	private static unsafe string GetClipboardFormatName(int format)
	{
		// Try to get the name of registered clipboard formats
		const int bufferSize = 256;
		var buffer = stackalloc char[bufferSize];
		var length = PInvoke.GetClipboardFormatName((uint)format, buffer, bufferSize);

		if (length > 0)
		{
			return new string(buffer, 0, (int)length);
		}

		// Return standard format names
		return format switch
		{
			1 => "CF_TEXT",
			2 => "CF_BITMAP",
			3 => "CF_METAFILEPICT",
			4 => "CF_SYLK",
			5 => "CF_DIF",
			6 => "CF_TIFF",
			7 => "CF_OEMTEXT",
			8 => "CF_DIB",
			9 => "CF_PALETTE",
			10 => "CF_PENDATA",
			11 => "CF_RIFF",
			12 => "CF_WAVE",
			13 => "CF_UNICODETEXT",
			14 => "CF_ENHMETAFILE",
			15 => "CF_HDROP",
			16 => "CF_LOCALE",
			17 => "CF_DIBV5",
			_ => $"Unknown({format})"
		};
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

		this.Log().LogDebug($"{nameof(IDropTarget.Interface.Drop)} @ {nativeDragPoint}");

		// Try to extract file data with async operation support
		if (_hdropFormatEtc.HasValue)
		{
			try
			{
				var hdropFormat = _hdropFormatEtc.Value;

				this.Log().LogDebug($"Using stored CF_HDROP format - tymed: {(TYMED)hdropFormat.tymed}, dwAspect: {hdropFormat.dwAspect}, lindex: {hdropFormat.lindex}");

				// Check if async operation is supported
				if (AsyncDataHelper.SupportsAsyncOperation(dataObject))
				{
					this.Log().LogInfo("Waiting for async operation to complete...");

					// Wait for the async operation to complete (with 5 second timeout)
					if (AsyncDataHelper.WaitForAsyncCompletion(dataObject, 5000))
					{
						this.Log().LogInfo("Async operation completed successfully");
					}
					else
					{
						this.Log().LogWarning("Async operation did not complete in time");
					}
				}

				var queryResult = dataObject->QueryGetData(&hdropFormat);
				if (queryResult.Succeeded)
				{
					this.Log().LogDebug("CF_HDROP is available, attempting to retrieve file paths");

					var hr = dataObject->GetData(hdropFormat, out STGMEDIUM medium);
					if (hr.Succeeded && medium.tymed == TYMED.TYMED_HGLOBAL && medium.u.hGlobal != IntPtr.Zero)
					{
						this.Log().LogInfo("Successfully retrieved CF_HDROP data");

						try
						{
							var filePaths = ExtractFilePathsFromHDrop(medium.u.hGlobal);
							this.Log().LogInfo($"Extracted {filePaths.Count} file path(s): {string.Join(", ", filePaths)}");

							if (filePaths.Count > 0)
							{
								var files = filePaths
									.Where(System.IO.File.Exists)
									.Select(path => Windows.Storage.StorageFile.GetFileFromPath(path) as Windows.Storage.IStorageItem)
									.ToList();

								if (files.Count > 0)
								{
									// Create a new data package with the files
									var package = new DataPackage();
									package.SetStorageItems(files);

									// Update the core drag drop manager with the new data
									var info = new CoreDragInfo(src, package.GetView(), (DataPackageOperation)(*dropEffect), null);
									_coreDragDropManager.DragStarted(info);

									this.Log().LogInfo($"Successfully updated drag data with {files.Count} file(s) from async operation");
								}
							}
						}
						finally
						{
							PInvoke.ReleaseStgMedium(&medium);
						}
					}
					else
					{
						this.Log().LogWarning($"GetData for CF_HDROP failed: {Win32Helper.GetErrorMessage(hr)} (0x{hr.Value:X8}), tymed: {medium.tymed}");
					}
				}
				else
				{
					this.Log().LogWarning($"CF_HDROP not available: {Win32Helper.GetErrorMessage(queryResult)} (0x{queryResult.Value:X8})");
				}

				// Complete the async operation
				if (AsyncDataHelper.SupportsAsyncOperation(dataObject))
				{
					AsyncDataHelper.CompleteAsyncOperation(dataObject, HRESULT.S_OK);
				}
			}
			catch (Exception ex)
			{
				this.LogError()?.Error($"Error extracting files in Drop event: {ex.Message}", ex);

				// Complete the async operation with error
				if (AsyncDataHelper.SupportsAsyncOperation(dataObject))
				{
					AsyncDataHelper.CompleteAsyncOperation(dataObject, HRESULT.E_UNEXPECTED);
				}
			}
		}
		else
		{
			this.Log().LogDebug("No CF_HDROP format was stored from DragEnter");
		}

		// Process the drop
		*dropEffect = (DROPEFFECT)_manager.ProcessReleased(src);

		// Clear the stored data
		_currentDataObject = null;
		_hdropFormatEtc = null;

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
