#if UNO_HOTRELOAD
namespace Uno.HotReload.IO;
#else
namespace Uno.UI.RemoteControl.HotReload;
#endif

/// <summary>
/// Represents a file edit operation.
/// </summary>
/// <param name="FilePath">Gets or sets the file system path to edit.</param>
/// <param name="OldText">The old text to replace in the file, or <c>null</c> to create a new file (only if <paramref name="IsCreateDeleteAllowed"/> is <c>true</c>).</param>
/// <param name="NewText">The new text to replace in the file, or <c>null</c> to delete the file (only if <paramref name="IsCreateDeleteAllowed"/> is <c>true</c>).</param>
/// <param name="IsCreateDeleteAllowed">Indicates if the file can be created or deleted.</param>
public record FileEdit(
	string FilePath,
	string? OldText,
	string? NewText,
	bool IsCreateDeleteAllowed = false
);
