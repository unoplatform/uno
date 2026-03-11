using System;
using System.Diagnostics.CodeAnalysis;
using Uno.Extensions;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.Messaging.IdeChannel;

namespace Uno.UI.RemoteControl.HotReload;

/// <summary>
/// Describes the result of a single <see cref="FileEdit"/>.
/// </summary>
public record FileEditResult(
	string FilePath,
	FileUpdateResult Result,
	string? Error);
