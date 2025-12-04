using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
			typeof(VirtualFileHelper).Log().LogError("Failed to register FileGroupDescriptorW clipboard format");
			return false;
		}

		typeof(VirtualFileHelper).Log().LogInfo($"Registered FileGroupDescriptorW format: {fileDescriptorFormat}");

		var formatEtc = new FORMATETC
		{
			cfFormat = (ushort)fileDescriptorFormat,
			ptd = null,
			dwAspect = 1, // DVASPECT_CONTENT
			lindex = -1,
			tymed = (uint)TYMED.TYMED_HGLOBAL
		};

		var result = dataObject->QueryGetData(&formatEtc);
		
		typeof(VirtualFileHelper).Log().LogInfo($"QueryGetData for FileGroupDescriptorW returned: {result} (0x{result.Value:X8})");

		if (result.Failed)
		{
			// Try alternative format name (some apps might use different variant)
			var altFormat = PInvoke.RegisterClipboardFormat("FileGroupDescriptor");
			if (altFormat != 0)
			{
				typeof(VirtualFileHelper).Log().LogInfo($"Trying alternative FileGroupDescriptor format: {altFormat}");
				formatEtc.cfFormat = (ushort)altFormat;
				result = dataObject->QueryGetData(&formatEtc);
				typeof(VirtualFileHelper).Log().LogInfo($"QueryGetData for FileGroupDescriptor returned: {result} (0x{result.Value:X8})");
			}
		}

		return result.Succeeded;
	}

	/// <summary>
	/// Extracts virtual files from the data object and saves them to temporary storage.
	/// </summary>
	public static List<IStorageFile> ExtractVirtualFiles(IDataObject* dataObject)
	{
		var files = new List<IStorageFile>();

		var fileDescriptorFormat = PInvoke.RegisterClipboardFormat(CFSTR_FILEDESCRIPTORW);
		var fileContentsFormat = PInvoke.RegisterClipboardFormat(CFSTR_FILECONTENTS);

		if (fileDescriptorFormat == 0 || fileContentsFormat == 0)
		{
			typeof(VirtualFileHelper).Log().LogError($"Failed to register clipboard formats - FileGroupDescriptorW: {fileDescriptorFormat}, FileContents: {fileContentsFormat}");
			return files;
		}

		typeof(VirtualFileHelper).Log().LogInfo($"Registered formats - FileGroupDescriptorW: {fileDescriptorFormat}, FileContents: {fileContentsFormat}");

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
			typeof(VirtualFileHelper).Log().LogError($"Failed to get file descriptors: {Win32Helper.GetErrorMessage(hr)} (0x{hr.Value:X8})");
			
			// Try alternative format
			var altFormat = PInvoke.RegisterClipboardFormat("FileGroupDescriptor");
			if (altFormat != 0)
			{
				typeof(VirtualFileHelper).Log().LogInfo($"Trying alternative FileGroupDescriptor format: {altFormat}");
				descriptorFormatEtc.cfFormat = (ushort)altFormat;
				hr = dataObject->GetData(descriptorFormatEtc, out descriptorMedium);
				
				if (hr.Failed)
				{
					typeof(VirtualFileHelper).Log().LogError($"Alternative format also failed: {Win32Helper.GetErrorMessage(hr)} (0x{hr.Value:X8})");
					return files;
				}
				
				typeof(VirtualFileHelper).Log().LogInfo("Alternative format succeeded!");
			}
			else
			{
				return files;
			}
		}

		try
		{
			using var lockDisposable = Win32Helper.GlobalLock(descriptorMedium.u.hGlobal, out var descriptorPtr);
			if (lockDisposable is null)
			{
				typeof(VirtualFileHelper).Log().LogError("Failed to lock global memory for file descriptors");
				return files;
			}

			// Read FILEGROUPDESCRIPTORW structure
			var fileGroupDescriptor = (FILEGROUPDESCRIPTORW*)descriptorPtr;
			var fileCount = fileGroupDescriptor->cItems;

			typeof(VirtualFileHelper).Log().LogInfo($"Found {fileCount} virtual file(s)");

			if (fileCount == 0)
			{
				typeof(VirtualFileHelper).Log().LogWarning("File count is 0");
				return files;
			}

			// Get temporary folder for extracted files
			var tempFolder = GetTempFolder();
			typeof(VirtualFileHelper).Log().LogInfo($"Using temp folder: {tempFolder}");

			// Extract each file
			for (uint i = 0; i < fileCount; i++)
			{
				try
				{
					var file = ExtractVirtualFile(dataObject, fileContentsFormat, fileGroupDescriptor, i, tempFolder);
					if (file is not null)
					{
						files.Add(file);
						typeof(VirtualFileHelper).Log().LogInfo($"Successfully added file {i + 1}/{fileCount}");
					}
					else
					{
						typeof(VirtualFileHelper).Log().LogWarning($"File {i + 1}/{fileCount} extraction returned null");
					}
				}
				catch (Exception ex)
				{
					typeof(VirtualFileHelper).Log().LogError($"Failed to extract virtual file {i}: {ex.Message}", ex);
				}
			}
			
			typeof(VirtualFileHelper).Log().LogInfo($"Extraction complete. Successfully extracted {files.Count}/{fileCount} files");
		}
		finally
		{
			PInvoke.ReleaseStgMedium(&descriptorMedium);
		}

		return files;
	}

	private static IStorageFile? ExtractVirtualFile(
		IDataObject* dataObject,
		uint fileContentsFormat,
		FILEGROUPDESCRIPTORW* fileGroupDescriptor,
		uint fileIndex,
		string tempFolderPath)
	{
		// Get file descriptor for this file using pointer arithmetic
		// The FILEGROUPDESCRIPTORW struct contains only the first descriptor,
		// but subsequent descriptors follow it in memory
		var fileDescriptor = (FILEDESCRIPTORW*)(&fileGroupDescriptor->fgd) + fileIndex;

		// Get filename
		var fileName = Marshal.PtrToStringUni((IntPtr)fileDescriptor->cFileName);
		if (string.IsNullOrEmpty(fileName))
		{
			fileName = $"file_{fileIndex}";
		}

		// Make filename safe
		fileName = MakeSafeFileName(fileName);

		typeof(VirtualFileHelper).Log().LogInfo($"Extracting virtual file: {fileName}");

		// Get file contents
		var contentsFormatEtc = new FORMATETC
		{
			cfFormat = (ushort)fileContentsFormat,
			ptd = null,
			dwAspect = 1, // DVASPECT_CONTENT
			lindex = (int)fileIndex, // Important: lindex specifies which file to get
			tymed = (uint)TYMED.TYMED_HGLOBAL | (uint)TYMED.TYMED_ISTREAM
		};

		var hr = dataObject->GetData(contentsFormatEtc, out var contentsMedium);
		if (hr.Failed)
		{
			typeof(VirtualFileHelper).Log().LogError($"Failed to get file contents for {fileName}: {Win32Helper.GetErrorMessage(hr)}");
			return null;
		}

		try
		{
			// Create file path
			var filePath = Path.Combine(tempFolderPath, fileName);

			// Ensure unique filename
			filePath = GetUniqueFilePath(filePath);

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

				File.WriteAllBytes(filePath, data);
			}
			else if (contentsMedium.tymed == TYMED.TYMED_ISTREAM)
			{
				// Data is in IStream
				var stream = (IStream*)contentsMedium.u.pstm;

				using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);

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
					fileStream.Write(managedBuffer, 0, (int)bytesRead);
				}
			}
			else
			{
				typeof(VirtualFileHelper).Log().LogError($"Unsupported TYMED type: {contentsMedium.tymed}");
				return null;
			}

			typeof(VirtualFileHelper).Log().LogInfo($"Successfully extracted: {fileName}");

			// Create StorageFile from path
			return Windows.Storage.StorageFile.GetFileFromPath(filePath);
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

	private static string GetTempFolder()
	{
		var tempFolder = Path.Combine(Path.GetTempPath(), "UnoDragDrop_" + DateTime.Now.Ticks);

		if (!Directory.Exists(tempFolder))
		{
			Directory.CreateDirectory(tempFolder);
		}

		return tempFolder;
	}

	private static string GetUniqueFilePath(string filePath)
	{
		if (!File.Exists(filePath))
		{
			return filePath;
		}

		var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
		var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
		var extension = Path.GetExtension(filePath);

		var counter = 1;
		string newPath;
		do
		{
			newPath = Path.Combine(directory, $"{fileNameWithoutExtension} ({counter}){extension}");
			counter++;
		}
		while (File.Exists(newPath));

		return newPath;
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
