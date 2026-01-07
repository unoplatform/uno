using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Disposables;
using Uno.Extensions.Storage.Pickers;
using Uno.Foundation.Logging;
using Uno.UI.Helpers;
using Uno.UI.Helpers.WinUI;
using Uno.UI.Runtime.Skia.Win32.Storage.Pickers;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;

namespace Uno.UI.Runtime.Skia.Win32;

internal class Win32FileFolderPickerExtension(IFilePicker picker) : IFileOpenPickerExtension, IFolderPickerExtension
{
	internal const int FilePathBuffer = 1000;

	public Task<StorageFile?> PickSingleFileAsync(CancellationToken token)
		=> PickFiles(false, true).ContinueWith(task => task.Result.Count == 0 ? null : task.Result[0], token);

	public Task<IReadOnlyList<StorageFile>> PickMultipleFilesAsync(CancellationToken token)
		=> PickFiles(false, false);

	public Task<StorageFolder?> PickSingleFolderAsync(CancellationToken token)
		=> PickFiles(true, true).ContinueWith(task => task.Result.Select(file => StorageFolder.GetFolderFromPathAsync(file.Path).GetResults()).FirstOrDefault((StorageFolder?)null), token);

	private unsafe Task<IReadOnlyList<StorageFile>> PickFiles(bool directory, bool single)
	{
		using ComScope<IFileOpenDialog> iFileOpenDialog = default;
		var fileOpenDialogClsid = CLSID.FileOpenDialog;
		var iFileOpenDialogRiid = IFileOpenDialog.IID_Guid;
		var hResult = PInvoke.CoCreateInstance(
			&fileOpenDialogClsid,
			null,
			CLSCTX.CLSCTX_ALL,
			&iFileOpenDialogRiid,
			iFileOpenDialog);

		if (hResult.Failed)
		{
			this.LogError()?.Error($"{nameof(PInvoke.CoCreateInstance)} failed: {Win32Helper.GetErrorMessage(hResult)}");
			return Task.FromResult<IReadOnlyList<StorageFile>>([]);
		}

		hResult = iFileOpenDialog.Value->GetOptions(out var dialogOptions);
		if (hResult.Failed)
		{
			this.LogError()?.Error($"{nameof(IFileDialog.GetOptions)} failed: {Win32Helper.GetErrorMessage(hResult)}");
			return Task.FromResult<IReadOnlyList<StorageFile>>([]);
		}

		dialogOptions |=
			FILEOPENDIALOGOPTIONS.FOS_NOCHANGEDIR
			| FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM
			| FILEOPENDIALOGOPTIONS.FOS_FILEMUSTEXIST
			| FILEOPENDIALOGOPTIONS.FOS_PATHMUSTEXIST
			| (directory ? FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS : 0)
			| (single ? 0 : FILEOPENDIALOGOPTIONS.FOS_ALLOWMULTISELECT);

		hResult = iFileOpenDialog.Value->SetOptions(dialogOptions);
		if (hResult.Failed)
		{
			this.LogError()?.Error($"{nameof(IFileDialog.SetOptions)} failed: {Win32Helper.GetErrorMessage(hResult)}");
			return Task.FromResult<IReadOnlyList<StorageFile>>([]);
		}

		if (!directory)
		{
			var fileTypeList = GetFilterString();
			using var fileTypeListDisposable =
				new DisposableStruct<List<(Win32Helper.NativeNulTerminatedUtf16String friendlyName,
					Win32Helper.NativeNulTerminatedUtf16String pattern)>>(
					static list =>
					{
						foreach (var (name, pattern) in list)
						{
							name.Dispose();
							pattern.Dispose();
						}
					}, fileTypeList);

			hResult = iFileOpenDialog.Value->SetFileTypes(fileTypeList
				.Select(t => new COMDLG_FILTERSPEC { pszName = t.friendlyName, pszSpec = t.pattern })
				.ToArray()
				.AsSpan());
			if (hResult.Failed)
			{
				this.LogError()?.Error($"{nameof(IFileDialog.SetFileTypes)} failed: {Win32Helper.GetErrorMessage(hResult)}");
				return Task.FromResult<IReadOnlyList<StorageFile>>([]);
			}
		}

		using var defaultFolder = SuggestedStartLocationHandler.GetStartLocationShellItem(picker.SuggestedStartLocationInternal);
		if (defaultFolder != default)
		{
			iFileOpenDialog.Value->SetDefaultFolder(defaultFolder);
		}

		hResult = iFileOpenDialog.Value->SetOkButtonLabel(string.IsNullOrEmpty(picker.CommitButtonTextInternal) ? ResourceAccessor.GetLocalizedStringResource("FILE_PICKER_ACCEPT_LABEL") : picker.CommitButtonTextInternal);
		if (hResult.Failed)
		{
			this.LogError()?.Error($"{nameof(IFileDialog.SetOkButtonLabel)} failed: {Win32Helper.GetErrorMessage(hResult)}");
			return Task.FromResult<IReadOnlyList<StorageFile>>([]);
		}

		var hwnd = PInvoke.GetActiveWindow();
		if (hwnd == IntPtr.Zero)
		{
			var hwnds = Win32WindowWrapper.GetHwnds();
			if (!hwnds.Any())
			{
				this.LogError()?.Error("No window handles available for file picker dialog");
				return Task.FromResult<IReadOnlyList<StorageFile>>([]);
			}
			hwnd = hwnds.First();
		}
		this.LogDebug()?.Debug($"Showing file dialog with hwnd: {hwnd.Value:X}");
		hResult = iFileOpenDialog.Value->Show(hwnd);
		this.LogDebug()?.Debug($"File dialog returned with hResult: {hResult}");
		if (hResult.Failed)
		{
			if (hResult != (uint)WIN32_ERROR.ERROR_CANCELLED)
			{
				this.LogError()?.Error($"{nameof(IFileDialog.Show)} failed: {Win32Helper.GetErrorMessage(hResult)}");
			}
			return Task.FromResult<IReadOnlyList<StorageFile>>([]);
		}

		using ComScope<IShellItemArray> iShellItemArray = default;
		hResult = iFileOpenDialog.Value->GetResults(iShellItemArray);
		if (hResult.Failed)
		{
			this.LogError()?.Error($"{nameof(IFileOpenDialog.GetResults)} failed: {Win32Helper.GetErrorMessage(hResult)}");
			return Task.FromResult<IReadOnlyList<StorageFile>>([]);
		}

		hResult = iShellItemArray.Value->GetCount(out var count);
		if (hResult.Failed)
		{
			this.LogError()?.Error($"{nameof(IShellItemArray.GetCount)} failed: {Win32Helper.GetErrorMessage(hResult)}");
			return Task.FromResult<IReadOnlyList<StorageFile>>([]);
		}

		var items = new IShellItem*[count];
		using var itemsDisposable =
			new DisposableStruct<IShellItem*[]>(
				static list =>
				{
					foreach (var ptr in list)
					{
						if (ptr is null)
						{
							continue;
						}
						var hResult = (HRESULT)ptr->Release();
						if (hResult.Failed) { typeof(Win32FileFolderPickerExtension).LogError()?.Error($"{nameof(IUnknown.Release)} failed: {Win32Helper.GetErrorMessage(hResult)}"); }
					}
				}, items);

		var ret = new List<StorageFile>();
		char* resultName = stackalloc char[FilePathBuffer];
		for (var i = 0; i < count; i++)
		{
			IShellItem* temp = default;
			hResult = iShellItemArray.Value->GetItemAt((uint)i, &temp);
			if (hResult.Failed)
			{
				this.LogError()?.Error($"{nameof(IShellItemArray.GetItemAt)} failed: {Win32Helper.GetErrorMessage(hResult)}");
				return Task.FromResult<IReadOnlyList<StorageFile>>([]);
			}
			items[i] = temp;

			temp->GetDisplayName(SIGDN.SIGDN_FILESYSPATH, (PWSTR*)&resultName);
			if (hResult.Failed)
			{
				this.LogError()?.Error($"{nameof(IShellItem.GetDisplayName)} failed: {Win32Helper.GetErrorMessage(hResult)}");
				continue;
			}

			ret.Add(StorageFile.GetFileFromPath(new string(resultName)));
		}

		return Task.FromResult<IReadOnlyList<StorageFile>>(ret);
	}

	private List<(Win32Helper.NativeNulTerminatedUtf16String friendlyName, Win32Helper.NativeNulTerminatedUtf16String pattern)> GetFilterString()
	{
		var list = new List<(Win32Helper.NativeNulTerminatedUtf16String, Win32Helper.NativeNulTerminatedUtf16String)>();

		var hasAnyFilePattern = false;
		var wildcardPatterns = new List<string>(picker.FileTypeFilterInternal.Count);
		foreach (var pattern in picker.FileTypeFilterInternal.Distinct())
		{
			if (pattern is null)
			{
				continue;
			}

			if (pattern == "*")
			{
				hasAnyFilePattern = true;
			}
			else if (pattern.StartsWith('.') && pattern[1..] is var ext && ext.All(char.IsLetterOrDigit))
			{
				list.Add((new Win32Helper.NativeNulTerminatedUtf16String($"{ext.ToUpperInvariant()} files"), new Win32Helper.NativeNulTerminatedUtf16String($"*.{ext}")));
				wildcardPatterns.Add($"*.{ext}");
			}
			else
			{
				this.LogError()?.Error($"Skipping invalid file extension filter: '{pattern}'");
			}
		}

		if (hasAnyFilePattern || list.Count == 0)
		{
			list.Insert(0, (new Win32Helper.NativeNulTerminatedUtf16String("All files"), new Win32Helper.NativeNulTerminatedUtf16String("*")));
		}
		else if (wildcardPatterns.Count > 1)
		{
			// If there are multiple patterns, add an "All Files" entry with all patterns merged.
			var allPatterns = string.Join(';', wildcardPatterns);
			list.Insert(0, (new Win32Helper.NativeNulTerminatedUtf16String("All files"), new Win32Helper.NativeNulTerminatedUtf16String(allPatterns)));
		}

		return list;
	}
}
