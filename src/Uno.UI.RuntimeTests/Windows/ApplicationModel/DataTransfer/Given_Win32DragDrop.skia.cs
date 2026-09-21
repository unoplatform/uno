#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
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
