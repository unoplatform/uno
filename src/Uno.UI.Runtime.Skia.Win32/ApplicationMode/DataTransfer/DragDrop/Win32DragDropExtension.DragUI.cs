using System;
using System.IO;
using System.Linq;
using Windows.Win32;
using Windows.Win32.System.Com;
using Windows.Win32.System.Ole;
using Microsoft.UI.Xaml;
using Uno.Foundation.Logging;
using IDataObject = Windows.Win32.System.Com.IDataObject;

namespace Uno.UI.Runtime.Skia.Win32;

// DragUI creation for external drag operations
internal partial class Win32DragDropExtension
{
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
}
