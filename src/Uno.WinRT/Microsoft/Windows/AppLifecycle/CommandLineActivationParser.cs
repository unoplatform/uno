#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Uno.Foundation.Logging;

namespace Microsoft.Windows.AppLifecycle;

/// <summary>
/// Reads a protocol activation from the process command line, using the Windows App SDK's
/// <c>App.exe "----ms-protocol:&lt;uri&gt;"</c> format so a scheme registered by either toolchain works with both.
/// </summary>
/// <remarks>
/// Ported from <c>ParseCommandLine</c> and <c>GetActivationArguments</c> in the Windows App SDK's
/// <c>dev/AppLifecycle/AppInstance.cpp</c>.
/// </remarks>
internal static class CommandLineActivationParser
{
	internal const string ArgumentPrefix = "----";
	internal const string ArgumentSuffix = ":";
	internal const string ProtocolMarker = "ms-protocol";
	internal const string PushMarker = "WindowsAppRuntimePushServer";
	internal const string AppNotificationMarker = "AppNotificationActivated";

	// Tried in this order, matching AppInstance.cpp.
	private static readonly string[] _markers = [ProtocolMarker, PushMarker, AppNotificationMarker];

	/// <summary>
	/// Looks for a protocol activation in the process arguments.
	/// </summary>
	/// <param name="arguments">The process arguments, without the executable.</param>
	/// <param name="allowBareUri">
	/// Whether a lone absolute URI argument counts as an activation. Linux launchers substitute the URI
	/// straight into a desktop entry's <c>Exec=App %u</c>, whereas on Windows a bare URI is indistinguishable
	/// from one the user typed.
	/// </param>
	/// <param name="uri">The activation URI, when there is one.</param>
	/// <param name="remainingArguments">
	/// The arguments with the activation token removed, for the app's <c>OnLaunched</c>; <c>null</c> when
	/// nothing was removed, so the original command line can be used as is.
	/// </param>
	internal static bool TryParse(
		IReadOnlyList<string> arguments,
		bool allowBareUri,
		[NotNullWhen(true)] out Uri? uri,
		out string? remainingArguments)
	{
		uri = null;
		remainingArguments = null;

		if (TryFindMarker(arguments, out var index, out var marker, out var data))
		{
			remainingArguments = JoinArguments(arguments, skipIndex: index);

			if (marker != ProtocolMarker)
			{
				if (typeof(CommandLineActivationParser).Log().IsEnabled(LogLevel.Warning))
				{
					typeof(CommandLineActivationParser).Log().LogWarning(
						$"The app was started with a '{marker}' activation, which Uno Platform does not support. It is reported as a plain launch.");
				}

				return false;
			}

			return TryCreateUri(data, out uri);
		}

		if (allowBareUri
			&& arguments.Count == 1
			&& Uri.TryCreate(arguments[0], UriKind.Absolute, out var bareUri)
			&& !bareUri.IsFile)
		{
			uri = bareUri;
			remainingArguments = string.Empty;
			return true;
		}

		return false;
	}

	private static bool TryFindMarker(IReadOnlyList<string> arguments, out int index, [NotNullWhen(true)] out string? marker, out string data)
	{
		foreach (var candidate in _markers)
		{
			var qualifier = ArgumentPrefix + candidate + ArgumentSuffix;

			for (index = 0; index < arguments.Count; index++)
			{
				var argument = arguments[index];

				// A substring match rather than a prefix test, as in the Windows App SDK.
				var start = argument.IndexOf(qualifier, StringComparison.Ordinal);
				if (start < 0)
				{
					continue;
				}

				// The data may itself contain ':', so split on the first one only.
				var kindAndData = argument[(start + ArgumentPrefix.Length)..];
				var separator = kindAndData.IndexOf(':');

				marker = candidate;
				data = kindAndData[(separator + 1)..];
				return true;
			}
		}

		index = -1;
		marker = null;
		data = string.Empty;
		return false;
	}

	private static bool TryCreateUri(string data, [NotNullWhen(true)] out Uri? uri)
	{
		if (Uri.TryCreate(data, UriKind.Absolute, out uri))
		{
			return true;
		}

		// Scheme only: an activation URI routinely carries an OAuth code or token.
		var schemeEnd = data.IndexOf(':');
		typeof(CommandLineActivationParser).Log().LogError(
			$"An activation URI with scheme '{(schemeEnd > 0 ? data[..schemeEnd] : "(none)")}' could not be parsed as an absolute URI.");
		return false;
	}

	private static string JoinArguments(IReadOnlyList<string> arguments, int skipIndex)
	{
		StringBuilder builder = new();

		for (var i = 0; i < arguments.Count; i++)
		{
			if (i == skipIndex)
			{
				continue;
			}

			if (builder.Length > 0)
			{
				builder.Append(' ');
			}

			AppendArgument(builder, arguments[i]);
		}

		return builder.ToString();
	}

	// Quotes an argument so CommandLineToArgvW reads it back unchanged, the format .NET uses for Environment.CommandLine.
	private static void AppendArgument(StringBuilder builder, string argument)
	{
		if (argument.Length > 0 && argument.AsSpan().IndexOfAny(" \t\n\v\"") < 0)
		{
			builder.Append(argument);
			return;
		}

		builder.Append('"');

		for (var i = 0; i < argument.Length; i++)
		{
			var backslashes = 0;
			while (i < argument.Length && argument[i] == '\\')
			{
				backslashes++;
				i++;
			}

			if (i == argument.Length)
			{
				// Doubled so they do not escape the closing quote.
				builder.Append('\\', backslashes * 2);
			}
			else if (argument[i] == '"')
			{
				builder.Append('\\', backslashes * 2 + 1);
				builder.Append('"');
			}
			else
			{
				builder.Append('\\', backslashes);
				builder.Append(argument[i]);
			}
		}

		builder.Append('"');
	}
}
