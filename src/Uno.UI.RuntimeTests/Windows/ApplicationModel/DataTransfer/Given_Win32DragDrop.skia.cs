#nullable enable

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Uno.Disposables;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Uno.UI.RuntimeTests.Tests;

[TestClass]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
public class Given_Win32DragDrop
{
	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_File_Drop_Failure_Completes_Task(bool failDuringCleanup)
	{
		var completion = new TaskCompletionSource<List<IStorageItem>>(TaskCreationOptions.RunContinuationsAsynchronously);
		var error = new FormatException("Invalid file drop data.");
		var cleanedUp = false;
		Func<List<IStorageItem>?> getFiles = () => failDuringCleanup ? new() : throw error;
		var cleanup = Disposable.Create(() =>
		{
			cleanedUp = true;
			if (failDuringCleanup)
			{
				throw error;
			}
		});

		CompleteFileDrop(completion, getFiles, cleanup);

		var actual = await Assert.ThrowsExactlyAsync<FormatException>(async () =>
		{
			await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
		});
		Assert.AreSame(error, actual);
		Assert.IsTrue(cleanedUp);
	}

	[TestMethod]
	public async Task When_File_Drop_Completes_Only_After_Cleanup()
	{
		var completion = new TaskCompletionSource<List<IStorageItem>>(TaskCreationOptions.RunContinuationsAsynchronously);
		var files = new List<IStorageItem>();
		var cleanedUp = false;
		var cleanup = Disposable.Create(() =>
		{
			Assert.IsFalse(completion.Task.IsCompleted);
			cleanedUp = true;
		});

		CompleteFileDrop(completion, () => files, cleanup);

		Assert.AreSame(files, await completion.Task.WaitAsync(TimeSpan.FromSeconds(5)));
		Assert.IsTrue(cleanedUp);
	}

	[TestMethod]
	public void When_Large_File_Drop_All_Paths_Are_Returned()
	{
		// Selecting a whole large folder in Explorer easily exceeds a few thousand items.
		var paths = Enumerable.Range(0, 5000).Select(i => $@"C:\Drop\file{i:D5}.txt").ToArray();
		var handle = CreateHDrop(paths);
		try
		{
			var result = GetFileDropPaths(handle);

			Assert.IsNotNull(result);
			CollectionAssert.AreEqual(paths, result);
		}
		finally
		{
			GlobalFree(handle);
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25863")]
	public async Task When_Bitmap_Drop_Read_After_Medium_Released()
	{
		// DragEnter releases every STGMEDIUM it reads before it returns, but apps read the bitmap later, in DragOver or Drop.
		var dib = CreateDib(width: 2, height: 3);
		var handle = CreateHGlobal(dib);
		var package = new DataPackage();
		IntPtr freeResult;
		try
		{
			ReadContentIntoPackage(package, "CF_DIB", handle);
		}
		finally
		{
			// What ReleaseStgMedium does for an HGLOBAL medium without pUnkForRelease.
			freeResult = GlobalFree(handle);
		}
		Assert.AreEqual(IntPtr.Zero, freeResult, "GlobalFree failed.");

		var reference = await package.GetView().GetBitmapAsync();
		using var stream = (await reference.OpenReadAsync()).AsStreamForRead();
		using var bmp = new MemoryStream();
		await stream.CopyToAsync(bmp);

		var bytes = bmp.ToArray();
		const int FileHeaderSize = 14; // BITMAPFILEHEADER
		Assert.AreEqual(FileHeaderSize + dib.Length, bytes.Length);
		Assert.AreEqual((byte)'B', bytes[0]);
		Assert.AreEqual((byte)'M', bytes[1]);
		Assert.AreEqual((uint)bytes.Length, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(2))); // bfSize
		Assert.AreEqual(54u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(10))); // bfOffBits: both headers, no color table
		CollectionAssert.AreEqual(dib, bytes[FileHeaderSize..]);
	}

	private static IntPtr CreateHDrop(string[] paths)
	{
		const int DropFilesHeaderSize = 20;
		var payload = Encoding.Unicode.GetBytes(string.Join("\0", paths) + "\0\0");
		var handle = GlobalAlloc(0x0042 /* GMEM_MOVEABLE | GMEM_ZEROINIT */, (nuint)(DropFilesHeaderSize + payload.Length));
		Assert.AreNotEqual(IntPtr.Zero, handle);
		var memory = GlobalLock(handle);
		try
		{
			Marshal.WriteInt32(memory, 0, DropFilesHeaderSize); // pFiles
			Marshal.WriteInt32(memory, 16, 1); // fWide
			Marshal.Copy(payload, 0, memory + DropFilesHeaderSize, payload.Length);
		}
		finally
		{
			GlobalUnlock(handle);
		}
		return handle;
	}

	private static List<string>? GetFileDropPaths(IntPtr handle)
	{
		var type = Type.GetType("Uno.UI.Runtime.Win32.Win32ClipboardExtension, Uno.UI.Runtime.Win32", throwOnError: true)!;
		var method = type.GetMethod("GetFileDropPaths", BindingFlags.Static | BindingFlags.NonPublic);
		Assert.IsNotNull(method);
		var hglobalType = method.GetParameters()[0].ParameterType;
		var toHglobal = hglobalType.GetMethod("op_Explicit", new[] { typeof(IntPtr) });
		Assert.IsNotNull(toHglobal);
		return (List<string>?)method.Invoke(null, new[] { toHglobal.Invoke(null, new object[] { handle }) });
	}

	private static byte[] CreateDib(int width, int height)
	{
		const int HeaderSize = 40; // BITMAPINFOHEADER
		var dib = new byte[HeaderSize + width * height * 4];
		BinaryPrimitives.WriteInt32LittleEndian(dib.AsSpan(0), HeaderSize); // biSize
		BinaryPrimitives.WriteInt32LittleEndian(dib.AsSpan(4), width);
		BinaryPrimitives.WriteInt32LittleEndian(dib.AsSpan(8), height);
		BinaryPrimitives.WriteUInt16LittleEndian(dib.AsSpan(12), 1); // biPlanes
		BinaryPrimitives.WriteUInt16LittleEndian(dib.AsSpan(14), 32); // biBitCount
		BinaryPrimitives.WriteInt32LittleEndian(dib.AsSpan(16), 0); // biCompression = BI_RGB
		BinaryPrimitives.WriteInt32LittleEndian(dib.AsSpan(20), width * height * 4); // biSizeImage
		for (var i = HeaderSize; i < dib.Length; i++)
		{
			dib[i] = (byte)(i * 7);
		}
		return dib;
	}

	private static IntPtr CreateHGlobal(byte[] data)
	{
		var handle = GlobalAlloc(0x0002 /* GMEM_MOVEABLE */, (nuint)data.Length);
		Assert.AreNotEqual(IntPtr.Zero, handle);
		var memory = GlobalLock(handle);
		if (memory == IntPtr.Zero)
		{
			var error = Marshal.GetLastWin32Error();
			GlobalFree(handle);
			Assert.Fail($"GlobalLock failed with error {error}.");
		}
		try
		{
			Marshal.Copy(data, 0, memory, data.Length);
		}
		finally
		{
			GlobalUnlock(handle);
		}
		return handle;
	}

	private static void ReadContentIntoPackage(DataPackage package, string formatName, IntPtr handle)
	{
		var type = Type.GetType("Uno.UI.Runtime.Win32.Win32ClipboardExtension, Uno.UI.Runtime.Win32", throwOnError: true)!;
		var method = type.GetMethod("ReadContentIntoPackage", BindingFlags.Static | BindingFlags.NonPublic);
		Assert.IsNotNull(method);
		var parameters = method.GetParameters(); // (DataPackage, IEnumerable<CLIPBOARD_FORMAT>, Func<CLIPBOARD_FORMAT, HGLOBAL?>)
		Assert.HasCount(3, parameters);
		var formatType = parameters[1].ParameterType.GetGenericArguments()[0];
		var handleType = parameters[2].ParameterType.GetGenericArguments()[1];
		var hglobalType = Nullable.GetUnderlyingType(handleType);
		Assert.IsNotNull(hglobalType, "The data getter is expected to return HGLOBAL?.");
		var toHglobal = hglobalType.GetMethod("op_Explicit", new[] { typeof(IntPtr) });
		Assert.IsNotNull(toHglobal);

		var formats = Array.CreateInstance(formatType, 1);
		formats.SetValue(Enum.Parse(formatType, formatName), 0);
		var dataGetter = typeof(Given_Win32DragDrop)
			.GetMethod(nameof(ConstantGetter), BindingFlags.Static | BindingFlags.NonPublic)!
			.MakeGenericMethod(formatType, handleType)
			.Invoke(null, new[] { toHglobal.Invoke(null, new object[] { handle }) });
		method.Invoke(null, new[] { package, formats, dataGetter });
	}

	private static Func<TFormat, THandle> ConstantGetter<TFormat, THandle>(THandle handle) => _ => handle;

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern IntPtr GlobalAlloc(uint flags, nuint bytes);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern IntPtr GlobalLock(IntPtr handle);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool GlobalUnlock(IntPtr handle);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern IntPtr GlobalFree(IntPtr handle);

	private static void CompleteFileDrop(
		TaskCompletionSource<List<IStorageItem>> completion,
		Func<List<IStorageItem>?> getFiles,
		IDisposable cleanup)
	{
		var type = Type.GetType("Uno.UI.Runtime.Win32.Win32DragDropExtension, Uno.UI.Runtime.Win32", throwOnError: true)!;
		var method = type.GetMethod("CompleteFileDrop", BindingFlags.Static | BindingFlags.NonPublic);
		Assert.IsNotNull(method);
		method.Invoke(null, new object[] { completion, getFiles, cleanup });
	}
}
