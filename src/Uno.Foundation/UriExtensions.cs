using System;

namespace Uno.Extensions
{
	internal static class UriExtensions
	{
		internal static Uri TrimEndUriSlash(this Uri uri) => new(uri.OriginalString.TrimEnd("/"));

		internal static bool IsAppData(this Uri uri)
		{
			if (uri is null)
			{
				throw new ArgumentNullException(nameof(uri));
			}

			return uri.Scheme.Equals("ms-appdata", StringComparison.OrdinalIgnoreCase);
		}

		public static bool IsLocalResource(this Uri uri)
		{
			if (uri is null)
			{
				throw new ArgumentNullException(nameof(uri));
			}

			return uri.Scheme.Equals("ms-appx", StringComparison.OrdinalIgnoreCase);
		}
	}
}
