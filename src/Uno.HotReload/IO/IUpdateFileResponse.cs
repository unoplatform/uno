using System.Collections.Immutable;

namespace Uno.HotReload.IO;

/// <summary>
/// Response from a file update operation performed by <see cref="FileUpdater"/>.
/// </summary>
public interface IUpdateFileResponse
{
	string RequestId { get; }

	string? GlobalError { get; }

	ImmutableArray<FileEditResult> Results { get; }

	long? HotReloadCorrelationId { get; }
}
