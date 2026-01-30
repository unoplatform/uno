using System;
using System.Collections.Immutable;

namespace Uno.HotReload.Tracking;

public record struct HotReloadOperationInfo(
	long Id,
	DateTimeOffset StartTime,
	ImmutableHashSet<string> FilePaths,
	ImmutableHashSet<string>? IgnoredFilePaths,
	DateTimeOffset? EndTime,
	HotReloadOperationResult? Result,
	IImmutableList<string>? Diagnostics);
