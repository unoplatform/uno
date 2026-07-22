#nullable enable

#if UNO_HOTRELOAD
namespace Uno.HotReload.IO;
#elif UNO_RC_MESSAGING
namespace Uno.UI.RemoteControl.Messaging.IdeChannel.HotReload;
#else
using Uno.UI.RemoteControl.HotReload.Messages;

namespace Uno.UI.RemoteControl.HotReload;
#endif

/// <summary>
/// Describes the result of a single <see cref="FileEdit"/>.
/// </summary>
public record FileEditResult(
	string FilePath,
	FileUpdateResult Result,
	string? Error);
