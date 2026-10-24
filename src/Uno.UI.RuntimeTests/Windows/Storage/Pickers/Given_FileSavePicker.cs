#if HAS_UNO
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage.Pickers;

// These cover the iOS save picker that is used when no extension is registered. The export sheet itself needs a user, so the tests only
// present it to cancel it, and wait for it to be gone before they end. Some present another sheet outside the export sheets' queue, which
// makes UIKit drop the export sheet's presentation, or close the sheet, or dismiss the sheet as app code would. [GitHubWorkItem] is on each
// test: it only applies to methods.
[TestClass]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaIOS)]
public class Given_FileSavePicker
{
	private static readonly TimeSpan _sheetTimeout = TimeSpan.FromSeconds(30);

	// A name can have 255 bytes, and a transacted write adds ".tmp" to it for its temporary file.
	private const int MaxNameBytes = 255 - 4;

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25667")]
	public async Task When_No_Extension_Is_Registered_Then_An_Empty_Staged_File_Is_Picked()
	{
		var file = await PickAsync("Report");

		try
		{
			Assert.AreEqual("Report.txt", file.Name);
			Assert.AreEqual("iosexportpicker", file.Provider.Id);
			Assert.StartsWith(Path.GetTempPath(), file.Path);
			Assert.AreEqual(0, new FileInfo(file.Path).Length);
		}
		finally
		{
			await file.DeleteAsync();
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25667")]
	[DataRow("Report", "Report.txt")]
	[DataRow("Report.TXT", "Report.TXT")]
	[DataRow("Report.text", "Report.text")]
	[DataRow("Report v1.2", "Report v1.2.txt")]
	[DataRow("Report.", "Report.txt")]
	[DataRow("Report .", "Report.txt")]
	[DataRow("  Report  ", "Report.txt")]
	[DataRow(".hidden", "hidden.txt")]
	[DataRow(".txt", "txt.txt")]
	[DataRow("Plans/Report", "Plans_Report.txt")]
	[DataRow("Plans:Report", "Plans_Report.txt")]
	[DataRow("../../Report", "_.._Report.txt")]
	public async Task When_Name_Is_Suggested_Then_It_Ends_With_A_Choice_Extension(string suggestedFileName, string expectedName)
	{
		var file = await PickAsync(suggestedFileName);

		try
		{
			Assert.AreEqual(expectedName, file.Name);
		}
		finally
		{
			await file.DeleteAsync();
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25667")]
	[DataRow("")]
	[DataRow(".")]
	public async Task When_Name_Is_Empty_Then_A_Guid_Is_Used(string suggestedFileName)
	{
		var file = await PickAsync(suggestedFileName);

		try
		{
			Assert.AreEqual(".txt", file.FileType);
			Assert.IsTrue(Guid.TryParse(file.DisplayName, out _), file.Name);
		}
		finally
		{
			await file.DeleteAsync();
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25667")]
	[DataRow("Report.pdf", "Report.pdf")]
	[DataRow("Report", "Report")]
	[DataRow("Archive.tar.gz", "Archive.tar.gz")]
	[DataRow("Minutes. Board meeting", "Minutes. Board meeting")]
	[DataRow(".gitignore", "gitignore")]
	public async Task When_There_Are_No_File_Type_Choices_Then_The_Name_Is_Kept(string suggestedFileName, string expectedName)
	{
		var file = await PickWithoutChoicesAsync(suggestedFileName);

		try
		{
			Assert.AreEqual(expectedName, file.Name);
		}
		finally
		{
			await file.DeleteAsync();
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25667")]
	public async Task When_A_Save_File_Is_Suggested_Then_Its_Name_Wins()
	{
		var path = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, Guid.NewGuid().ToString("N"), "Suggested.txt");
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, "Hello, world!");
		StorageFile? file = null;

		try
		{
			file = await PickAsync("Other", await StorageFile.GetFileFromPathAsync(path));

			Assert.AreEqual("Suggested.txt", file.Name);
		}
		finally
		{
			if (file is not null)
			{
				await file.DeleteAsync();
			}

			Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25667")]
	public async Task When_Name_Is_Too_Long_Then_It_Is_Shortened()
	{
		var file = await PickAsync(new string('é', 300));

		try
		{
			Assert.EndsWith(".txt", file.Name);
			Assert.IsLessThanOrEqualTo(MaxNameBytes, Encoding.UTF8.GetByteCount(file.Name));
		}
		finally
		{
			await file.DeleteAsync();
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25667")]
	public async Task When_Name_Is_Too_Long_Then_A_Transacted_Write_Can_Commit()
	{
		var file = await PickAsync(new string('a', 300));

		try
		{
			Assert.AreEqual(MaxNameBytes, Encoding.UTF8.GetByteCount(file.Name));

			using (var transaction = await file.OpenTransactedWriteAsync())
			{
				var stream = transaction.Stream.AsStreamForWrite();
				await stream.WriteAsync(Encoding.UTF8.GetBytes("Hello, world!"));
				await stream.FlushAsync();
				await transaction.CommitAsync();
			}

			Assert.AreEqual("Hello, world!", File.ReadAllText(file.Path));
		}
		finally
		{
			await file.DeleteAsync();
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25667")]
	public async Task When_Name_Has_A_Long_Last_Part_And_There_Are_No_Choices_Then_It_Is_Shortened()
	{
		var file = await PickWithoutChoicesAsync("Minutes. " + new string('x', 300));

		try
		{
			Assert.StartsWith("Minutes. x", file.Name);
			Assert.IsLessThanOrEqualTo(MaxNameBytes, Encoding.UTF8.GetByteCount(file.Name));
		}
		finally
		{
			await file.DeleteAsync();
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25667")]
	public async Task When_Same_Name_Is_Picked_Twice_Then_They_Are_In_Different_Folders()
	{
		var first = await PickAsync("Report");
		var second = await PickAsync("Report");

		try
		{
			Assert.AreEqual(first.Name, second.Name);
			Assert.AreNotEqual(Path.GetDirectoryName(first.Path), Path.GetDirectoryName(second.Path));
		}
		finally
		{
			await first.DeleteAsync();
			await second.DeleteAsync();
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25667")]
	public async Task When_Written_Then_The_File_Has_The_Content()
	{
		var file = await PickAsync("Report");

		try
		{
			await FileIO.WriteTextAsync(file, "Hello, world!");

			Assert.AreEqual("Hello, world!", File.ReadAllText(file.Path));
		}
		finally
		{
			await file.DeleteAsync();
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25667")]
	[DataRow(".txt/../../existing.txt")]
	[DataRow("./../existing")]
	[DataRow(".txt\\..\\existing")]
	[DataRow("..txt")]
	[DataRow(".t:xt")]
	[DataRow(".t\u0007xt")]
	public async Task When_A_Choice_Extension_Is_Unsafe_Then_It_Is_Skipped(string unsafeExtension)
	{
		var picker = new FileSavePicker { SuggestedFileName = "Report" };
		picker.FileTypeChoices.Add("Unsafe", new List<string> { unsafeExtension });
		picker.FileTypeChoices.Add("Plain Text", new List<string> { ".txt" });

		var file = await picker.PickSaveFileAsync();
		Assert.IsNotNull(file);

		try
		{
			// The file is directly in its own folder, which is directly in the staging folder.
			var folder = Path.GetDirectoryName(file.Path)!;
			Assert.AreEqual("Report.txt", file.Name);
			Assert.AreEqual(Path.Combine(Path.GetTempPath(), "Uno.FileSavePicker"), Path.GetDirectoryName(folder));
		}
		finally
		{
			await file.DeleteAsync();
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25667")]
	public async Task When_Deleted_Then_Its_Folder_Is_Deleted_And_Completing_Fails()
	{
		var file = await PickAsync("Report");
		var folder = Path.GetDirectoryName(file.Path)!;

		await file.DeleteAsync();

		Assert.IsFalse(Directory.Exists(folder));
		Assert.AreEqual(FileUpdateStatus.Failed, await CachedFileManager.CompleteUpdatesAsync(file));
		await file.DeleteAsync();
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25667")]
	public async Task When_Completion_Is_Cancelled_Before_The_Sheet_Is_Presented_Then_None_Is_Shown()
	{
		var file = await PickAsync("Report");
		IAsyncOperation<FileUpdateStatus>? completion = null;

		try
		{
			// On the UI thread, the sheet can't be presented before the cancellation.
			var presented = StorageFile.ExportSheetsPresented;
			completion = CachedFileManager.CompleteUpdatesAsync(file);
			completion.Cancel();

			await Assert.ThrowsAsync<OperationCanceledException>(async () => await completion);
			await WaitUntilIdleAsync();

			Assert.AreEqual(presented, StorageFile.ExportSheetsPresented, "The cancelled sheet was presented.");
			Assert.IsTrue(File.Exists(file.Path));
		}
		finally
		{
			await EndAsync(file, completion);
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25667")]
	public async Task When_Completion_Is_Cancelled_Then_The_Sheet_Closes_And_It_Can_Be_Completed_Again()
	{
		var file = await PickAsync("Report");
		IAsyncOperation<FileUpdateStatus>? completion = null;

		try
		{
			await FileIO.WriteTextAsync(file, "Hello, world!");

			// Cancelled after the sheet was asked for, usually before it appears (the document picker takes seconds): the sheet is then closed
			// once it does, which lets the next one be presented.
			completion = CachedFileManager.CompleteUpdatesAsync(file);
			await Task.Delay(100);
			completion.Cancel();

			await Assert.ThrowsAsync<OperationCanceledException>(async () => await completion);
			await WaitUntilIdleAsync();

			// Cancelled while the sheet is shown.
			completion = CachedFileManager.CompleteUpdatesAsync(file);
			await WaitUntilShownAsync();
			completion.Cancel();

			await Assert.ThrowsAsync<OperationCanceledException>(async () => await completion);
			await WaitUntilIdleAsync();

			Assert.AreEqual("Hello, world!", File.ReadAllText(file.Path));
		}
		finally
		{
			await EndAsync(file, completion);
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25667")]
	public async Task When_Completed_Again_While_The_Sheet_Is_Shown_Then_The_Call_Joins_It_Unless_The_First_Is_Cancelled()
	{
		var file = await PickAsync("Report");
		IAsyncOperation<FileUpdateStatus>? first = null;
		IAsyncOperation<FileUpdateStatus>? second = null;

		try
		{
			var askedFor = StorageFile.ExportSheetsAskedFor;
			first = CachedFileManager.CompleteUpdatesAsync(file);
			await WaitUntilShownAsync();
			var firstGone = StorageFile.ExportSheetsIdle;

			second = CachedFileManager.CompleteUpdatesAsync(file);
			Assert.AreEqual(askedFor + 1, StorageFile.ExportSheetsAskedFor, "The second completion asked for a sheet of its own.");

			// The first caller cancels: its sheet closes, and then the second call shows its own sheet.
			first.Cancel();
			await Assert.ThrowsAsync<OperationCanceledException>(async () => await first);
			await WaitAsync(firstGone, $"The first sheet wasn't gone within {_sheetTimeout.TotalSeconds:0} s.");
			await WaitUntilShownAsync();

			Assert.AreEqual(askedFor + 2, StorageFile.ExportSheetsAskedFor);
			Assert.AreEqual(AsyncStatus.Started, second.Status);

			second.Cancel();
			await Assert.ThrowsAsync<OperationCanceledException>(async () => await second);
		}
		finally
		{
			await EndAsync(file, first, second);
		}
	}

	// The other picker appears only once its service runs, seconds after it is asked for, and UIKit drops the sheet asked for meanwhile.
	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25667")]
	[DataRow(false)]
	[DataRow(true)]
	public Task When_Another_Picker_Is_Still_Appearing_Then_The_Dropped_Sheet_Ends_And_A_Later_One_Is_Shown(bool cancel)
		=> AssertDroppedSheetEndsAsync(documentPicker: true, cancel);

	// The sheet is presented from the other sheet, the top-most controller, which is dismissed while the picker waits for its service.
	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25667")]
	public Task When_The_Presenter_Is_Dismissed_Before_The_Sheet_Appears_Then_The_Dropped_Sheet_Ends_And_A_Later_One_Is_Shown()
		=> AssertDroppedSheetEndsAsync(documentPicker: false, cancel: false);

	// The sheet is presented from the other sheet, the top-most controller. UIKit closes the sheet with it, without calling the picker's delegate.
	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25667")]
	public async Task When_The_Presenter_Is_Dismissed_While_The_Sheet_Is_Shown_Then_It_Is_Incomplete_And_A_Later_Sheet_Is_Shown()
	{
		var file = await PickAsync("Report");
		var dismissOther = StorageFile.PresentOtherSheet(documentPicker: false);
		var isOtherShown = true;
		IAsyncOperation<FileUpdateStatus>? completion = null;
		IAsyncOperation<FileUpdateStatus>? later = null;

		try
		{
			completion = CachedFileManager.CompleteUpdatesAsync(file);
			await WaitUntilShownAsync();

			isOtherShown = false;
			await dismissOther().WaitAsync(_sheetTimeout);

			Assert.AreEqual(FileUpdateStatus.Incomplete, await AsTask(completion).WaitAsync(_sheetTimeout));
			await WaitUntilIdleAsync();

			later = CachedFileManager.CompleteUpdatesAsync(file);
			await WaitUntilShownAsync();
			later.Cancel();
			await Assert.ThrowsAsync<OperationCanceledException>(async () => await later);
		}
		finally
		{
			try
			{
				if (isOtherShown)
				{
					await dismissOther().WaitAsync(_sheetTimeout);
				}
			}
			finally
			{
				await EndAsync(file, completion, later);
			}
		}
	}

	// App code that closes what is presented dismisses the sheet through the controller it was presented from, which stays. UIKit then closes
	// the sheet without calling the picker's delegate.
	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25667")]
	public async Task When_The_Sheet_Is_Dismissed_Through_Its_Presenter_Then_It_Is_Incomplete_And_A_Later_Sheet_Is_Shown()
	{
		var file = await PickAsync("Report");
		IAsyncOperation<FileUpdateStatus>? completion = null;
		IAsyncOperation<FileUpdateStatus>? later = null;

		try
		{
			completion = CachedFileManager.CompleteUpdatesAsync(file);
			await WaitUntilShownAsync();
			await StorageFile.DismissExportSheetThroughPresenter().WaitAsync(_sheetTimeout);

			Assert.AreEqual(FileUpdateStatus.Incomplete, await AsTask(completion).WaitAsync(_sheetTimeout));
			await WaitUntilIdleAsync();

			later = CachedFileManager.CompleteUpdatesAsync(file);
			await WaitUntilShownAsync();
			later.Cancel();
			await Assert.ThrowsAsync<OperationCanceledException>(async () => await later);
		}
		finally
		{
			await EndAsync(file, completion, later);
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25667")]
	public async Task When_Another_File_Is_Completed_Then_It_Is_Complete()
	{
		var path = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, Guid.NewGuid().ToString("N") + ".txt");
		File.WriteAllText(path, "Hello, world!");
		IAsyncOperation<FileUpdateStatus>? completion = null;

		try
		{
			completion = CachedFileManager.CompleteUpdatesAsync(await StorageFile.GetFileFromPathAsync(path));

			Assert.AreEqual(FileUpdateStatus.Complete, await AsTask(completion).WaitAsync(_sheetTimeout));
			Assert.IsTrue(File.Exists(path));
		}
		finally
		{
			completion?.Cancel();
			await WaitUntilIdleAsync();
			File.Delete(path);
		}
	}

	private static async Task<StorageFile> PickAsync(string suggestedFileName, StorageFile? suggestedSaveFile = null)
	{
		var picker = new FileSavePicker { SuggestedFileName = suggestedFileName, SuggestedSaveFile = suggestedSaveFile };
		picker.FileTypeChoices.Add("Plain Text", new List<string> { ".txt", ".text" });

		var file = await picker.PickSaveFileAsync();
		Assert.IsNotNull(file);

		return file;
	}

	private static async Task<StorageFile> PickWithoutChoicesAsync(string suggestedFileName)
	{
		var file = await new FileSavePicker { SuggestedFileName = suggestedFileName }.PickSaveFileAsync();
		Assert.IsNotNull(file);

		return file;
	}

	private static async Task AssertDroppedSheetEndsAsync(bool documentPicker, bool cancel)
	{
		var file = await PickAsync("Report");
		var dismissOther = StorageFile.PresentOtherSheet(documentPicker);
		var isOtherShown = true;
		IAsyncOperation<FileUpdateStatus>? dropped = null;
		IAsyncOperation<FileUpdateStatus>? later = null;

		try
		{
			var presented = StorageFile.ExportSheetsPresented;
			dropped = CachedFileManager.CompleteUpdatesAsync(file);
			await WaitUntilAsync(
				() => StorageFile.ExportSheetsPresented > presented,
				$"The export sheet wasn't presented within {_sheetTimeout.TotalSeconds:0} s.");

			if (!documentPicker)
			{
				isOtherShown = false;
				await dismissOther().WaitAsync(_sheetTimeout);
			}

			if (cancel)
			{
				dropped.Cancel();
				await Assert.ThrowsAsync<OperationCanceledException>(async () => await dropped);
			}
			else
			{
				await Assert.ThrowsAsync<InvalidOperationException>(async () => await AsTask(dropped).WaitAsync(_sheetTimeout));
			}

			// The drop releases the queue, also after a cancellation and while the other picker is still shown.
			await WaitUntilIdleAsync();

			if (isOtherShown)
			{
				isOtherShown = false;
				await dismissOther().WaitAsync(_sheetTimeout);
			}

			later = CachedFileManager.CompleteUpdatesAsync(file);
			await WaitUntilShownAsync();
			later.Cancel();
			await Assert.ThrowsAsync<OperationCanceledException>(async () => await later);
		}
		finally
		{
			try
			{
				if (isOtherShown)
				{
					await dismissOther().WaitAsync(_sheetTimeout);
				}
			}
			finally
			{
				await EndAsync(file, dropped, later);
			}
		}
	}

	private static Task WaitUntilShownAsync()
		=> WaitUntilAsync(() => StorageFile.IsExportSheetShown, $"The export sheet didn't appear within {_sheetTimeout.TotalSeconds:0} s.");

	private static async Task WaitUntilAsync(Func<bool> condition, string timeoutMessage)
	{
		var waited = Stopwatch.StartNew();

		while (!condition())
		{
			if (waited.Elapsed > _sheetTimeout)
			{
				Assert.Fail(timeoutMessage);
			}

			await Task.Delay(100);
		}
	}

	private static Task WaitUntilIdleAsync()
		=> WaitAsync(StorageFile.ExportSheetsIdle, $"An export sheet wasn't gone within {_sheetTimeout.TotalSeconds:0} s.");

	private static async Task WaitAsync(Task task, string timeoutMessage)
	{
		if (await Task.WhenAny(task, Task.Delay(_sheetTimeout)) != task)
		{
			Assert.Fail(timeoutMessage);
		}
	}

	// Cancels what the test left running, so that no sheet stays up during the later tests, then waits for its sheets to be gone.
	private static async Task EndAsync(StorageFile file, params IAsyncOperation<FileUpdateStatus>?[] completions)
	{
		foreach (var completion in completions)
		{
			completion?.Cancel();
		}

		await WaitUntilIdleAsync();
		await file.DeleteAsync();
	}

	private static async Task<T> AsTask<T>(IAsyncOperation<T> operation) => await operation;
}
#endif
