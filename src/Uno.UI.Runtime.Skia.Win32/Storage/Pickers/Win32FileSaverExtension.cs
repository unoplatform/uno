using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;
using Uno.Disposables;
using Uno.Extensions.Storage.Pickers;
using Uno.Foundation.Logging;
using Uno.UI.Helpers.WinUI;
using Uno.UI.Helpers;
using System.Runtime.InteropServices;
using System.IO;

namespace Uno.UI.Runtime.Skia.Win32;

internal class Win32FileSaverExtension(FileSavePicker picker) : IFileSavePickerExtension
{
	public unsafe Task<StorageFile?> PickSaveFileAsync(CancellationToken token)
	{
		using ComScope<IFileSaveDialog> iFileSaveDialog = default;
		var fileSaveDialogClsid = CLSID.FileSaveDialog;
		var iFileSaveDialogRiid = IFileSaveDialog.IID_Guid;
		var hResult = PInvoke.CoCreateInstance(
			&fileSaveDialogClsid,
			null,
			CLSCTX.CLSCTX_ALL,
			&iFileSaveDialogRiid,
			iFileSaveDialog);

		if (hResult.Failed)
		{
			this.LogError()?.Error($"{nameof(PInvoke.CoCreateInstance)} failed: {Win32Helper.GetErrorMessage(hResult)}");
			return Task.FromResult<StorageFile?>(null);
		}

		hResult = iFileSaveDialog.Value->GetOptions(out var dialogOptions);
		if (hResult.Failed)
		{
			this.LogError()?.Error($"{nameof(IFileDialog.GetOptions)} failed: {Win32Helper.GetErrorMessage(hResult)}");
			return Task.FromResult<StorageFile?>(null);
		}

		dialogOptions |=
			FILEOPENDIALOGOPTIONS.FOS_NOCHANGEDIR
			| FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM
			| FILEOPENDIALOGOPTIONS.FOS_FILEMUSTEXIST
			| FILEOPENDIALOGOPTIONS.FOS_PATHMUSTEXIST
			| FILEOPENDIALOGOPTIONS.FOS_STRICTFILETYPES;

		hResult = iFileSaveDialog.Value->SetOptions(dialogOptions);
		if (hResult.Failed)
		{
			this.LogError()?.Error($"{nameof(IFileDialog.SetOptions)} failed: {Win32Helper.GetErrorMessage(hResult)}");
			return Task.FromResult<StorageFile?>(null);
		}

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

		if (!string.IsNullOrWhiteSpace(picker.SuggestedFileName))
		{
			iFileSaveDialog.Value->SetFileName(picker.SuggestedFileName);
		}

		if (picker.SuggestedStartLocation != PickerLocationId.Unspecified)
		{
			hResult = default;
			if (picker.SuggestedStartLocation == PickerLocationId.ComputerFolder)
			{
				var folderGuid = PickerHelpers.WindowsComputerFolderGUID;

				hResult = PInvoke.SHCreateItemInKnownFolder(folderGuid, KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT, null, IShellItem.IID_Guid, out var defaultFolderItemRaw);

				if (hResult.Failed)
				{
					this.LogError()?.Error($"{nameof(PInvoke.SHCreateItemInKnownFolder)} failed: {Win32Helper.GetErrorMessage(hResult)}");
					return Task.FromResult<StorageFile?>(null);
				}

				using ComScope<IShellItem> defaultFolderItem = new((IShellItem*)defaultFolderItemRaw);

				hResult = iFileSaveDialog.Value->SetDefaultFolder(defaultFolderItem);
				if (hResult.Failed)
				{
					this.LogError()?.Error($"{nameof(IFileDialog.SetDefaultFolder)} failed: {Win32Helper.GetErrorMessage(hResult)}");
					return Task.FromResult<StorageFile?>(null);
				}
			}
			else
			{
				var initialDirectory = PickerHelpers.GetInitialDirectory(picker.SuggestedStartLocation);

				if (!string.IsNullOrEmpty(initialDirectory))
				{
					hResult = PInvoke.SHCreateItemFromParsingName(initialDirectory, null, IShellItem.IID_Guid, out var defaultFolderItemRaw);

					if (hResult.Failed)
					{
						this.LogError()?.Error($"{nameof(PInvoke.SHCreateItemFromParsingName)} failed: {Win32Helper.GetErrorMessage(hResult)}");
						return Task.FromResult<StorageFile?>(null);
					}

					using ComScope<IShellItem> defaultFolderItem = new((IShellItem*)defaultFolderItemRaw);

					hResult = iFileSaveDialog.Value->SetDefaultFolder(defaultFolderItem);
					if (hResult.Failed)
					{
						this.LogError()?.Error($"{nameof(IFileDialog.SetDefaultFolder)} failed: {Win32Helper.GetErrorMessage(hResult)}");
						return Task.FromResult<StorageFile?>(null);
					}
				}
			}
		}

		if (fileTypeList.Count > 0)
		{
			hResult = iFileSaveDialog.Value->SetFileTypes(fileTypeList
				.Select(t => new COMDLG_FILTERSPEC { pszName = t.friendlyName, pszSpec = t.pattern })
				.ToArray()
				.AsSpan());
			if (hResult.Failed)
			{
				this.LogError()?.Error($"{nameof(IFileDialog.SetFileTypes)} failed: {Win32Helper.GetErrorMessage(hResult)}");
				return Task.FromResult<StorageFile?>(null);
			}

			// Set default extension (e.g., "txt" from "*.txt")
			var firstPattern = !string.IsNullOrEmpty(picker.DefaultFileExtension) ? picker.DefaultFileExtension : picker.FileTypeChoices.First().Value.FirstOrDefault();
			if (!string.IsNullOrEmpty(firstPattern) && firstPattern.StartsWith('.'))
			{
				hResult = iFileSaveDialog.Value->SetDefaultExtension(firstPattern.TrimStart('.'));
				if (hResult.Failed)
				{
					this.LogError()?.Error($"{nameof(IFileDialog.SetDefaultExtension)} failed: {Win32Helper.GetErrorMessage(hResult)}");
					return Task.FromResult<StorageFile?>(null);
				}
			}
		}

		hResult = iFileSaveDialog.Value->SetOkButtonLabel(string.IsNullOrEmpty(picker.CommitButtonText) ? ResourceAccessor.GetLocalizedStringResource("FILE_SAVER_ACCEPT_LABEL") : picker.CommitButtonText);
		if (hResult.Failed)
		{
			this.LogError()?.Error($"{nameof(IFileDialog.SetOkButtonLabel)} failed: {Win32Helper.GetErrorMessage(hResult)}");
			return Task.FromResult<StorageFile?>(null);
		}

		var hwnd = PInvoke.GetActiveWindow();
		if (hwnd == IntPtr.Zero)
		{
			hwnd = Win32WindowWrapper.GetHwnds().First();
		}
		hResult = iFileSaveDialog.Value->Show(hwnd);
		if (hResult.Failed)
		{
			if (hResult != (uint)WIN32_ERROR.ERROR_CANCELLED)
			{
				this.LogError()?.Error($"{nameof(IFileDialog.Show)} failed: {Win32Helper.GetErrorMessage(hResult)}");
			}
			return Task.FromResult<StorageFile?>(null);
		}

		using ComScope<IShellItem> iShellItem = default;
		hResult = iFileSaveDialog.Value->GetResult(iShellItem);
		if (hResult.Failed)
		{
			if (hResult != (uint)WIN32_ERROR.ERROR_CANCELLED)
			{
				this.LogError()?.Error($"{nameof(IFileDialog.GetResult)} failed: {Win32Helper.GetErrorMessage(hResult)}");
			}
			return Task.FromResult<StorageFile?>(null);
		}

		char* resultName = stackalloc char[Win32FileFolderPickerExtension.FilePathBuffer];
		iShellItem.Value->GetDisplayName(SIGDN.SIGDN_FILESYSPATH, (PWSTR*)&resultName);
		if (hResult.Failed)
		{
			this.LogError()?.Error($"{nameof(IShellItem.GetDisplayName)} failed: {Win32Helper.GetErrorMessage(hResult)}");
			return Task.FromResult<StorageFile?>(null);
		}

		var path = new string(resultName);
		try
		{
			// FileSavePicker creates the file if it does not exist yet.
			if (!File.Exists(path))
			{
				File.Create(path).Dispose();
			}
		}
		catch (Exception ex)
		{
			this.LogError()?.Error($"Could not create file at '{path}'", ex);
		}

		var file = StorageFile.GetFileFromPath(path);
		return Task.FromResult<StorageFile?>(file);
	}

	private List<(Win32Helper.NativeNulTerminatedUtf16String friendlyName, Win32Helper.NativeNulTerminatedUtf16String pattern)> GetFilterString()
	{
		var ret = new List<(Win32Helper.NativeNulTerminatedUtf16String friendlyName, Win32Helper.NativeNulTerminatedUtf16String pattern)>();
		foreach (var entry in picker.FileTypeChoices)
		{
			List<string> patternList = new();

			foreach (var pattern in entry.Value)
			{
				if (pattern.StartsWith('.') && pattern[1..] is var ext && ext.All(char.IsLetterOrDigit))
				{
					patternList.Add($"*{pattern}");
				}
				else
				{
					this.LogError()?.Error($"Skipping invalid file extension pattern '{pattern}' for key {entry.Key}");
				}
			}

			ret.Add((new Win32Helper.NativeNulTerminatedUtf16String(entry.Key), new Win32Helper.NativeNulTerminatedUtf16String(string.Join(";", patternList))));
		}

		return ret;
	}
}
