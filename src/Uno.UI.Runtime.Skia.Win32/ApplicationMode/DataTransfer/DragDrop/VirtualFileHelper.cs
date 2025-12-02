using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.Ole;
using Uno.Foundation.Logging;
using IDataObject = Windows.Win32.System.Com.IDataObject;

namespace Uno.UI.Runtime.Skia.Win32;

/// <summary>
/// Helper class to extract virtual files from Outlook and other applications
/// that use FileGroupDescriptor format (e.g., email attachments).
/// </summary>
internal static unsafe class VirtualFileHelper
{
	private const string CFSTR_FILEDESCRIPTORW = "FileGroupDescriptorW";
	private const string CFSTR_FILECONTENTS = "FileContents";

	/// <summary>
	/// Checks if the data object contains virtual files (like Outlook attachments).
	/// </summary>
	public static bool ContainsVirtualFiles(IDataObject* dataObject)
	{
		var fileDescriptorFormat = PInvoke.RegisterClipboardFormat(CFSTR_FILEDESCRIPTORW);
		if (fileDescriptorFormat == 0)
		{
			return false;
		}

		var formatEtc = new FORMATETC
		{
			cfFormat = (ushort)fileDescriptorFormat,
			ptd = null,
			dwAspect = 1, // DVASPECT_CONTENT
			lindex = -1,
			tymed = (uint)TYMED.TYMED_HGLOBAL
		};

		var result = dataObject->QueryGetData(&formatEtc);
		return result.Succeeded;
	}

	/// <summary>
	/// Extracts virtual files from the data object and saves them to temporary storage.
	/// </summary>
	public static async Task<List<IStorageFile>> ExtractVirtualFilesAsync(IDataObject* dataObject)
	{
		var files = new List<IStorageFile>();

		var fileDescriptorFormat = PInvoke.RegisterClipboardFormat(CFSTR_FILEDESCRIPTORW);
		var fileContentsFormat = PInvoke.RegisterClipboardFormat(CFSTR_FILECONTENTS);

		if (fileDescriptorFormat == 0 || fileContentsFormat == 0)
		{
			typeof(VirtualFileHelper).Log().LogError("Failed to register clipboard formats for virtual files");
			return files;
		}

		// Get file descriptors
		var descriptorFormatEtc = new FORMATETC
		{
			cfFormat = (ushort)fileDescriptorFormat,
			ptd = null,
			dwAspect = 1, // DVASPECT_CONTENT
			lindex = -1,
			tymed = (uint)TYMED.TYMED_HGLOBAL
		};

		var hr = dataObject->GetData(descriptorFormatEtc, out var descriptorMedium);
		if (hr.Failed)
		{
			typeof(VirtualFileHelper).Log().LogError($"Failed to get file descriptors: {Win32Helper.GetErrorMessage(hr)}");
			return files;
		}

		try
		{
			using var lockDisposable = Win32Helper.GlobalLock(descriptorMedium.u.hGlobal, out var descriptorPtr);
			if (lockDisposable is null)
			{
				return files;
			}

			// Read FILEGROUPDESCRIPTORW structure
			var fileGroupDescriptor = (FILEGROUPDESCRIPTORW*)descriptorPtr;
			var fileCount = fileGroupDescriptor->cItems;

			typeof(VirtualFileHelper).Log().LogInformation($"Found {fileCount} virtual file(s)");

			// Get temporary folder for extracted files
			var tempFolder = await GetTempFolderAsync();

			// Extract each file
			for (uint i = 0; i < fileCount; i++)
			{
				try
				{
					var file = await ExtractVirtualFileAsync(dataObject, fileContentsFormat, fileGroupDescriptor, i, tempFolder);
					if (file is not null)
					{
						files.Add(file);
					}
				}
				catch (Exception ex)
				{
					typeof(VirtualFileHelper).Log().LogError($"Failed to extract virtual file {i}: {ex.Message}");
				}
			}
		}
		finally
		{
			PInvoke.ReleaseStgMedium(&descriptorMedium);
		}

		return files;
	}

	private static async Task<IStorageFile?> ExtractVirtualFileAsync(
		IDataObject* dataObject,
		uint fileContentsFormat,
		FILEGROUPDESCRIPTORW* fileGroupDescriptor,
		uint fileIndex,
		StorageFolder tempFolder)
	{
		// Get file descriptor for this file
		var fileDescriptor = &fileGroupDescriptor->fgd[fileIndex];

		// Get filename
		var fileName = Marshal.PtrToStringUni((IntPtr)fileDescriptor->cFileName);
		if (string.IsNullOrEmpty(fileName))
		{
			fileName = $"file_{fileIndex}";
		}

		// Make filename safe
		fileName = MakeSafeFileName(fileName);

		typeof(VirtualFileHelper).Log().LogInformation($"Extracting virtual file: {fileName}");

		// Get file contents
		var contentsFormatEtc = new FORMATETC
		{
			cfFormat = (ushort)fileContentsFormat,
			ptd = null,
			dwAspect = 1, // DVASPECT_CONTENT
			lindex = (int)fileIndex, // Important: lindex specifies which file to get
			tymed = (uint)TYMED.TYMED_HGLOBAL | (uint)TYMED.TYMED_ISTREAM
		};

		STGMEDIUM contentsMedium;
		var hr = dataObject->GetData(contentsFormatEtc, &contentsMedium);
		if (hr.Failed)
		{
			typeof(VirtualFileHelper).Log().LogError($"Failed to get file contents for {fileName}: {Win32Helper.GetErrorMessage(hr)}");
			return null;
		}

		try
		{
			// Create file in temp folder
			var file = tempFolder.CreateFileAsync(fileName, Windows.Storage.CreationCollisionOption.GenerateUniqueName);

			if (contentsMedium.tymed == TYMED.TYMED_HGLOBAL)
			{
				// Data is in HGLOBAL
				using var lockDisposable = Win32Helper.GlobalLock(contentsMedium.u.hGlobal, out var contentsPtr);
				if (lockDisposable is null)
				{
					return null;
				}

				var size = (int)PInvoke.GlobalSize(contentsMedium.u.hGlobal);
				var data = new byte[size];
				Marshal.Copy((IntPtr)contentsPtr, data, 0, size);

				await Windows.Storage.FileIO.WriteBytesAsync(file, data);
			}
			else if (contentsMedium.tymed == TYMED.TYMED_ISTREAM)
			{
				// Data is in IStream
				var stream = (IStream*)contentsMedium.u.pstm;

				using var fileStream = await file.OpenStreamForWriteAsync();

				// Read from IStream and write to file
				const int bufferSize = 4096;
				var buffer = stackalloc byte[bufferSize];
				uint bytesRead;

				while (true)
				{
					hr = stream->Read(buffer, bufferSize, &bytesRead);
					if (hr.Failed || bytesRead == 0)
					{
						break;
					}

					var managedBuffer = new byte[bytesRead];
					Marshal.Copy((IntPtr)buffer, managedBuffer, 0, (int)bytesRead);
					await fileStream.WriteAsync(managedBuffer, 0, (int)bytesRead);
				}
			}

			typeof(VirtualFileHelper).Log().LogInformation($"Successfully extracted: {fileName}");
			return file;
		}
		catch (Exception ex)
		{
			typeof(VirtualFileHelper).Log().LogError($"Error writing file {fileName}: {ex.Message}");
			return null;
		}
		finally
		{
			PInvoke.ReleaseStgMedium(&contentsMedium);
		}
	}

	private static string GetTempFolderPath()
	{
		var tempFolder = Windows.Storage.ApplicationData.Current.TemporaryFolder.Path;

		if (tempFolder is null)
		{
			throw new InvalidOperationException("Unknown tempoarary folder");
		}

		var subFolder = Path.Combine(tempFolder, "DragDrop_" + DateTime.Now.Ticks);
		Directory.CreateDirectory(subFolder);

		return subFolder;
	}

	private static string MakeSafeFileName(string fileName)
	{
		var invalidChars = Path.GetInvalidFileNameChars();
		foreach (var c in invalidChars)
		{
			fileName = fileName.Replace(c, '_');
		}
		return fileName;
	}
}
