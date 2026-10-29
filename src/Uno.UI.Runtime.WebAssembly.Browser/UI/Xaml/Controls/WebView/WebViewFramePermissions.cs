#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.UI.Runtime;

/// <summary>
/// Builds the permission attributes of the iframe that shows a WebView2 on WebAssembly.
/// </summary>
/// <remarks>
/// It has no browser dependency, so Uno.UI.UnitTests links this file and tests it directly.
/// </remarks>
internal static class WebViewFramePermissions
{
	private const string Fullscreen = "fullscreen";

	private static readonly char[] _asciiWhitespace = [' ', '\t', '\n', '\f', '\r'];

	/// <summary>
	/// Returns the value of the iframe's allow attribute, or null if it has no directive, and whether the iframe gets allowfullscreen.
	/// </summary>
	/// <param name="policy">The Permissions Policy directives, as in <see cref="FeatureConfiguration.WebView2.PermissionsPolicy"/>.</param>
	internal static (string? Allow, bool AllowFullscreen) GetFrameAttributes(IEnumerable<string?> policy)
	{
		var directives = new List<(string Feature, string Allowlist, bool IsForEveryOrigin)>();
		var indexes = new Dictionary<string, int>(StringComparer.Ordinal);

		foreach (var entry in policy)
		{
			if (entry is null)
			{
				continue;
			}

			// Split as the browser splits the attribute, so that a directive is read here as the browser will read it.
			foreach (var text in entry.Split(';'))
			{
				var tokens = text.Split(_asciiWhitespace, StringSplitOptions.RemoveEmptyEntries);
				if (tokens.Length == 0)
				{
					continue;
				}

				// A bare feature would only be allowed for the frame's declared origin (its src, or the app's own origin without one),
				// so it's written with *.
				var directive = tokens.Length == 1
					? (tokens[0], "*", true)
					: (tokens[0], string.Join(' ', tokens, 1, tokens.Length - 1), Array.IndexOf(tokens, "*", 1) != -1);

				// The browser keeps the first directive for a feature; replace it here so a later entry, such as an added
				// autoplay 'none', overrides a default.
				if (indexes.TryGetValue(tokens[0], out var index))
				{
					directives[index] = directive;
				}
				else
				{
					indexes.Add(tokens[0], directives.Count);
					directives.Add(directive);
				}
			}
		}

		// allowfullscreen alone means fullscreen for every origin, and Chrome warns when allow has a fullscreen directive as well.
		var allowFullscreen = indexes.TryGetValue(Fullscreen, out var fullscreenIndex) && directives[fullscreenIndex].IsForEveryOrigin;
		var allow = string.Join("; ", directives
			.Where(directive => !(allowFullscreen && directive.Feature == Fullscreen))
			.Select(directive => $"{directive.Feature} {directive.Allowlist}"));

		return (allow.Length > 0 ? allow : null, allowFullscreen);
	}
}
