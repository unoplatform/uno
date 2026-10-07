#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Uno.Disposables;
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
		var type = Type.GetType("Uno.UI.Runtime.Skia.Win32.Win32ClipboardExtension, Uno.UI.Runtime.Skia.Win32", throwOnError: true)!;
		var method = type.GetMethod("GetFileDropPaths", BindingFlags.Static | BindingFlags.NonPublic);
		Assert.IsNotNull(method);
		var hglobalType = method.GetParameters()[0].ParameterType;
		var toHglobal = hglobalType.GetMethod("op_Explicit", new[] { typeof(IntPtr) });
		Assert.IsNotNull(toHglobal);
		return (List<string>?)method.Invoke(null, new[] { toHglobal.Invoke(null, new object[] { handle }) });
	}

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
		var type = Type.GetType("Uno.UI.Runtime.Skia.Win32.Win32DragDropExtension, Uno.UI.Runtime.Skia.Win32", throwOnError: true)!;
		var method = type.GetMethod("CompleteFileDrop", BindingFlags.Static | BindingFlags.NonPublic);
		Assert.IsNotNull(method);
		method.Invoke(null, new object[] { completion, getFiles, cleanup });
	}
}
