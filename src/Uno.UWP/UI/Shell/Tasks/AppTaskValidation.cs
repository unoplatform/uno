#nullable enable

using System;

namespace Windows.UI.Shell.Tasks;

internal static class AppTaskValidation
{
	internal static Uri RequireAbsoluteUri(Uri uri, string parameterName)
	{
		ArgumentNullException.ThrowIfNull(uri, parameterName);

		if (!uri.IsAbsoluteUri)
		{
			throw new ArgumentException("App task URIs must be absolute.", parameterName);
		}

		return uri;
	}
}
