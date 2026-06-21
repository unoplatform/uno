using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Uno.HotReload.Tracking;

namespace Uno.HotReload.IO;

/// <summary>
/// <see cref="IFileEditor"/> implementation that performs file operations directly on disk.
/// </summary>
public class OnDiskFileEditor(IReporter? reporter = null) : IFileEditor
{
	/// <inheritdoc/>
	public ValueTask<(FileUpdateResult Result, string? Error)> EditAsync(FileEdit edit, bool? forceSaveOnDisk, CancellationToken ct)
	{
		return edit switch
		{
			{ OldText: not null, NewText: not null } => UpdateAsync(edit),
			{ OldText: null, NewText: not null } => WriteAsync(edit),
			{ NewText: null, IsCreateDeleteAllowed: true } => DeleteAsync(edit),
			_ => ValueTask.FromResult<(FileUpdateResult, string?)>((FileUpdateResult.BadRequest, "Invalid request"))
		};
	}

	private async ValueTask<(FileUpdateResult, string?)> UpdateAsync(FileEdit edit)
	{
		if (!File.Exists(edit.FilePath))
		{
			reporter?.Verbose($"Requested file '{edit.FilePath}' does not exist.");
			return (FileUpdateResult.FileNotFound, $"Requested file '{edit.FilePath}' does not exist.");
		}

		var originalContent = await File.ReadAllTextAsync(edit.FilePath);
		var updatedContent = originalContent.Replace(edit.OldText!, edit.NewText!);

		if (updatedContent == originalContent)
		{
			reporter?.Verbose($"No changes detected in {edit.FilePath}.");
			return (FileUpdateResult.NoChanges, null);
		}

		var effectiveUpdate = FileSystemHelper.WaitForFileUpdated(edit.FilePath, reporter);
		await File.WriteAllTextAsync(edit.FilePath, updatedContent);
		await effectiveUpdate;

		return (FileUpdateResult.Success, null);
	}

	private async ValueTask<(FileUpdateResult, string?)> WriteAsync(FileEdit edit)
	{
		if (!edit.IsCreateDeleteAllowed && !File.Exists(edit.FilePath))
		{
			reporter?.Verbose($"Requested file '{edit.FilePath}' does not exist.");
			return (FileUpdateResult.FileNotFound, $"Requested file '{edit.FilePath}' does not exist.");
		}

		var effectiveUpdate = FileSystemHelper.WaitForFileUpdated(edit.FilePath, reporter);
		await File.WriteAllTextAsync(edit.FilePath, edit.NewText!);
		await effectiveUpdate;

		return (FileUpdateResult.Success, null);
	}

	private async ValueTask<(FileUpdateResult, string?)> DeleteAsync(FileEdit edit)
	{
		if (!File.Exists(edit.FilePath))
		{
			reporter?.Verbose($"Requested file '{edit.FilePath}' does not exist.");
			return (FileUpdateResult.FileNotFound, $"Requested file '{edit.FilePath}' does not exist.");
		}

		var effectiveUpdate = FileSystemHelper.WaitForFileUpdated(edit.FilePath, reporter);
		File.Delete(edit.FilePath);
		await effectiveUpdate;

		return (FileUpdateResult.Success, null);
	}
}
