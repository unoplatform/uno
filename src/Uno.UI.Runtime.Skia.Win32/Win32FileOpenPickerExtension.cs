using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

internal class Win32FileOpenPickerExtension(IFilePicker picker) : IFileOpenPickerExtension
{
	public Task<StorageFile?> PickSingleFileAsync(CancellationToken token)
		=> PickFiles(true).ContinueWith(task => task.Result.Count == 0 ? null : task.Result[0], token);

	public Task<IReadOnlyList<StorageFile>> PickMultipleFilesAsync(CancellationToken token)
		=> PickFiles(false);

	private unsafe Task<IReadOnlyList<StorageFile>> PickFiles(bool single)
	{
		using var initialDir = new Win32Helper.NativeNulTerminatedUtf16String(PickerHelpers.GetInitialDirectory(picker.SuggestedStartLocationInternal));
		using var filters = new Win32Helper.NativeNulTerminatedUtf16String(GetFilterString());
		const int nMaxFile = 1000;
		var fileNameBuffer = stackalloc char[nMaxFile];

		var ofn = new OPENFILENAMEW
		{
			lStructSize = (uint)Marshal.SizeOf<OPENFILENAMEW>(),
			lpstrInitialDir = initialDir,
			lpstrFilter = filters,
			lpstrFile = new PWSTR(fileNameBuffer),
			nMaxFile = nMaxFile,
			Flags = (single ? 0 : OPEN_FILENAME_FLAGS.OFN_ALLOWMULTISELECT) | OPEN_FILENAME_FLAGS.OFN_EXPLORER
		};
		if (!PInvoke.GetOpenFileName(ref ofn))
		{
			_ = this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.GetOpenFileName)} failed: {PInvoke.CommDlgExtendedError()}");
			return Task.FromResult<IReadOnlyList<StorageFile>>([]);
		}


		if (single)
		{
			return Task.FromResult<IReadOnlyList<StorageFile>>([StorageFile.GetFileFromPath(Marshal.PtrToStringUni((IntPtr)fileNameBuffer)!)]);
		}
		else
		{
			var span = new Span<char>(fileNameBuffer, nMaxFile);
			var currentRun = 0;
			var paths = new List<string>();
			for (int i = 0; i < nMaxFile; i++)
			{
				if (span[i] == '\0')
				{
					paths.Add(new string(fileNameBuffer + currentRun));
					currentRun = i + 1;

					if (i + 1 < nMaxFile && span[i + 1] == '\0')
					{
						break;
					}
				}
			}

			if (paths.Count == 1)
			{
				return Task.FromResult<IReadOnlyList<StorageFile>>([StorageFile.GetFileFromPath(Marshal.PtrToStringUni((IntPtr)fileNameBuffer)!)]);
			}
			else
			{
				var dir = paths[0];
				var completePaths = paths.Skip(1).Select(file => StorageFile.GetFileFromPath($"{dir}\\{file}"));
				return Task.FromResult<IReadOnlyList<StorageFile>>(completePaths.ToImmutableList());
			}
		}
	}

	private string GetFilterString()
	{
		var builder = new StringBuilder();
		foreach (var pattern in picker.FileTypeFilterInternal.Distinct())
		{
			if (pattern is null)
			{
				continue;
			}

			if (pattern == "*")
			{
				builder.Append("All Files\0*.*\0");
			}
			else if (pattern.StartsWith('.') && pattern[1..] is var ext && ext.All(char.IsLetterOrDigit))
			{
				builder.Append($"{ext.ToUpperInvariant()} Files\0*.{ext}\0");
			}
			else
			{
				_ = this.Log().Log(LogLevel.Error, pattern, static pattern => $"Skipping invalid file extension filter: '{pattern}'");
			}
		}

		return builder.Length > 0 ? builder.Append('\0').ToString() : "All Files\0*.*\0\0";
	}
}
