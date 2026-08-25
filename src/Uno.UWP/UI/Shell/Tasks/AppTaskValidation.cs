#nullable enable
#pragma warning disable CS8305

using System;

namespace Windows.UI.Shell.Tasks;

internal static class AppTaskValidation
{
	internal const string UserTextInputPlaceholder = "{userTextInput}";

	// Windows returns E_INVALIDARG for a relative URI and dereferences a null one; both are surfaced
	// as ArgumentException so the whole API family has one predictable failure mode.
	internal static Uri RequireAbsoluteUri(Uri uri, string parameterName)
	{
		if (uri is null)
		{
			throw new ArgumentException("App task URIs are required.", parameterName);
		}

		if (!uri.IsAbsoluteUri)
		{
			throw new ArgumentException("App task URIs must be absolute.", parameterName);
		}

		return uri;
	}

	internal static string RequireNonEmpty(string value, string parameterName)
	{
		if (string.IsNullOrEmpty(value))
		{
			throw new ArgumentException($"'{parameterName}' is required and cannot be empty.", parameterName);
		}

		return value;
	}

	internal static bool IsDefinedState(AppTaskState state) =>
		state is >= AppTaskState.Running and <= AppTaskState.Error;
}
