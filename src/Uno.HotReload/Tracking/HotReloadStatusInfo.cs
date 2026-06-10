using System.Collections.Immutable;

namespace Uno.HotReload.Tracking;

public record struct HotReloadStatusInfo(
	HotReloadState State,
	IImmutableList<HotReloadOperationInfo> Operations,
	string? ServerError = null);
