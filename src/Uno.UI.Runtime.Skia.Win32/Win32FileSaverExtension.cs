using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Controls.Dialogs;
using Uno.Extensions.Storage.Pickers;
using Uno.Foundation.Logging;
using Uno.UI.Helpers;

namespace Uno.UI.Runtime.Skia.Win32;

internal class Win32FileSaverExtension(FileSavePicker picker) : IFileSavePickerExtension
{
	public unsafe Task<StorageFile?> PickSaveFileAsync(CancellationToken token)
	{
		using var initialDir = new Win32Helper.NativeNulTerminatedUtf16String(PickerHelpers.GetInitialDirectory(picker.SuggestedStartLocation));
		using var filters = new Win32Helper.NativeNulTerminatedUtf16String(GetFilterString());
		var fileNameBuffer = stackalloc char[Win32FileFolderPickerExtension.FilePathBuffer];

		var ofn = new OPENFILENAMEW
		{
			lStructSize = (uint)Marshal.SizeOf<OPENFILENAMEW>(),
			lpstrInitialDir = initialDir,
			lpstrFilter = filters,
			lpstrFile = new PWSTR(fileNameBuffer),
			nMaxFile = Win32FileFolderPickerExtension.FilePathBuffer,
			Flags = OPEN_FILENAME_FLAGS.OFN_EXPLORER
		};

		if (!PInvoke.GetSaveFileName(ref ofn))
		{
			_ = this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.GetOpenFileName)} failed: {PInvoke.CommDlgExtendedError()}");
			return Task.FromResult<StorageFile?>(null);
		}

		return Task.FromResult<StorageFile?>(StorageFile.GetFileFromPath(Marshal.PtrToStringUni((IntPtr)fileNameBuffer)!));
	}

	private string? GetFilterString()
	{
		var builder = new StringBuilder();
		foreach (var entry in picker.FileTypeChoices)
		{
			builder.Append($"{entry.Key}\0");
			var extensions = string.Join(
				';',
				entry.Value
					.Where(pattern => pattern.StartsWith('.') && pattern[1..] is var ext && ext.All(char.IsLetterOrDigit))
					.Select(pattern => $"*{pattern}")
				);
			if (string.IsNullOrEmpty(extensions))
			{
				_ = this.Log().Log(LogLevel.Error, entry, static entry => $"Skipping invalid file extension filter entry: '{entry}'");
			}
			else
			{
				builder.Append(extensions).Append('\0');
			}
		}

		return builder.Length > 0 ? builder.Append('\0').ToString() : null;
	}
}
