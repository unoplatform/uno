using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Uno.UI.Runtime.Skia.Win32;

// Image and icon helper methods for drag-and-drop operations
internal partial class Win32DragDropExtension
{
	/// <summary>
	/// Extracts one or more icons from the specified executable file, DLL, or icon file.
	/// </summary>
	/// <param name="lpszFile">The path to the file from which to extract icons.</param>
	/// <param name="nIconIndex">The zero-based index of the first icon to extract.</param>
	/// <param name="phiconLarge">
	/// Pointer to an array that receives handles to the large icons extracted. Can be null if large icons are not needed.
	/// </param>
	/// <param name="phiconSmall">
	/// Pointer to an array that receives handles to the small icons extracted. Can be null if small icons are not needed.
	/// </param>
	/// <param name="nIcons">The number of icons to extract from the file.</param>
	/// <returns>
	/// If <paramref name="phiconLarge"/> and <paramref name="phiconSmall"/> are both null, returns the total number of icons in the file.
	/// Otherwise, returns the number of icons successfully extracted.
	/// </returns>
	[DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
	private static extern unsafe uint ExtractIconExW(string lpszFile, int nIconIndex, HICON* phiconLarge, HICON* phiconSmall, uint nIcons);

	/// <summary>
	/// Retrieves information about an object in the file system, such as a file, folder, directory, or drive root.
	/// </summary>
	/// <param name="pszPath">The path to the file or folder.</param>
	/// <param name="dwFileAttributes">File attribute flags. Used with <paramref name="uFlags"/> to specify the type of file information to retrieve.</param>
	/// <param name="psfi">Pointer to a SHFILEINFOW structure to receive the file information.</param>
	/// <param name="cbFileInfo">The size, in bytes, of the SHFILEINFOW structure pointed to by <paramref name="psfi"/>.</param>
	/// <param name="uFlags">Flags that specify which file information to retrieve.</param>
	/// <returns>
	/// Returns a value whose meaning depends on the <paramref name="uFlags"/> parameter. Typically, returns a handle or zero on failure.
	/// </returns>
	[DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
	private static extern unsafe IntPtr SHGetFileInfoW(string pszPath, uint dwFileAttributes, IntPtr psfi, uint cbFileInfo, uint uFlags);

	// Constants for SHGetFileInfo flags
	private const uint SHGFI_ICON = 0x100;
	private const uint SHGFI_LARGEICON = 0x0;
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

	/// <summary>
	/// Attempts to extract an icon directly from an executable file or DLL.
	/// This method uses ExtractIconExW which only works for executables and DLLs that contain icon resources.
	/// For other file types, use <see cref="GetFileTypeIcon"/> instead.
	/// </summary>
	/// <param name="filePath">The path to the executable or DLL file.</param>
	/// <returns>A BitmapImage containing the icon, or null if extraction fails.</returns>
	private static unsafe Microsoft.UI.Xaml.Media.Imaging.BitmapImage? TryExtractIconFromExecutable(string filePath)
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
				// Clean up the small icon if it was extracted
				if (smallIcon != default)
				{
					PInvoke.DestroyIcon(smallIcon);
				}
				return null;
			}

			// Clean up the small icon if extracted (we only need the large one)
			if (smallIcon != default)
			{
				PInvoke.DestroyIcon(smallIcon);
			}

			try
			{
				// Convert HICON to BitmapImage
				return ConvertHIconToBitmapImage(largeIcon);
			}
			finally
			{
				// Cleanup: destroy the large icon handle
				PInvoke.DestroyIcon(largeIcon);
			}
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
		{
			// Failed to extract icon
			return null;
		}
	}

	/// <summary>
	/// Gets the icon associated with a file type using Shell API.
	/// This method works for all file types by retrieving the icon based on file extension.
	/// </summary>
	/// <param name="filePath">The path to the file.</param>
	/// <returns>An HICON handle to the file type icon, or default if retrieval fails.</returns>
	private static unsafe HICON GetFileTypeIcon(string filePath)
	{
		try
		{
			var fileInfo = new SHFILEINFOW();
			var flags = SHGFI_ICON | SHGFI_LARGEICON;

			// Check if file exists once and store the result
			var fileExists = File.Exists(filePath);

			// If the file doesn't exist, use SHGFI_USEFILEATTRIBUTES to get icon based on extension
			if (!fileExists)
			{
				flags |= SHGFI_USEFILEATTRIBUTES;
			}

			var pFileInfo = Marshal.AllocHGlobal(Marshal.SizeOf<SHFILEINFOW>());
			try
			{
				Marshal.StructureToPtr(fileInfo, pFileInfo, false);

				var result = SHGetFileInfoW(
					filePath,
					fileExists ? 0 : FILE_ATTRIBUTE_NORMAL,
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
							var stride = width * 4; // 4 bytes per pixel (BGRA)
							var dataSize = stride * height;
							var pixelData = new byte[dataSize];
							Marshal.Copy((IntPtr)bits, pixelData, 0, dataSize);

							// Create PNG format to preserve transparency
							// PNG file structure: signature + IHDR + IDAT + IEND
							using var memoryStream = new MemoryStream();

							// PNG signature
							byte[] pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
							memoryStream.Write(pngSignature, 0, 8);

							// IHDR chunk
							WriteIhdrChunk(memoryStream, width, height);

							// IDAT chunk (image data)
							WriteIdatChunk(memoryStream, pixelData, width, height);

							// IEND chunk
							WriteIendChunk(memoryStream);

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
}
