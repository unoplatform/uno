using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.HotReload.IO;

namespace Uno.UI.RemoteControl.Host.HotReload;

/// <summary>
/// <see cref="IFileEditor"/> implementation that delegates file updates to the IDE (e.g. Visual Studio)
/// for operations it supports, and falls back to an inner <see cref="IFileEditor"/> for others (e.g. delete).
/// </summary>
internal class IDEFileEditor(
	Func<string, string, bool, ValueTask<(bool IsSuccess, string? Error)>> updateFileInIde,
	IFileEditor inner) : IFileEditor
{
	/// <inheritdoc/>
	public ValueTask<(FileUpdateResult Result, string? Error)> EditAsync(FileEdit edit, bool? forceSaveOnDisk, CancellationToken ct)
	{
		return edit switch
		{
			// IDE supports update and write (anything with NewText)
			{ NewText: not null } => RemoteUpdateInIdeAsync(edit, forceSaveOnDisk),
			// Delegate everything else (e.g. delete) to the inner editor
			_ => inner.EditAsync(edit, forceSaveOnDisk, ct)
		};
	}

	private async ValueTask<(FileUpdateResult, string?)> RemoteUpdateInIdeAsync(FileEdit edit, bool? forceSaveOnDisk)
	{
		// Temporary set to true until this issue is fixed: https://github.com/unoplatform/uno.hotdesign/issues/3454
		var saveToDisk = forceSaveOnDisk ?? true;

		var (isSuccess, error) = await updateFileInIde(edit.FilePath, edit.NewText!, saveToDisk);
		return isSuccess
			? (FileUpdateResult.Success, (string?)null)
			: (FileUpdateResult.Failed, error);
	}
}
