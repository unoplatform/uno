using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Uno.UI.DevServer.Cli.Helpers;

/// <summary>
/// Represents one hop in a process ancestry chain used for diagnostics.
/// </summary>
public sealed class ProcessChainEntry
{
	public int ProcessId { get; init; }

	public string? ProcessName { get; init; }

	/// <summary>
	/// Formats a process chain for display. The chain is reversed so that the
	/// top-level ancestor (e.g. the IDE) appears first and the DevServer Host
	/// appears last. Well-known verbose names are shortened for readability.
	/// </summary>
	public static string FormatChain(IEnumerable<ProcessChainEntry> chain)
		=> string.Join(
			" → ",
			chain.Reverse().Select(entry =>
			{
				var name = ShortenProcessName(entry.ProcessName);
				var pid = entry.ProcessId.ToString(CultureInfo.InvariantCulture);
				return string.IsNullOrWhiteSpace(name)
					? pid
					: $"{name} ({pid})";
			}));

	public static string? ShortenProcessName(string? name)
		=> name is not null && name.StartsWith("Uno.UI.RemoteControl.Host", StringComparison.OrdinalIgnoreCase)
			? "Host"
			: name;
}
